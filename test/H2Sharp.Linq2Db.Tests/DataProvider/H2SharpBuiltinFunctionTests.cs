/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using LinqToDB.DataProvider.H2Sharp;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
///     The built-in functions of H2, called through the provider.
/// </summary>
/// <remarks>
///     <para>
///         These are the functions the EF Core provider of H2Sharp offers, the same names and the
///         same meaning, so that a query reads the same in both providers.
///     </para>
/// </remarks>
public class H2SharpBuiltinFunctionTests
{
    [Table("FunctionRows")]
    public class FunctionRow
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = "";

        [Column("Created")]
        public DateTime Created { get; set; }
    }

    private static DataConnection OpenWithTable(string name)
    {
        var db = H2SharpDatabase.Open(name);

        db.Execute("""
            CREATE TABLE "FunctionRows" (
                "Id" integer NOT NULL,
                "Name" varchar(50) NOT NULL,
                "Created" timestamp NOT NULL,
                CONSTRAINT "PK_FunctionRows" PRIMARY KEY ("Id")
            )
            """);

        db.Insert(new FunctionRow { Id = 1, Name = "abc", Created = new DateTime(2020, 3, 5, 10, 20, 30) });
        db.Insert(new FunctionRow { Id = 2, Name = "ABC", Created = new DateTime(2021, 7, 8, 1, 2, 3) });

        return db;
    }

    [Fact]
    public void A_function_becomes_its_name_in_the_query()
    {
        using var db = OpenWithTable(nameof(A_function_becomes_its_name_in_the_query));

        db.GetTable<FunctionRow>()
            .Select(r => new
            {
                Pad   = H2SharpSpecificExtensions.LeftPad(r.Name, 10, "*"),
                Space = H2SharpSpecificExtensions.Space(3),
            })
            .ToList();

        var sql = db.LastQuery!;

        Assert.Contains("LPAD(", sql);
        Assert.Contains("SPACE(", sql);
    }

    [Fact]
    public void The_unit_of_a_date_function_is_written_out()
    {
        using var db = OpenWithTable(nameof(The_unit_of_a_date_function_is_written_out));

        db.GetTable<FunctionRow>()
            .Select(r => H2SharpSpecificExtensions.DateAdd("DAY", 1, r.Created))
            .ToList();

        // H2 2.5 wants the unit of a date function as a word in the statement, not as a parameter.
        Assert.Contains("DATEADD('DAY'", db.LastQuery!);
    }

    [Fact]
    public void A_function_is_called_in_the_database()
    {
        using var db = OpenWithTable(nameof(A_function_is_called_in_the_database));

        var rows = db.GetTable<FunctionRow>()
            .Select(r => new
            {
                r.Name,
                Padded = H2SharpSpecificExtensions.LeftPad(r.Name, 6, "*"),
                Space = H2SharpSpecificExtensions.Space(3),
                Code  = H2SharpSpecificExtensions.Ascii(r.Name),
                Char  = H2SharpSpecificExtensions.Char(65),
                Found = H2SharpSpecificExtensions.Locate("b", r.Name),
            })
            .ToList();

        var row = Assert.Single(rows, r => r.Name == "abc");

        // H2 2.5 pads on the left: LPAD('abc', 6, '*') is '***abc'.
        Assert.Equal("***abc", row.Padded);
        Assert.Equal("   ", row.Space);
        Assert.Equal(97, row.Code);
        Assert.Equal("A", row.Char);
        Assert.Equal(2, row.Found);
    }

    [Fact]
    public void The_parts_of_a_date_are_taken_by_their_own_functions()
    {
        using var db = OpenWithTable(nameof(The_parts_of_a_date_are_taken_by_their_own_functions));

        // H2 2.5 has a function for every part of a date: YEAR, MONTH, DAY and the rest.
        var rows = db.GetTable<FunctionRow>()
            .Select(r => new
            {
                Year   = H2SharpSpecificExtensions.Year(r.Created),
                Month  = H2SharpSpecificExtensions.Month(r.Created),
                Day    = H2SharpSpecificExtensions.Day(r.Created),
                Hour   = H2SharpSpecificExtensions.Hour(r.Created),
                Minute = H2SharpSpecificExtensions.Minute(r.Created),
                Second = H2SharpSpecificExtensions.Second(r.Created),
            })
            .ToList();

        Assert.Equal(2020, rows[0].Year);
        Assert.Equal(3, rows[0].Month);
        Assert.Equal(5, rows[0].Day);
        Assert.Equal(10, rows[0].Hour);
        Assert.Equal(20, rows[0].Minute);
        Assert.Equal(30, rows[0].Second);
    }

    [Fact]
    public void The_texts_are_put_together_with_a_separator()
    {
        using var db = OpenWithTable(nameof(The_texts_are_put_together_with_a_separator));

        var rows = db.GetTable<FunctionRow>()
            .Select(r => H2SharpSpecificExtensions.ConcatWithSeparator(", ", r.Name, "b", "c"))
            .ToList();

        Assert.Equal("abc, b, c", rows[0]);
    }

    [Fact]
    public void A_text_matches_a_pattern_ignoring_the_case()
    {
        using var db = OpenWithTable(nameof(A_text_matches_a_pattern_ignoring_the_case));

        var names = db.GetTable<FunctionRow>()
            .Where(r => H2SharpSpecificExtensions.ILike(r.Name, "A%"))
            .OrderBy(r => r.Id)
            .Select(r => r.Name)
            .ToList();

        Assert.Equal(new[] { "abc", "ABC" }, names);
    }
}
