-- What each order came to, with the customer's name: the join everybody writes first.
SELECT o.id            AS order_id,
       c.name          AS customer,
       o.status,
       sum(i.quantity * i.unit_price) AS total
  FROM orders o
  JOIN customers c   ON c.id = o.customer_id
  LEFT JOIN order_items i ON i.order_id = o.id
 GROUP BY o.id, c.name, o.status
 ORDER BY total DESC NULLS LAST;
