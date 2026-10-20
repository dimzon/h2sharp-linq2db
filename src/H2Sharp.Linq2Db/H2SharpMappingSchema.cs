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
        // An array is a column of its own, and linq2db asks the schema for the type of a column
        // before the provider is asked to write it. The name of an array in H2 is the name of the
        // type of its elements and the word ARRAY after it, and the provider writes that name
        // itself -- the schema only has to know that the type is an array at all.
        SetDataType(typeof(int[]), DataType.Int32 | DataType.Array);
        SetDataType(typeof(long[]), DataType.Int64 | DataType.Array);
        SetDataType(typeof(short[]), DataType.Int16 | DataType.Array);
        SetDataType(typeof(byte[]), DataType.Byte | DataType.Array);
        SetDataType(typeof(sbyte[]), DataType.SByte | DataType.Array);
        SetDataType(typeof(bool[]), DataType.Boolean | DataType.Array);
        SetDataType(typeof(decimal[]), DataType.Decimal | DataType.Array);
        SetDataType(typeof(double[]), DataType.Double | DataType.Array);
        SetDataType(typeof(float[]), DataType.Single | DataType.Array);
        SetDataType(typeof(string[]), DataType.VarChar | DataType.Array);
        SetDataType(typeof(char[]), DataType.Char | DataType.Array);
        SetDataType(typeof(Guid[]), DataType.Guid | DataType.Array);
        SetDataType(typeof(DateTime[]), DataType.DateTime | DataType.Array);
        SetDataType(typeof(DateTimeOffset[]), DataType.DateTimeOffset | DataType.Array);
        SetDataType(typeof(TimeSpan[]), DataType.Time | DataType.Array);
        SetDataType(typeof(List<int>), DataType.Int32 | DataType.Array);
        SetDataType(typeof(List<string>), DataType.VarChar | DataType.Array);
    }
}
