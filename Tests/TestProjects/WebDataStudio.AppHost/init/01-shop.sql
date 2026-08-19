-- Seed data for the demo AppHost: a small shop to click around in.
-- Postgres runs everything in /docker-entrypoint-initdb.d on first start of an empty data volume.

CREATE TABLE customers (
    id          serial PRIMARY KEY,
    name        text NOT NULL,
    email       text NOT NULL UNIQUE,
    city        text,
    signed_up   date NOT NULL DEFAULT current_date
);

CREATE TABLE products (
    id          serial PRIMARY KEY,
    sku         text NOT NULL UNIQUE,
    name        text NOT NULL,
    price       numeric(10,2) NOT NULL CHECK (price >= 0),
    in_stock    integer NOT NULL DEFAULT 0
);

CREATE TABLE orders (
    id          serial PRIMARY KEY,
    customer_id integer NOT NULL REFERENCES customers(id),
    placed_at   timestamptz NOT NULL DEFAULT now(),
    status      text NOT NULL DEFAULT 'new'
);

CREATE TABLE order_items (
    id          serial PRIMARY KEY,
    order_id    integer NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id  integer NOT NULL REFERENCES products(id),
    quantity    integer NOT NULL CHECK (quantity > 0),
    unit_price  numeric(10,2) NOT NULL
);

CREATE INDEX ix_orders_customer ON orders(customer_id);
CREATE INDEX ix_order_items_order ON order_items(order_id);

-- A view, so the tree has one of those too.
CREATE VIEW order_totals AS
SELECT o.id AS order_id,
       c.name AS customer,
       o.status,
       sum(i.quantity * i.unit_price) AS total
  FROM orders o
  JOIN customers c ON c.id = o.customer_id
  LEFT JOIN order_items i ON i.order_id = o.id
 GROUP BY o.id, c.name, o.status;

INSERT INTO customers (name, email, city) VALUES
    ('Ada Lovelace',   'ada@example.com',   'London'),
    ('Linus Torvalds', 'linus@example.com', 'Helsinki'),
    ('Grace Hopper',   'grace@example.com', 'New York'),
    ('Alan Turing',    'alan@example.com',  'Manchester');

INSERT INTO products (sku, name, price, in_stock) VALUES
    ('KB-01', 'Mechanical keyboard', 129.00, 42),
    ('MS-02', 'Trackball mouse',      79.50, 17),
    ('MN-03', 'Portable monitor',    249.99,  8),
    ('CB-04', 'USB-C cable',           9.90, 320);

INSERT INTO orders (customer_id, status) VALUES
    (1, 'shipped'), (1, 'new'), (2, 'shipped'), (3, 'cancelled');

INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES
    (1, 1, 1, 129.00),
    (1, 4, 3,   9.90),
    (2, 3, 1, 249.99),
    (3, 2, 2,  79.50),
    (4, 4, 1,   9.90);
