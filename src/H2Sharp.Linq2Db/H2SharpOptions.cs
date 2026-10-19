/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Data;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     The H2 provider's own options.
/// </summary>
/// <remarks>
///     <para>
///         H2Sharp has no bulk copy: the engine's data adapter refuses batch updates, because the
///         schema machinery behind <c>GetInsertCommand</c> is not there. The option says so rather
///         than pretending.
///     </para>
/// </remarks>
public sealed record H2SharpOptions(BulkCopyType BulkCopyType = BulkCopyType.RowByRow)
    : DataProviderOptions<H2SharpOptions>(BulkCopyType)
{
    public H2SharpOptions()
        : this(BulkCopyType.RowByRow)
    {
    }

    H2SharpOptions(H2SharpOptions original)
        : base(original)
    {
    }

    protected override IdentifierBuilder CreateID(IdentifierBuilder builder)
        => builder
            .Add(BulkCopyType)
            ;

    public override string ToString()
        => BulkCopyType.ToString();
}

/// <summary>
///     The H2 versions this provider knows about.
/// </summary>
public enum H2SharpVersion
{
    /// <summary>
    ///     Ask the server.
    /// </summary>
    AutoDetect,

    /// <summary>
    ///     H2 2.5, which is the version the port tracks.
    /// </summary>
    v2_5,
}

/// <summary>
///     The provider's name, as it appears in a configuration string.
/// </summary>
public static class H2SharpProviderName
{
    /// <summary>
    ///     The configuration string that selects this provider.
    /// </summary>
    public const string H2Sharp = "H2Sharp";
}
