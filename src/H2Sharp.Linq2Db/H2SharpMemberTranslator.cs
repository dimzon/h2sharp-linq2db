/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Expressions;
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
    /// <summary>
    ///     Creates the translator.
    /// </summary>
    public H2SharpMemberTranslator()
    {
        // An element in an array column is asked with ARRAY_CONTAINS, the function H2
        // 2.5 has for it: operators of containment H2 does not have, and the reference
        // answers the nulls the way .NET's Contains does.
        //
        // The call over an array binds to MemoryExtensions.Contains, whose span
        // parameter cannot stand in an expression tree, so the pattern cannot be
        // written as a lambda: the registration is entered by its key. The key is
        // what MemberHelper.GetMemberInfoWithType reads from the call: the declaring
        // type and the generic method definition.
        Registration.RegisterMethod(
            () => Enumerable.Contains(Enumerable.Empty<int>(), 0),
            TranslateArrayContains,
            isGenericTypeMatch: true);
        Registration.RegisterMethod(
            () => new List<int>().Contains(0),
            TranslateArrayContains,
            isGenericTypeMatch: true);
        Registration.RegisterMethod(
            () => Enumerable.SequenceEqual(Enumerable.Empty<int>(), Enumerable.Empty<int>()),
            TranslateArrayEqual,
            isGenericTypeMatch: true);
        Registration.RegisterConstructor(
            (int year, int month, int day) => new DateTime(year, month, day),
            TranslateNewDateTime);
        Registration.RegisterConstructor(
            (int year, int month, int day, int hour, int minute, int second) => new DateTime(year, month, day, hour, minute, second),
            TranslateNewDateTime);
        Registration.RegisterConstructor(
            (int year, int month, int day) => new DateOnly(year, month, day),
            TranslateNewDateTime);

        var spanContains = FindMemoryExtensionsMethod(nameof(MemoryExtensions.Contains), 2);

        if (spanContains is not null)
        {
            var key = new MemberHelper.MemberInfoWithType(
                typeof(MemoryExtensions), spanContains.GetGenericMethodDefinition());
            var translations = (Dictionary<MemberHelper.MemberInfoWithType, TranslationRegistration.TranslateFunc>)
                typeof(TranslationRegistration).GetField("_translations", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .GetValue(Registration)!;
            translations[key] = (context, member, flags) =>
                TranslateArrayContains(context, (MethodCallExpression)member, flags);
        }

        var spanSequenceEqual = FindMemoryExtensionsMethod(nameof(MemoryExtensions.SequenceEqual), 2);

        if (spanSequenceEqual is not null)
        {
            var key = new MemberHelper.MemberInfoWithType(
                typeof(MemoryExtensions), spanSequenceEqual.GetGenericMethodDefinition());
            var translations = (Dictionary<MemberHelper.MemberInfoWithType, TranslationRegistration.TranslateFunc>)
                typeof(TranslationRegistration).GetField("_translations", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .GetValue(Registration)!;
            translations[key] = (context, member, flags) =>
                TranslateArrayEqual(context, (MethodCallExpression)member, flags);
        }

        // H2 2.5 has no &, |, ^, << or >>: the reference reads BITAND, BITOR, BITXOR,
        // LSHIFT and RSHIFT instead. The translator writes the functions.
        Registration.RegisterBinary<int, int, int>((a, b) => a & b, BitFunction("BITAND"));
        Registration.RegisterBinary<int, int, int>((a, b) => a | b, BitFunction("BITOR"));
        Registration.RegisterBinary<int, int, int>((a, b) => a ^ b, BitFunction("BITXOR"));
        Registration.RegisterBinary<int, int, int>((a, b) => a << b, BitFunction("LSHIFT"));
        Registration.RegisterBinary<int, int, int>((a, b) => a >> b, BitFunction("RSHIFT"));
        Registration.RegisterBinary<long, long, long>((a, b) => a & b, BitFunction("BITAND"));
        Registration.RegisterBinary<long, long, long>((a, b) => a | b, BitFunction("BITOR"));
        Registration.RegisterBinary<long, long, long>((a, b) => a ^ b, BitFunction("BITXOR"));
        Registration.RegisterBinary<long, int, long>((a, b) => a << b, BitFunction("LSHIFT"));
        Registration.RegisterBinary<long, int, long>((a, b) => a >> b, BitFunction("RSHIFT"));

        // The day of the week is counted the ISO way by H2 (Monday is one and Sunday
        // is seven) and the .NET way by the program (Sunday is zero). The base
        // translation decrements what the date part answers, so going through it
        // would count twice: the remainder of the division by seven is asked here
        // directly instead.
        Registration.RegisterMember(
            (DateTime date) => date.DayOfWeek, TranslateDayOfWeek);
        Registration.RegisterMember(
            (DateTimeOffset date) => date.DayOfWeek, TranslateDayOfWeek);
        Registration.RegisterMember(
            (DateOnly date) => date.DayOfWeek, TranslateDayOfWeek);
    }

    /// <summary>
    ///     Makes a translator of a bit operation into the function of H2 with the name.
    /// </summary>
    private static TranslationRegistration.TranslateBinaryFunc BitFunction(string name)
        => (translationContext, binaryExpression, translationFlags) =>
        {
            if (translationContext.CanBeEvaluatedOnClient(binaryExpression))
            {
                return null;
            }

            var left = translationContext.Translate(binaryExpression.Left, translationFlags);
            var right = translationContext.Translate(binaryExpression.Right, translationFlags);

            if (left is not SqlPlaceholderExpression { Sql: { } leftSql }
                || right is not SqlPlaceholderExpression { Sql: { } rightSql })
            {
                return null;
            }

            var factory = translationContext.ExpressionFactory;
            var function = new SqlFunction(
                factory.GetDbDataType(binaryExpression.Type),
                name,
                leftSql,
                rightSql);

            return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, function, binaryExpression);
        };

    /// <summary>
    ///     The ISO day of the week as the .NET day of the week.
    /// </summary>
    private static Expression? TranslateDayOfWeek(
        ITranslationContext translationContext,
        MemberExpression memberExpression,
        TranslationFlags translationFlags)
    {
        if (translationContext.CanBeEvaluatedOnClient(memberExpression)
            || memberExpression.Expression is null)
        {
            return null;
        }

        var translated = translationContext.Translate(memberExpression.Expression, translationFlags);
        if (translated is not SqlPlaceholderExpression { Sql: { } dateSql })
        {
            return null;
        }

        var factory = translationContext.ExpressionFactory;
        var intDataType = factory.GetDbDataType(typeof(int));
        var dow = factory.Function(
            intDataType,
            "EXTRACT",
            factory.Expression(intDataType, "DOW FROM {0}", dateSql));
        var sundayZero = factory.Binary(intDataType, dow, "%", factory.Value(intDataType, 7));

        return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sundayZero, memberExpression);
    }

    /// <summary>
    ///     Finds a generic <c>MemoryExtensions</c> method over read-only spans by its name.
    /// </summary>
    private static MethodInfo? FindMemoryExtensionsMethod(string name, int parameterCount)
        => typeof(MemoryExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is { IsGenericMethodDefinition: true } && m.Name == name)
            .Select(m => (Method: m, Parameters: m.GetParameters()))
            .Where(t => t.Parameters.Length == parameterCount
                && t.Parameters.All(p => p.ParameterType is { IsGenericType: true, Name: "ReadOnlySpan`1" }
                    || p.ParameterType.IsGenericParameter))
            .Select(t => t.Method)
            .SingleOrDefault();

    /// <summary>
    ///     The array underneath a span conversion: the overload resolution wraps the array
    ///     into a span, and the array is what the query asks.
    /// </summary>
    private static Expression UnwrapArray(Expression source)
    {
        while (true)
        {
            if (source is UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                    Operand: var operand
                })
            {
                source = operand;
                continue;
            }

            if (source is MethodCallExpression
                {
                    Method.IsSpecialName: true,
                    Method.Name: "op_Implicit" or "op_Explicit",
                    Arguments: [var converted]
                })
            {
                source = converted;
                continue;
            }

            return source;
        }
    }

    private static bool IsArrayModel(Expression source)
    {
        var unwrapped = UnwrapArray(source);
        return unwrapped.Type.IsArray
            || unwrapped.Type.IsGenericType && unwrapped.Type.GetGenericTypeDefinition() == typeof(List<>);
    }
    private static Expression? TranslateArrayContains(
        ITranslationContext translationContext,
        MethodCallExpression methodCall,
        TranslationFlags translationFlags)
    {
        if (translationContext.CanBeEvaluatedOnClient(methodCall))
        {
            return null;
        }

        // Enumerable.Contains(source, value[, comparer]) is static, List<T>.Contains is not.
        if (methodCall.Arguments.Count > 2)
        {
            return null;
        }

        var source = UnwrapArray(methodCall.Object ?? methodCall.Arguments[0]);
        var value = methodCall.Object is null ? methodCall.Arguments[1] : methodCall.Arguments[0];

        if (!IsArrayModel(source))
        {
            return null;
        }

        if (translationContext.Translate(source) is not SqlPlaceholderExpression { Sql: { } sourceSql })
        {
            return null;
        }

        if (sourceSql is SqlParameter)
        {
            return null;
        }

        if (translationContext.Translate(value) is not SqlPlaceholderExpression { Sql: { } valueSql })
        {
            return null;
        }

        var factory = translationContext.ExpressionFactory;
        var contains = new SqlFunction(
            factory.GetDbDataType(typeof(bool)),
            "ARRAY_CONTAINS",
            sourceSql,
            valueSql);

        return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, contains, methodCall);
    }

    /// <summary>
    ///     Translates <c>array1.SequenceEqual(array2)</c> into <c>array1 = array2</c>.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 compares whole arrays with <c>=</c>, and the reference answers what .NET
    ///     answers. A parameter array cannot be bound by the engine yet, but the form is
    ///     still the one H2 reads, so it is written and the engine answers for it.
    /// </remarks>
    private static Expression? TranslateArrayEqual(
        ITranslationContext translationContext,
        MethodCallExpression methodCall,
        TranslationFlags translationFlags)
    {
        if (translationContext.CanBeEvaluatedOnClient(methodCall)
            || methodCall.Arguments.Count != 2)
        {
            return null;
        }

        var left = UnwrapArray(methodCall.Arguments[0]);
        var right = UnwrapArray(methodCall.Arguments[1]);

        if (!IsArrayModel(left) || !IsArrayModel(right))
        {
            return null;
        }

        if (translationContext.Translate(left, translationFlags) is not SqlPlaceholderExpression { Sql: { } leftSql }
            || translationContext.Translate(right, translationFlags) is not SqlPlaceholderExpression { Sql: { } rightSql })
        {
            return null;
        }

        var factory = translationContext.ExpressionFactory;
        var equal = new SqlBinaryExpression(
            factory.GetDbDataType(typeof(bool)), leftSql, "=", rightSql, Precedence.Comparison);

        return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, equal, methodCall);
    }

    /// <summary>
    ///     Translates <c>new DateTime(y, mo, d[, h, mi, s])</c> and <c>new DateOnly(y, m, d)</c>
    ///     into a cast of the concatenated parts: H2 2.5 has no <c>make_date</c> or
    ///     <c>make_timestamp</c>, but reads <c>CAST('2024-1-15' AS TIMESTAMP)</c> (and
    ///     <c>AS DATE</c>) the way a literal reads, padding included.
    /// </summary>
    private static Expression? TranslateNewDateTime(
        ITranslationContext translationContext,
        Expression newExpression,
        TranslationFlags translationFlags)
    {
        if (newExpression is not NewExpression newCall
            || translationContext.CanBeEvaluatedOnClient(newCall))
        {
            return null;
        }

        var factory = translationContext.ExpressionFactory;
        var stringType = factory.GetDbDataType(typeof(string));
        var parts = new List<ISqlExpression>(newCall.Arguments.Count * 2 - 1);

        for (var i = 0; i < newCall.Arguments.Count; i++)
        {
            // A date is year-month-day, a moment adds the time after a space:
            // the separator after the day is a space when the time follows.
            if (i > 0)
            {
                var separator = i < 3 ? "-" : i == 3 ? " " : ":";
                parts.Add(factory.Value(stringType, separator));
            }

            if (translationContext.Translate(newCall.Arguments[i], translationFlags) is not SqlPlaceholderExpression { Sql: { } partSql })
            {
                return null;
            }

            parts.Add(partSql);
        }

        var text = new SqlFunction(stringType, "CONCAT", parts.ToArray());

        var isDate = newCall.Type == typeof(DateOnly);
        var targetType = isDate ? factory.GetDbDataType(typeof(DateOnly)) : factory.GetDbDataType(typeof(DateTime));
        var cast = factory.Function(
            targetType,
            "CAST",
            factory.Expression(targetType, "{0} AS " + (isDate ? "DATE" : "TIMESTAMP"), text));

        return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, cast, newCall);
    }

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
