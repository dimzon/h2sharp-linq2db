/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;
using System.Data.Common;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Generates H2's SQL.
/// </summary>
/// <remarks>
///     <para>
///         The shape of the query is linq2db's; what this class decides is how H2 spells it:
///         the quoting, the limit and offset, the concatenation, the data types.
///     </para>
///     <para>
///         Identifiers are always quoted, the way H2 documents it: an unquoted identifier is folded
///         to upper case, and a quoted one is not, so a table called <c>"Rows"</c> and one called
///         <c>ROWS</c> are different tables. Always quoting is the only spelling that reaches the
///         same name the model has.
///     </para>
/// </remarks>
public class H2SharpSqlBuilder : BasicSqlBuilder<H2SharpOptions>
{
    /// <summary>
    ///     Creates the builder.
    /// </summary>
    public H2SharpSqlBuilder(
        IDataProvider? provider,
        MappingSchema mappingSchema,
        DataOptions dataOptions,
        ISqlOptimizer sqlOptimizer,
        SqlProviderFlags sqlProviderFlags)
        : base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
    {
    }

    /// <summary>
    ///     Creates a builder that shares this one's settings, for a nested statement.
    /// </summary>
    protected H2SharpSqlBuilder(BasicSqlBuilder parentBuilder)
        : base(parentBuilder)
    {
    }

    /// <inheritdoc />
    protected override ISqlBuilder CreateSqlBuilder()
        => new H2SharpSqlBuilder(this);

    /// <inheritdoc />
    public override StringBuilder BuildObjectName(
        StringBuilder sb,
        SqlObjectName name,
        ConvertType objectType = ConvertType.NameToQueryTable,
        bool escape = true,
        TableOptions tableOptions = TableOptions.NotSet,
        bool withoutSuffix = false)
    {
        if (escape)
        {
            if (name.Schema is { } schema)
            {
                sb.Append('"').Append(schema).Append('"').Append('.');
            }

            sb.Append('"').Append(name.Name).Append('"');
        }
        else
        {
            if (name.Schema is { } schema)
            {
                sb.Append(schema).Append('.');
            }

            sb.Append(name.Name);
        }

        return sb;
    }

    /// <inheritdoc />
    protected override ConcatBuildStyle ConcatStyle => ConcatBuildStyle.Pipes;

    /// <inheritdoc />
    protected override string LimitFormat(SelectQuery selectQuery)
        => "LIMIT ";

    /// <inheritdoc />
    protected override string OffsetFormat(SelectQuery selectQuery)
        => "OFFSET ";

    /// <inheritdoc />
    protected override bool IsRecursiveCteKeywordRequired => false;

    /// <inheritdoc />
    protected override bool SupportsMaterializedCteHint => false;

    /// <inheritdoc />
    public override ISqlExpression? GetIdentityExpression(SqlTable table)
        // H2 2.5 has no SCOPE_IDENTITY(), no IDENTITY() and no LAST_INSERT_ID(): the reference was
        // asked, and it refuses all three. The generated value is delivered through the ADO
        // command's generated keys channel instead, which is not what this expression is for.
        => null;

    /// <inheritdoc />
    /// <remarks>
    ///     <para>
    ///         H2 2.5 has no <c>RETURNING</c>: the reference was asked, and it refuses the clause.
    ///         The row an insert made is read through <c>FINAL TABLE</c>, which is the form the H2
    ///         grammar has for it (<c>{ OLD | NEW | FINAL } TABLE</c>).
    ///     </para>
    ///     <para>
    ///         <c>RETURNING</c> only appends to the insert, and PostgreSQL's provider builds it
    ///         in the place where the insert is written. <c>FINAL TABLE</c> wraps the insert, so
    ///         the insert is built into a buffer of its own and the statement is wrapped around
    ///         it: the value the insert made comes back in the first column of the first row,
    ///         which is where linq2db reads it from.
    ///     </para>
    /// </remarks>
    protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
    {
        if (!insertClause.WithIdentity)
        {
            base.BuildInsertQuery(statement, insertClause, addAlias);
            return;
        }

        SqlField? identityField = insertClause.Into?.GetIdentityField();
        if (identityField is null)
        {
            throw new LinqToDBException(
                $"Identity field must be defined for '{insertClause.Into?.TableName.Name}'.");
        }

        var insert = new StringBuilder();

        StringBuilder outer = StringBuilder;
        StringBuilder = insert;
        try
        {
            base.BuildInsertQuery(statement, insertClause, addAlias);
        }
        finally
        {
            StringBuilder = outer;
        }

        outer.Append("SELECT ");
        Convert(outer, identityField.PhysicalName, ConvertType.NameToQueryField);
        outer.Append(" FROM FINAL TABLE (");
        outer.Append(insert);
        outer.Append(')');
    }

    /// <inheritdoc />
    protected override void BuildGetIdentity(SqlInsertClause insertClause)
        // The identity clause of H2 wraps the insert, and the wrap is written where the insert
        // is built: there is nothing to append here.
        => _ = insertClause;

    /// <inheritdoc />
    public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
        => convertType switch
        {
            // H2 names a parameter "@name" and rewrites it into "?" before the statement is parsed.
            ConvertType.NameToQueryParameter or
            ConvertType.NameToCommandParameter or
            ConvertType.NameToSprocParameter => sb.Append('@').Append(value),

            // H2 folds an unquoted identifier to upper case and leaves a quoted one alone, so the
            // two spellings of the same name are two different names. Every identifier is quoted
            // here, which is the only way to reach the name the model has.
            ConvertType.NameToQueryField or
            ConvertType.NameToQueryFieldAlias or
            ConvertType.NameToQueryTable or
            ConvertType.NameToQueryTableAlias or
            ConvertType.NameToCteName or
            ConvertType.NameToDatabase or
            ConvertType.NameToSchema or
            ConvertType.SequenceName => sb.Append('"').Append(value).Append('"'),

            _ => base.Convert(sb, value, convertType)
        };

    /// <inheritdoc />
    protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
    {
        switch (type.DataType & ~DataType.Array)
        {
            case DataType.Boolean  : StringBuilder.Append("boolean"); break;
            case DataType.Byte     : StringBuilder.Append("smallint"); break;
            case DataType.SByte    : StringBuilder.Append("smallint"); break;
            case DataType.Int16    : StringBuilder.Append("smallint"); break;
            case DataType.Int32    : StringBuilder.Append("integer"); break;
            case DataType.Int64    : StringBuilder.Append("bigint"); break;
            case DataType.Single   : StringBuilder.Append("real"); break;
            case DataType.Double   : StringBuilder.Append("double"); break;
            case DataType.Decimal  : StringBuilder.Append("numeric"); break;
            case DataType.Money    : StringBuilder.Append("numeric"); break;
            case DataType.NVarChar : StringBuilder.Append("varchar"); break;
            case DataType.Text    : StringBuilder.Append("clob"); break;
            case DataType.NText   : StringBuilder.Append("clob"); break;
            case DataType.Char     : StringBuilder.Append("char"); break;
            case DataType.Date     : StringBuilder.Append("date"); break;
            case DataType.Time     : StringBuilder.Append("time"); break;
            case DataType.DateTime : StringBuilder.Append("timestamp"); break;
            case DataType.Guid     : StringBuilder.Append("uuid"); break;
            case DataType.VarBinary: StringBuilder.Append("varbinary"); break;
            case DataType.Json     : StringBuilder.Append("json"); break;
            default               : StringBuilder.Append("varchar"); break;
        }

        if (forCreateTable && !canBeNull)
        {
            StringBuilder.Append(" not null");
        }
    }
}
