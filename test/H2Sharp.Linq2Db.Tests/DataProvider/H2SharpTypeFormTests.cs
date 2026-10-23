/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System;
using System.Data.Common;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.H2Sharp;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
/// The types of H2 in the DDL the provider writes.
/// </summary>
/// <remarks>
/// The names are H2's own: the type of a point of time with an offset is TIMESTAMP WITH TIME
/// ZONE, and an array is the type of its elements and the word ARRAY after it. The forms of
/// PostgreSQL -- a DateTimeOffset column as a varchar, an array as "integer[]" -- H2 does not
/// know.
/// </remarks>
public class H2SharpTypeFormTests
{
    [Table("TypedRows")]
    public class TypedRow
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Moment")]
        public DateTimeOffset Moment { get; set; }
    }

    [Table("ArrayRows")]
    public class ArrayRow
    {
        [Column("Id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("Numbers")]
        public int[] Numbers { get; set; } = [];
    }

    /// <summary>
    ///     Records the statements the provider sends, so that a test can say what the
    ///     provider wrote without waiting for the engine to run it.
    /// </summary>
    private sealed class SqlRecorder : CommandInterceptor
    {
        public string? Text { get; private set; }

        public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
        {
            Text = command.CommandText;
            return command;
        }
    }

    [Fact]
    public void A_point_of_time_with_an_offset_is_a_column_of_H2s_type()
    {
        using var db = H2SharpDatabase.Open(nameof(A_point_of_time_with_an_offset_is_a_column_of_H2s_type));

        SqlRecorder recorder = new();
        db.AddInterceptor(recorder);

        db.CreateTable<TypedRow>();

        // A DateTimeOffset is a point of time with an offset, and H2 keeps that in the type
        // TIMESTAMP WITH TIME ZONE. Without this the column was created as a varchar.
        // The names of the columns are aligned in the DDL, so the type is looked for on its
        // own.
        Assert.Contains("timestamp with time zone", recorder.Text!, StringComparison.Ordinal);
        Assert.DoesNotContain("varchar", recorder.Text!, StringComparison.Ordinal);
    }

    [Fact]
    public void An_array_is_a_column_of_H2s_type()
    {
        using var db = H2SharpDatabase.Open(nameof(An_array_is_a_column_of_H2s_type));

        SqlRecorder recorder = new();
        db.AddInterceptor(recorder);

        // An array has no name of its own in the mapping schema -- the name of an array is
        // the name of its elements and the word ARRAY after it -- so the type of the column
        // is said here, the way the mapping schema of the provider says the type of every
        // other column.
        db.MappingSchema.SetDataType(typeof(int[]), DataType.Int32 | DataType.Array);

        // The engine reads the type ARRAY and creates the table; the recorder has the
        // statement it ran.
        db.CreateTable<ArrayRow>();

        // H2 writes the type of the elements and the word ARRAY after it. The brackets of
        // PostgreSQL (integer[]) the reference engine does not take.
        Assert.Contains("\"Numbers\" integer ARRAY", recorder.Text!, StringComparison.Ordinal);
        Assert.DoesNotContain("integer[]", recorder.Text!, StringComparison.Ordinal);
    }

    [Fact]
    public void The_functions_of_an_array_are_the_functions_of_H2()
    {
        using var db = H2SharpDatabase.Open(nameof(The_functions_of_an_array_are_the_functions_of_H2));

        SqlRecorder recorder = new();
        db.AddInterceptor(recorder);

        // The functions H2 2.5 has for an array: ARRAY_APPEND, ARRAY_CAT, ARRAY_CONTAINS,
        // CARDINALITY and ARRAY_LENGTH. The reference answers [10, 20, 30, 40] for
        // ARRAY_APPEND(ARRAY[10,20,30], 40) and TRUE for ARRAY_CONTAINS(ARRAY[1,NULL], NULL).
        var query =
            from row in db.GetTable<ArrayRow>()
            select new
            {
                Appended = H2SharpSpecificExtensions.ArrayAppend(new[] { 10, 20, 30 }, 40),
                Joined = H2SharpSpecificExtensions.ArrayCat(new[] { 10 }, new[] { 20 }),
                Holds = H2SharpSpecificExtensions.ArrayContains(new[] { 10, 20 }, 20),
                Size = H2SharpSpecificExtensions.Cardinality(new[] { 10, 20 }),
                Length = H2SharpSpecificExtensions.ArrayLength(new[] { 10, 20 }),
            };

        _ = Record.Exception(() => query.ToList());

        var text = recorder.Text ?? "";

        Assert.Contains("ARRAY_APPEND", text, StringComparison.Ordinal);
        Assert.Contains("ARRAY_CAT", text, StringComparison.Ordinal);
        Assert.Contains("ARRAY_CONTAINS", text, StringComparison.Ordinal);
        Assert.Contains("CARDINALITY", text, StringComparison.Ordinal);
        Assert.Contains("ARRAY_LENGTH", text, StringComparison.Ordinal);
    }
}
