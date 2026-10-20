/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;

using LinqToDB.DataProvider.H2Sharp;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.Documentation;

/// <summary>
///     The examples of docs/USAGE.md, as tests.
/// </summary>
/// <remarks>
///     <para>
///         The guide is only worth reading if it is true, so what it says is written here and
///         checked. A test that stands red because the engine cannot do the thing yet is marked
///         as such: it is the engine's defect, and the guide says so too.
///     </para>
/// </remarks>
public class UsageExamplesTests
{
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

    public class BlogContext : DataContext
    {
        public BlogContext(string connectionString)
            : base(new DataOptions()
                .UseConnectionString(H2SharpTools.GetDataProvider(), connectionString))
        {
        }

        public ITable<Blog> Blogs => this.GetTable<Blog>();
    }

    [Fact]
    public async Task The_quick_start_works()
    {
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

        Assert.Equal("Foo", Assert.Single(foo).Name);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void A_data_context_works_the_same_way()
    {
        // A data context returns its connection to the pool between commands, and an in-memory
        // database is gone when the last connection to it is closed. The port knows neither
        // DB_CLOSE_DELAY nor a file store yet, so there is no database that outlives a pooled
        // connection -- and this is what a data context has.
        using var ctx = new BlogContext("jdbc:h2:mem:datacontext");

        ctx.Execute("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        ctx.Insert(new Blog { Id = 1, Name = "Foo", Created = DateTime.UtcNow });

        Assert.Equal("Foo", ctx.Blogs.Single(b => b.Name.StartsWith("F")).Name);
    }

    [Theory]
    [InlineData("jdbc:h2:mem:mydb", "jdbc:h2:mem:mydb")]
    [InlineData("Data Source=mem:mydb", "jdbc:h2:mem:mydb")]
    [InlineData("Data Source=mem:mydb;MODE=PostgreSQL", "jdbc:h2:mem:mydb;mode=PostgreSQL")]
    public void A_connection_string_becomes_a_url(string connectionString, string url)
    {
        Assert.Equal(url, H2SharpConnectionString.ToUrl(connectionString));
    }

    [Fact]
    public async Task A_transaction_holds_and_rolls_back()
    {
        await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:transaction");

        await db.ExecuteAsync("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        await using (var tx = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(new Blog { Id = 2, Name = "Bar", Created = DateTime.UtcNow });
            await tx.CommitAsync();
        }

        await using (var tx = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(new Blog { Id = 3, Name = "Baz", Created = DateTime.UtcNow });
            await tx.RollbackAsync();
        }

        var names = await db.GetTable<Blog>().OrderBy(b => b.Id).Select(b => b.Name).ToListAsync();

        Assert.Equal(new[] { "Bar" }, names);
    }

    [Fact]
    public void The_last_statement_is_on_the_connection()
    {
        using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:lastquery");

        db.Execute("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        db.GetTable<Blog>().Where(b => b.Name == "Foo").ToList();

        Assert.Contains("\"Blogs\"", db.LastQuery!);
    }

    [Fact]
    public async Task A_function_of_H2_is_called_in_the_database()
    {
        await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:functions");

        await db.ExecuteAsync("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        await db.InsertAsync(new Blog { Id = 1, Name = "abc", Created = new DateTime(2020, 3, 5, 10, 20, 30) });

        var rows = await db.GetTable<Blog>()
            .Select(b => new
            {
                Padded  = H2SharpSpecificExtensions.LeftPad(b.Name, 6, "*"),
                Year    = H2SharpSpecificExtensions.Year(b.Created),
                Day     = H2SharpSpecificExtensions.Day(b.Created),
                Located = H2SharpSpecificExtensions.Locate("b", b.Name),
            })
            .ToListAsync();

        var row = Assert.Single(rows);

        // H2 2.5 pads on the left: LPAD('abc', 6, '*') is '***abc'.
        Assert.Equal("***abc", row.Padded);
        Assert.Equal(2020, row.Year);
        Assert.Equal(5, row.Day);
        Assert.Equal(2, row.Located);
    }

    [Fact]
    public async Task A_text_matches_a_pattern_ignoring_the_case()
    {
        await using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:ilike");

        await db.ExecuteAsync("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        await db.InsertAsync(new Blog { Id = 1, Name = "abc", Created = DateTime.UtcNow });
        await db.InsertAsync(new Blog { Id = 2, Name = "ABC", Created = DateTime.UtcNow });

        var names = await db.GetTable<Blog>()
            .Where(b => H2SharpSpecificExtensions.ILike(b.Name, "A%"))
            .OrderBy(b => b.Id)
            .Select(b => b.Name)
            .ToListAsync();

        Assert.Equal(new[] { "abc", "ABC" }, names);
    }

    [Fact]
    public void The_unit_of_a_date_function_is_written_out()
    {
        using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:dateadd");

        db.Execute("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        db.GetTable<Blog>()
            .Select(b => H2SharpSpecificExtensions.DateAdd("DAY", 1, b.Created))
            .ToList();

        // H2 2.5 wants the unit of a date function as a word in the statement.
        Assert.Contains("DATEADD('DAY'", db.LastQuery!);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void A_window_function_runs_in_the_database()
    {
        using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:windowrun");

        db.Execute("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        db.Insert(new Blog { Id = 1, Name = "Foo", Created = DateTime.UtcNow });

        // The port does not read OVER yet.
        var numbers = db.GetTable<Blog>()
            .OrderBy(b => b.Id)
            .Select(b => new
            {
                b.Id,
                Number = Sql.Ext.RowNumber().Over().OrderBy(b.Id).ToValue(),
            })
            .ToList();

        Assert.Equal(1, Assert.Single(numbers).Number);
    }

    [Fact]
    public void The_catalogue_is_read()
    {
        using var db = H2SharpTools.CreateDataConnection("jdbc:h2:mem:catalogue");

        db.Execute("""
            CREATE TABLE "Blogs" (
                "Id" integer NOT NULL,
                "Name" varchar(100) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_Blogs" PRIMARY KEY ("Id")
            )
            """);

        // The port has no INFORMATION_SCHEMA yet.
        var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

        var table = Assert.Single(schema.Tables, t => t.TableName == "Blogs");
        var id = table.Columns.First(c => c.ColumnName == "Id");

        Assert.Equal(DataType.Int32, id.DataType);
        Assert.True(id.IsPrimaryKey);
    }
}
