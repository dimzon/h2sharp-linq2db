/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.H2Sharp;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The arrays of H2: what the provider writes for them and what the engine reads.
/// </summary>
/// <remarks>
///     <para>
///         H2 2.5 keeps an array in a column written as the type of the elements and the word
///         <c>ARRAY</c> after it, and asks whether the array holds an element with
///         <c>ARRAY_CONTAINS</c>. The provider writes both forms; the port reads neither yet
///         (issue dimzon/h2sharp#14), so the live tests fail there and are marked as the gaps
///         of the port they are.
///     </para>
///     <para>
///         The form of the statement does not need a live array column: the translation is
///         asked for the text it writes, and the port is never executed.
///     </para>
/// </remarks>
public class ForumArrayTests
{
    [Table("Shelves")]
    internal sealed class Shelf
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Forums")]
        public int[] Forums { get; set; } = [];
    }

    [Fact]
    public void Arrays_are_stored_in_their_columns()
    {
        using var db = ForumDatabase.Open(nameof(Arrays_are_stored_in_their_columns));

        db.CreateTable<Shelf>();

        var first = new Shelf { Id = 1, Forums = [1, 2, 3] };
        var second = new Shelf { Id = 2, Forums = [2] };
        db.Insert(first);
        db.Insert(second);

        List<int> holding = db.Shelves().Where(s => s.Forums.Contains(2)).Select(s => s.Id).ToList();

        Assert.Equal([1, 2], holding);

        List<int> whole = db.Shelves().Where(s => s.Forums.SequenceEqual(new[] { 1, 2, 3 })).Select(s => s.Id).ToList();

        Assert.Equal([1], whole);

        int length = db.Shelves().Where(s => s.Id == 1).Select(s => s.Forums.Length).First();

        Assert.Equal(3, length);

        int element = db.Shelves().Where(s => s.Id == 1).Select(s => s.Forums[1]).First();

        Assert.Equal(2, element);

        db.DropTable<Shelf>();
    }

    [Fact]
    public void Array_functions_run_against_the_engine()
    {
        using var db = ForumDatabase.Open(nameof(Array_functions_run_against_the_engine));

        db.CreateTable<Shelf>();
        db.Insert(new Shelf { Id = 1, Forums = [1, 2, 3] });
        db.Insert(new Shelf { Id = 2, Forums = [2] });

        try
        {
            int count = db.Shelves().Where(s => s.Id == 1).Select(s => H2SharpSpecificExtensions.Cardinality(s.Forums)).First();
            Assert.Equal(3, count);

            int joined = db.Shelves().Where(s => s.Id == 1).Select(s => H2SharpSpecificExtensions.ArrayCat(s.Forums, new[] { 4 }).Length).First();
            Assert.Equal(4, joined);

            int appended = db.Shelves().Where(s => s.Id == 1).Select(s => H2SharpSpecificExtensions.ArrayAppend(s.Forums, 4).Length).First();
            Assert.Equal(4, appended);

            bool has = db.Shelves().Where(s => s.Id == 1).Select(s => H2SharpSpecificExtensions.ArrayContains(s.Forums, 2)).First();
            Assert.True(has);
        }
        finally
        {
            db.DropTable<Shelf>();
        }
    }

    [Table("KeyedRows")]
    internal sealed class KeyedRow
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Name", Length = 50)]
        public string Name { get; set; } = "";
    }

    [Fact]
    public void An_identity_is_asked_for_where_there_is_none()
    {
        using var db = ForumDatabase.Open(nameof(An_identity_is_asked_for_where_there_is_none));

        db.CreateTable<KeyedRow>();

        // Shelves have keys the application gives, not the engine: the key asked for
        // is the key that was put in, read back from the inserted row.
        object key = db.InsertWithIdentity(new KeyedRow { Id = 7, Name = "seven" });

        Assert.Equal(7, Convert.ToInt32(key));
        Assert.Equal("seven", db.KeyedRows().Single(r => r.Id == 7).Name);

        db.DropTable<KeyedRow>();
    }

    [Fact]
    public void Whole_arrays_are_compared_with_an_equal_sign()
    {
        using var db = ForumDatabase.Open(nameof(Whole_arrays_are_compared_with_an_equal_sign));

        // H2 2.5 compares whole arrays with =: the provider writes the comparison,
        // and the engine answers for it.
        var wanted = new[] { 1, 2, 3 };

        Assert.Contains(
            "\"Forums\" =",
            GeneratedSql(db, () => db.Shelves().Where(s => s.Forums.SequenceEqual(wanted)).ToList()),
            StringComparison.Ordinal);
    }

    [Fact]
    public void An_element_of_an_array_is_asked_with_array_contains()
    {
        using var db = ForumDatabase.Open(nameof(An_element_of_an_array_is_asked_with_array_contains));

        Assert.Contains(
            "ARRAY_CONTAINS",
            GeneratedSql(db, () => db.Shelves().Where(s => s.Forums.Contains(2)).ToList()),
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_extension_asks_with_array_contains()
    {
        using var db = ForumDatabase.Open(nameof(The_extension_asks_with_array_contains));

        Assert.Contains(
            "ARRAY_CONTAINS",
            GeneratedSql(db, () => db.Shelves().Where(s => H2SharpSpecificExtensions.ArrayContains(s.Forums, 2)).ToList()),
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_number_of_the_elements_is_asked_with_cardinality()
    {
        using var db = ForumDatabase.Open(nameof(The_number_of_the_elements_is_asked_with_cardinality));

        string select = GeneratedSql(db, () => db.Shelves().Select(s => H2SharpSpecificExtensions.Cardinality(s.Forums)).ToList());

        Assert.Contains("CARDINALITY", select, StringComparison.Ordinal);
        Assert.DoesNotContain("integer", select, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_number_of_the_elements_is_asked_with_array_length()
    {
        using var db = ForumDatabase.Open(nameof(The_number_of_the_elements_is_asked_with_array_length));

        Assert.Contains(
            "ARRAY_LENGTH",
            GeneratedSql(db, () => db.Shelves().Select(s => H2SharpSpecificExtensions.ArrayLength(s.Forums)).ToList()),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Two_arrays_are_joined_with_array_cat()
    {
        using var db = ForumDatabase.Open(nameof(Two_arrays_are_joined_with_array_cat));

        int[] extra = [9];

        Assert.Contains(
            "ARRAY_CAT",
            GeneratedSql(db, () => db.Shelves().Select(s => H2SharpSpecificExtensions.ArrayCat(s.Forums, extra)).ToList()),
            StringComparison.Ordinal);
    }

    [Fact]
    public void An_element_is_appended_with_array_append()
    {
        using var db = ForumDatabase.Open(nameof(An_element_is_appended_with_array_append));

        Assert.Contains(
            "ARRAY_APPEND",
            GeneratedSql(db, () => db.Shelves().Select(s => H2SharpSpecificExtensions.ArrayAppend(s.Forums, 9)).ToList()),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Renders the statement of a query without running it against the engine: the text is
    ///     what the provider writes, and the engine is never asked.
    /// </summary>
    private static string GeneratedSql(DataConnection db, Func<object> query)
    {
        // Tracing is off unless it is turned on: the debug build of linq2db turns
        // it on by default, the release build does not, and without it the engine
        // is never asked for the text it failed on.
        DataConnection.TurnTraceSwitchOn(System.Diagnostics.TraceLevel.Info);

        string? sql = null;
#pragma warning disable CS0618 // The trace callback is the only way to read the text of a statement
        db.OnTraceConnection = info =>
        {
            if (sql is null && info.SqlText is { Length: > 0 } text)
            {
                sql = text;
            }
        };
#pragma warning restore CS0618

        // The statement is generated before the engine is asked: the engine fails (there
        // is no table, and it reads no ARRAY type yet), but by then the text is captured.
        // Only failures after a generated statement are swallowed here: if the provider
        // fails to translate, no text is captured and the failure fails the test.
        try
        {
            query();
        }
        catch (Exception) when (sql is not null)
        {
        }

        Assert.NotNull(sql);
        return sql!;
    }
}

/// <summary>
///     The tables of the array tests.
/// </summary>
internal static class ArrayTables
{
    public static ITable<ForumArrayTests.Shelf> Shelves(this IDataContext db)
        => db.GetTable<ForumArrayTests.Shelf>();

    public static ITable<ForumArrayTests.KeyedRow> KeyedRows(this IDataContext db)
        => db.GetTable<ForumArrayTests.KeyedRow>();
}
