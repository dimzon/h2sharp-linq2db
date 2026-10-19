/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Reads the catalogue of an H2 database.
/// </summary>
/// <remarks>
///     <para>
///         H2 has a catalogue -- <c>INFORMATION_SCHEMA</c> -- and the reference reads it. The port
///         does not have it yet, and the ADO layer does not reach the engine's own catalogue
///         either. So this provider cannot enumerate anything, and it says so rather than
///         answering with an empty list that would look like a database with no tables in it.
///     </para>
/// </remarks>
public class H2SharpSchemaProvider : ISchemaProvider
{
    /// <inheritdoc />
    public DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
        => throw new NotSupportedException(
            "The H2 catalogue cannot be read yet: the port has no INFORMATION_SCHEMA, and the " +
            "engine's own catalogue is not reachable from the ADO layer. The provider does not " +
            "answer with an empty schema, because an empty schema is a statement about the " +
            "database, and this one is a statement about the port.");
}
