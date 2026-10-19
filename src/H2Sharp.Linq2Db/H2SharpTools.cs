/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data.Common;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The H2Sharp provider's tools: the way an application gets hold of the provider.
/// </summary>
public static class H2SharpTools
{
    /// <summary>
    ///     The provider.
    /// </summary>
    /// <param name="version">The H2 version to generate SQL for.</param>
    /// <param name="connectionString">A JDBC URL, or an ADO.NET connection string.</param>
    /// <param name="connection">An open connection to use instead of a connection string.</param>
    /// <param name="transaction">A transaction to run the commands in.</param>
    public static IDataProvider GetDataProvider(
        H2SharpVersion version = H2SharpVersion.AutoDetect,
        string? connectionString = null,
        DbConnection? connection = null,
        DbTransaction? transaction = null)
        => new H2SharpProvider(H2SharpProviderName.H2Sharp, ResolveVersion(version, connectionString, connection));

    /// <summary>
    ///     A data connection over a connection string.
    /// </summary>
    public static DataConnection CreateDataConnection(
        string connectionString,
        H2SharpVersion version = H2SharpVersion.AutoDetect)
        => new(new DataOptions()
            .UseConnectionString(GetDataProvider(version, connectionString), connectionString));

    /// <summary>
    ///     A data connection over an open connection.
    /// </summary>
    public static DataConnection CreateDataConnection(
        DbConnection connection,
        H2SharpVersion version = H2SharpVersion.AutoDetect)
        => new(new DataOptions()
            .UseConnection(GetDataProvider(version, null, connection), connection));

    /// <summary>
    ///     A data connection over a transaction.
    /// </summary>
    public static DataConnection CreateDataConnection(
        DbTransaction transaction,
        H2SharpVersion version = H2SharpVersion.AutoDetect)
        => new(new DataOptions()
            .UseTransaction(GetDataProvider(version, null, null, transaction), transaction));

    /// <summary>
    ///     Resolves the H2 version to generate SQL for.
    /// </summary>
    /// <remarks>
    ///     The engine reports its version itself, so the version can be asked of the server rather
    ///     than configured. H2Sharp tracks H2 2.5, and that is what the provider generates for
    ///     unless the caller says otherwise.
    /// </remarks>
    private static H2SharpVersion ResolveVersion(
        H2SharpVersion version,
        string? connectionString,
        DbConnection? connection)
    {
        if (version != H2SharpVersion.AutoDetect)
        {
            return version;
        }

        return H2SharpVersion.v2_5;
    }
}
