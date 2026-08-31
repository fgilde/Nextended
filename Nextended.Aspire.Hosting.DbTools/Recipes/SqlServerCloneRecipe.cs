namespace Nextended.Aspire.Hosting.DbTools;

/// SQL Server, cloned with `sqlpackage`.
///
/// The other engines keep their dump tools inside their own image; SQL Server's are split. `sqlcmd`
/// and `bcp` are in the server image and neither carries a schema; `sqlpackage`, which does, is a
/// .NET tool that is in no image at all. So the clone runs in the .NET SDK image and installs it —
/// which means a clone needs the internet the first time it runs, and an air-gapped stack points
/// <see cref="DbCloneOptions.Image"/> at an image with sqlpackage already in it.
///
/// It also means the .NET *8* SDK: sqlpackage 170 runs on net8, and the 9 image has no 8 runtime.
///
/// Two ways through, because sqlpackage has two formats and they are good at different things:
///
/// * the whole database is a **BACPAC** — `/a:Export` then `/a:Import` — schema and rows in one
///   file, and the format Azure's own portal uses;
/// * the shape alone is a **DACPAC** — `/a:Extract` then `/a:Publish` — which is minutes rather
///   than hours on a real database, and can leave out what a container has no answer for: logins,
///   permissions, external data sources, everything an Azure database has and a local one cannot.
///
/// So <see cref="DbCloneOptions.SchemaOnly"/> is the honest first question to ask of a database
/// nobody has cloned yet: it says within minutes whether the shape arrives at all.
///
/// Two more things fall out. BACPAC goes server to server over the network, so an external source
/// works exactly like one in the stack. And an import into a database that already holds objects is
/// refused by the tool itself, with SQL71659 — which is the "only when empty" rule, enforced without
/// asking anybody.
internal static class SqlServerCloneRecipe
{
    internal const int DefaultPort = 1433;
    internal const string DefaultImage = "mcr.microsoft.com/dotnet/sdk";
    internal const string DefaultTag = "8.0";

    /// The container that clears the way, for the one case the tool cannot: replacing a database
    /// that is already there. It runs in the server's own image, because `sqlcmd` lives there.
    internal const string PrepareImage = "mcr.microsoft.com/mssql/server";
    internal const string PrepareTag = "2022-latest";

    internal static string PrepareScript() => """
        set -e

        sqlcmd="/opt/mssql-tools18/bin/sqlcmd -C -S $CLONE_TARGET_HOST,$CLONE_TARGET_PORT"
        sqlcmd="$sqlcmd -U $CLONE_TARGET_USER -P $CLONE_TARGET_PASSWORD"

        echo "waiting for $CLONE_TARGET_HOST:$CLONE_TARGET_PORT"
        until $sqlcmd -Q "select 1" >/dev/null 2>&1; do sleep 2; done

        # SINGLE_USER first: a database with a session on it cannot be dropped, and the studio or an
        # application may well have opened one already.
        echo "dropping $CLONE_TARGET_DB so the clone can replace it"
        $sqlcmd -Q "IF DB_ID('$CLONE_TARGET_DB') IS NOT NULL BEGIN
                      ALTER DATABASE [$CLONE_TARGET_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                      DROP DATABASE [$CLONE_TARGET_DB];
                    END"

        echo "done"
        """;

    internal static string Script() => """
        set -e
        export PATH="$PATH:/root/.dotnet/tools"

        echo "installing sqlpackage"
        dotnet tool install -g microsoft.sqlpackage >/dev/null

        # Encrypt is on by default in the modern drivers, and a development server's certificate is
        # self-signed: without this every connection fails on a certificate nobody can fix.
        common="TrustServerCertificate=True"

        source_cs="Server=$CLONE_SOURCE_HOST,$CLONE_SOURCE_PORT;Database=$CLONE_SOURCE_DB"
        source_cs="$source_cs;User Id=$CLONE_SOURCE_USER;Password=$CLONE_SOURCE_PASSWORD;$common"

        target_cs="Server=$CLONE_TARGET_HOST,$CLONE_TARGET_PORT;Database=$CLONE_TARGET_DB"
        target_cs="$target_cs;User Id=$CLONE_TARGET_USER;Password=$CLONE_TARGET_PASSWORD;$common"

        _started=$(date +%s)

        _elapsed() {
            _now=$(date +%s)
            echo "$(( (_now - _started) / 60 ))m$(( (_now - _started) % 60 ))s"
        }

        # sqlpackage says what it is doing — table by table on an export, object by object on an
        # extract — and every word of it belongs in this resource's log rather than in a file nobody
        # reads. `tee` keeps a copy anyway, because the retry loop below reads it to tell a target
        # that is not up yet from a real failure.
        #
        # The status has to travel through a file: this is POSIX sh, where the exit code of a
        # pipeline is the last command's, and the last command here is tee.
        _run() {
            _log="$1"; shift
            rm -f /tmp/status

            # A heartbeat, because "Extracting schema" can be the last thing said for ten minutes on
            # a real database, and silence is indistinguishable from a hang.
            ( while :; do sleep 60; echo "  still working, $(_elapsed) so far"; done ) &
            _beat=$!

            # timeout, because a stalled query is the failure mode here: a serverless database that
            # never wakes, a connection a firewall drops silently. Without it the clone waits for
            # ever and looks like it is working.
            ( timeout "${CLONE_TIMEOUT:-3600}" "$@" 2>&1; echo $? >/tmp/status ) | tee -a "$_log"

            kill "$_beat" 2>/dev/null || true
            _code=$(cat /tmp/status 2>/dev/null || echo 1)

            [ "$_code" = "124" ] && echo "  gave up after ${CLONE_TIMEOUT:-3600}s (DbCloneOptions.TimeoutSeconds)"
            return "$_code"
        }

        if [ "$CLONE_DATA_ONLY" = "1" ]; then
          echo "sqlpackage has no data-only mode: a BACPAC is schema and rows together"
          exit 1
        fi

        # --- the shape alone: extract to a dacpac, publish it -------------------------------------
        if [ "$CLONE_SCHEMA_ONLY" = "1" ]; then
          echo "extracting the schema of $CLONE_SOURCE_DB"

          extract="/p:ExtractAllTableData=False /p:VerifyExtraction=False /p:IgnorePermissions=True"
          extract="$extract /p:IgnoreUserLoginMappings=True /p:CommandTimeout=0"

          if ! _run /tmp/extract.log sqlpackage /a:Extract /scs:"$source_cs" \
               /tf:/tmp/clone.dacpac $extract /q:True; then
            if grep -qiE "View Definition permission|permission was denied|is not a member of" /tmp/extract.log; then
              echo "this login may not read the database's definitions (see above)."
              echo "reading rows is not enough for a schema: db_owner on the source, or at least"
              echo "GRANT VIEW DEFINITION to it."
            fi

            echo "the schema could not be extracted, $(_elapsed) in"
            exit 1
          fi

          echo "extracted $(wc -c < /tmp/clone.dacpac) byte(s) in $(_elapsed)"

          # What an Azure database has and a container cannot have. Excluded rather than fought: a
          # copy for development wants the tables, not the logins of the server it came from.
          # Nothing here may destroy what is already in the target: DropObjectsNotInSource keeps
          # anything the source does not have, and BlockOnPossibleDataLoss stays at its default, so a
          # publish that would have to rewrite a table stops instead. That also makes this the one
          # clone where "only when empty" does not apply: a schema publish adds and never removes.
          publish="/p:AllowIncompatiblePlatform=True /p:CommandTimeout=0"
          publish="$publish /p:DropObjectsNotInSource=False"
          publish="$publish /p:IgnorePermissions=True /p:IgnoreRoleMembership=True"
          publish="$publish /p:IgnoreLoginSids=True /p:IgnoreUserSettingsObjects=True"
          publish="$publish /p:ExcludeObjectTypes=Users;Logins;RoleMembership;Permissions;Credentials;DatabaseScopedCredentials;ExternalDataSources;ExternalFileFormats;ExternalTables;ServerRoles;ServerRoleMembership;Audits;DatabaseAuditSpecifications;ServerAuditSpecifications;Endpoints;LinkedServers;LinkedServerLogins"

          echo "publishing the schema into $CLONE_TARGET_DB (nothing is dropped)"

          for attempt in $(seq 1 60); do
            if _run /tmp/publish.log sqlpackage /a:Publish /tcs:"$target_cs" \
               /sf:/tmp/clone.dacpac $publish /q:True; then
              echo "done: the schema of $CLONE_SOURCE_DB is in $CLONE_TARGET_DB, $(_elapsed) in total"
              exit 0
            fi

            # A target that is not up yet says so on the connection; anything else has been printed
            # already and another attempt will print it again.
            if ! grep -qiE "could not open a connection|not currently available|Login timeout" /tmp/publish.log; then
              echo "the schema could not be published, $(_elapsed) in"
              exit 1
            fi

            sleep 5
          done

          echo "the schema could not be published: the target never answered"
          exit 1
        fi

        # --- the whole database: export to a bacpac, import it ------------------------------------

        # No client here to ask "are you up yet" with, so the export is the question: it fails while
        # the source is still starting, and a stack's databases take their time.
        echo "exporting $CLONE_SOURCE_DB"
        exported=0

        for attempt in $(seq 1 60); do
          if _run /tmp/export.log sqlpackage /a:Export /scs:"$source_cs" /tf:/tmp/clone.bacpac \
             /p:CommandTimeout=0 /q:True; then
            exported=1
            break
          fi

          # Only a source that is not up yet is worth another attempt. Everything else is a verdict,
          # and repeating it sixty times over four minutes only hides it: an Azure-only construct a
          # BACPAC cannot carry, or a login that may read the rows but not the definitions.
          if grep -qE "SQL715[0-9][0-9]|SQL7156[0-9]" /tmp/export.log; then
            echo "this database has objects a BACPAC cannot carry (see above)."
            echo "DbCloneOptions { SchemaOnly = true } leaves them out."
            exit 1
          fi

          if grep -qiE "View Definition permission|permission was denied|is not a member of" /tmp/export.log; then
            echo "this login may not read the database's definitions (see above)."
            echo "an export needs more than reading rows: db_owner on the source, or at least"
            echo "GRANT VIEW DEFINITION to it."
            exit 1
          fi

          if ! grep -qiE "could not open a connection|not currently available|Login timeout|error: 35" /tmp/export.log; then
            echo "the export failed, $(_elapsed) in"
            exit 1
          fi

          sleep 5
        done

        if [ "$exported" != "1" ]; then
          echo "the source could not be exported, $(_elapsed) in"
          exit 1
        fi

        echo "exported $(wc -c < /tmp/clone.bacpac) byte(s) in $(_elapsed)"

        # Import creates the database. Into one that already holds objects it refuses with SQL71659,
        # which is exactly the rule we want — so that refusal is read rather than fought.
        echo "importing into $CLONE_TARGET_DB"

        for attempt in $(seq 1 60); do
          if _run /tmp/import.log sqlpackage /a:Import /tcs:"$target_cs" /sf:/tmp/clone.bacpac \
             /p:CommandTimeout=0 /q:True; then
            echo "done: $CLONE_TARGET_DB, $(_elapsed) in total"
            exit 0
          fi

          if grep -q "SQL71659" /tmp/import.log; then
            if [ "$CLONE_ONLY_WHEN_EMPTY" = "1" ]; then
              echo "$CLONE_TARGET_DB already has objects in it; nothing was copied"
              exit 0
            fi

            echo "$CLONE_TARGET_DB already has objects in it, and this clone was told to replace it"
            exit 1
          fi

          # A target that is not up yet is worth another attempt; a refusal is not.
          if ! grep -qiE "could not open a connection|not currently available|Login timeout|error: 35" /tmp/import.log; then
            echo "the import failed, $(_elapsed) in"
            exit 1
          fi

          sleep 5
        done

        echo "the import did not succeed, $(_elapsed) in"
        exit 1
        """;
}
