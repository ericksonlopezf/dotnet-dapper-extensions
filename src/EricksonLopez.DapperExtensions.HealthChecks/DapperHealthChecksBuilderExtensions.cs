// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EricksonLopez.DapperExtensions.HealthChecks;

/// <summary>
/// Provides extension methods for registering Dapper database connectivity health checks on <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class DapperHealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds a Dapper database connectivity health check to the health checks builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        var options = new DapperHealthCheckOptions();
        configure?.Invoke(options);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new DapperHealthCheck(ct => connectionFactory(sp, ct), options),
            failureStatus,
            tags));
    }

    private static readonly string[] _defaultPostgreSqlTags = ["db", "postgresql", "sql"];
    private static readonly string[] _defaultSqlServerTags = ["db", "sqlserver", "sql"];
    private static readonly string[] _defaultOracleTags = ["db", "oracle", "sql"];
    private static readonly string[] _defaultMySqlTags = ["db", "mysql", "sql"];
    private static readonly string[] _defaultSqliteTags = ["db", "sqlite", "sql"];

    /// <summary>
    /// Adds a PostgreSQL database connectivity health check configured with default PostgreSQL settings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddPostgreSqlDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        => builder.AddDapperHealthCheck(name, connectionFactory, configure, failureStatus, tags ?? _defaultPostgreSqlTags);

    /// <summary>
    /// Adds a Microsoft SQL Server database connectivity health check configured with default SQL Server settings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddSqlServerDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        => builder.AddDapperHealthCheck(name, connectionFactory, configure, failureStatus, tags ?? _defaultSqlServerTags);

    /// <summary>
    /// Adds an Oracle Database connectivity health check configured with default Oracle settings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddOracleDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddDapperHealthCheck(name, connectionFactory, opt =>
        {
            opt.CommandText = "SELECT 1 FROM DUAL";
            configure?.Invoke(opt);
        }, failureStatus, tags ?? _defaultOracleTags);
    }

    /// <summary>
    /// Adds a MySQL or MariaDB database connectivity health check configured with default MySQL settings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddMySqlDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        => builder.AddDapperHealthCheck(name, connectionFactory, configure, failureStatus, tags ?? _defaultMySqlTags);

    /// <summary>
    /// Adds a SQLite database connectivity health check configured with default SQLite settings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The unique health check registration name.</param>
    /// <param name="connectionFactory">The factory that resolves an <see cref="IDbConnection"/> given the service provider.</param>
    /// <param name="configure">The optional action to configure health check options.</param>
    /// <param name="failureStatus">The health status reported upon probe failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The optional tags used for filtering health check executions.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="connectionFactory"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace</exception>
    public static IHealthChecksBuilder AddSqliteDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        => builder.AddDapperHealthCheck(name, connectionFactory, configure, failureStatus, tags ?? _defaultSqliteTags);
}
