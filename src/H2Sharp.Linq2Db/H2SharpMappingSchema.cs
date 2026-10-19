/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;

using LinqToDB.Common;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The H2 data types, as linq2db sees them.
/// </summary>
/// <remarks>
///     The names are the ones the frozen H2 documentation in the engine's repository lists: a type
///     the documentation does not have is not a type the provider invents.
/// </remarks>
public class H2SharpMappingSchema : MappingSchema
{
    /// <summary>
    ///     Creates the mapping schema.
    /// </summary>
    public H2SharpMappingSchema()
        // The ordinary mappings, not an empty schema under a configuration name: linq2db decides
        // from them that a member is a column, and an entity whose members are not columns has
        // nothing to insert.
        : base((string?)null)
    {
    }
}
