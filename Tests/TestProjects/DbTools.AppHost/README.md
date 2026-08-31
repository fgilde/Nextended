# DbTools.AppHost

The sample for [Nextended.Aspire.Hosting.DbTools](../../../Nextended.Aspire.Hosting.DbTools/README.md).

```bash
dotnet run
```

Not one table in this app host is described by hand. Both databases the studio shows arrive by
clone — and they come from the two kinds of source a stack ever has:

| Resource | Where it comes from |
|---|---|
| `northwind` (PostgreSQL) | A **connection string**. `northwind-legacy` is a plain container standing in for the server that lives somewhere else — Aspire does not model it as a database, and the clone reaches it the way it would reach that server |
| `parts` (MySQL) | **Another resource in this stack**: `legacy`, seeded by the MySQL image from `mysql-init/`. The typed overload, so two engines cannot be mixed up by accident |

Each clone is a resource of its own in the dashboard — `northwind-clone` and `parts-clone` — and its
log is the dump and restore output. They exit when they are done; that is what finished looks like,
not a failure.

Afterwards the studio (`webdatastudio`, from
[Nextended.Aspire.Hosting.WebDataStudio](../../../Nextended.Aspire.Hosting.WebDataStudio/README.md),
here only so the rows can be clicked through) lists:

* **NORTHWIND** — eight tables, their foreign keys, twelve indexes and the `order_totals` view,
* **PARTS** — suppliers, parts, the `stock_value` view and the `price_with_vat` function.

## Things to try

* **Start it twice.** Both servers keep a data volume, so the second start finds the targets full and
  says `northwind already has 9 table(s); nothing was copied` instead of copying again —
  `OnlyWhenEmpty` is the default. `new DbCloneOptions { Overwrite = true }` is the other answer, and

  ```bash
  docker volume rm dbtools-demo-pg dbtools-demo-mysql
  ```

  is how you get the first start back.
* **Take the studio out.** Delete the two `WithWebDataStudio()` calls; the clones do not care.
* **Point it at something of your own.** Replace the connection string in `northwind-source` with a
  server you have, and the whole demo becomes a copy of that database.

## What is in the folder

| | |
|---|---|
| `Program.cs` | The whole stack, some forty lines |
| `northwind/northwind.sql` | Northwind's structure with made-up rows, loaded by `northwind-legacy` on its first start |
| `mysql-init/legacy.sql` | The MySQL source: suppliers, parts, a view, a function |

The first start pulls a Postgres and a MySQL image and copies both databases, so give it a minute.
