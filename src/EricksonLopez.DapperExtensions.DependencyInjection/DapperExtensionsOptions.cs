// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.DapperExtensions.Resilience;

namespace EricksonLopez.DapperExtensions.DependencyInjection;

/// <summary>
/// Represents configuration options for Dapper extensions services and type handler registrations.
/// </summary>
public sealed class DapperExtensionsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether standard Dapper type handlers
    /// (<see cref="DateOnly"/>, <see cref="TimeOnly"/>) are registered in Dapper's global registry automatically.
    /// </summary>
    public bool RegisterStandardTypeHandlers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether singleton instances of provider-specific
    /// <see cref="ISqlTransientErrorDetector"/> services are registered in the dependency injection container.
    /// </summary>
    public bool RegisterTransientErrorDetectors { get; set; } = true;
}
