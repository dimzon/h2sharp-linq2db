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

namespace H2Sharp.Linq2Db.Tests;

/// <summary>
///     The provider against the engine: a query that goes through linq2db and comes back.
/// </summary>
public class H2SharpProviderTests
{
    [Table("Rows")]
    public class Row
    {
        [Column("Id", IsPrimaryKey = true, IsIdentity = false)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = "";

        [Column("Price")]
        public decimal Price { get; set; }
    }

    private static DataConnection Open(string name)
        => H2SharpTools.CreateDataConnection($"jdbc:h2:mem:{name}");

    [Fact]
    public async Task A_query_goes_through_the_provider_and_the_engine()
    {
        await using var db = Open(nameof(A_query_goes_through_the_provider_and_the_engine));

        await db.ExecuteAsync(
            """
            CREATE TABLE "Rows" (
                "Id" integer NOT NULL,
                "Name" varchar(50) NOT NULL,
                "Price" numeric(10,2) NOT NULL,
                CONSTRAINT "PK_Rows" PRIMARY KEY ("Id")
            )
            """);

        await db.InsertAsync(new Row { Id = 1, Name = "abc", Price = 5m });
        await db.InsertAsync(new Row { Id = 2, Name = "def", Price = 15m });

        var rows = await db.GetTable<Row>().OrderBy(r => r.Id).ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal("abc", rows[0].Name);
        Assert.Equal(15m, rows[1].Price);
    }

    [Fact]
    public async Task A_filter_becomes_a_parameter()
    {
        await using var db = Open(nameof(A_filter_becomes_a_parameter));

        await db.ExecuteAsync(
            """
            CREATE TABLE "Rows" (
                "Id" integer NOT NULL,
                "Name" varchar(50) NOT NULL,
                "Price" numeric(10,2) NOT NULL,
                CONSTRAINT "PK_Rows" PRIMARY KEY ("Id")
            )
            """);

        await db.InsertAsync(new Row { Id = 1, Name = "abc", Price = 5m });
        await db.InsertAsync(new Row { Id = 2, Name = "def", Price = 15m });

        var rows = await db.GetTable<Row>().Where(r => r.Price > 10m).ToListAsync();

        Assert.Single(rows);
        Assert.Equal("def", rows[0].Name);
    }

    [Fact]
    public async Task The_generated_SQL_is_H2s()
    {
        await using var db = Open(nameof(The_generated_SQL_is_H2s));

        await db.ExecuteAsync(
            """
            CREATE TABLE "Rows" (
                "Id" integer NOT NULL,
                "Name" varchar(50) NOT NULL,
                "Price" numeric(10,2) NOT NULL,
                CONSTRAINT "PK_Rows" PRIMARY KEY ("Id")
            )
            """);

        await db.InsertAsync(new Row { Id = 1, Name = "abc", Price = 5m });

        _ = await db.GetTable<Row>().Where(r => r.Price > 10m).ToListAsync();

        var sql = db.LastQuery ?? "";

        // Every identifier is quoted: H2 folds an unquoted one to upper case and leaves a quoted
        // one alone, so the two spellings of a name are two different names.
        Assert.Contains("\"Rows\"", sql);
        Assert.Contains("\"Price\"", sql);

        // The alias is written without the AS. The grammar allows both ("[ [ AS ] newTableAlias ]"),
        // and this is the form the engine reads.
        Assert.Contains("\"Rows\" \"r\"", sql);
        Assert.DoesNotContain(" AS ", sql);
    }
}
