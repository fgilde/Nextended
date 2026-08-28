-- The second half of the demo: one thing for each of the studio's less obvious panels, so nothing in
-- the tree is a heading with nothing under it.
--
-- Postgres runs everything in /docker-entrypoint-initdb.d on first start of an empty data volume, in
-- file-name order, so this lands after 01-shop.sql and can point at its tables.

-- An enum and a domain: the tree lists both, and the table designer offers the enum as a type.
CREATE TYPE shipment_state AS ENUM ('picked', 'packed', 'handed_over', 'delivered', 'lost');
CREATE DOMAIN email AS text CHECK (VALUE LIKE '%@%');

-- pgcrypto ships with the image and gives the extensions node something to show.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- --- a document column, for the JSON shape panel -------------------------------------------------
-- The same paths in most rows, one row with an extra one, one with a nested object and one with an
-- array: enough for the report to have something to be honest about.
CREATE TABLE events (
    id          bigserial PRIMARY KEY,
    customer_id integer REFERENCES customers(id),
    kind        text NOT NULL,
    payload     jsonb NOT NULL,
    at          timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE events IS 'What happened, as the application wrote it: one JSONB document per event.';
COMMENT ON COLUMN events.payload IS 'Shape varies by kind — ask the column menu what is in it.';

INSERT INTO events (customer_id, kind, payload, at) VALUES
    (1, 'signup',   '{"plan":"pro","seats":4,"source":"web"}',                                    now() - interval '90 days'),
    (2, 'signup',   '{"plan":"free","seats":1,"source":"web"}',                                   now() - interval '80 days'),
    (1, 'upgrade',  '{"plan":"team","seats":12,"source":"sales","note":"call on friday"}',         now() - interval '40 days'),
    (3, 'signup',   '{"plan":"pro","seats":2,"source":"referral"}',                               now() - interval '35 days'),
    (4, 'cancel',   '{"plan":"pro","seats":2,"reason":"price","refund":{"amount":49.5,"currency":"EUR"}}', now() - interval '20 days'),
    (2, 'signup',   '{"plan":"pro","seats":3,"source":"web","tags":["beta","invited"]}',           now() - interval '10 days'),
    (3, 'login',    '{"ip":"203.0.113.7","agent":{"browser":"Firefox","os":"Linux"}}',             now() - interval '2 days'),
    (1, 'login',    '{"ip":"203.0.113.9","agent":{"browser":"Chrome","os":"Windows"}}',            now() - interval '1 day');

CREATE INDEX ix_events_customer ON events(customer_id);
-- A GIN index over a document column, which the structure panel spells out.
CREATE INDEX ix_events_payload ON events USING gin (payload);

-- --- data worth having rules about ---------------------------------------------------------------
-- Deliberately dirty, so the Data quality tab has something to count: a row with no customer, a
-- duplicate reference, a negative total, a reference to a customer that does not exist, and a
-- timestamp nobody has touched in a month.
CREATE TABLE invoices (
    id          serial PRIMARY KEY,
    customer_id integer,
    reference   text,
    total       numeric(10,2),
    issued_at   timestamptz,
    paid_at     timestamptz
);

COMMENT ON TABLE invoices IS
    'Left dirty on purpose: five rules in Administration -> Data quality each find something here.';

INSERT INTO invoices (customer_id, reference, total, issued_at, paid_at) VALUES
    (1,    'INV-1001',  129.00, now() - interval '3 days',  now() - interval '1 day'),
    (2,    'INV-1002',   79.50, now() - interval '2 days',  NULL),
    (NULL, 'INV-1003',  249.99, now() - interval '2 days',  NULL),   -- no customer at all
    (3,    'INV-1002',    9.90, now() - interval '1 day',   NULL),   -- the same reference twice
    (4,    'INV-1005',  -20.00, now() - interval '1 day',   NULL),   -- a total below zero
    (99,   'INV-1006',   64.00, now() - interval '40 days', NULL);   -- customer 99 does not exist

-- --- a partitioned table, with partitions in the tree --------------------------------------------
CREATE TABLE readings (
    id       bigserial,
    sensor   text NOT NULL,
    taken_at timestamptz NOT NULL,
    value    numeric(10,3) NOT NULL
) PARTITION BY RANGE (taken_at);

CREATE TABLE readings_2026_q2 PARTITION OF readings
    FOR VALUES FROM ('2026-04-01') TO ('2026-07-01');
CREATE TABLE readings_2026_q3 PARTITION OF readings
    FOR VALUES FROM ('2026-07-01') TO ('2026-10-01');

INSERT INTO readings (sensor, taken_at, value)
SELECT 'sensor-' || (1 + (n % 3)),
       timestamptz '2026-04-02 00:00:00' + (n || ' hours')::interval,
       20 + (n % 17) * 0.25
  FROM generate_series(0, 3000) AS n;

-- --- something big enough to be worth an index -------------------------------------------------
-- 60 000 rows and no index on the column the demo queries look up by: the health report and the
-- capture advisor both have something real to say, and the sizes and growth panels have a table
-- worth watching.
CREATE TABLE page_views (
    id        bigserial PRIMARY KEY,
    path      text NOT NULL,
    session   uuid NOT NULL,
    viewed_at timestamptz NOT NULL,
    ms        integer NOT NULL
);

INSERT INTO page_views (path, session, viewed_at, ms)
SELECT '/' || (ARRAY['', 'pricing', 'docs', 'blog', 'signup'])[1 + (n % 5)],
       gen_random_uuid(),
       now() - ((n % 60) || ' days')::interval,
       5 + (n % 400)
  FROM generate_series(1, 60000) AS n;

-- --- a materialised view, a function and a trigger ----------------------------------------------
CREATE MATERIALIZED VIEW busiest_paths AS
SELECT path, count(*) AS views, round(avg(ms)) AS avg_ms
  FROM page_views
 GROUP BY path
 ORDER BY views DESC;

CREATE FUNCTION order_total(order_id integer) RETURNS numeric
LANGUAGE plpgsql STABLE AS $$
DECLARE
    result numeric;
BEGIN
    SELECT COALESCE(sum(quantity * unit_price), 0) INTO result
      FROM order_items WHERE order_items.order_id = $1;

    RAISE NOTICE 'order % totals %', $1, result;
    RETURN result;
END;
$$;

COMMENT ON FUNCTION order_total(integer) IS
    'Raises a notice as well as returning: try "Run and roll back" in the object menu.';

CREATE TABLE audit_log (
    id      bigserial PRIMARY KEY,
    table_name text NOT NULL,
    action  text NOT NULL,
    at      timestamptz NOT NULL DEFAULT now()
);

CREATE FUNCTION note_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO audit_log (table_name, action) VALUES (TG_TABLE_NAME, TG_OP);
    RETURN NULL;
END;
$$;

CREATE TRIGGER orders_noted
    AFTER INSERT OR UPDATE OR DELETE ON orders
    FOR EACH ROW EXECUTE FUNCTION note_change();

-- --- row-level security, with policies the structure panel lists --------------------------------
CREATE TABLE tickets (
    id       serial PRIMARY KEY,
    owner    text NOT NULL,
    subject  text NOT NULL,
    body     text,
    severity integer NOT NULL DEFAULT 3 CHECK (severity BETWEEN 1 AND 5)
);

INSERT INTO tickets (owner, subject, body, severity) VALUES
    ('ada',   'Keyboard arrived with two keys missing', 'Both shift keys.', 2),
    ('linus', 'Invoice INV-1002 is duplicated',        'Two invoices, same reference.', 1),
    ('grace', 'Monitor flickers below 60 Hz',          NULL, 3);

ALTER TABLE tickets ENABLE ROW LEVEL SECURITY;

CREATE POLICY tickets_own ON tickets
    FOR SELECT USING (owner = current_user);

CREATE POLICY tickets_admin ON tickets
    FOR ALL TO postgres USING (true);

-- --- geography, for the map view -----------------------------------------------------------------
CREATE TABLE warehouses (
    id      serial PRIMARY KEY,
    name    text NOT NULL,
    lat     double precision NOT NULL,
    lon     double precision NOT NULL,
    opened  date NOT NULL
);

INSERT INTO warehouses (name, lat, lon, opened) VALUES
    ('London',    51.5072,  -0.1276, '2024-03-01'),
    ('Helsinki',  60.1699,  24.9384, '2025-01-15'),
    ('New York',  40.7128, -74.0060, '2023-11-20'),
    ('Lisbon',    38.7223,  -9.1393, '2026-02-02'),
    ('Nairobi',   -1.2921,  36.8219, '2026-05-11');

-- --- a sequence and a role, so those nodes are not empty either ----------------------------------
CREATE SEQUENCE picking_slip_no START 5000;
CREATE ROLE reporting NOLOGIN;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO reporting;

-- --- a second schema, so the schema scope has something to scope ---------------------------------
CREATE SCHEMA warehouse;

CREATE TABLE warehouse.shipments (
    id           serial PRIMARY KEY,
    order_id     integer NOT NULL REFERENCES orders(id),
    warehouse_id integer NOT NULL REFERENCES warehouses(id),
    state        shipment_state NOT NULL DEFAULT 'picked',
    shipped_at   timestamptz
);

INSERT INTO warehouse.shipments (order_id, warehouse_id, state, shipped_at) VALUES
    (1, 1, 'delivered',   now() - interval '5 days'),
    (2, 2, 'packed',      NULL),
    (3, 3, 'handed_over', now() - interval '1 day');

ANALYZE;
