/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data.Common;
using System.Globalization;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Reads the catalogue of an H2 database.
/// </summary>
/// <remarks>
///     <para>
///         H2 keeps its catalogue in <c>INFORMATION_SCHEMA</c>, and this provider reads it the way
///         H2 2.5 documents it: the objects in <c>INFORMATION_SCHEMA.TABLES</c>, the columns in
///         <c>INFORMATION_SCHEMA.COLUMNS</c>, the keys in <c>INFORMATION_SCHEMA.TABLE_CONSTRAINTS</c>
///         and <c>INFORMATION_SCHEMA.KEY_COLUMN_USAGE</c>, the references in
///         <c>INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS</c>, the routines in
///         <c>INFORMATION_SCHEMA.ROUTINES</c> and their parameters in
///         <c>INFORMATION_SCHEMA.PARAMETERS</c>.
///     </para>
///     <para>
///         The catalogue's own tables live in the same schema as everything else, so they are left
///         out unless the caller asks for that schema by name.
///     </para>
/// </remarks>
public class H2SharpSchemaProvider : ISchemaProvider
{
    /// <summary>
    ///     The schema H2 keeps its own catalogue in.
    /// </summary>
    private const string MetaSchema = "INFORMATION_SCHEMA";

    /// <summary>
    ///     The schema H2 puts an object in when the name does not say.
    /// </summary>
    private const string DefaultSchema = "PUBLIC";

    private const string TablesSql =
        """
        SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE, REMARKS
        FROM INFORMATION_SCHEMA.TABLES
        ORDER BY TABLE_SCHEMA, TABLE_NAME
        """;

    private const string ColumnsSql =
        """
        SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, IS_NULLABLE, DATA_TYPE,
               CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_IDENTITY, REMARKS
        FROM INFORMATION_SCHEMA.COLUMNS
        ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
        """;

    private const string PrimaryKeysSql =
        """
        SELECT c.TABLE_SCHEMA, c.TABLE_NAME, k.CONSTRAINT_NAME, k.COLUMN_NAME, k.ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
          ON k.CONSTRAINT_SCHEMA = c.CONSTRAINT_SCHEMA
         AND k.CONSTRAINT_NAME   = c.CONSTRAINT_NAME
        WHERE c.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, k.ORDINAL_POSITION
        """;

    private const string ForeignKeysSql =
        """
        SELECT f.TABLE_SCHEMA, f.TABLE_NAME, f.CONSTRAINT_NAME, k.COLUMN_NAME, k.ORDINAL_POSITION,
               r.UNIQUE_CONSTRAINT_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS f
        JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r
          ON r.CONSTRAINT_SCHEMA = f.CONSTRAINT_SCHEMA
         AND r.CONSTRAINT_NAME   = f.CONSTRAINT_NAME
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
          ON k.CONSTRAINT_SCHEMA = f.CONSTRAINT_SCHEMA
         AND k.CONSTRAINT_NAME   = f.CONSTRAINT_NAME
        WHERE f.CONSTRAINT_TYPE = 'FOREIGN KEY'
        ORDER BY f.TABLE_SCHEMA, f.TABLE_NAME, f.CONSTRAINT_NAME, k.ORDINAL_POSITION
        """;

    private const string ReferencedColumnsSql =
        """
        SELECT CONSTRAINT_SCHEMA, CONSTRAINT_NAME, TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        ORDER BY CONSTRAINT_SCHEMA, CONSTRAINT_NAME, ORDINAL_POSITION
        """;

    private const string RoutinesSql =
        """
        SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, SPECIFIC_NAME, ROUTINE_DEFINITION, REMARKS
        FROM INFORMATION_SCHEMA.ROUTINES
        ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME
        """;

    private const string ParametersSql =
        """
        SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, PARAMETER_NAME, ORDINAL_POSITION, PARAMETER_MODE,
               IS_RESULT, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
        FROM INFORMATION_SCHEMA.PARAMETERS
        ORDER BY SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION
        """;

    /// <inheritdoc />
    public DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
    {
        options ??= new GetSchemaOptions();

        var connection = dataConnection.OpenDbConnection();

        var schema = new DatabaseSchema
        {
            DataSource    = connection.DataSource,
            Database      = connection.Database,
            ServerVersion = connection.ServerVersion,
            Tables        = [],
            Procedures    = [],
        };

        if (options.GetTables)
        {
            ReadTables(connection, options, schema);
            ReadColumns(connection, options, schema);
            ReadPrimaryKeys(connection, options, schema);

            if (options.GetForeignKeys)
                ReadForeignKeys(connection, options, schema);
        }

        if (options.GetProcedures)
            ReadProcedures(connection, options, schema);

        return schema;
    }

    private static void ReadTables(DbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        var defaultSchema = options.DefaultSchema ?? DefaultSchema;

        foreach (var row in Query(connection, TablesSql))
        {
            var schemaName = row.GetString(1);
            var name       = row.GetString(2);

            if (!Included(schemaName, options))
                continue;

            schema.Tables.Add(new TableSchema
            {
                ID              = TableId(schemaName, name),
                CatalogName     = row.GetString(0),
                SchemaName      = schemaName,
                TableName       = name,
                Description     = row.GetString(4),
                IsView          = string.Equals(row.GetString(3), "VIEW", StringComparison.OrdinalIgnoreCase),
                IsDefaultSchema = options.StringComparer.Equals(schemaName, defaultSchema),
                TypeName        = ToValidName(name),
                Columns         = [],
                ForeignKeys     = [],
            });
        }
    }

    private static void ReadColumns(DbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        foreach (var row in Query(connection, ColumnsSql))
        {
            var schemaName = row.GetString(0);
            var tableName  = row.GetString(1);

            if (!Included(schemaName, options))
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));

            if (table is null)
                continue;

            var dataType = row.GetString(5);
            var length   = row.GetInt32(6);
            var precision = row.GetInt32(7);
            var scale     = row.GetInt32(8);
            var nullable  = string.Equals(row.GetString(4), "YES", StringComparison.OrdinalIgnoreCase);
            var identity  = string.Equals(row.GetString(9), "YES", StringComparison.OrdinalIgnoreCase);
            var systemType = MapSystemType(dataType);

            table.Columns.Add(new ColumnSchema
            {
                ColumnName     = row.GetString(2) ?? string.Empty,
                ColumnType     = ColumnType(dataType, length, precision, scale),
                IsNullable     = nullable,
                IsIdentity     = identity,
                Description    = row.GetString(10),
                MemberName     = ToValidName(row.GetString(2)),
                MemberType     = ToTypeName(systemType, nullable),
                SystemType     = systemType,
                DataType       = MapDataType(dataType),
                SkipOnInsert   = identity,
                SkipOnUpdate   = identity,
                Length         = length,
                Precision      = precision,
                Scale          = scale,
            });
        }
    }

    private static void ReadPrimaryKeys(DbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        foreach (var row in Query(connection, PrimaryKeysSql))
        {
            var schemaName = row.GetString(0);
            var tableName  = row.GetString(1);

            if (!Included(schemaName, options))
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));

            if (table is null)
                continue;

            var column = table.Columns.FirstOrDefault(c =>
                string.Equals(c.ColumnName, row.GetString(3), StringComparison.Ordinal));

            if (column is null)
                continue;

            column.IsPrimaryKey   = true;
            column.PrimaryKeyOrder = row.GetInt32(4) ?? 0;
        }
    }

    private static void ReadForeignKeys(DbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        // The columns of the referenced key are read as well: a reference names the constraint it
        // points at, and that constraint names its own columns.
        var referenced = new Dictionary<(string Schema, string Name), (string Schema, string Table, string Column)[]>();

        foreach (var row in Query(connection, ReferencedColumnsSql))
        {
            var key = (row.GetString(0) ?? string.Empty, row.GetString(1) ?? string.Empty);

            referenced[key] = referenced.TryGetValue(key, out var columns)
                ? [.. columns, (row.GetString(2)!, row.GetString(3)!, row.GetString(4)!)]
                : [(row.GetString(2)!, row.GetString(3)!, row.GetString(4)!)];
        }

        var keys = new List<(string Name, TableSchema Table, string Column, TableSchema Other, string OtherColumn)>();

        foreach (var row in Query(connection, ForeignKeysSql))
        {
            var schemaName = row.GetString(0);
            var tableName  = row.GetString(1);

            if (!Included(schemaName, options))
                continue;

            var uniqueName = row.GetString(5);
            var position   = row.GetInt32(4) ?? 1;

            if (uniqueName is null ||
                !referenced.TryGetValue((schemaName!, uniqueName), out var target) ||
                position < 1 ||
                position > target.Length)
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));
            var other = FindTable(schema, TableId(target[position - 1].Schema, target[position - 1].Table));

            if (table is null || other is null)
                continue;

            keys.Add((row.GetString(2)!, table, row.GetString(3)!, other, target[position - 1].Column));
        }

        foreach (var group in keys.GroupBy(k => (k.Name, k.Table)))
        {
            var name  = group.Key.Name;
            var table = group.Key.Table;

            var thisColumns  = new List<ColumnSchema>();
            var otherColumns = new List<ColumnSchema>();

            foreach (var key in group)
            {
                var thisColumn = table.Columns.FirstOrDefault(c =>
                    string.Equals(c.ColumnName, key.Column, StringComparison.Ordinal));

                var otherColumn = key.Other.Columns.FirstOrDefault(c =>
                    string.Equals(c.ColumnName, key.OtherColumn, StringComparison.Ordinal));

                if (thisColumn is null || otherColumn is null)
                    continue;

                thisColumns.Add(thisColumn);
                otherColumns.Add(otherColumn);
            }

            if (thisColumns.Count == 0)
                continue;

            var foreignKey = new ForeignKeySchema
            {
                KeyName        = name,
                ThisTable      = table,
                OtherTable     = group.First().Other,
                ThisColumns   = thisColumns,
                OtherColumns  = otherColumns,
                CanBeNull      = thisColumns.All(c => c.IsNullable),
                MemberName     = ToValidName(name),
                AssociationType = AssociationType.OneToMany,
            };

            table.ForeignKeys.Add(foreignKey);
        }
    }

    private static void ReadProcedures(DbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        var parameters = new Dictionary<(string Schema, string Name), List<ParameterSchema>>();

        foreach (var row in Query(connection, ParametersSql))
        {
            var key = (row.GetString(0) ?? string.Empty, row.GetString(1) ?? string.Empty);

            var dataType   = row.GetString(6);
            var length     = row.GetInt32(7);
            var precision  = row.GetInt32(8);
            var scale      = row.GetInt32(9);
            var mode       = row.GetString(4);
            var systemType = MapSystemType(dataType);

            var parameter = new ParameterSchema
            {
                ParameterName = ToValidName(row.GetString(2)),
                ParameterType = ToTypeName(systemType, true),
                SystemType    = systemType,
                DataType      = MapDataType(dataType),
                Size          = length,
                IsIn          = string.Equals(mode, "IN", StringComparison.OrdinalIgnoreCase),
                IsOut         = string.Equals(mode, "OUT", StringComparison.OrdinalIgnoreCase),
                IsResult      = string.Equals(row.GetString(5), "YES", StringComparison.OrdinalIgnoreCase),
            };

            if (!parameters.TryGetValue(key, out var list))
            {
                list = [];
                parameters[key] = list;
            }

            list.Add(parameter);
        }

        var defaultSchema = options.DefaultSchema ?? DefaultSchema;

        foreach (var row in Query(connection, RoutinesSql))
        {
            var schemaName = row.GetString(0);
            var name       = row.GetString(1);

            if (!Included(schemaName, options))
                continue;

            var specificName = row.GetString(3);

            schema.Procedures.Add(new ProcedureSchema
            {
                SchemaName      = schemaName,
                ProcedureName   = name ?? string.Empty,
                MemberName      = ToValidName(name),
                IsFunction      = string.Equals(row.GetString(2), "FUNCTION", StringComparison.OrdinalIgnoreCase),
                IsDefaultSchema = options.StringComparer.Equals(schemaName, defaultSchema),
                Description     = row.GetString(5),
                IsLoaded        = true,
                Parameters      = specificName is not null &&
                                 parameters.TryGetValue((schemaName!, specificName), out var list)
                    ? list
                    : [],
            });
        }
    }

    private static TableSchema? FindTable(DatabaseSchema schema, string id)
        => schema.Tables.FirstOrDefault(t => string.Equals(t.ID, id, StringComparison.Ordinal));

    /// <summary>
    ///     Whether a schema is part of the answer.
    /// </summary>
    private static bool Included(string? schemaName, GetSchemaOptions options)
    {
        if (options.IncludedSchemas is { Length: > 0 } &&
            !options.IncludedSchemas.Contains(schemaName, options.StringComparer))
            return false;

        if (options.ExcludedSchemas is { Length: > 0 } &&
            options.ExcludedSchemas.Contains(schemaName, options.StringComparer))
            return false;

        // The catalogue's own tables are not the database's tables. They come back only when the
        // caller asks for that schema by name.
        if (options.StringComparer.Equals(schemaName, MetaSchema) &&
            !(options.IncludedSchemas?.Contains(MetaSchema, options.StringComparer) ?? false))
            return false;

        return true;
    }

    private static string TableId(string? schema, string? name)
        => $"{schema}.{name}";

    /// <summary>
    ///     The SQL type of a column, as H2 would spell it.
    /// </summary>
    private static string? ColumnType(string? dataType, int? length, int? precision, int? scale)
    {
        if (dataType is null)
            return null;

        if (length is not null)
            return $"{dataType}({length})";

        if (precision is not null)
            return scale is null ? $"{dataType}({precision})" : $"{dataType}({precision}, {scale})";

        return dataType;
    }

    /// <summary>
    ///     Runs a query and hands the rows over one at a time.
    /// </summary>
    private static List<Row> Query(DbConnection connection, string sql)
    {
        var rows = new List<Row>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();

        while (reader.Read())
            rows.Add(new Row(reader));

        return rows;
    }

    /// <summary>
    ///     A name that can be a member name.
    /// </summary>
    private static string ToValidName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return "_";

        var builder = new System.Text.StringBuilder(name!.Length);

        foreach (var c in name)
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

        if (char.IsDigit(builder[0]))
            builder.Insert(0, '_');

        return builder.ToString();
    }

    /// <summary>
    ///     The name of a type, as a member type is written.
    /// </summary>
    private static string ToTypeName(Type? type, bool isNullable)
    {
        if (type is null)
            return "object";

        return isNullable ? type.Name + "?" : type.Name;
    }

    /// <summary>
    ///     The H2 data types, and the linq2db types they are.
    /// </summary>
    private static DataType MapDataType(string? dataType)
        => dataType?.ToUpperInvariant() switch
        {
            "CHARACTER VARYING" or "VARCHAR" or "VARCHAR_IGNORECASE" => DataType.NVarChar,
            "CHARACTER"                                         => DataType.Char,
            "CHARACTER LARGE OBJECT"                            => DataType.Text,
            "BINARY VARYING"                                    => DataType.VarBinary,
            "BINARY LARGE OBJECT"                               => DataType.Blob,
            "BOOLEAN"                                           => DataType.Boolean,
            "TINYINT"                                           => DataType.SByte,
            "SMALLINT"                                          => DataType.Int16,
            "INTEGER"                                           => DataType.Int32,
            "BIGINT"                                            => DataType.Int64,
            "REAL"                                              => DataType.Single,
            "DOUBLE PRECISION"                                  => DataType.Double,
            "NUMERIC" or "DECIMAL"                              => DataType.Decimal,
            "DECFLOAT"                                          => DataType.DecFloat,
            "DATE"                                              => DataType.Date,
            "TIME"                                              => DataType.Time,
            "TIME WITH TIME ZONE"                               => DataType.TimeTZ,
            "TIMESTAMP"                                         => DataType.DateTime,
            "TIMESTAMP WITH TIME ZONE"                          => DataType.DateTimeOffset,
            "UUID"                                              => DataType.Guid,
            "JSON"                                              => DataType.Json,
            "INTERVAL"                                          => DataType.Interval,
            "ENUM"                                              => DataType.Enum,
            "GEOMETRY"                                          => DataType.Blob,
            _                                                   => DataType.Undefined,
        };

    /// <summary>
    ///     The H2 data types, and the types the engine hands them over as.
    /// </summary>
    private static Type? MapSystemType(string? dataType)
        => dataType?.ToUpperInvariant() switch
        {
            "CHARACTER VARYING" or "VARCHAR" or "VARCHAR_IGNORECASE" or "CHARACTER" or "CHARACTER LARGE OBJECT"
                or "ENUM" => typeof(string),
            "BINARY VARYING" or "BINARY LARGE OBJECT" or "GEOMETRY" => typeof(byte[]),
            "BOOLEAN"          => typeof(bool),
            "TINYINT"          => typeof(sbyte),
            "SMALLINT"         => typeof(short),
            "INTEGER"          => typeof(int),
            "BIGINT"           => typeof(long),
            "REAL"             => typeof(float),
            "DOUBLE PRECISION" => typeof(double),
            "NUMERIC" or "DECIMAL" or "DECFLOAT" => typeof(decimal),
            "DATE" or "TIMESTAMP" => typeof(DateTime),
            "TIME"             => typeof(TimeSpan),
            "TIME WITH TIME ZONE" or "TIMESTAMP WITH TIME ZONE" => typeof(DateTimeOffset),
            "INTERVAL"         => typeof(TimeSpan),
            "UUID"             => typeof(Guid),
            _                  => null,
        };

    /// <summary>
    ///     One row of a result, read by position.
    /// </summary>
    private sealed class Row(DbDataReader reader)
    {
        public string? GetString(int ordinal)
        {
            var value = reader.GetValue(ordinal);

            return value is DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public int? GetInt32(int ordinal)
        {
            var value = reader.GetValue(ordinal);

            return value is DBNull ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }
}
