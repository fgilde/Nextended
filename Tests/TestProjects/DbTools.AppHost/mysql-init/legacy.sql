-- The MySQL database this stack clones *from*. Unlike the Postgres one it is a resource of the stack
-- itself, which is the other half of the demo: a clone reads a resource as readily as a string.
--
-- Loaded by the MySQL image on its first start. The database is created here rather than left to the
-- app host, so this file works whoever starts it.

CREATE DATABASE IF NOT EXISTS legacy;
USE legacy;

CREATE TABLE suppliers (
    supplier_id  int AUTO_INCREMENT PRIMARY KEY,
    company      varchar(60) NOT NULL,
    country      varchar(40) NOT NULL,
    contact      varchar(60)
) ENGINE = InnoDB;

CREATE TABLE parts (
    part_id      int AUTO_INCREMENT PRIMARY KEY,
    supplier_id  int NOT NULL,
    sku          varchar(20) NOT NULL UNIQUE,
    description  varchar(120) NOT NULL,
    price        decimal(10, 2) NOT NULL,
    in_stock     int NOT NULL DEFAULT 0,
    CONSTRAINT fk_parts_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (supplier_id)
) ENGINE = InnoDB;

CREATE INDEX ix_parts_supplier ON parts (supplier_id);

-- A view and a routine, because a clone that only copied tables would be the easy half.
CREATE VIEW stock_value AS
SELECT s.company,
       COUNT(p.part_id)              AS parts,
       SUM(p.price * p.in_stock)     AS value
FROM suppliers s
         JOIN parts p ON p.supplier_id = s.supplier_id
GROUP BY s.company;

CREATE FUNCTION price_with_vat(net decimal(10, 2)) RETURNS decimal(10, 2) DETERMINISTIC
    RETURN ROUND(net * 1.19, 2);

INSERT INTO suppliers (company, country, contact) VALUES
    ('Nordwerk GmbH', 'Germany', 'Ilse Brandt'),
    ('Peraltas Metais', 'Portugal', 'Rui Peralta'),
    ('Kestrel Tooling', 'United Kingdom', 'Dana Okafor'),
    ('Aomori Seiki', 'Japan', 'Haruka Ito');

INSERT INTO parts (supplier_id, sku, description, price, in_stock) VALUES
    (1, 'NW-1001', 'Hex bolt M8, galvanised, 100 pcs', 12.40, 320),
    (1, 'NW-1002', 'Hex nut M8, galvanised, 200 pcs', 8.90, 540),
    (1, 'NW-2010', 'Roller bearing 6204-2RS', 6.75, 96),
    (2, 'PM-330', 'Aluminium profile 40x40, 2 m', 21.50, 44),
    (2, 'PM-331', 'Aluminium profile 40x80, 2 m', 38.00, 18),
    (2, 'PM-540', 'Corner bracket 40, black', 3.20, 610),
    (3, 'KT-77', 'Torque wrench 20-100 Nm', 118.00, 7),
    (3, 'KT-78', 'Socket set 1/2", 24 pieces', 74.50, 12),
    (3, 'KT-91', 'Digital caliper 150 mm', 43.90, 25),
    (4, 'AS-4', 'Linear rail HGR15, 600 mm', 61.00, 16),
    (4, 'AS-5', 'Ball screw SFU1605, 600 mm', 89.90, 9),
    (4, 'AS-6', 'Stepper motor NEMA 23, 1.9 Nm', 47.00, 31);
