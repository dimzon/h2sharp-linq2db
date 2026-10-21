/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;
using System.Data.Common;

using H2Sharp.Data;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The Entity Framework-style data provider for H2Sharp.
/// </summary>
/// <remarks>
///     <para>
///         The capabilities this provider declares are H2 2.5's, not the port's. The port is being
///         filled in, and a provider that declared the port's current abilities would be quietly
///         describing a database that does not exist. Where the port cannot run what H2 2.5 can, the
///         failure is the port's, and it is listed in <c>docs/DIVERGENCES.md</c>.
///     </para>
/// </remarks>
public class H2SharpProvider : DataProviderBase
{
    /// <summary>
    ///     Creates the provider.
    /// </summary>
    /// <param name="name">The provider's name, as it appears in a configuration string.</param>
    /// <param name="version">The H2 version to generate SQL for.</param>
    public H2SharpProvider(string name, H2SharpVersion version)
        : base(name, new H2SharpMappingSchema())
    {
        Version = version;

        // H2 2.5, as the frozen documentation in the engine's repository records it.
        SqlProviderFlags.IsApplyJoinSupported                        = true;
        SqlProviderFlags.IsCrossApplyJoinSupportsCondition           = true;
        SqlProviderFlags.IsOuterApplyJoinSupportsCondition           = true;
        SqlProviderFlags.IsCommonTableExpressionsSupported           = true;
        SqlProviderFlags.IsSubQueryOrderBySupported                  = true;
        SqlProviderFlags.IsNullsOrderingSupported                    = true;
        SqlProviderFlags.DefaultNullsOrdering                        = NullsDefaultOrdering.Largest;
        SqlProviderFlags.IsUnionAllOrderBySupported                  = true;
        SqlProviderFlags.IsAllSetOperationsSupported                 = true;
        SqlProviderFlags.IsDistinctFromSupported                     = true;
        SqlProviderFlags.SupportsPredicatesComparison                = true;
        SqlProviderFlags.IsWindowFunctionsSupported                  = true;
        SqlProviderFlags.IsInsertOrUpdateSupported                   = true;
        SqlProviderFlags.IsUpdateFromSupported                       = false;
        SqlProviderFlags.IsDerivedTableOrderBySupported              = true;
        SqlProviderFlags.IsMultiTablesSupportsJoins                  = true;
        SqlProviderFlags.IsNestedJoinsSupported                      = true;
        SqlProviderFlags.IsOuterJoinSupportsInnerJoin                = true;
        SqlProviderFlags.RowConstructorSupport                       =
            RowFeature.Equality | RowFeature.Comparisons | RowFeature.CompareToSelect |
            RowFeature.In | RowFeature.IsNull | RowFeature.Update | RowFeature.UpdateLiteral |
            RowFeature.Overlaps | RowFeature.Between;
    }

    /// <summary>
    ///     The H2 version this provider generates SQL for.
    /// </summary>
    public H2SharpVersion Version { get; }

    /// <inheritdoc />
    public override string? ConnectionNamespace => "H2Sharp.Data";

    /// <inheritdoc />
    public override Type DataReaderType => typeof(H2DataReader);

    /// <inheritdoc />
    public override TableOptions SupportedTableOptions => TableOptions.NotSet;

    /// <inheritdoc />
    protected override DbConnection CreateConnectionInternal(string connectionString)
        => new H2Connection(H2SharpConnectionString.ToUrl(connectionString));

    /// <inheritdoc />
    public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
        => new H2SharpSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);

    /// <inheritdoc />
    public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
        => new H2SharpSqlOptimizer(SqlProviderFlags);

    /// <inheritdoc />
    public override ISchemaProvider GetSchemaProvider()
        => new H2SharpSchemaProvider();

    /// <inheritdoc />
    protected override IMemberTranslator CreateMemberTranslator()
        => new H2SharpMemberTranslator();

    /// <inheritdoc />
    public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
    {
        if (parameter is H2Parameter h2Parameter)
        {
            // The engine's convention: the SQL names the parameter "@name" and the engine rewrites
            // that into "?" before the statement is parsed, looking the parameter up by the name
            // without the "@". A parameter named with the "@" would not be found.
            h2Parameter.ParameterName = name;
            h2Parameter.Value         = value ?? DBNull.Value;
        }
        else
        {
            base.SetParameter(dataConnection, parameter, name, dataType, value);
        }
    }
}
