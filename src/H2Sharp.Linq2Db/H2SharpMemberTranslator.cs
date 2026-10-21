/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Translates the members linq2db knows into H2's functions.
/// </summary>
/// <remarks>
///     <para>
///         The default translation is right for H2: the common members -- the string functions, the
///         date functions, the math functions -- are the ones H2 spells the way linq2db's default
///         spells them. What H2 spells differently is handled where it is generated, in the SQL
///         builder, and not here.
///     </para>
///     <para>
///         The dates are the exception: H2 2.5 takes a part of a date with <c>EXTRACT</c> and adds
///         to a date with <c>DATEADD</c>, and the functions that take a part one by one are
///         deprecated in the reference. The translator below writes the two forms the reference
///         takes, and the reference was asked about both of them.
///     </para>
/// </remarks>
public class H2SharpMemberTranslator : ProviderMemberTranslatorDefault
{
    /// <inheritdoc />
    protected override IMemberTranslator CreateDateMemberTranslator()
        => new DateFunctionsTranslator();

    /// <summary>
    ///     The dates of H2 2.5: a part of a date comes from <c>EXTRACT</c>, and a date is moved
    ///     with <c>DATEADD</c>.
    /// </summary>
    private sealed class DateFunctionsTranslator : DateFunctionsTranslatorBase
    {
        /// <inheritdoc />
        protected override ISqlExpression? TranslateDateTimeDatePart(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            ISqlExpression dateTimeExpression,
            Sql.DateParts datepart)
        {
            var factory = translationContext.ExpressionFactory;
            var intDataType = factory.GetDbDataType(typeof(int));

            string? field = datepart switch
            {
                Sql.DateParts.Year        => "YEAR",
                Sql.DateParts.Quarter     => "QUARTER",
                Sql.DateParts.Month       => "MONTH",
                Sql.DateParts.Day         => "DAY",
                Sql.DateParts.DayOfYear   => "DOY",
                Sql.DateParts.Week        => "WEEK",
                Sql.DateParts.Hour        => "HOUR",
                Sql.DateParts.Minute      => "MINUTE",
                Sql.DateParts.Second      => "SECOND",
                Sql.DateParts.Millisecond => "MILLISECOND",
                Sql.DateParts.WeekDay     => "DOW",
                _                         => null,
            };

            if (field is null)
            {
                return null;
            }

            // EXTRACT(YEAR FROM "CreatedAt"): the field of the extract is a word of H2 and not
            // a string, and the reference was asked about the form.
            ISqlExpression extract = factory.Function(
                intDataType,
                "EXTRACT",
                factory.Expression(intDataType, field + " FROM {0}", dateTimeExpression));

            if (datepart != Sql.DateParts.WeekDay)
            {
                return extract;
            }

            // H2 counts the days of the week the ISO way: Monday is one and Sunday is seven.
            // .NET counts them the other way: Sunday is zero and Saturday is six. The days in
            // between are the same, so the remainder of the division by seven is the number
            // .NET knows.
            return factory.Binary(intDataType, extract, "%", factory.Value(intDataType, 7));
        }

        /// <inheritdoc />
        protected override ISqlExpression? TranslateDateTimeOffsetDatePart(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            ISqlExpression dateTimeExpression,
            Sql.DateParts datepart)
            => TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

        /// <inheritdoc />
        protected override ISqlExpression? TranslateDateTimeDateAdd(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            ISqlExpression dateTimeExpression,
            ISqlExpression increment,
            Sql.DateParts datepart)
        {
            var factory = translationContext.ExpressionFactory;

            string? field = datepart switch
            {
                Sql.DateParts.Year        => "YEAR",
                Sql.DateParts.Quarter     => "QUARTER",
                Sql.DateParts.Month       => "MONTH",
                Sql.DateParts.Day         => "DAY",
                Sql.DateParts.Week        => "WEEK",
                Sql.DateParts.Hour        => "HOUR",
                Sql.DateParts.Minute      => "MINUTE",
                Sql.DateParts.Second      => "SECOND",
                Sql.DateParts.Millisecond => "MILLISECOND",
                _                         => null,
            };

            if (field is null)
            {
                return null;
            }

            // DATEADD(DAY, 1, "CreatedAt"): the field of the add is a word of H2 and not a
            // string, and the reference was asked about the form.
            return factory.Function(
                factory.GetDbDataType(dateTimeExpression),
                "DATEADD",
                ParametersNullabilityType.SameAsFirstParameter,
                factory.Fragment(field),
                increment,
                dateTimeExpression);
        }

        /// <inheritdoc />
        protected override ISqlExpression? TranslateDateTimeOffsetDateAdd(
            ITranslationContext translationContext,
            TranslationFlags translationFlag,
            ISqlExpression dateTimeExpression,
            ISqlExpression increment,
            Sql.DateParts datepart)
            => TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
    }
}
