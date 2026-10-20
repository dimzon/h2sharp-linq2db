/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     One row of H2's range table.
/// </summary>
/// <remarks>
///     <para>
///         The range table is a table function of the database: <c>SYSTEM_RANGE(min, max[, step])</c>
///         gives a row per number of the range, and the number is the only column it has. The
///         column is called <c>X</c> and it is a <c>BIGINT</c> — the same in H2 2.5 and in the
///         port.
///     </para>
/// </remarks>
public sealed class H2SharpRangeRow
{
    /// <summary>
    ///     The number of the row.
    /// </summary>
    public long X { get; set; }
}
