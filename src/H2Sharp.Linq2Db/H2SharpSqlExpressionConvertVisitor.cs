/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Rewrites the functions of linq2db into the functions of H2.
/// </summary>
/// <remarks>
///     <para>
///         H2 2.5 finds a word in a text with <c>LOCATE</c>, and the reference was asked about
///         it. The common translation of linq2db looks for the word with <c>CHARINDEX</c>, which
///         is the function of SQL Server and not of H2, so it is rewritten here.
///     </para>
///     <para>
///         <c>LOCATE</c> counts the places from one, and so does <c>CHARINDEX</c>: the number
///         .NET knows is one less, and the one less is already in the translation of linq2db.
///     </para>
/// </remarks>
public class H2SharpSqlExpressionConvertVisitor(bool allowModify) : SqlExpressionConvertVisitor(allowModify)
{
    /// <inheritdoc />
    public override ISqlExpression ConvertSqlFunction(SqlFunction func)
        => func switch
        {
            {
                Name: "CharIndex",
                Parameters: [var searched, var text],
                Type: var type,
            } => new SqlExpression(type, "LOCATE({0}, {1})", Precedence.Primary, searched, text),

            {
                Name: "CharIndex",
                Parameters: [var searched, var text, var start],
                Type: var type,
            } => Add<int>(
                new SqlExpression(
                    type,
                    "LOCATE({0}, {1})",
                    Precedence.Primary,
                    searched,
                    (ISqlExpression)Visit(
                        new SqlFunction(
                            MappingSchema.GetDbDataType(typeof(string)),
                            "Substring",
                            text,
                            start))),
                Sub(start, 1)),

            _ => base.ConvertSqlFunction(func),
        };
}
