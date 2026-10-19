/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using System.Runtime.CompilerServices;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.H2Sharp;

/// <summary>
///     Makes the provider known to linq2db by name.
/// </summary>
/// <remarks>
///     <para>
///         linq2db finds a provider either by the type that is configured or by the name a
///         connection is opened under, and it keeps a list of the providers it knows. A provider
///         that is not on that list is invisible to it, however good it is.
///     </para>
///     <para>
///         So the provider puts itself on the list when the assembly is loaded. That is what makes
///         "H2Sharp" usable as a configuration name, in a connection string, and in a test run that
///         asks for a provider by name.
///     </para>
/// </remarks>
internal static class H2SharpProviderRegistration
{
    // CA2255 says a module initializer belongs to application code. Here it is the point: a
    // provider that is not on linq2db's list of known providers is invisible to it, and the
    // registration is what puts it there. It runs once, when the assembly is loaded, and it only
    // adds a detector to a list.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register()
        => DataConnection.AddProviderDetector(
            options => string.Equals(options.ProviderName, H2SharpProviderName.H2Sharp, StringComparison.OrdinalIgnoreCase)
                ? new H2SharpProvider(H2SharpProviderName.H2Sharp, H2SharpVersion.v2_5)
                : null);
}
