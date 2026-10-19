/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Data;

using LinqToDB.DataProvider.H2Sharp;

namespace H2Sharp.Linq2Db.Tests;

/// <summary>
///     A database to work in.
/// </summary>
/// <remarks>
///     <para>
///         The engine keeps an in-memory database for as long as something is connected to it, so
///         a test that opens a database under a name of its own and closes it again has a
///         database of its own, empty, every time.
///     </para>
/// </remarks>
internal static class H2SharpDatabase
{
    /// <summary>
    ///     Opens a database that belongs to the test alone.
    /// </summary>
    public static DataConnection Open(string name)
        => H2SharpTools.CreateDataConnection($"jdbc:h2:mem:{name}");
}
