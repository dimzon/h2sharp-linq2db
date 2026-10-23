/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Data;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Translates a connection string into the URL the engine opens.
/// </summary>
/// <remarks>
///     <para>
///         H2Sharp's ADO layer takes a JDBC URL, and the .NET world spells the same thing
///         <c>Data Source=mem:test</c>. Both name the same database, so both are accepted here.
///     </para>
///     <para>
///         The translation is this provider's, not the engine's: the engine is not being changed for
///         it, and the rule that belongs to the engine -- an H2 URL and an ADO connection string
///         name the same database -- is documented in the engine's own connection string builder.
///     </para>
/// </remarks>
public static class H2SharpConnectionString
{
    /// <summary>
    ///     The URL the engine opens.
    /// </summary>
    /// <param name="connectionString">A JDBC URL, or an ADO.NET connection string.</param>
    /// <returns>The URL.</returns>
    public static string ToUrl(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        if (connectionString.StartsWith("jdbc:h2:", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var builder = new H2ConnectionStringBuilder(connectionString);
        var url = builder.ToH2ConnectionString();

        // Whatever is not the database name is a setting, and a setting belongs on
        // the URL. The name, the user and the password are already in the URL the
        // builder made, so they are not settings here (the engine reads the same
        // three names the same way when it opens the connection).
        var settings = new List<string>();

        foreach (var key in builder.Keys)
        {
            var name = key.ToString() ?? string.Empty;

            if (!string.Equals(name, "Data Source", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name, "User ID", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name, "Password", StringComparison.OrdinalIgnoreCase))
            {
                settings.Add($"{name}={builder[name]?.ToString()}");
            }
        }

        return settings.Count == 0 ? url : url + ";" + string.Join(";", settings);
    }
}
