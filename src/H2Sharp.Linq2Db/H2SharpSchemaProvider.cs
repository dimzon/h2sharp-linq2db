/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;
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

    // The keys are read with three queries and matched in the provider, and not with one query
    // that joins the tables of the catalogue. Both are SQL of H2 2.5, and the second one asks the
    // engine for less than the first one does.
    private const string ConstraintsSql =
        """
        SELECT CONSTRAINT_SCHEMA, CONSTRAINT_NAME, TABLE_SCHEMA, TABLE_NAME, CONSTRAINT_TYPE
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        ORDER BY CONSTRAINT_SCHEMA, CONSTRAINT_NAME
        """;

    private const string KeyColumnsSql =
        """
        SELECT CONSTRAINT_SCHEMA, CONSTRAINT_NAME, TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME,
               ORDINAL_POSITION, POSITION_IN_UNIQUE_CONSTRAINT
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        ORDER BY CONSTRAINT_SCHEMA, CONSTRAINT_NAME, ORDINAL_POSITION
        """;

    private const string ReferencesSql =
        """
        SELECT CONSTRAINT_SCHEMA, CONSTRAINT_NAME, UNIQUE_CONSTRAINT_NAME
        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
        ORDER BY CONSTRAINT_SCHEMA, CONSTRAINT_NAME
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

    private static void ReadTables(IDbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        var defaultSchema = options.DefaultSchema ?? DefaultSchema;

        foreach (var (catalog, schemaName, name, type, remarks) in Query(connection, TablesSql, r => (
                     Catalog:  Text(r, 0),
                     Schema:   Text(r, 1),
                     Name:     Text(r, 2),
                     Type:     Text(r, 3),
                     Remarks:  Text(r, 4))))
        {
            if (!Included(schemaName, options))
                continue;

            schema.Tables.Add(new TableSchema
            {
                ID              = TableId(schemaName, name),
                CatalogName     = catalog,
                SchemaName      = schemaName,
                TableName       = name,
                Description     = remarks,
                IsView          = string.Equals(type, "VIEW", StringComparison.OrdinalIgnoreCase),
                IsDefaultSchema = options.StringComparer.Equals(schemaName, defaultSchema),
                TypeName        = ToValidName(name),
                Columns         = [],
                ForeignKeys     = [],
            });
        }
    }

    private static void ReadColumns(IDbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        foreach (var row in Query(connection, ColumnsSql, r => (
                     Schema:     Text(r, 0),
                     Table:      Text(r, 1),
                     Name:       Text(r, 2),
                     Ordinal:    Number(r, 3),
                     Nullable:   Text(r, 4),
                     DataType:   Text(r, 5),
                     Length:     (int?)BigNumber(r, 6),
                     Precision:  Number(r, 7),
                     Scale:      Number(r, 8),
                     Identity:   Text(r, 9),
                     Remarks:    Text(r, 10))))
        {
            var schemaName = row.Schema;
            var tableName  = row.Table;

            if (!Included(schemaName, options))
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));

            if (table is null)
                continue;

            var dataType = row.DataType;
            var length   = row.Length;
            var precision = row.Precision;
            var scale     = row.Scale;
            var nullable  = string.Equals(row.Nullable, "YES", StringComparison.OrdinalIgnoreCase);
            var identity  = string.Equals(row.Identity, "YES", StringComparison.OrdinalIgnoreCase);
            var systemType = MapSystemType(dataType);

            table.Columns.Add(new ColumnSchema
            {
                ColumnName     = row.Name ?? string.Empty,
                ColumnType     = ColumnType(dataType, length, precision, scale),
                IsNullable     = nullable,
                IsIdentity     = identity,
                Description    = row.Remarks,
                MemberName     = ToValidName(row.Name),
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

    private static void ReadPrimaryKeys(IDbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        // The constraint and the columns of it live in two tables of the catalogue, and they are
        // asked for separately and matched here.
        var primaryKeys = new HashSet<(string Schema, string Name)>();

        foreach (var row in Query(connection, ConstraintsSql, r => (
                     Schema:    Text(r, 0) ?? string.Empty,
                     Name:      Text(r, 1) ?? string.Empty,
                     Type:      Text(r, 4))))
        {
            if (string.Equals(row.Type, "PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                primaryKeys.Add((row.Schema, row.Name));
        }

        foreach (var row in Query(connection, KeyColumnsSql, r => (
                     Schema:    Text(r, 0) ?? string.Empty,
                     Name:      Text(r, 1) ?? string.Empty,
                     Table:     Text(r, 2),
                     TableName: Text(r, 3),
                     Column:    Text(r, 4),
                     Position:  Number(r, 5),
                     InKey:     Number(r, 6))))
        {
            var key = (row.Schema, row.Name);

            if (!primaryKeys.Contains(key))
                continue;

            var schemaName = row.Table;
            var tableName  = row.TableName;

            if (!Included(schemaName, options))
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));

            if (table is null)
                continue;

            var column = table.Columns.FirstOrDefault(c =>
                string.Equals(c.ColumnName, row.Column, StringComparison.Ordinal));

            if (column is null)
                continue;

            column.IsPrimaryKey   = true;
            // The catalogue counts the columns of a key from one, and the order of a key in
            // linq2db starts at zero.
            column.PrimaryKeyOrder = (row.Position ?? 1) - 1;
        }
    }

    private static void ReadForeignKeys(IDbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        // Three questions: the constraints, the columns of every constraint, and the constraint
        // that every reference points at. A reference names that constraint, and the constraint
        // names its own columns.
        var foreignKeys = new HashSet<(string Schema, string Name)>();
        var references  = new Dictionary<(string Schema, string Name), string>();

        foreach (var row in Query(connection, ConstraintsSql, r => (
                     Schema:    Text(r, 0) ?? string.Empty,
                     Name:      Text(r, 1) ?? string.Empty,
                     Type:      Text(r, 4))))
        {
            if (string.Equals(row.Type, "FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                foreignKeys.Add((row.Schema, row.Name));
        }

        foreach (var row in Query(connection, ReferencesSql, r => (
                     Schema:   Text(r, 0) ?? string.Empty,
                     Name:     Text(r, 1) ?? string.Empty,
                     Unique:   Text(r, 2) ?? string.Empty)))
        {
            references[(row.Schema, row.Name)] = row.Unique;
        }

        // The columns of every constraint, by the constraint they belong to.
        var keyColumns = new Dictionary<(string Schema, string Name), (string Schema, string Table, string Column, int Position, int InKey)[]>();

        foreach (var row in Query(connection, KeyColumnsSql, r => (
                     Schema:   Text(r, 0) ?? string.Empty,
                     Name:     Text(r, 1) ?? string.Empty,
                     Table:    Text(r, 2) ?? string.Empty,
                     TableName:Text(r, 3) ?? string.Empty,
                     Column:   Text(r, 4) ?? string.Empty,
                     Position: Number(r, 5) ?? 0,
                     InKey:    Number(r, 6) ?? 0)))
        {
            var key   = (row.Schema, row.Name);
            var value = (row.Table, row.TableName, row.Column, row.Position, row.InKey);

            keyColumns[key] = keyColumns.TryGetValue(key, out var list) ? [.. list, value] : [value];
        }

        var keys = new List<(string Name, TableSchema Table, string Column, TableSchema Other, string OtherColumn)>();

        foreach (var key in foreignKeys)
        {
            if (!keyColumns.TryGetValue(key, out var own) || own.Length == 0)
                continue;

            var schemaName = own[0].Schema;
            var tableName  = own[0].Table;

            if (!Included(schemaName, options))
                continue;

            if (!references.TryGetValue(key, out var uniqueName) ||
                !keyColumns.TryGetValue((key.Schema, uniqueName), out var target))
                continue;

            var table = FindTable(schema, TableId(schemaName, tableName));
            var other = FindTable(schema, TableId(target[0].Schema, target[0].Table));

            if (table is null || other is null)
                continue;

            foreach (var column in own)
            {
                // The position the column has in the referenced key says which column of that
                // key it points at.
                var referenced = target.FirstOrDefault(t => t.Position == column.InKey);

                if (referenced.Column is null)
                    continue;

                keys.Add((key.Name, table, column.Column, other, referenced.Column));
            }
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

    private static void ReadProcedures(IDbConnection connection, GetSchemaOptions options, DatabaseSchema schema)
    {
        var parameters = new Dictionary<(string Schema, string Name), List<ParameterSchema>>();

        foreach (var row in Query(connection, ParametersSql, r => (
                     Schema:     Text(r, 0) ?? string.Empty,
                     Specific:   Text(r, 1) ?? string.Empty,
                     Name:       Text(r, 2),
                     Mode:       Text(r, 4),
                     IsResult:   Text(r, 5),
                     DataType:   Text(r, 6),
                     Length:     (int?)BigNumber(r, 7),
                     Precision:  Number(r, 8),
                     Scale:      Number(r, 9))))
        {
            var key = (row.Schema, row.Specific);

            var systemType = MapSystemType(row.DataType);

            var parameter = new ParameterSchema
            {
                ParameterName = ToValidName(row.Name),
                ParameterType = ToTypeName(systemType, true),
                SystemType    = systemType,
                DataType      = MapDataType(row.DataType),
                Size          = row.Length is null ? null : (int)row.Length,
                IsIn          = string.Equals(row.Mode, "IN", StringComparison.OrdinalIgnoreCase),
                IsOut         = string.Equals(row.Mode, "OUT", StringComparison.OrdinalIgnoreCase),
                IsResult      = string.Equals(row.IsResult, "YES", StringComparison.OrdinalIgnoreCase),
            };

            if (!parameters.TryGetValue(key, out var list))
            {
                list = [];
                parameters[key] = list;
            }

            list.Add(parameter);
        }

        var defaultSchema = options.DefaultSchema ?? DefaultSchema;

        foreach (var row in Query(connection, RoutinesSql, r => (
                     Schema:    Text(r, 0),
                     Name:      Text(r, 1),
                     Type:      Text(r, 2),
                     Specific:  Text(r, 3),
                     Definition:Text(r, 4),
                     Remarks:   Text(r, 5))))
        {
            var schemaName = row.Schema;
            var name       = row.Name;

            if (!Included(schemaName, options))
                continue;

            var specificName = row.Specific;

            schema.Procedures.Add(new ProcedureSchema
            {
                SchemaName      = schemaName,
                ProcedureName   = name ?? string.Empty,
                MemberName      = ToValidName(name),
                IsFunction      = string.Equals(row.Type, "FUNCTION", StringComparison.OrdinalIgnoreCase),
                IsDefaultSchema = options.StringComparer.Equals(schemaName, defaultSchema),
                Description     = row.Remarks,
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
    ///     Runs a query and reads the rows it answers.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The row is read while the reader is open, and it is read through the typed
    ///         accessors of the reader, so that every value is taken as the type the catalogue
    ///         gives it and is not converted afterwards.
    ///     </para>
    /// </remarks>
    private static List<T> Query<T>(IDbConnection connection, string sql, Func<IDataReader, T> read)
    {
        var rows = new List<T>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();

        while (reader.Read())
            rows.Add(read(reader));

        return rows;
    }

    /// <summary>
    ///     A text of a row, or nothing where the row has none.
    /// </summary>
    private static string? Text(IDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    /// <summary>
    ///     A number of a row, or nothing where the row has none.
    /// </summary>
    private static int? Number(IDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    /// <summary>
    ///     A big number of a row, or nothing where the row has none.
    /// </summary>
    private static long? BigNumber(IDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

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
}
