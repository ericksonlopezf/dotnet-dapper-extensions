// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.DapperExtensions.OpenTelemetry;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry instrumentation on <see cref="IServiceCollection"/>.
/// </summary>
public static class DapperOpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dapper OpenTelemetry configuration options to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">The optional action to configure OpenTelemetry options.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddDapperOpenTelemetry(
        this IServiceCollection services,
        Action<DapperOpenTelemetryOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new DapperOpenTelemetryOptions();
        configure?.Invoke(options);

        return services.AddSingleton(options);
    }
}
