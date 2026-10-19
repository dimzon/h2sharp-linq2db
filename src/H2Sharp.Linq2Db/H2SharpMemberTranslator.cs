/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Translates the members linq2db knows into H2's functions.
/// </summary>
/// <remarks>
///     The default translation is right for H2: the common members -- the string functions, the
///     date functions, the math functions -- are the ones H2 spells the way linq2db's default
///     spells them. What H2 spells differently is handled where it is generated, in the SQL
///     builder, and not here.
/// </remarks>
public class H2SharpMemberTranslator : ProviderMemberTranslatorDefault
{
    /// <inheritdoc />
    protected override IMemberTranslator CreateDateMemberTranslator()
        // H2 takes a part of a date with its own function per part -- YEAR, MONTH, DAY -- and
        // adds to a date with DATEADD, not with the arithmetic PostgreSQL uses. That translation
        // is not written yet, and until it is, a date member is not translated rather than
        // translated wrongly.
        => new DateFunctionsTranslator();

    private sealed class DateFunctionsTranslator : IMemberTranslator
    {
        public Expression? Translate(
            ITranslationContext translationContext,
            Expression memberExpression,
            TranslationFlags translationFlags)
            => null;
    }
}
