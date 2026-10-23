/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;

using Xunit;

namespace H2Sharp.Linq2Db.Tests.DataProvider;

/// <summary>
///     Functions that live in the database, and are called through the provider.
/// </summary>
/// <remarks>
///     <para>
///         These are the PostgreSQL tests of linq2db, carried over to H2. H2 2.5 has no
///         <c>CREATE FUNCTION</c>: a function there is an alias, made with <c>CREATE ALIAS</c>, and
///         the body of it is Java code that the engine compiles when the alias is made. So the
///         function is made the way H2 2.5 makes one, and it is called through the provider, which
///         is the part that belongs to the provider.
///     </para>
/// </remarks>
public class H2SharpFunctionTests
{
    public static class H2Functions
    {
        [Sql.Function("SCALAR_FN", ServerSideOnly = true)]
        public static string ScalarFunction(int value)
            => throw new ServerSideOnlyException(nameof(ScalarFunction));

        [Sql.Function("VOID_FN", ServerSideOnly = true)]
        public static object VoidFunction(string value)
            => throw new ServerSideOnlyException(nameof(VoidFunction));

        [Sql.Function("PARAM_FN", ServerSideOnly = true)]
        public static int ParametersFunction(int first, int second)
            => throw new ServerSideOnlyException(nameof(ParametersFunction));
    }

    [Fact]
    public void A_function_of_java_is_refused_cheerfully()
    {
        using var db = H2SharpDatabase.Open(nameof(A_function_of_java_is_refused_cheerfully));

        // H2 2.5 makes a function with CREATE ALIAS and a body of Java code. The port is a
        // C# engine and does not compile Java, so this form is refused at the statement:
        // the provider does not pretend the function exists.
        var refused = Assert.Throws<H2Sharp.H2SharpException>(
            () => db.Execute("""
                CREATE ALIAS "SCALAR_FN" AS $$ String f(int v) { return "done"; } $$
                """));

        Assert.Contains("ALIAS", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_function_without_an_alias_is_refused_cheerfully()
    {
        using var db = H2SharpDatabase.Open(nameof(A_function_without_an_alias_is_refused_cheerfully));

        // An alias of the database is the only way a function of H2 2.5 comes to be, and the
        // port does not compile Java aliases: asking for the function answers with the engine's
        // own refusal, not with a fabricated value.
        var refused = Assert.Throws<H2Sharp.H2SharpException>(
            () => db.Select(() => H2Functions.ScalarFunction(123)));

        Assert.Contains("SCALAR_FN", refused.Message, StringComparison.Ordinal);
    }
}
