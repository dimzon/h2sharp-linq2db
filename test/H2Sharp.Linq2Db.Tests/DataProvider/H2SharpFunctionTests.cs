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
    [Trait("Category", "BlockedByPort")]
    public void A_function_comes_back_with_its_result()
    {
        using var db = H2SharpDatabase.Open(nameof(A_function_comes_back_with_its_result));

        // H2 2.5 makes a function with CREATE ALIAS and a body of Java code, and the engine
        // compiles the body when the alias is made. The port has no CREATE ALIAS yet.
        db.Execute("""
            CREATE ALIAS "SCALAR_FN" AS $$ String f(int v) { return "done"; } $$
            """);

        var result = db.Select(() => H2Functions.ScalarFunction(123));

        Assert.Equal("done", result);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void A_function_that_returns_nothing_comes_back_as_nothing()
    {
        using var db = H2SharpDatabase.Open(nameof(A_function_that_returns_nothing_comes_back_as_nothing));

        db.Execute("""
            CREATE ALIAS "VOID_FN" AS $$ void f(String s) { } $$
            """);

        var result = db.Select(() => H2Functions.VoidFunction("test"));

        // A function that returns nothing has no value, and in C# there is no type for that,
        // so the value is an object and it is null.
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void A_function_is_called_with_its_parameters()
    {
        using var db = H2SharpDatabase.Open(nameof(A_function_is_called_with_its_parameters));

        db.Execute("""
            CREATE ALIAS "PARAM_FN" AS $$ int g(int a, int b) { return a + b; } $$
            """);

        var result = db.Select(() => H2Functions.ParametersFunction(20, 22));

        Assert.Equal(42, result);
    }
}
