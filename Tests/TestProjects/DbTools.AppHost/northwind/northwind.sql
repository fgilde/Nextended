-- Northwind, as the shape everybody recognises: eight tables, the relationships between them, a
-- view, and enough rows to join. The structure is Northwind's; the rows are made up, so this file
-- carries nobody else's data.
--
-- This is the database the demo *clones from*. It runs in a container this stack does not model as
-- a database resource — it stands in for the server that lives somewhere else, and the clone reaches
-- it by connection string exactly as it would reach one.

CREATE TABLE categories (
    category_id     serial PRIMARY KEY,
    category_name   varchar(40) NOT NULL UNIQUE,
    description     text
);

CREATE TABLE suppliers (
    supplier_id     serial PRIMARY KEY,
    company_name    varchar(80) NOT NULL,
    contact_name    varchar(60),
    city            varchar(40),
    country         varchar(40)
);

CREATE TABLE shippers (
    shipper_id      serial PRIMARY KEY,
    company_name    varchar(80) NOT NULL,
    phone           varchar(30)
);

CREATE TABLE customers (
    customer_id     char(5) PRIMARY KEY,
    company_name    varchar(80) NOT NULL,
    contact_name    varchar(60),
    contact_title   varchar(40),
    city            varchar(40),
    country         varchar(40)
);

CREATE TABLE employees (
    employee_id     serial PRIMARY KEY,
    last_name       varchar(40) NOT NULL,
    first_name      varchar(40) NOT NULL,
    title           varchar(60),
    hire_date       date,
    reports_to      integer REFERENCES employees(employee_id)
);

CREATE TABLE products (
    product_id      serial PRIMARY KEY,
    product_name    varchar(80) NOT NULL,
    supplier_id     integer REFERENCES suppliers(supplier_id),
    category_id     integer REFERENCES categories(category_id),
    quantity_per_unit varchar(40),
    unit_price      numeric(10,2) NOT NULL CHECK (unit_price >= 0),
    units_in_stock  smallint NOT NULL DEFAULT 0,
    discontinued    boolean NOT NULL DEFAULT false
);

CREATE TABLE orders (
    order_id        serial PRIMARY KEY,
    customer_id     char(5) NOT NULL REFERENCES customers(customer_id),
    employee_id     integer REFERENCES employees(employee_id),
    order_date      date NOT NULL,
    shipped_date    date,
    ship_via        integer REFERENCES shippers(shipper_id),
    freight         numeric(10,2) NOT NULL DEFAULT 0,
    ship_city       varchar(40),
    ship_country    varchar(40)
);

CREATE TABLE order_details (
    order_id        integer NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_id      integer NOT NULL REFERENCES products(product_id),
    unit_price      numeric(10,2) NOT NULL,
    quantity        smallint NOT NULL CHECK (quantity > 0),
    discount        real NOT NULL DEFAULT 0,
    PRIMARY KEY (order_id, product_id)
);

CREATE INDEX ix_orders_customer ON orders(customer_id);
CREATE INDEX ix_orders_date ON orders(order_date);
CREATE INDEX ix_products_category ON products(category_id);

-- A view, so the clone has something other than tables to carry.
CREATE VIEW order_totals AS
SELECT o.order_id,
       o.order_date,
       c.company_name AS customer,
       e.first_name || ' ' || e.last_name AS sold_by,
       round(sum(d.unit_price * d.quantity * (1 - d.discount))::numeric, 2) AS total
  FROM orders o
  JOIN customers c ON c.customer_id = o.customer_id
  LEFT JOIN employees e ON e.employee_id = o.employee_id
  JOIN order_details d ON d.order_id = o.order_id
 GROUP BY o.order_id, o.order_date, c.company_name, e.first_name, e.last_name;

INSERT INTO categories (category_name, description) VALUES
    ('Beverages', 'Soft drinks, coffees, teas, beers and ales'),
    ('Condiments', 'Sweet and savoury sauces, relishes, spreads and seasonings'),
    ('Confections', 'Desserts, candies and sweet breads'),
    ('Dairy Products', 'Cheeses'),
    ('Grains/Cereals', 'Breads, crackers, pasta and cereal'),
    ('Meat/Poultry', 'Prepared meats'),
    ('Produce', 'Dried fruit and bean curd'),
    ('Seafood', 'Seaweed and fish');

INSERT INTO suppliers (company_name, contact_name, city, country) VALUES
    ('Exotic Liquids', 'Charlotte Cooper', 'London', 'UK'),
    ('New Orleans Cajun Delights', 'Shelley Burke', 'New Orleans', 'USA'),
    ('Grandma Kelly''s Homestead', 'Regina Murphy', 'Ann Arbor', 'USA'),
    ('Tokyo Traders', 'Yoshi Nagase', 'Tokyo', 'Japan'),
    ('Bigfoot Breweries', 'Cheryl Saylor', 'Bend', 'USA'),
    ('Formaggi Fortini', 'Elio Rossi', 'Ravenna', 'Italy');

INSERT INTO shippers (company_name, phone) VALUES
    ('Speedy Express', '(503) 555-9831'),
    ('United Package', '(503) 555-3199'),
    ('Federal Shipping', '(503) 555-9931');

INSERT INTO customers (customer_id, company_name, contact_name, contact_title, city, country) VALUES
    ('ALFKI', 'Alfreds Futterkiste', 'Maria Anders', 'Sales Representative', 'Berlin', 'Germany'),
    ('ANATR', 'Ana Trujillo Emparedados', 'Ana Trujillo', 'Owner', 'México D.F.', 'Mexico'),
    ('AROUT', 'Around the Horn', 'Thomas Hardy', 'Sales Representative', 'London', 'UK'),
    ('BERGS', 'Berglunds snabbköp', 'Christina Berglund', 'Order Administrator', 'Luleå', 'Sweden'),
    ('BLAUS', 'Blauer See Delikatessen', 'Hanna Moos', 'Sales Representative', 'Mannheim', 'Germany'),
    ('BONAP', 'Bon app''', 'Laurence Lebihan', 'Owner', 'Marseille', 'France'),
    ('CHOPS', 'Chop-suey Chinese', 'Yang Wang', 'Owner', 'Bern', 'Switzerland'),
    ('EASTC', 'Eastern Connection', 'Ann Devon', 'Sales Agent', 'London', 'UK'),
    ('FRANK', 'Frankenversand', 'Peter Franken', 'Marketing Manager', 'München', 'Germany'),
    ('QUICK', 'QUICK-Stop', 'Horst Kloss', 'Accounting Manager', 'Cunewalde', 'Germany');

INSERT INTO employees (last_name, first_name, title, hire_date, reports_to) VALUES
    ('Fuller', 'Andrew', 'Vice President, Sales', '2019-08-14', NULL);

INSERT INTO employees (last_name, first_name, title, hire_date, reports_to) VALUES
    ('Davolio', 'Nancy', 'Sales Representative', '2020-05-01', 1),
    ('Leverling', 'Janet', 'Sales Representative', '2020-04-01', 1),
    ('Peacock', 'Margaret', 'Sales Representative', '2021-05-03', 1),
    ('Buchanan', 'Steven', 'Sales Manager', '2021-10-17', 1),
    ('Suyama', 'Michael', 'Sales Representative', '2022-10-17', 5),
    ('King', 'Robert', 'Sales Representative', '2023-01-02', 5);

INSERT INTO products (product_name, supplier_id, category_id, quantity_per_unit, unit_price, units_in_stock, discontinued) VALUES
    ('Chai', 1, 1, '10 boxes x 20 bags', 18.00, 39, false),
    ('Chang', 1, 1, '24 - 12 oz bottles', 19.00, 17, false),
    ('Aniseed Syrup', 1, 2, '12 - 550 ml bottles', 10.00, 13, false),
    ('Chef Anton''s Cajun Seasoning', 2, 2, '48 - 6 oz jars', 22.00, 53, false),
    ('Chef Anton''s Gumbo Mix', 2, 2, '36 boxes', 21.35, 0, true),
    ('Grandma''s Boysenberry Spread', 3, 2, '12 - 8 oz jars', 25.00, 120, false),
    ('Uncle Bob''s Organic Dried Pears', 3, 7, '12 - 1 lb pkgs.', 30.00, 15, false),
    ('Mishi Kobe Niku', 4, 6, '18 - 500 g pkgs.', 97.00, 29, true),
    ('Ikura', 4, 8, '12 - 200 ml jars', 31.00, 31, false),
    ('Sasquatch Ale', 5, 1, '24 - 12 oz bottles', 14.00, 111, false),
    ('Steeleye Stout', 5, 1, '24 - 12 oz bottles', 18.00, 20, false),
    ('Mozzarella di Giovanni', 6, 4, '24 - 200 g pkgs.', 34.80, 14, false),
    ('Gorgonzola Telino', 6, 4, '12 - 100 g pkgs', 12.50, 0, false),
    ('Pavlova', 3, 3, '32 - 500 g boxes', 17.45, 29, false),
    ('Tarte au sucre', 3, 3, '48 pies', 49.30, 17, false);

INSERT INTO orders (customer_id, employee_id, order_date, shipped_date, ship_via, freight, ship_city, ship_country) VALUES
    ('ALFKI', 2, '2026-01-15', '2026-01-19', 1, 29.46, 'Berlin', 'Germany'),
    ('ALFKI', 3, '2026-02-03', '2026-02-06', 2, 61.02, 'Berlin', 'Germany'),
    ('ANATR', 4, '2026-02-11', NULL, 1, 12.75, 'México D.F.', 'Mexico'),
    ('AROUT', 2, '2026-02-20', '2026-02-24', 3, 88.01, 'London', 'UK'),
    ('BERGS', 6, '2026-03-01', '2026-03-05', 2, 41.34, 'Luleå', 'Sweden'),
    ('BLAUS', 7, '2026-03-08', NULL, 1, 3.25, 'Mannheim', 'Germany'),
    ('BONAP', 3, '2026-03-14', '2026-03-18', 2, 55.09, 'Marseille', 'France'),
    ('CHOPS', 4, '2026-03-22', '2026-03-25', 3, 19.80, 'Bern', 'Switzerland'),
    ('EASTC', 2, '2026-04-02', '2026-04-04', 1, 7.45, 'London', 'UK'),
    ('FRANK', 6, '2026-04-09', NULL, 2, 96.12, 'München', 'Germany'),
    ('QUICK', 5, '2026-04-16', '2026-04-20', 3, 143.28, 'Cunewalde', 'Germany'),
    ('QUICK', 5, '2026-04-28', '2026-05-02', 1, 22.98, 'Cunewalde', 'Germany');

INSERT INTO order_details (order_id, product_id, unit_price, quantity, discount) VALUES
    (1, 1, 18.00, 12, 0),
    (1, 6, 25.00, 4, 0),
    (2, 2, 19.00, 6, 0.05),
    (2, 10, 14.00, 24, 0),
    (3, 4, 22.00, 3, 0),
    (4, 12, 34.80, 10, 0.10),
    (4, 14, 17.45, 6, 0),
    (5, 9, 31.00, 8, 0),
    (5, 11, 18.00, 12, 0.05),
    (6, 3, 10.00, 2, 0),
    (7, 15, 49.30, 4, 0),
    (7, 13, 12.50, 20, 0.15),
    (8, 8, 97.00, 1, 0),
    (9, 7, 30.00, 5, 0),
    (10, 1, 18.00, 30, 0.20),
    (10, 2, 19.00, 15, 0.20),
    (11, 5, 21.35, 10, 0),
    (11, 12, 34.80, 25, 0.10),
    (11, 14, 17.45, 12, 0),
    (12, 10, 14.00, 6, 0);

-- Something to notice in a data-quality rule: one order was shipped before it was placed.
UPDATE orders SET shipped_date = order_date - 1 WHERE order_id = 12;
