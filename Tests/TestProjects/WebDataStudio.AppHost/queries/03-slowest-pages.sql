-- 60 000 rows and no index on `path`: run this, then ask the health report or a capture what it
-- would change.
SELECT path,
       count(*)      AS views,
       round(avg(ms)) AS avg_ms,
       max(ms)        AS worst_ms
  FROM page_views
 WHERE viewed_at > now() - interval '30 days'
 GROUP BY path
 ORDER BY avg_ms DESC;
