/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System;
using System.Collections.Generic;
using System.Data.Common;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.H2Sharp;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
/// The range table of H2: <c>SYSTEM_RANGE(min, max[, step])</c>, a table function that gives a
/// row per number of the range.
/// </summary>
/// <remarks>
/// The rows a query needs per row of a table are cut by a condition, and not by the bound of
/// the function: H2 2.5 has no <c>LATERAL</c> and no <c>CROSS APPLY</c>, and the argument of a
/// table function cannot depend on the row of an outer query.
/// </remarks>
public class H2SharpTableFunctionTests
{
    [Table("Rows")]
    public class Row
    {
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Records the statements the provider sends, so that a test can say what the provider
    /// wrote without waiting for the engine to run it.
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
    public void The_range_table_is_written_the_way_H2_writes_it()
    {
        using var db = H2SharpDatabase.Open(nameof(The_range_table_is_written_the_way_H2_writes_it));

        SqlRecorder recorder = new();
        db.AddInterceptor(recorder);

        var query =
            from row in db.GetTable<Row>()
            from range in db.SystemRange(1, 3)
            where range.X <= row.Id
            select new { row.Name, range.X };

        // The engine does not read a join yet, so the statement cannot run; the statement is
        // what this test is about, and the recorder has it whether the engine ran it or not.
        Assert.ThrowsAny<Exception>(() => query.ToList());

        Assert.Equal(
            """
            SELECT
            	"row_1"."Name",
            	"range_1"."X"
            FROM
            	"Rows" "row_1",
            	"SYSTEM_RANGE"(1, 3) "range_1"
            WHERE
            	"range_1"."X" <= CAST("row_1"."Id" AS bigint)

            """.ReplaceLineEndings("\n"),
            recorder.Text!.ReplaceLineEndings("\n"));
    }

    [Trait("Category", "BlockedByPort")]
    // The engine does not read a join yet -- not a comma join, not CROSS JOIN, not JOIN ... ON
    // -- so the rows of the range table never come back. The failure is the port's, not the
    // provider's: see docs/DIVERGENCES.md.
    [Fact]
    public void The_rows_of_the_range_table_come_back()
    {
        using var db = H2SharpDatabase.Open(nameof(The_rows_of_the_range_table_come_back));

        db.CreateTable<Row>();

        List<string> rows =
            (from row in db.GetTable<Row>()
             from range in db.SystemRange(1, 3)
             where range.X <= row.Id
             select row.Name + ":" + range.X).ToList();

        Assert.Equal(6, rows.Count);
    }

    [Fact]
    public void A_bound_that_depends_on_the_outer_row_is_refused()
    {
        using var db = H2SharpDatabase.Open(nameof(A_bound_that_depends_on_the_outer_row_is_refused));

        var query =
            from row in db.GetTable<Row>()
            from range in db.SystemRange(1, row.Id)
            select range.X;

        NotSupportedException e = Assert.Throws<NotSupportedException>(() => query.ToList());

        Assert.Contains("LATERAL", e.Message, StringComparison.Ordinal);
    }
}
