#!/usr/bin/env python3
# Database backups of a Supabase stack — the service behind WithDatabaseBackups(...)
# (Nextended.Aspire.Hosting.Supabase). Runs in the stack's own supabase/postgres image, so pg_dump
# always matches the server; Python standard library only (the image has Python 3.8).
#
# What it does, polling every BACKUP_POLL_SECONDS:
#   * keeps its state in the schema nx_backup (settings, runs, restores, service heartbeat) — a
#     schema it never backs up or restores, so a restore cannot rewind its own bookkeeping;
#   * scheduled backups once a day at the configured local time, manual ones on request;
#   * one backup = a tar.gz stream (manifest.json, db.dump from pg_dump -Fc of the app's schemas,
#     files/<bucket>/<name> of every stored file) cut into parts and written to every target:
#     the app's own storage bucket and any external target (Azure Blob via SAS URL, S3);
#   * restore = safety backup of the current state, then the DATA of the backup into the current
#     schema in ONE transaction (all tables of the backed-up schemas emptied and refilled; the
#     migration bookkeeping tables and the backup bucket's own rows stay as they are), then the
#     files back into storage. The schema itself stays current, so a restore never fights the
#     migrations; a backup whose tables no longer fit fails as a whole and changes nothing;
#   * retention of scheduled backups (newest N days + newest per week for M weeks);
#   * finds backups on the targets it does not know (a fresh database after a rebuild) and
#     lists them again.
#
# `python3 database-backup.py --self-test` checks the pure parts (retention, schedule, parts,
# S3 signing) without a database.

import datetime
import hashlib
import hmac
import io
import json
import os
import shutil
import subprocess
import sys
import tarfile
import time
import traceback
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

VERSION = "1"

# Schemas of Supabase itself (and this service): never backed up or restored.
SYSTEM_SCHEMAS = {
    "information_schema", "extensions", "graphql", "graphql_public", "realtime", "_realtime",
    "supabase_functions", "supabase_migrations", "vault", "pgsodium", "pgsodium_masks", "net",
    "cron", "pgbouncer", "_analytics", "_supavisor", "pgtle", "repack", "topology", "nx_backup",
}

# Tables whose content describes the schema, not the data: restored they would tell the
# migration runners that newer migrations never ran.
KEEP_TABLES = {"public._aspire_applied_migrations", "auth.schema_migrations", "storage.migrations"}


def env(name, default=""):
    return os.environ.get(name, default)


def log(message):
    print("[db-backup] " + message, flush=True)


# ---------------------------------------------------------------------------------------------
# Pure helpers (covered by --self-test)
# ---------------------------------------------------------------------------------------------

def expired_runs(runs, keep_daily, keep_weekly):
    """Scheduled backups to delete. runs: [(id, 'YYYY-MM-DD')] of finished scheduled backups.
    Kept: the newest keep_daily days, and the newest backup of each of the newest keep_weekly
    ISO weeks."""
    ordered = sorted(runs, key=lambda r: r[1], reverse=True)
    keep = {rid for rid, _ in ordered[:max(1, keep_daily)]}
    weeks = []
    for rid, day in ordered:
        week = datetime.date.fromisoformat(day).isocalendar()[:2]
        if all(w != week for w, _ in weeks):
            weeks.append((week, rid))
    keep |= {rid for _, rid in weeks[:max(0, keep_weekly)]}
    return [rid for rid, _ in runs if rid not in keep]


def schedule_due(local_now, run_at, last_scheduled_day):
    """True when today's scheduled backup is due: the local time reached run_at ('HH:MM') and
    there is none for today yet. local_now: 'YYYY-MM-DD HH:MM'."""
    day, hm = local_now.split(" ")
    return hm >= run_at[:5] and (last_scheduled_day is None or last_scheduled_day < day)


def part_key(run_id, index):
    return "%s/part-%05d" % (run_id, index)


def sigv4_headers(method, url, region, service, access_key, secret_key, payload_hash, amz_date, extra=None):
    """AWS Signature Version 4 for one request; returns the headers to send."""
    parsed = urllib.parse.urlsplit(url)
    headers = {"host": parsed.netloc, "x-amz-date": amz_date}
    if payload_hash is not None:
        headers["x-amz-content-sha256"] = payload_hash
    headers.update({k.lower(): v for k, v in (extra or {}).items()})
    signed = sorted(headers)
    query = "&".join(
        "%s=%s" % (urllib.parse.quote(k, safe="-_.~"), urllib.parse.quote(v, safe="-_.~"))
        for k, v in sorted(urllib.parse.parse_qsl(parsed.query, keep_blank_values=True)))
    canonical = "\n".join([
        method, urllib.parse.quote(parsed.path or "/", safe="/-_.~"), query,
        "".join("%s:%s\n" % (h, " ".join(str(headers[h]).split())) for h in signed),
        ";".join(signed), payload_hash if payload_hash is not None else hashlib.sha256(b"").hexdigest()])
    scope = "%s/%s/%s/aws4_request" % (amz_date[:8], region, service)
    to_sign = "\n".join(["AWS4-HMAC-SHA256", amz_date, scope, hashlib.sha256(canonical.encode()).hexdigest()])
    key = ("AWS4" + secret_key).encode()
    for part in (amz_date[:8], region, service, "aws4_request"):
        key = hmac.new(key, part.encode(), hashlib.sha256).digest()
    signature = hmac.new(key, to_sign.encode(), hashlib.sha256).hexdigest()
    headers["authorization"] = "AWS4-HMAC-SHA256 Credential=%s/%s, SignedHeaders=%s, Signature=%s" % (
        access_key, scope, ";".join(signed), signature)
    return headers


class PartWriter(io.RawIOBase):
    """File object for tarfile: cuts the stream into parts of part_size and hands each to
    on_part(index, bytes) as soon as it is full — the backup never sits on disk as a whole."""

    def __init__(self, part_size, on_part):
        super().__init__()
        self.part_size, self.on_part = part_size, on_part
        self.buffer, self.index, self.size = bytearray(), 0, 0

    def writable(self):
        return True

    def write(self, data):
        self.buffer += data
        self.size += len(data)
        while len(self.buffer) >= self.part_size:
            self.on_part(self.index, bytes(self.buffer[:self.part_size]))
            del self.buffer[:self.part_size]
            self.index += 1
        return len(data)

    def finish(self):
        if self.buffer or self.index == 0:
            self.on_part(self.index, bytes(self.buffer))
            self.index += 1
            self.buffer = bytearray()
        return self.index


class PartReader(io.RawIOBase):
    """The parts get(0) .. get(count-1) as one stream, fetched one after the other."""

    def __init__(self, count, get):
        super().__init__()
        self.count, self.get = count, get
        self.index, self.current = 0, b""

    def readable(self):
        return True

    def readinto(self, target):
        while not self.current and self.index < self.count:
            self.current = self.get(self.index)
            self.index += 1
        n = min(len(target), len(self.current))
        target[:n] = self.current[:n]
        self.current = self.current[n:]
        return n


# ---------------------------------------------------------------------------------------------
# Database and HTTP
# ---------------------------------------------------------------------------------------------

def pg_env():
    e = dict(os.environ)
    e["PGPASSWORD"] = env("DB_PASSWORD")
    return e


def pg_args():
    return ["-h", env("DB_HOST"), "-p", env("DB_PORT", "5432"), "-U", env("DB_USER", "supabase_admin"),
            "-d", env("DB_NAME", "postgres")]


def sql(statement, **params):
    """Runs SQL through psql (variables :'name' are quoted literals). The statement's last result
    is returned parsed from JSON (or None when there is no result)."""
    args = ["psql", "-X", "-q", "-A", "-t", "-v", "ON_ERROR_STOP=1"] + pg_args()
    for k, v in params.items():
        args += ["-v", "%s=%s" % (k, "" if v is None else v)]
    proc = subprocess.run(args + ["-f", "-"], input=statement, capture_output=True, text=True, env=pg_env())
    if proc.returncode != 0:
        raise RuntimeError("psql: " + (proc.stderr.strip() or proc.stdout.strip()))
    out = proc.stdout.strip()
    return json.loads(out.splitlines()[-1]) if out else None


def http(method, url, data=None, headers=None, ok=(200, 201, 202, 204, 206)):
    request = urllib.request.Request(url, data=data, method=method, headers=headers or {})
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as error:
        body = error.read()
        if error.code in ok:
            return error.code, body
        raise RuntimeError("%s %s -> %s %s" % (method, url.split("?")[0], error.code, body[:300].decode("utf-8", "replace")))


def quote_path(path):
    return urllib.parse.quote(path, safe="/")


# ---------------------------------------------------------------------------------------------
# Targets: put/get/delete by key "<run>/part-00000", "<run>/manifest.json"; list_runs() = run ids
# ---------------------------------------------------------------------------------------------

class AppStorage:
    """The stack's own storage (Supabase Storage API with the service key), bucket BACKUP_BUCKET."""
    name = "app"

    def __init__(self):
        self.base = env("STORAGE_URL").rstrip("/")
        self.bucket = env("BACKUP_BUCKET", "db-backups")
        key = env("SERVICE_KEY")
        self.auth = {"Authorization": "Bearer " + key, "apikey": key}

    def ensure(self):
        status, _ = http("GET", "%s/bucket/%s" % (self.base, self.bucket), headers=self.auth, ok=(200, 400, 404))
        if status != 200:
            body = json.dumps({"id": self.bucket, "name": self.bucket, "public": False}).encode()
            http("POST", self.base + "/bucket", body, dict(self.auth, **{"Content-Type": "application/json"}), ok=(200, 409))

    def object_url(self, bucket, key):
        return "%s/object/%s/%s" % (self.base, bucket, quote_path(key))

    def put(self, key, data, content_type="application/octet-stream", bucket=None):
        http("POST", self.object_url(bucket or self.bucket, key), data,
             dict(self.auth, **{"Content-Type": content_type, "x-upsert": "true"}))

    def get(self, key, bucket=None):
        return http("GET", self.object_url(bucket or self.bucket, key), headers=self.auth)[1]

    def delete_run(self, run_id):
        _, body = http("POST", "%s/object/list/%s" % (self.base, self.bucket),
                       json.dumps({"prefix": run_id + "/", "limit": 10000}).encode(),
                       dict(self.auth, **{"Content-Type": "application/json"}))
        names = [run_id + "/" + o["name"] for o in json.loads(body)]
        if names:
            http("DELETE", "%s/object/%s" % (self.base, self.bucket), json.dumps({"prefixes": names}).encode(),
                 dict(self.auth, **{"Content-Type": "application/json"}))

    def list_runs(self):
        _, body = http("POST", "%s/object/list/%s" % (self.base, self.bucket),
                       json.dumps({"prefix": "", "limit": 10000}).encode(),
                       dict(self.auth, **{"Content-Type": "application/json"}))
        return [o["name"] for o in json.loads(body) if o.get("id") is None]  # folders


class AzureBlob:
    """A blob container addressed by a SAS URL (https://<account>.blob.core.windows.net/<container>?<sas>)."""

    def __init__(self, name, url, prefix):
        self.name, self.prefix = name, prefix
        parsed = urllib.parse.urlsplit(url)
        self.base, self.sas = "%s://%s%s" % (parsed.scheme, parsed.netloc, parsed.path.rstrip("/")), parsed.query

    def url(self, key, extra=""):
        return "%s/%s?%s%s" % (self.base, quote_path(self.prefix + key), self.sas, extra)

    def ensure(self):
        pass

    def put(self, key, data, content_type="application/octet-stream"):
        http("PUT", self.url(key), data, {"x-ms-blob-type": "BlockBlob", "x-ms-version": "2021-08-06",
                                         "Content-Type": content_type})

    def get(self, key):
        return http("GET", self.url(key), headers={"x-ms-version": "2021-08-06"})[1]

    def blobs(self, prefix):
        names, marker = [], ""
        while True:
            _, body = http("GET", "%s?restype=container&comp=list&prefix=%s&marker=%s&%s" % (
                self.base, urllib.parse.quote(self.prefix + prefix), urllib.parse.quote(marker), self.sas),
                headers={"x-ms-version": "2021-08-06"})
            root = ET.fromstring(body)
            names += [b.findtext("Name") for b in root.iter("Blob")]
            marker = root.findtext("NextMarker") or ""
            if not marker:
                return [n[len(self.prefix):] for n in names]

    def delete_run(self, run_id):
        for key in self.blobs(run_id + "/"):
            http("DELETE", self.url(key), headers={"x-ms-version": "2021-08-06"}, ok=(202, 404))

    def list_runs(self):
        return sorted({k.split("/")[0] for k in self.blobs("") if k.endswith("/manifest.json")})


class S3:
    """An S3-compatible bucket (AWS, MinIO, …), path-style, Signature Version 4."""

    def __init__(self, name, endpoint, bucket, region, access_key, secret_key, prefix):
        self.name, self.prefix = name, prefix
        self.endpoint, self.bucket, self.region = endpoint.rstrip("/"), bucket, region or "us-east-1"
        self.access_key, self.secret_key = access_key, secret_key

    def request(self, method, key="", query="", data=b""):
        url = "%s/%s/%s" % (self.endpoint, self.bucket, quote_path(self.prefix + key) if key else "")
        if query:
            url += "?" + query
        amz_date = datetime.datetime.utcnow().strftime("%Y%m%dT%H%M%SZ")
        headers = sigv4_headers(method, url, self.region, "s3", self.access_key, self.secret_key,
                                hashlib.sha256(data or b"").hexdigest(), amz_date)
        headers.pop("host")
        return http(method, url, data if method == "PUT" else None, headers)[1]

    def ensure(self):
        pass

    def put(self, key, data, content_type="application/octet-stream"):
        self.request("PUT", key, data=data)

    def get(self, key):
        return self.request("GET", key)

    def keys(self, prefix):
        keys, token = [], ""
        while True:
            query = "list-type=2&prefix=" + urllib.parse.quote(self.prefix + prefix, safe="")
            if token:
                query += "&continuation-token=" + urllib.parse.quote(token, safe="")
            root = ET.fromstring(self.request("GET", query=query))
            ns = root.tag[:root.tag.index("}") + 1] if root.tag.startswith("{") else ""
            keys += [c.findtext(ns + "Key") for c in root.iter(ns + "Contents")]
            token = root.findtext(ns + "NextContinuationToken") or ""
            if not token:
                return [k[len(self.prefix):] for k in keys]

    def delete_run(self, run_id):
        for key in self.keys(run_id + "/"):
            self.request("DELETE", key)

    def list_runs(self):
        return sorted({k.split("/")[0] for k in self.keys("") if k.endswith("/manifest.json")})


def configured_targets():
    targets = [AppStorage()]
    for i in range(int(env("BACKUP_TARGET_COUNT", "0"))):
        def field(f):
            return env("BACKUP_TARGET_%d_%s" % (i, f))
        kind, name, prefix = field("KIND"), field("NAME"), field("PREFIX")
        if prefix and not prefix.endswith("/"):
            prefix += "/"
        # No address (empty, "none", anything that is not a URL) = the target is off in this deployment
        if kind == "azure-blob" and "://" in field("URL"):
            targets.append(AzureBlob(name, field("URL"), prefix))
        elif kind == "s3" and "://" in field("ENDPOINT") and field("SECRET_KEY"):
            targets.append(S3(name, field("ENDPOINT"), field("BUCKET"), field("REGION"),
                              field("ACCESS_KEY"), field("SECRET_KEY"), prefix))
        else:
            log("target '%s' (%s) has no address configured - skipped" % (name, kind))
    return targets


# ---------------------------------------------------------------------------------------------
# Schema of the service
# ---------------------------------------------------------------------------------------------

SCHEMA_SQL = r"""
CREATE SCHEMA IF NOT EXISTS nx_backup;
REVOKE ALL ON SCHEMA nx_backup FROM PUBLIC;
CREATE TABLE IF NOT EXISTS nx_backup.settings (
  id int PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  enabled boolean NOT NULL,
  run_at text NOT NULL CHECK (run_at ~ '^([01][0-9]|2[0-3]):[0-5][0-9]$'),
  time_zone text NOT NULL,
  keep_daily int NOT NULL CHECK (keep_daily >= 1),
  keep_weekly int NOT NULL CHECK (keep_weekly >= 0),
  include_files boolean NOT NULL,
  updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS nx_backup.runs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  kind text NOT NULL CHECK (kind IN ('scheduled', 'manual', 'pre_restore')),
  status text NOT NULL DEFAULT 'queued' CHECK (status IN ('queued', 'running', 'done', 'failed', 'deleting')),
  local_date date,
  requested_by uuid,
  note text,
  created_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz,
  finished_at timestamptz,
  heartbeat_at timestamptz,
  progress text,
  size_bytes bigint,
  parts int,
  includes_files boolean,
  file_count int,
  schemas text[],
  targets jsonb NOT NULL DEFAULT '{}'::jsonb,
  error text);
CREATE UNIQUE INDEX IF NOT EXISTS runs_one_scheduled_per_day ON nx_backup.runs (local_date) WHERE kind = 'scheduled';
CREATE TABLE IF NOT EXISTS nx_backup.restores (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  run_id uuid NOT NULL,
  status text NOT NULL DEFAULT 'queued' CHECK (status IN ('queued', 'running', 'done', 'failed')),
  requested_by uuid,
  safety_run_id uuid,
  progress text,
  created_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz,
  finished_at timestamptz,
  heartbeat_at timestamptz,
  error text);
CREATE TABLE IF NOT EXISTS nx_backup.service (
  id int PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  seen_at timestamptz,
  version text,
  bucket text,
  targets jsonb);

-- The API. SECURITY DEFINER (owner: the service's superuser); granted to service_role and
-- postgres only — an app exposes it through its own functions that check its admin rule.
CREATE OR REPLACE FUNCTION nx_backup.overview() RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path = nx_backup AS $f$
  SELECT jsonb_build_object(
    'settings', (SELECT to_jsonb(s) - 'id' FROM settings s),
    'service', (SELECT to_jsonb(v) - 'id' || jsonb_build_object('alive', v.seen_at > now() - interval '3 minutes') FROM service v),
    'runs', coalesce((SELECT jsonb_agg(to_jsonb(r) ORDER BY r.created_at DESC) FROM (SELECT * FROM runs ORDER BY created_at DESC LIMIT 200) r), '[]'),
    'restores', coalesce((SELECT jsonb_agg(to_jsonb(x) ORDER BY x.created_at DESC) FROM (SELECT * FROM restores ORDER BY created_at DESC LIMIT 20) x), '[]'))
$f$;
CREATE OR REPLACE FUNCTION nx_backup.request_backup(p_by uuid, p_note text DEFAULT NULL) RETURNS uuid LANGUAGE sql SECURITY DEFINER SET search_path = nx_backup AS $f$
  INSERT INTO runs (kind, requested_by, note) VALUES ('manual', p_by, p_note) RETURNING id
$f$;
CREATE OR REPLACE FUNCTION nx_backup.request_restore(p_run uuid, p_by uuid) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path = nx_backup AS $f$
DECLARE v uuid;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM runs WHERE id = p_run AND status = 'done') THEN
    RAISE EXCEPTION 'backup % is not complete', p_run;
  END IF;
  IF EXISTS (SELECT 1 FROM restores WHERE status IN ('queued', 'running')) THEN
    RAISE EXCEPTION 'a restore is already running';
  END IF;
  INSERT INTO restores (run_id, requested_by) VALUES (p_run, p_by) RETURNING id INTO v;
  RETURN v;
END $f$;
CREATE OR REPLACE FUNCTION nx_backup.request_delete(p_run uuid) RETURNS void LANGUAGE sql SECURITY DEFINER SET search_path = nx_backup AS $f$
  UPDATE runs SET status = 'deleting' WHERE id = p_run AND status IN ('done', 'failed')
$f$;
CREATE OR REPLACE FUNCTION nx_backup.save_settings(p jsonb) RETURNS jsonb LANGUAGE sql SECURITY DEFINER SET search_path = nx_backup AS $f$
  UPDATE settings SET
    enabled = coalesce((p->>'enabled')::boolean, enabled),
    run_at = coalesce(p->>'run_at', run_at),
    time_zone = coalesce(p->>'time_zone', time_zone),
    keep_daily = coalesce((p->>'keep_daily')::int, keep_daily),
    keep_weekly = coalesce((p->>'keep_weekly')::int, keep_weekly),
    include_files = coalesce((p->>'include_files')::boolean, include_files),
    updated_at = now()
  RETURNING to_jsonb(settings) - 'id'
$f$;
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA nx_backup FROM PUBLIC;
DO $g$ BEGIN
  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'service_role') THEN
    GRANT USAGE ON SCHEMA nx_backup TO service_role;
    GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA nx_backup TO service_role;
  END IF;
  GRANT USAGE ON SCHEMA nx_backup TO postgres;
  GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA nx_backup TO postgres;
END $g$;
INSERT INTO nx_backup.settings (enabled, run_at, time_zone, keep_daily, keep_weekly, include_files)
VALUES (:'enabled'::boolean, :'run_at', :'time_zone', :'keep_daily'::int, :'keep_weekly'::int, :'include_files'::boolean)
ON CONFLICT (id) DO NOTHING;
"""


def ensure_schema():
    sql(SCHEMA_SQL, enabled=env("BACKUP_DEFAULT_ENABLED", "true"), run_at=env("BACKUP_DEFAULT_RUN_AT", "02:00"),
        time_zone=env("BACKUP_DEFAULT_TIME_ZONE", "UTC"), keep_daily=env("BACKUP_DEFAULT_KEEP_DAILY", "14"),
        keep_weekly=env("BACKUP_DEFAULT_KEEP_WEEKLY", "8"), include_files=env("BACKUP_DEFAULT_INCLUDE_FILES", "true"))


def update_run(table, row_id, **fields):
    sets = ", ".join("%s = NULL" % k if v is None else "%s = :'%s'" % (k, k) for k, v in fields.items())
    sql("UPDATE nx_backup.%s SET %s, heartbeat_at = now() WHERE id = :'id';" % (table, sets),
        id=row_id, **{k: (json.dumps(v) if isinstance(v, (dict, list)) else v) for k, v in fields.items() if v is not None})


def progress(table, row_id, text):
    sql("UPDATE nx_backup.%s SET progress = :'p', heartbeat_at = now() WHERE id = :'id';" % table, id=row_id, p=text)


# ---------------------------------------------------------------------------------------------
# Backup
# ---------------------------------------------------------------------------------------------

def app_schemas():
    excluded = SYSTEM_SCHEMAS | {s.strip() for s in env("BACKUP_EXCLUDE_SCHEMAS").split(",") if s.strip()}
    rows = sql("SELECT coalesce(json_agg(nspname ORDER BY nspname), '[]') FROM pg_namespace "
               "WHERE nspname NOT LIKE 'pg\\_%' AND nspname <> 'information_schema';")
    return [s for s in rows if s not in excluded]


def stored_files(bucket):
    return sql("SELECT coalesce(json_agg(json_build_object('bucket', bucket_id, 'name', name, 'type', metadata->>'mimetype') "
               "ORDER BY bucket_id, name), '[]') FROM storage.objects WHERE bucket_id <> :'bucket' "
               "AND name IS NOT NULL;", bucket=bucket)


def work_dir(name):
    path = os.path.join(env("BACKUP_WORK_DIR", "/tmp/nx-backup"), name)
    shutil.rmtree(path, ignore_errors=True)
    os.makedirs(path)
    return path


def tar_bytes(tar, name, data):
    info = tarfile.TarInfo(name)
    info.size, info.mtime = len(data), int(time.time())
    tar.addfile(info, io.BytesIO(data))


def run_backup(run_id, kind, targets):
    """One backup into every target. A target that fails is recorded and skipped; the backup
    counts when at least one target holds all of it."""
    folder = work_dir(run_id)
    results = {t.name: "ok" for t in targets}
    live = list(targets)
    try:
        sql("UPDATE nx_backup.runs SET status = 'running', started_at = now(), heartbeat_at = now() WHERE id = :'id';", id=run_id)
        settings = sql("SELECT to_json(s) FROM nx_backup.settings s;")
        schemas = app_schemas()
        progress("runs", run_id, "database")
        dump = os.path.join(folder, "db.dump")
        args = ["pg_dump"] + pg_args() + ["-Fc", "-Z", "6", "-f", dump]
        for s in schemas:
            args += ["-n", s]
        proc = subprocess.run(args, capture_output=True, text=True, env=pg_env())
        if proc.returncode != 0:
            raise RuntimeError("pg_dump: " + proc.stderr.strip())
        app = targets[0]
        files = stored_files(app.bucket) if settings["include_files"] else []
        manifest = {"format": "nx-backup/1", "id": run_id, "kind": kind,
                    "created_at": datetime.datetime.utcnow().isoformat() + "Z", "schemas": schemas,
                    "files": len(files), "server_version": sql("SELECT to_json(current_setting('server_version'));"),
                    "stack": env("BACKUP_STACK_NAME")}

        def on_part(index, data):
            for t in list(live):
                try:
                    t.put(part_key(run_id, index), data)
                except Exception as error:  # noqa: BLE001 - one target must not stop the others
                    results[t.name] = "error: %s" % error
                    live.remove(t)
                    log("target %s failed: %s" % (t.name, error))
            if not live:
                raise RuntimeError("no target accepted the backup")
            progress("runs", run_id, "part %d" % (index + 1))

        writer = PartWriter(int(float(env("BACKUP_PART_MB", "25")) * 1024 * 1024), on_part)
        with tarfile.open(fileobj=writer, mode="w|gz") as tar:
            tar_bytes(tar, "manifest.json", json.dumps(manifest, indent=1).encode())
            tar.add(dump, "db.dump")
            os.remove(dump)
            for i, f in enumerate(files):
                if i % 50 == 0:
                    progress("runs", run_id, "files %d/%d" % (i, len(files)))
                data = app.get(f["name"], bucket=f["bucket"])
                tar_bytes(tar, "files/%s/%s" % (f["bucket"], f["name"]), data)
        parts = writer.finish()
        manifest.update(parts=parts, size=writer.size)
        for t in list(live):
            try:
                t.put(run_id + "/manifest.json", json.dumps(manifest, indent=1).encode(), "application/json")
            except Exception as error:  # noqa: BLE001
                results[t.name] = "error: %s" % error
                live.remove(t)
        if not live:
            raise RuntimeError("no target accepted the backup")
        update_run("runs", run_id, status="done", finished_at=datetime.datetime.utcnow().isoformat() + "Z",
                   size_bytes=writer.size, parts=parts, includes_files=bool(settings["include_files"]),
                   file_count=len(files), schemas="{%s}" % ",".join(schemas), targets=results, progress=None, error=None)
        log("backup %s done: %d bytes, %d parts, %d files" % (run_id, writer.size, parts, len(files)))
        return True
    except Exception as error:  # noqa: BLE001
        log("backup %s failed: %s" % (run_id, error))
        for t in targets:
            try:
                t.delete_run(run_id)
            except Exception:  # noqa: BLE001 - best effort
                pass
        update_run("runs", run_id, status="failed", finished_at=datetime.datetime.utcnow().isoformat() + "Z",
                   error=str(error)[:2000], targets=results)
        return False
    finally:
        shutil.rmtree(folder, ignore_errors=True)


# ---------------------------------------------------------------------------------------------
# Restore
# ---------------------------------------------------------------------------------------------

RESTORE_PRE_SQL = r"""
SET lock_timeout = '60s';
-- The backup bucket's own rows are not part of the point in time.
CREATE TEMP TABLE nx_keep_buckets AS SELECT * FROM storage.buckets WHERE id = :'bucket';
CREATE TEMP TABLE nx_keep_objects AS SELECT * FROM storage.objects WHERE bucket_id = :'bucket';
"""

RESTORE_POST_SQL = r"""
-- storage-api refuses direct deletes (storage.protect_delete) unless told so
SET LOCAL storage.allow_delete_query = 'true';
DELETE FROM storage.objects WHERE bucket_id = :'bucket';
DELETE FROM storage.buckets WHERE id = :'bucket';
-- Without generated columns (storage.objects.path_tokens)
DO $nx$
DECLARE
  t text;
  cols text;
BEGIN
  FOREACH t IN ARRAY ARRAY['buckets', 'objects'] LOOP
    SELECT string_agg(quote_ident(attname), ', ' ORDER BY attnum) INTO cols FROM pg_attribute
     WHERE attrelid = format('storage.%I', t)::regclass AND attnum > 0 AND NOT attisdropped AND attgenerated = '';
    EXECUTE format('INSERT INTO storage.%I (%s) SELECT %s FROM nx_keep_%s', t, cols, cols, t);
  END LOOP;
END $nx$;
"""


def restore_database(dump, schemas, folder):
    """The backup's data into the current schema, in one transaction."""
    keep = KEEP_TABLES | {t.strip() for t in env("BACKUP_KEEP_TABLES").split(",") if t.strip()}
    # Tables and sequences as they are NOW: [schema, name, relkind]
    current = sql("SELECT coalesce(json_agg(json_build_array(n.nspname, c.relname, c.relkind) ORDER BY 1), '[]') FROM pg_class c "
                  "JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relkind IN ('r', 'p', 'S') AND NOT c.relispartition "
                  "AND n.nspname = ANY (string_to_array(:'schemas', ','));", schemas=",".join(schemas))
    existing = {"%s.%s" % (s, n) for s, n, _ in current}
    tables = ['"%s"."%s"' % (s.replace('"', '""'), n.replace('"', '""'))
              for s, n, kind in current if kind in ("r", "p") and "%s.%s" % (s, n) not in keep]
    toc = subprocess.run(["pg_restore", "-l", dump], capture_output=True, text=True, check=True).stdout
    lines = []
    for line in toc.splitlines():
        # "1234; 0 16500 TABLE DATA public _aspire_applied_migrations postgres" / "... SEQUENCE SET public x_seq postgres"
        parts = line.split()
        if (" TABLE DATA " in line or " SEQUENCE SET " in line) and len(parts) >= 7:
            name = "%s.%s" % (parts[5], parts[6])
            # Kept tables keep their rows; a table or sequence that no longer exists has no rows to get back.
            if name in keep or name not in existing:
                line = ";" + line
        lines.append(line)
    listing = os.path.join(folder, "restore.list")
    with open(listing, "w") as handle:
        handle.write("\n".join(lines) + "\n")
    script = os.path.join(folder, "restore.sql")
    subprocess.run(["pg_restore", "--data-only", "--disable-triggers", "-L", listing, "-f", script, dump],
                   capture_output=True, text=True, check=True)
    pre, post = os.path.join(folder, "pre.sql"), os.path.join(folder, "post.sql")
    with open(pre, "w") as handle:
        handle.write(RESTORE_PRE_SQL)
        if tables:
            handle.write("TRUNCATE TABLE %s;\n" % ", ".join(tables))
    with open(post, "w") as handle:
        handle.write(RESTORE_POST_SQL)
    # Sessions of the app would hold locks the TRUNCATE waits for; they reconnect on their own.
    sql("SELECT count(pg_terminate_backend(pid)) FROM pg_stat_activity WHERE datname = current_database() "
        "AND pid <> pg_backend_pid() AND backend_type = 'client backend';")
    proc = subprocess.run(["psql", "-X", "-q", "-1", "-v", "ON_ERROR_STOP=1", "-v", "bucket=" + env("BACKUP_BUCKET", "db-backups")]
                          + pg_args() + ["-f", pre, "-f", script, "-f", post],
                          capture_output=True, text=True, env=pg_env())
    if proc.returncode != 0:
        raise RuntimeError("restore (nothing was changed): " + proc.stderr.strip()[-2000:])


def source_for(run, targets):
    for t in targets:
        if run["targets"].get(t.name) == "ok":
            return t
    raise RuntimeError("no target holds backup %s" % run["id"])


def run_restore(restore, targets):
    rid = restore["id"]
    folder = work_dir("restore-" + rid)
    try:
        sql("UPDATE nx_backup.restores SET status = 'running', started_at = now(), heartbeat_at = now() WHERE id = :'id';", id=rid)
        run = sql("SELECT to_json(r) FROM nx_backup.runs r WHERE id = :'id';", id=restore["run_id"])
        progress("restores", rid, "safety backup")
        safety = sql("INSERT INTO nx_backup.runs (kind, requested_by, note) VALUES ('pre_restore', NULLIF(:'by', '')::uuid, :'note') "
                     "RETURNING to_json(id);", by=restore.get("requested_by") or "", note="before restore of %s" % run["id"])
        sql("UPDATE nx_backup.restores SET safety_run_id = :'s' WHERE id = :'id';", s=safety, id=rid)
        if not run_backup(safety, "pre_restore", targets):
            raise RuntimeError("the safety backup failed - nothing was restored")
        source = source_for(run, targets)
        manifest = json.loads(source.get(run["id"] + "/manifest.json"))
        progress("restores", rid, "download")
        reader = PartReader(manifest["parts"], lambda i: source.get(part_key(run["id"], i)))
        app = targets[0]
        restored_files = 0
        with tarfile.open(fileobj=reader, mode="r|gz") as tar:
            for member in tar:
                if member.name == "db.dump":
                    dump = os.path.join(folder, "db.dump")
                    with open(dump, "wb") as handle:
                        shutil.copyfileobj(tar.extractfile(member), handle)
                    progress("restores", rid, "database")
                    restore_database(dump, manifest["schemas"], folder)
                    os.remove(dump)
                elif member.name.startswith("files/") and member.isfile():
                    bucket, name = member.name[len("files/"):].split("/", 1)
                    if restored_files % 50 == 0:
                        progress("restores", rid, "files %d/%d" % (restored_files, manifest.get("files", 0)))
                    kind = sql("SELECT to_json(metadata->>'mimetype') FROM storage.objects WHERE bucket_id = :'b' AND name = :'n';",
                               b=bucket, n=name) or "application/octet-stream"
                    app.put(name, tar.extractfile(member).read(), kind, bucket=bucket)
                    restored_files += 1
        update_run("restores", rid, status="done", finished_at=datetime.datetime.utcnow().isoformat() + "Z",
                   progress=None, error=None)
        log("restore of %s done (%d files)" % (run["id"], restored_files))
    except Exception as error:  # noqa: BLE001
        log("restore %s failed: %s" % (rid, error))
        update_run("restores", rid, status="failed", finished_at=datetime.datetime.utcnow().isoformat() + "Z",
                   error=str(error)[:2000])
    finally:
        shutil.rmtree(folder, ignore_errors=True)


# ---------------------------------------------------------------------------------------------
# Loop
# ---------------------------------------------------------------------------------------------

def claim(table):
    return sql(("UPDATE nx_backup.%s SET status = 'running', heartbeat_at = now() WHERE id = ("
                "SELECT id FROM nx_backup.%s WHERE status = 'queued' ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED) "
                "RETURNING to_json(%s);") % (table, table, table))


def schedule():
    s = sql("SELECT json_build_object('enabled', enabled, 'run_at', run_at, "
            "'now', to_char(now() AT TIME ZONE time_zone, 'YYYY-MM-DD HH24:MI'), "
            "'last', (SELECT to_char(max(local_date), 'YYYY-MM-DD') FROM nx_backup.runs WHERE kind = 'scheduled')) "
            "FROM nx_backup.settings;")
    if s and s["enabled"] and schedule_due(s["now"], s["run_at"], s["last"]):
        sql("INSERT INTO nx_backup.runs (kind, local_date) VALUES ('scheduled', :'d'::date) "
            "ON CONFLICT (local_date) WHERE kind = 'scheduled' DO NOTHING;", d=s["now"][:10])


def delete_run(run, targets):
    for t in targets:
        try:
            t.delete_run(run["id"])
        except Exception as error:  # noqa: BLE001
            log("delete %s on %s: %s" % (run["id"], t.name, error))
    sql("DELETE FROM nx_backup.runs WHERE id = :'id';", id=run["id"])


def retention(targets):
    s = sql("SELECT to_json(s) FROM nx_backup.settings s;")
    runs = sql("SELECT coalesce(json_agg(json_build_array(id, to_char(local_date, 'YYYY-MM-DD'))), '[]') "
               "FROM nx_backup.runs WHERE kind = 'scheduled' AND status = 'done';")
    for rid in expired_runs([tuple(r) for r in runs], s["keep_daily"], s["keep_weekly"]):
        delete_run({"id": rid}, targets)
    sql("DELETE FROM nx_backup.runs WHERE status = 'failed' AND kind = 'scheduled' AND created_at < now() - interval '30 days';")


def recover():
    """Work a stopped service left half done; a live one beats its heartbeat every part."""
    for table in ("runs", "restores"):
        sql("UPDATE nx_backup.%s SET status = 'failed', error = 'interrupted (service restarted)', finished_at = now() "
            "WHERE status = 'running' AND coalesce(heartbeat_at, created_at) < now() - interval '15 minutes';" % table)


def catalog(targets):
    """Backups on the targets the database does not know (a fresh database after a rebuild)."""
    # Known before this pass; one found now is recorded with every target that holds it.
    known = set(sql("SELECT coalesce(json_agg(id::text), '[]') FROM nx_backup.runs;"))
    for t in targets:
        try:
            for run_id in t.list_runs():
                if run_id in known:
                    continue
                try:
                    m = json.loads(t.get(run_id + "/manifest.json"))
                except Exception:  # noqa: BLE001 - a folder without manifest is no finished backup
                    continue
                sql("INSERT INTO nx_backup.runs (id, kind, status, created_at, finished_at, size_bytes, parts, includes_files, "
                    "file_count, schemas, targets, note) VALUES (:'id', CASE WHEN :'kind' = 'scheduled' THEN 'manual' ELSE :'kind' END, "
                    "'done', :'at'::timestamptz, :'at'::timestamptz, :'size'::bigint, :'parts'::int, :'files'::int > 0, :'files'::int, "
                    "string_to_array(:'schemas', ','), jsonb_build_object(:'target', 'ok'), 'found on ' || :'target') "
                    "ON CONFLICT (id) DO UPDATE SET targets = nx_backup.runs.targets || excluded.targets;",
                    id=run_id, kind=m.get("kind", "manual"), at=m["created_at"], size=m.get("size", 0), parts=m["parts"],
                    files=m.get("files", 0), schemas=",".join(m.get("schemas", [])), target=t.name)
                log("found backup %s on %s" % (run_id, t.name))
        except Exception as error:  # noqa: BLE001
            log("catalog of %s: %s" % (t.name, error))


def main():
    targets = configured_targets()
    poll = int(env("BACKUP_POLL_SECONDS", "30"))
    while True:
        try:
            ensure_schema()
            for t in targets:
                t.ensure()
            break
        except Exception as error:  # noqa: BLE001 - the database or storage is still starting
            log("waiting for database/storage: %s" % error)
            time.sleep(10)
    sql("INSERT INTO nx_backup.service (id, seen_at, version, bucket, targets) VALUES (1, now(), :'v', :'b', :'t'::jsonb) "
        "ON CONFLICT (id) DO UPDATE SET seen_at = now(), version = excluded.version, bucket = excluded.bucket, targets = excluded.targets;",
        v=VERSION, b=env("BACKUP_BUCKET", "db-backups"),
        t=json.dumps([{"name": t.name, "kind": type(t).__name__} for t in targets]))
    recover()
    catalog(targets)
    log("ready: targets %s" % ", ".join(t.name for t in targets))
    last_catalog = time.time()
    while True:
        try:
            sql("UPDATE nx_backup.service SET seen_at = now();")
            recover()
            restore = claim("restores")
            if restore:
                run_restore(restore, targets)
                continue
            schedule()
            run = claim("runs")
            if run:
                if run_backup(run["id"], run["kind"], targets) and run["kind"] == "scheduled":
                    retention(targets)
                continue
            for run in sql("SELECT coalesce(json_agg(to_json(r)), '[]') FROM nx_backup.runs r WHERE status = 'deleting';"):
                delete_run(run, targets)
            if time.time() - last_catalog > 3600:
                catalog(targets)
                last_catalog = time.time()
        except Exception:  # noqa: BLE001 - the loop must survive anything
            log(traceback.format_exc())
        time.sleep(poll)


# ---------------------------------------------------------------------------------------------
# Self test
# ---------------------------------------------------------------------------------------------

def self_test():
    days = [("d%02d" % i, (datetime.date(2026, 11, 30) - datetime.timedelta(days=i)).isoformat()) for i in range(60)]
    gone = set(expired_runs(days, 14, 8))
    kept = [d for rid, d in days if rid not in gone]
    assert len([d for d in kept if d >= "2026-11-17"]) == 14, kept
    weeks = {datetime.date.fromisoformat(d).isocalendar()[:2] for d in kept}
    # 14 days span 3 ISO weeks; the newest of each of the 5 weeks before them comes on top
    assert len(weeks) == 8 and len(kept) == 14 + 5, kept
    assert expired_runs([("a", "2026-11-01")], 1, 0) == []

    assert schedule_due("2026-11-15 02:00", "02:00", "2026-11-14")
    assert not schedule_due("2026-11-15 01:59", "02:00", "2026-11-14")
    assert not schedule_due("2026-11-15 03:00", "02:00", "2026-11-15")
    assert schedule_due("2026-11-15 23:00", "02:00", None)

    parts = {}
    writer = PartWriter(1000, lambda i, b: parts.__setitem__(i, b))
    with tarfile.open(fileobj=writer, mode="w|gz") as tar:
        tar_bytes(tar, "manifest.json", b"{}")
        tar_bytes(tar, "files/b/x.bin", os.urandom(5000))
    count = writer.finish()
    assert count == len(parts) > 1 and all(len(parts[i]) == 1000 for i in range(count - 1))
    with tarfile.open(fileobj=PartReader(count, lambda i: parts[i]), mode="r|gz") as tar:
        names = [(m.name, len(tar.extractfile(m).read())) for m in tar]
    assert names == [("manifest.json", 2), ("files/b/x.bin", 5000)], names
    empty = {}
    assert PartWriter(10, lambda i, b: empty.__setitem__(i, b)).finish() == 1 and empty == {0: b""}

    # AWS Signature Version 4 test suite, "get-vanilla"
    headers = sigv4_headers("GET", "https://example.amazonaws.com/", "us-east-1", "service",
                            "AKIDEXAMPLE", "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY", None, "20150830T123600Z")
    assert headers["authorization"].endswith(
        "Signature=5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31"), headers["authorization"]
    print("self-test ok")


if __name__ == "__main__":
    if "--self-test" in sys.argv:
        self_test()
    else:
        main()
