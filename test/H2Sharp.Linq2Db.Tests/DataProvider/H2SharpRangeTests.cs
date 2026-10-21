/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
/// The range table of H2: <c>SYSTEM_RANGE(min, max[, step])</c>, a table function that gives a
/// row per number of the range.
/// </summary>
/// <remarks>
/// The engine reads it, and the provider would ask for it the way linq2db asks for every raw
/// SQL: the statement becomes a derived table with an alias, and the query is built over it.
/// The engine does not read a derived table yet, so the row never comes back.
/// </remarks>
public class H2SharpRangeTests
{
    [Table("RangeRows")]
    public class RangeRow
    {
        [Column("X")]
        public long X { get; set; }
    }

    [Fact]
    public void The_range_table_gives_a_row_per_number()
    {
        using var db = H2SharpDatabase.Open(nameof(The_range_table_gives_a_row_per_number));

        List<RangeRow> rows = db.FromSql<RangeRow>($"SELECT * FROM SYSTEM_RANGE(1, 10)").ToList();

        Assert.Equal(10, rows.Count);
        Assert.Equal(1, rows[0].X);
        Assert.Equal(10, rows[9].X);
    }
}
