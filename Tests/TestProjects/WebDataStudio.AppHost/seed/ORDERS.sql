-- Seed for the SQL Server database of the demo, run by the studio itself: WithSeedScript("seed")
-- looks for {CONNECTION}.sql, and this connection is called ORDERS.
--
-- It runs once per content — the studio remembers the script's hash — and never on a connection that
-- is read-only or marked as production.

CREATE TABLE dbo.carriers (
    id      int IDENTITY(1,1) PRIMARY KEY,
    name    nvarchar(60) NOT NULL,
    country char(2) NOT NULL,
    active  bit NOT NULL DEFAULT 1
);

CREATE TABLE dbo.deliveries (
    id           int IDENTITY(1,1) PRIMARY KEY,
    carrier_id   int NOT NULL REFERENCES dbo.carriers(id),
    tracking     nvarchar(40) NULL,
    weight_kg    decimal(9,3) NULL,
    handed_over  datetime2 NULL,
    delivered_at datetime2 NULL,
    details      nvarchar(max) NULL
);

CREATE INDEX ix_deliveries_carrier ON dbo.deliveries(carrier_id);
GO

CREATE VIEW dbo.delivery_summary AS
SELECT c.name AS carrier,
       count(*) AS deliveries,
       sum(CASE WHEN d.delivered_at IS NULL THEN 1 ELSE 0 END) AS still_out,
       avg(d.weight_kg) AS avg_weight
  FROM dbo.deliveries d
  JOIN dbo.carriers c ON c.id = d.carrier_id
 GROUP BY c.name;
GO

INSERT INTO dbo.carriers (name, country, active) VALUES
    ('Nordpost', 'FI', 1),
    ('Iberia Cargo', 'PT', 1),
    ('Thames Freight', 'GB', 1),
    ('Retired Couriers', 'DE', 0);

-- Left partly dirty on purpose, the same way the PostgreSQL invoices are: a tracking number that
-- appears twice, a weight below zero, a delivery with no tracking at all, and one that has not moved
-- in weeks.
INSERT INTO dbo.deliveries (carrier_id, tracking, weight_kg, handed_over, delivered_at, details) VALUES
    (1, N'NP-9001', 2.400, DATEADD(day, -6, SYSUTCDATETIME()), DATEADD(day, -4, SYSUTCDATETIME()),
        N'{"boxes":1,"fragile":false}'),
    (2, N'IB-4410', 12.750, DATEADD(day, -5, SYSUTCDATETIME()), NULL,
        N'{"boxes":3,"fragile":true,"note":"stacked"}'),
    (3, N'TF-2201', 0.900, DATEADD(day, -3, SYSUTCDATETIME()), DATEADD(day, -2, SYSUTCDATETIME()),
        N'{"boxes":1,"fragile":false}'),
    (1, N'NP-9001', 5.100, DATEADD(day, -2, SYSUTCDATETIME()), NULL, NULL),
    (2, NULL, 1.250, DATEADD(day, -1, SYSUTCDATETIME()), NULL, NULL),
    (3, N'TF-2202', -3.000, DATEADD(day, -30, SYSUTCDATETIME()), NULL,
        N'{"boxes":2,"fragile":false,"claim":{"amount":40.0,"currency":"GBP"}}');

-- Something with rows in it, so the sizes and the growth panel have a table worth watching on this
-- engine as well.
CREATE TABLE dbo.scans (
    id        bigint IDENTITY(1,1) PRIMARY KEY,
    tracking  nvarchar(40) NOT NULL,
    depot     nvarchar(40) NOT NULL,
    scanned_at datetime2 NOT NULL
);

INSERT INTO dbo.scans (tracking, depot, scanned_at)
SELECT CONCAT(N'NP-', 9000 + (n % 500)),
       CHOOSE(1 + (n % 4), N'Helsinki', N'Lisbon', N'London', N'Berlin'),
       DATEADD(minute, -n, SYSUTCDATETIME())
  FROM (SELECT TOP (20000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
          FROM sys.all_objects a CROSS JOIN sys.all_objects b) AS numbers;
