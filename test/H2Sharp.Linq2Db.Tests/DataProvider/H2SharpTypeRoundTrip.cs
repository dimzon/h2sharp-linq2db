/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Globalization;

using LinqToDB;
using LinqToDB.Data;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
///     A value that goes into the database and comes back out of it.
/// </summary>
/// <remarks>
///     <para>
///         This is the check the PostgreSQL tests of linq2db make, carried over to H2: a value of
///         a type is put into a column of that type, read back, and put in as a parameter to be
///         found again. The value has to survive the round trip, and NULL has to stay NULL.
///     </para>
///     <para>
///         The table is made here, by the test, and not by a set-up script: the test says what
///         it needs and nothing else does it for it.
///     </para>
/// </remarks>
internal static class H2SharpTypeRoundTrip
{
    private const string NullSql =
        """
        SELECT "{0}" FROM "{1}" WHERE "ID" = 1
        """;

    private const string ValueSql =
        """
        SELECT "{0}" FROM "{1}" WHERE "ID" = 2
        """;

    private const string PassNullSql =
        """
        SELECT "ID" FROM "{1}" WHERE @p IS NULL AND "{0}" IS NULL OR @p IS NOT NULL AND "{0}" = @p
        """;

    private const string PassValueSql =
        """
        SELECT "ID" FROM "{1}" WHERE "{0}" = @p
        """;

    /// <summary>
    ///     Puts a value into a column of the given type and reads it back.
    /// </summary>
    /// <param name="db">The database to work in.</param>
    /// <param name="columnName">The column to work with.</param>
    /// <param name="columnType">The type of the column, as H2 spells it.</param>
    /// <param name="value">The value to put in.</param>
    /// <param name="dataType">The type to tell the provider about, if it is to be told.</param>
    public static T Check<T>(DataConnection db, string columnName, string columnType, T value,
        DataType dataType = DataType.Undefined)
    {
        var tableName = $"Types_{columnName}";

        db.Execute($"""
            CREATE TABLE "{tableName}" (
                "ID" integer NOT NULL,
                "{columnName}" {columnType},
                CONSTRAINT "PK_{tableName}" PRIMARY KEY ("ID")
            )
            """);

        db.Execute($"""
            INSERT INTO "{tableName}" ("ID", "{columnName}") VALUES (1, NULL)
            """);

        db.Execute($"""
            INSERT INTO "{tableName}" ("ID", "{columnName}") VALUES (2, @p)
            """, new { p = value });

        // The value that was put in comes back out.
        var read = db.Execute<T>(Format(ValueSql, columnName, tableName));

        Assert.Equal(value, read);

        // The row that holds NULL holds NULL.
        var nullValue = db.Execute<T>(Format(NullSql, columnName, tableName));

        Assert.Equal(db.MappingSchema.GetDefaultValue(typeof(T)), nullValue);

        // The value is found again when it is passed as a parameter.
        var id = db.Execute<int?>(Format(PassValueSql, columnName, tableName), new { p = value });

        Assert.Equal(2, id);

        if (dataType != DataType.Undefined)
        {
            id = db.Execute<int?>(Format(PassValueSql, columnName, tableName),
                new DataParameter("p", value, dataType));

            Assert.Equal(2, id);
        }

        // NULL is found again when it is passed as a parameter.
        id = db.Execute<int?>(Format(PassNullSql, columnName, tableName), new { p = default(T) });

        Assert.Equal(1, id);

        return read;
    }

    private static string Format(string sql, string columnName, string tableName)
        => string.Format(CultureInfo.InvariantCulture, sql, columnName, tableName);
}
