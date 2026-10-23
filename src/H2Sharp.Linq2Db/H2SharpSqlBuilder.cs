/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;
using System.Data.Common;
using System.Globalization;
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
                sb.Append('"').Append(schema.Replace("\"", "\"\"")).Append('"').Append('.');
            }

            sb.Append('"').Append(name.Name.Replace("\"", "\"\"")).Append('"');
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
    /// <remarks>
    ///     H2 2.5 joins the texts with <c>CONCAT</c>. The pipes (<c>||</c>) the reference takes as
    ///     well are not what the provider writes: the reference was asked about both, and the
    ///     function is the one that says what it does.
    /// </remarks>
    protected override ConcatBuildStyle ConcatStyle => ConcatBuildStyle.Function;

    /// <inheritdoc />
    /// <remarks>
    ///     The count of the limit and of the offset goes into the format as <c>{0}</c>: the
    ///     count is written with a format, and a format without a placeholder keeps nothing of
    ///     what it is given. The statement ended with a bare "LIMIT" and no count at all.
    /// </remarks>
    protected override string LimitFormat(SelectQuery selectQuery)
        => "LIMIT {0}";

    /// <inheritdoc />
    protected override string OffsetFormat(SelectQuery selectQuery)
        => "OFFSET {0}";

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
    ///         H2 2.5 has no <c>LATERAL</c> and no <c>CROSS APPLY</c>: the grammar of H2 2.5 has
    ///         neither word, and the reference refuses both. A table function's argument cannot
    ///         depend on the row of an outer query either — the reference answers
    ///         <c>Column "T.NUM" not found</c> — because the correlation of H2 lives in the
    ///         subqueries of a statement, and the arguments of a table function are not one.
    ///     </para>
    ///     <para>
    ///         So the rows a query needs per row of a table are cut by a condition, and the
    ///         function is asked for its own bounds. The range table of H2 is the case in point:
    ///         <c>SYSTEM_RANGE(1, 3)</c> with the condition <c>r.X &lt;= t.Num</c> gives the rows
    ///         the caller wants.
    ///     </para>
    /// </remarks>
    protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
    {
        switch (join.JoinType)
        {
            case JoinType.CrossApply:
            case JoinType.OuterApply:
                throw new NotSupportedException(
                    "H2 2.5 has no LATERAL and no CROSS APPLY: the argument of a table function " +
                    "cannot depend on the row of an outer query. Ask the function for its own " +
                    "bounds and cut the rows with a condition.");
        }

        return base.BuildJoinType(join, condition);
    }

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
    /// <remarks>
    ///     <para>
    ///         H2 2.5 has no pseudo-type <c>serial</c>: the only column the engine fills by
    ///         itself is a column declared <c>GENERATED BY DEFAULT AS IDENTITY</c>, and that is
    ///         what the provider writes. Without it the column was created as an ordinary
    ///         integer, and the insert that leaves the key to the engine had nothing to put
    ///         into it.
    ///     </para>
    /// </remarks>
    protected override void BuildCreateTableIdentityAttribute1(SqlField field)
        => StringBuilder.Append("GENERATED BY DEFAULT AS IDENTITY");

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

            // A moment in time is a timestamp, and the way linq2db holds a DateTime is
            // DataType.DateTime2. Without this case the column was created as a varchar,
            // because the switch knew nothing about the type.
            case DataType.DateTime:
            case DataType.DateTime2:
            case DataType.SmallDateTime:
                StringBuilder.Append("timestamp");
                break;

            // A DateTimeOffset is a point of time with an offset, and H2 keeps that in the
            // type TIMESTAMP WITH TIME ZONE. Without this case the column was created as
            // a varchar, because the switch knew nothing about the type.
            case DataType.DateTimeOffset:
            case DataType.DateTimeTz:
            case DataType.DateTime2Tz:
                StringBuilder.Append("timestamp with time zone");
                break;

            case DataType.Guid     : StringBuilder.Append("uuid"); break;
            case DataType.VarBinary: StringBuilder.Append("varbinary"); break;

            // A sequence of bytes is a binary column: H2 keeps it in VARBINARY, and the
            // column of a byte array was created as a varchar before.
            case DataType.Binary   : StringBuilder.Append("varbinary"); break;

            case DataType.Json     : StringBuilder.Append("json"); break;
            default               : StringBuilder.Append("varchar"); break;
        }

        // An array is the type of its elements and the word ARRAY after it: INTEGER ARRAY.
        // The brackets of PostgreSQL (integer[]) H2 does not know. The port does not read
        // the type ARRAY yet, so a table with such a column cannot be created against it;
        // the form written here is the one the reference engine takes.
        if ((type.DataType & DataType.Array) != 0)
        {
            StringBuilder.Append(" ARRAY");
        }
        else
        {
            // The facets of the type: the length of a string, the precision and the scale of a
            // number. Without them the column was created as a plain "varchar" and a plain
            // "numeric", and the length and the precision of the model were lost.
            if (type.Length > 0)
            {
                StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "({0})", type.Length);
            }
            else if (type.Precision > 0)
            {
                if (type.Scale != null)
                {
                    StringBuilder.AppendFormat(
                        CultureInfo.InvariantCulture, "({0}{1}{2})", type.Precision, InlineComma, type.Scale);
                }
                else
                {
                    StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "({0})", type.Precision);
                }
            }        }
    }
}
