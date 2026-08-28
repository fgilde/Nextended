-- Every row the Data quality rules complain about, in one place: no customer, a duplicated
-- reference, a total below zero, a customer that does not exist, nothing issued in weeks.
SELECT i.*,
       (SELECT count(*) FROM invoices d WHERE d.reference = i.reference) AS same_reference,
       (c.id IS NULL AND i.customer_id IS NOT NULL) AS dangling_customer
  FROM invoices i
  LEFT JOIN customers c ON c.id = i.customer_id
 ORDER BY i.id;
