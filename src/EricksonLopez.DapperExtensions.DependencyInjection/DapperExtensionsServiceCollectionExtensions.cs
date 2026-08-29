// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.TypeHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.DapperExtensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring Dapper extensions services on an <see cref="IServiceCollection"/>.
/// </summary>
public static class DapperExtensionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dapper extensions services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An optional action to configure options.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddDapperExtensions(
        this IServiceCollection services,
        Action<DapperExtensionsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DapperExtensionsOptions();
        configure?.Invoke(options);

        if (options.RegisterStandardTypeHandlers)
        {
            services.AddDapperTypeHandlers();
        }

        if (options.RegisterTransientErrorDetectors)
        {
            services.AddDapperTransientErrorDetectors();
        }

        return services;
    }

    /// <summary>
    /// Registers standard Dapper type handlers (<see cref="DateOnly"/> and <see cref="TimeOnly"/>) into Dapper's runtime mapping registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddDapperTypeHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        DapperTypeHandlerRegistrar.RegisterStandardHandlers();
        return services;
    }

    /// <summary>
    /// Registers provider-specific transient error detector singletons into the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddDapperTransientErrorDetectors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<SqlServerTransientErrorDetector>(_ => SqlServerTransientErrorDetector.Default);
        services.TryAddSingleton<PostgreSqlTransientErrorDetector>(_ => PostgreSqlTransientErrorDetector.Default);
        services.TryAddSingleton<MySqlTransientErrorDetector>(_ => MySqlTransientErrorDetector.Default);
        services.TryAddSingleton<SqliteTransientErrorDetector>(_ => SqliteTransientErrorDetector.Default);
        services.TryAddSingleton<OracleTransientErrorDetector>(_ => OracleTransientErrorDetector.Default);

        return services;
    }
}
