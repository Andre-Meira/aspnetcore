// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection.Internal;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// A simplified default implementation of <see cref="IActivator"/> that understands
/// how to call ctors which take <see cref="IServiceProvider"/>.
/// </summary>
internal class SimpleActivator : IActivator
{
    private static readonly Type[] _serviceProviderTypeArray = { typeof(IServiceProvider) };

    /// <summary>
    /// A default <see cref="SimpleActivator"/> whose wrapped <see cref="IServiceProvider"/> is null.
    /// </summary>
    internal static readonly SimpleActivator DefaultWithoutServices = new SimpleActivator(null);

    private readonly IServiceProvider? _services;

    public SimpleActivator(IServiceProvider? services)
    {
        _services = services;
    }

    [RequiresUnreferencedCode(TrimmerWarning.Message)]
    public virtual object CreateInstance(Type expectedBaseType, string implementationTypeName)
    {
        // Would the assignment even work?
        var implementationType = Type.GetType(implementationTypeName, throwOnError: true)!;
        expectedBaseType.AssertIsAssignableFrom(implementationType);

        // If no IServiceProvider was specified, prefer .ctor() [if it exists]
        if (_services == null)
        {
            var ctorParameterless = implementationType.GetConstructor(Type.EmptyTypes);
            if (ctorParameterless != null)
            {
                return Activator.CreateInstance(implementationType)!;
            }
        }

        // If an IServiceProvider was specified or if .ctor() doesn't exist, prefer .ctor(IServiceProvider) [if it exists]
        var ctorWhichTakesServiceProvider = implementationType.GetConstructor(_serviceProviderTypeArray);
        if (ctorWhichTakesServiceProvider != null)
        {
            return ctorWhichTakesServiceProvider.Invoke(new[] { _services });
        }

        // Finally, prefer .ctor() as an ultimate fallback.
        // This will throw if the ctor cannot be called.
        return Activator.CreateInstance(implementationType)!;
    }
}
