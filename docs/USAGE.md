# Using H2 with linq2db

The **linq2db** provider for **H2Sharp** — the C# port of H2 Database 2.5. This guide is about
using it: connecting, querying, mapping, the functions of H2, reading the catalogue, and what
does not work yet.

- [Quick start](#quick-start)
- [Getting the provider](#getting-the-provider)
- [Opening a connection](#opening-a-connection)
- [The data context](#the-data-context)
- [Mapping](#mapping)
- [Scaffolding](#scaffolding)
- [The functions of H2](#the-functions-of-h2)
- [Window functions](#window-functions)
- [The catalogue](#the-catalogue)
- [The SQL it generates](#the-sql-it-generates)
- [What does not work yet](#what-does-not-work-yet)
- [Building and testing](#building-and-testing)

## Quick start

```csharp
using LinqToDB;
using LinqToDB.DataProvider.H2Sharp;

await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:quickstart");

await db.ExecuteAsync("""
    CREATE TABLE "Blogs" (
        "Id" integer NOT NULL,
        "Name" varchar(100) NOT NULL,
        "Created" timestamp NOT NULL,
        CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
    )
    """);

await db.InsertAsync(new Blog { Id = 1, Name = "Foo", Created = DateTime.UtcNow });

var foo = await db.GetTable<Blog>()
    .Where(b => b.Name.StartsWith("F"))
    .OrderBy(b => b.Id)
    .ToListAsync();

[Table("Blogs")]
public class Blog
{
    [Column("Id", IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = "";

    [Column("Created")]
    public DateTime Created { get; set; }
}
```

The database is an in-memory one, so it is gone when the connection is closed. A file database
is named the way H2 names it — `jdbc:h2:./data/mydb` — and a session does not open a file database yet, so
for now only the in-memory one works.

## Getting the provider

The provider is built for **.NET 8** and **.NET 10**, and so is the engine under it.

```
dotnet add package H2Sharp.Linq2Db
```

The provider sits on the engine, and the engine is a sibling checkout while it is not published
as a package. The path to it is overridden with the `H2SharpProject` property:

```xml
<ProjectReference Include="..\..\h2sharp\src\H2Sharp\H2Sharp.csproj" />
```

## Opening a connection

Three ways, all of them naming the same database.

**A data connection of your own.** The connection string is the JDBC URL:

```csharp
using LinqToDB.DataProvider.H2Sharp;

await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:mydb");
```

**A data context.** The provider is asked for by hand and the connection string is given to it:

```csharp
using LinqToDB;
using LinqToDB.DataProvider.H2Sharp;

public class BlogContext : DataContext
{
    public BlogContext(string connectionString)
        : base(new DataOptions()
            .UseConnectionString(H2SharpTools.GetDataProvider(), connectionString))
    {
    }

    public ITable<Blog> Blogs => GetTable<Blog>();
}
```

A data context returns its connection to the pool between commands, and an in-memory database
is gone when the last connection to it is closed. The engine knows neither `DB_CLOSE_DELAY` nor
a file database yet, so for now a data context has no database to work in: hold a `DataConnection`
instead, or wait for the engine to grow.

**By the name of a configuration.** The provider registers itself under the name `H2Sharp`
when its assembly is loaded, so a connection configured under that name is found:

```csharp
var db = new DataConnection("H2Sharp");
```

### The connection string

The engine's ADO layer takes a JDBC URL. The .NET world spells the same thing
`Data Source=mem:mydb`, and both are accepted:

| What is written | What the engine opens |
|---|---|
| `jdbc:h2:mem:mydb` | `jdbc:h2:mem:mydb` |
| `Data Source=mem:mydb` | `jdbc:h2:mem:mydb` |
| `Data Source=mem:mydb;MODE=PostgreSQL` | `jdbc:h2:mem:mydb;mode=PostgreSQL` |

The settings a URL carries are the connection string's other keys; the name of a setting is
written the way the engine writes it. The translation is this provider's, and it is one call if
you want to see it:

```csharp
var url = H2SharpConnectionString.ToUrl("Data Source=mem:mydb;MODE=PostgreSQL");
// jdbc:h2:mem:mydb;mode=PostgreSQL
```

## The data context

`DataConnection` is the whole API; `DataContext` is the same thing with a type that knows its
tables. Both work with the async methods, and both take part in a transaction:

```csharp
await using var tx = await db.BeginTransactionAsync();
try
{
    await db.InsertAsync(new Blog { Id = 2, Name = "Bar", Created = DateTime.UtcNow });
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

### Seeing the SQL

The last statement the provider sent is on the connection:

```csharp
db.GetTable<Blog>().Where(b => b.Name == "Foo").ToList();

Console.WriteLine(db.LastQuery);
```

The text is put there when the statement is sent, so a statement the engine refuses *while it
runs* is there too. A statement the engine refuses to *read* — one it cannot parse — never
reaches the database and is not on the connection.

## Mapping

The attributes are linq2db's own: `[Table]`, `[Column]`, and the rest.

```csharp
[Table("Blogs")]
public class Blog
{
    [Column("Id", IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column("Name", Length = 100)]
    public string Name { get; set; } = "";

    [Column("Created")]
    public DateTime Created { get; set; }
}
```

The types are H2's, not the ones of some other database:

| C# | H2 |
|---|---|
| `bool` | `boolean` |
| `byte`, `sbyte`, `short` | `smallint` |
| `int` | `integer` |
| `long` | `bigint` |
| `float` | `real` |
| `double` | `double` |
| `decimal` | `numeric` |
| `string` | `varchar` |
| `char` | `char` |
| `string` (long) | `clob` |
| `byte[]` | `varbinary` |
| `Guid` | `uuid` |
| `DateTime` | `timestamp` |
| `DateTimeOffset` | `timestamp with time zone` |
| `TimeSpan` | `time` |

An identity column and a sequence are the two things the engine cannot do yet; see
[What does not work yet](#what-does-not-work-yet).

## Scaffolding

Scaffolding reads the catalogue, and linq2db has three ways to do it. All of them ask the
provider's schema provider, and the schema provider reads `INFORMATION_SCHEMA` the way H2 2.5
keeps it.

**The command line.** linq2db ships a tool called `dbtools` with a `scaffold` command:

```bash
dbtools scaffold --config h2sharp.json --provider H2Sharp --output BlogContext.cs
```

**The API.** `LinqToDB.Scaffold` builds the model and the code:

```csharp
using LinqToDB.Scaffold;

var scaffolder = new Scaffolder(
    new CSharpLanguageProvider(),
    new DefaultNameConversionProvider(),
    ScaffoldOptions.Default());

var model = scaffolder.LoadDataModel(new DataOptions()
    .UseConnectionString(H2SharpTools.GetDataProvider(), "jdbc:h2:mem:mydb"));

var files = scaffolder.GenerateCodeModel(model);
```

**A template.** The templates of linq2db belong to the providers that ship with it, so a
provider outside it needs a template of its own.

Whichever way is used, the catalogue has to be there. It is in the port now, so scaffolding
reads it — with the tables, their columns, the primary keys and the references ✓.

## The functions of H2

H2 has functions of its own, and the provider offers them under
`H2SharpSpecificExtensions`. They are called in the query like any other method:

```csharp
using LinqToDB.DataProvider.H2Sharp;

var names = await db.GetTable<Blog>()
    .Where(b => H2SharpSpecificExtensions.ILike(b.Name, "f%"))
    .Select(b => H2SharpSpecificExtensions.Upper(b.Name))
    .ToListAsync();
```

| Group | Functions |
|---|---|
| Date and time | `DateAdd`, `DateDiff`, `DateTrunc`, `Year`, `Month`, `Day`, `DayOfMonth`, `DayOfWeek`, `DayOfYear`, `Hour`, `Minute`, `Second`, `Quarter`, `Week`, `DayName`, `MonthName` |
| Strings | `ILike`, `Locate`, `LeftPad`, `RightPad`, `Translate`, `Repeat`, `Space`, `QuoteIdentifier`, `Ascii`, `Char`, `Chr`, `StringDecode`, `StringEncode`, `StringToUtf8`, `Utf8ToString`, `HexToRaw`, `RawToHex`, `Insert`, `ConcatWithSeparator` |
| Numbers | `Sign`, `Truncate`, `Cotangent`, `RoundMagic` |
| Bits | `BitAnd`, `BitOr`, `BitXor`, `BitNot`, `BitNand`, `BitNor`, `BitXnor`, `BitGet`, `BitCount`, `LeftShift`, `RightShift`, `UnsignedLeftShift`, `UnsignedRightShift`, `RotateLeft`, `RotateRight` |
| Compressed text | `Compress`, `Expand` |
| Sounds | `Soundex`, `SoundexDifference` |

Three of them are worth a word.

**`ILike` is an operator in H2, not a function.** H2 2.5 has `ILIKE` as an operator, so the
text it stands on has to be a column of the table: a parameter is not a value an operator can
stand on.

**The unit of a date function is written out.** H2 wants the unit of `DATEADD` as a word in
the statement and not as a parameter, so the provider writes it in:

```csharp
H2SharpSpecificExtensions.DateAdd("DAY", 1, blog.Created)
// DATEADD('DAY', @p0, "Created")
```

**`LOCATE` takes the substring first.** H2 2.5 has the arguments the other way round from the
function of the SQL standard: `Locate(substring, value)` is `LOCATE(substring, value)`.

The calls belong to the database and nowhere else. A call whose arguments are known is still
sent to the database, because there the function may be a different one, or may not be there
at all.

## Window functions

H2 2.5 has the window functions: `ROW_NUMBER`, `RANK`, `DENSE_RANK`, `LAG`, `LEAD`,
`FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE`, `NTILE`, `PERCENT_RANK` and `CUME_DIST`. In linq2db
they are a chain:

```csharp
using LinqToDB;

var ranked = await db.GetTable<Blog>()
    .OrderBy(b => b.Id)
    .Select(b => new
    {
        b.Id,
        Number = Sql.Ext.RowNumber().Over().OrderBy(b.Id).ToValue(),
    })
    .ToListAsync();
```

and with a partition and a running neighbour:

```csharp
var withPrevious = await db.GetTable<Blog>()
    .OrderBy(b => b.Id)
    .Select(b => new
    {
        b.Id,
        Previous = Sql.Ext.Lag(b.Name).Over().PartitionBy(b.Name).OrderBy(b.Id).ToValue(),
    })
    .ToListAsync();
```

The window clause is linq2db's; the provider's part is the dialect around it — the identifiers
in quotes, the parameters named. The port does not read `OVER` yet, so these queries are among
the ones that wait for the engine to grow.

## The catalogue

The provider reads the catalogue the way H2 2.5 keeps it: the objects in
`INFORMATION_SCHEMA.TABLES`, the columns in `INFORMATION_SCHEMA.COLUMNS`, the keys in
`INFORMATION_SCHEMA.TABLE_CONSTRAINTS` and `INFORMATION_SCHEMA.KEY_COLUMN_USAGE`, the
references in `INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS`, the routines in
`INFORMATION_SCHEMA.ROUTINES` and their parameters in `INFORMATION_SCHEMA.PARAMETERS`.

```csharp
var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

var table = schema.Tables.First(t => t.TableName == "Blogs");
var id = table.Columns.First(c => c.ColumnName == "Id");

Console.WriteLine(id.DataType);        // Int32
Console.WriteLine(id.IsPrimaryKey);    // True
```

The options of `GetSchemaOptions` work as they do everywhere else in linq2db: the schemas to
include or to leave out, the default schema, whether the tables and the references are wanted
at all.

The catalogue's own tables are left out unless that schema is asked for by name.

The keys are read with three questions and matched in the provider — the constraints, the columns
of every constraint, and the constraint every reference points at. The queries that join the
tables of the catalogue are SQL of H2 2.5 as well, but they ask the engine for more than the
separate questions do, and the engine has no `JOIN` yet.

Two things in the catalogue of the port are not what H2 2.5 has: the name of the catalog is the
path of the database in the URL, where H2 2.5 gives the name of the database, and the collation
of a column is empty, where H2 2.5 gives the collation of the database. The port's own test of
the catalogue compares every column of it with the reference and names both.

## The SQL it generates

| | |
|---|---|
| Identifiers | always quoted, the way H2 documents it: an unquoted identifier folds to upper case and a quoted one does not |
| Types | H2's own: `varchar`, `char`, `numeric`, `varbinary`, `uuid`, `json`, `clob`, `blob`, `timestamp`, `time`, `boolean`, `smallint`, `integer`, `bigint`, `real`, `double` |
| Paging | `LIMIT n` / `OFFSET n` |
| Concatenation | `\|\|`, which H2 has |
| Parameters | named `@name`, which the engine rewrites into `?` before the statement is parsed |
| Table alias | without the `AS`: `FROM "Blogs" "b"` — the grammar allows both spellings ("[ [ AS ] newTableAlias ]"), and this is the one the engine reads |

The capabilities the provider declares are H2 2.5's, not the engine's current ones: a provider
that declared the engine's abilities would be describing a database that does not exist.

Two things to know when a query does not do what it should:

* `First()` and `Take()` put a number into `LIMIT`, and the engine wants that number written
  out. Until it does, read the rows and take the first one in the program.
* The engine has no `SCOPE_IDENTITY()`, `IDENTITY()` or `LAST_INSERT_ID()` — H2 2.5 has
  neither. The value of a generated key comes back through the ADO channel for generated keys.

## What does not work yet

The engine is a port in work, and the provider is written for H2 2.5 rather than for the port
as it stands. The things that wait for the engine:

   | What | What happens |
   |---|---|
   | `CREATE SEQUENCE` | `CREATE [*]SEQUENCE ...` — sequences |
| identity columns | `... integer [*]GENERATED BY DEFAULT AS IDENTITY ...` |
| `CREATE ALIAS` | `CREATE [*]ALIAS ...` — functions of the database |
| `COMMENT ON` | `COMMENT ON TABLE ...` |
| foreign keys in DDL | `CONSTRAINT ... FOREIGN KEY ...` |
| `OVER` | window functions |
| `LIMIT` with a number in a parameter | `LIMIT/OFFSET takes an integer literal here` |
| large objects | `ValueLob.copyToResult: lob storage is not ported` — the storage of a large object is in the port (`ValueLob`, `LobData`, MVStore); the copy from the storage into the result is not, so a `clob` column cannot be read back |
| `TIME` out of a time span | `Data conversion error converting "INTERVAL DAY TO SECOND to TIME"` |
| two parameters in one expression | `SELECT ? [*]+ ?` |
| `DB_CLOSE_DELAY` and a file database | `Not ported yet: Engine.openSession: persistent databases need the file store` — the store itself is in the port and its format is compatible with H2 2.5, but a session does not open a file database; and a pooled connection takes the database with it |

The full list, with what each of them costs, is in
[docs/DIVERGENCES.md](../docs/DIVERGENCES.md).

## Building and testing

The engine is a sibling checkout; the path is overridden with the `H2SharpProject` property.

```bash
dotnet build src/H2Sharp.Linq2Db/H2Sharp.Linq2Db.csproj -c Release
dotnet test test/H2Sharp.Linq2Db.Tests/H2Sharp.Linq2Db.Tests.csproj -c Release
```

The tests come in two runs. The first is what the provider can do; the second is what the
engine cannot do yet, and every test in it is a defect of the engine:

```bash
# green: everything that can pass does pass
dotnet test test/H2Sharp.Linq2Db.Tests/H2Sharp.Linq2Db.Tests.csproj -c Release --filter "Category!=BlockedByPort"

# the port's defects, listed
dotnet test test/H2Sharp.Linq2Db.Tests/H2Sharp.Linq2Db.Tests.csproj -c Release --filter "Category=BlockedByPort"
```
