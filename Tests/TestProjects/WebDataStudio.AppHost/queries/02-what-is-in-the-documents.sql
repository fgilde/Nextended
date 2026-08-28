-- The document column, flattened. The column menu writes this for you — "What is in this JSON" —
-- and this is what it produces on PostgreSQL.
SELECT id,
       kind,
       payload ->> 'plan'   AS plan,
       (payload ->> 'seats')::int AS seats,
       payload ->> 'source' AS source,
       payload #>> '{refund,amount}' AS refund_amount
  FROM events
 ORDER BY at DESC;
