/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Optimizes the statement linq2db built for H2.
/// </summary>
/// <remarks>
///     H2 needs nothing that the common optimizer does not already do, so this optimizer adds
///     nothing: the statement goes through as it was built. The functions of H2 are rewritten on
///     the way, in the visitor of the expressions.
/// </remarks>
public class H2SharpSqlOptimizer(SqlProviderFlags sqlProviderFlags) : BasicSqlOptimizer(sqlProviderFlags)
{
    /// <inheritdoc />
    public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
        => new H2SharpSqlExpressionConvertVisitor(allowModify);
}
