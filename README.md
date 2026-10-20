# H2Sharp.Linq2Db

The **linq2db** provider for **H2Sharp** — the C# port of H2 Database 2.5.

> **Status: in progress.** The provider generates H2's SQL and the plumbing is in place; the
> database-level half of it waits for the engine to grow. What is missing, what is the port's
> work and what is this provider's is listed in
> [docs/DIVERGENCES.md](docs/DIVERGENCES.md).

The provider is built for **.NET 8** and **.NET 10**; the engine under it has both as well.


## What it is

A provider for [linq2db](https://github.com/linq2db/linq2db), written from scratch against
linq2db's public API and against the engine's public API. It is not a port of another provider:
the dialect, the types, the functions and the identifier quoting are H2's, and they are checked
against H2's frozen documentation and against the reference engine on the JVM.

The rules the work follows are in [AGENTS.md](AGENTS.md); where the provider comes from is in
[UPSTREAM.md](UPSTREAM.md).

## Using it

```csharp
using LinqToDB;
using LinqToDB.DataProvider.H2Sharp;

await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:mydb");

var rows = await db.GetTable<Row>()
    .Where(r => r.Price > 10m)
    .OrderBy(r => r.Id)
    .ToListAsync();
```

The connection string is the JDBC URL. The ADO.NET spelling works too — `Data Source=mem:mydb`
names the same database and is translated — and the settings a URL carries (`MODE=PostgreSQL` and
the like) are the connection string's other keys.

**[docs/USAGE.md](docs/USAGE.md) is the guide**: connecting, a data context, mapping, the
functions of H2, the table functions of H2, window functions, scaffolding, reading the
catalogue, the SQL the provider generates, and what does not work yet.


## What it generates

| | |
|---|---|
| Identifiers | always quoted, the way H2 documents it: an unquoted identifier folds to upper case and a quoted one does not |
| Types | H2's own: `varchar`, `char`, `numeric`, `varbinary`, `uuid`, `json`, `clob`, `blob`, `timestamp`, `time`, `boolean`, `smallint`, `integer`, `bigint`, `real`, `double` |
| Paging | `LIMIT n` / `OFFSET n` |
| Concatenation | `\|\|`, which H2 has |
| Parameters | named `@name`, which the engine rewrites into `?` before the statement is parsed |
| Table alias | without the `AS`: `FROM "Rows" "r"` — the grammar allows both spellings ("[ [ AS ] newTableAlias ]"), and this is the one the engine reads |

The capabilities the provider declares are H2 2.5's, not the engine's current ones: a provider that
declared the engine's abilities would be describing a database that does not exist.

## The engine

The provider talks to **H2Sharp**, a sibling checkout (`../h2sharp`); the path is overridden with
the `H2SharpProject` property. The engine is dual-licensed under the MPL 2.0 and the EPL 1.0.

The engine is at <https://github.com/dimzon/h2sharp>, and the other provider of it, the Entity
Framework Core one, is at <https://github.com/dimzon/h2sharp-efcore>.


## Licensing

MIT; see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md).
