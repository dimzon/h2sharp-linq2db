/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     A function of H2, called in a query.
/// </summary>
/// <remarks>
///     <para>
///         A function of the database is not a function of the program: it is called by the
///         database and the value comes back from there. linq2db marks such a call with
///         <see cref="Sql.FunctionAttribute" />, and a call whose arguments are known can still
///         be made by the program itself instead -- which for a function of the database is
///         wrong, because there the function may be a different one, or may not be there at all.
///     </para>
///     <para>
///         This attribute says that the call belongs to the database and nowhere else, so that
///         the query carries it and the program never tries to do it on its own.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class H2FunctionAttribute : Sql.FunctionAttribute
{
    /// <summary>
    ///     The name of the function in the database.
    /// </summary>
    public H2FunctionAttribute(string name)
        : base(name)
    {
        ServerSideOnly = true;
    }
}
