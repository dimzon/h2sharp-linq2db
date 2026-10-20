/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The table functions of H2, as they are called in a query.
/// </summary>
/// <remarks>
///     <para>
///         A table function of the database is not a function of the program: it is called by the
///         database, and the rows come from there. linq2db marks such a call with
///         <see cref="Sql.TableFunctionAttribute" />, and the arguments of the call become the
///         arguments of the function in the statement — parameters of the statement, not literals.
///     </para>
///     <para>
///         The range table of H2 gives a row per number of a range:
///         <c>SYSTEM_RANGE(min, max[, step])</c>. The step is added to the start and the range
///         stops at the end, so <c>SYSTEM_RANGE(1, 10, 2)</c> gives 1, 3, 5, 7, 9.
///     </para>
///     <para>
///         H2 2.5 has no <c>LATERAL</c> and no <c>CROSS APPLY</c>: the argument of a table
///         function cannot depend on the row of an outer query. The rows a query needs per row of
///         a table are cut by a condition, not by the bound of the function.
///     </para>
/// </remarks>
public static class H2SharpTableFunctions
{
    /// <summary>
    ///     The range table of H2: <c>SYSTEM_RANGE(min, max)</c>.
    /// </summary>
    /// <param name="db">The data context.</param>
    /// <param name="min">The first number of the range.</param>
    /// <param name="max">The number the range stops at.</param>
    /// <returns>The rows of the range.</returns>
    [Sql.TableFunction("SYSTEM_RANGE", argIndices: new[] { 1, 2 })]
    public static ITable<H2SharpRangeRow> SystemRange(this IDataContext db, int min, int max)
        => throw new ServerSideOnlyException(nameof(SystemRange));

    /// <summary>
    ///     The range table of H2 with a step: <c>SYSTEM_RANGE(min, max, step)</c>.
    /// </summary>
    /// <param name="db">The data context.</param>
    /// <param name="min">The first number of the range.</param>
    /// <param name="max">The number the range stops at.</param>
    /// <param name="step">The step of the range.</param>
    /// <returns>The rows of the range.</returns>
    [Sql.TableFunction("SYSTEM_RANGE", argIndices: new[] { 1, 2, 3 })]
    public static ITable<H2SharpRangeRow> SystemRange(this IDataContext db, int min, int max, int step)
        => throw new ServerSideOnlyException(nameof(SystemRange));
}
