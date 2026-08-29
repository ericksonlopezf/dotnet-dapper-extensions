// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Polly.Adapters;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides factory methods for creating pre-configured database resilience pipelines.
/// </summary>
/// <remarks>
/// <para>
/// All pipelines follow the principle that the resilience pipeline must wrap the entire transactional unit
/// (BeginUnitOfWork → Execute → Commit). Never retry individual statements inside an open transaction.
/// </para>
/// </remarks>
public static class SqlResilienceDefaults
{
    /// <summary>
    /// Creates a standard resilience pipeline configured with retry and timeout strategies.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>
    /// A <see cref="ResiliencePipeline"/> configured with:
    /// <list type="bullet">
    ///   <item>3 retry attempts with exponential backoff (1s → 2s → 4s) and jitter</item>
    ///   <item>30-second total timeout</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static ResiliencePipeline Standard(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    /// <summary>
    /// Creates a resilience pipeline combining retry, circuit breaker, and timeout strategies to protect against cascading database outages.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="failureRatio">The failure ratio (from 0.0 to 1.0) that causes the circuit to open. Defaults to 0.5.</param>
    /// <param name="samplingDuration">The time window over which failure ratios are evaluated. Defaults to 10 seconds.</param>
    /// <param name="minimumThroughput">The minimum throughput required within the sampling window before the circuit evaluates. Defaults to 10.</param>
    /// <param name="breakDuration">The duration the circuit remains open before transitioning to a half-open state. Defaults to 30 seconds.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> configured with timeout, retry, and circuit breaker.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static ResiliencePipeline StandardWithCircuitBreaker(
        ISqlTransientErrorDetector detector,
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(detector.IsTransient)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureRatio,
                SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(10),
                MinimumThroughput = minimumThroughput,
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    /// <summary>
    /// Creates an aggressive resilience pipeline optimized for high-availability scenarios.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>
    /// A <see cref="ResiliencePipeline"/> configured with:
    /// <list type="bullet">
    ///   <item>5 retry attempts with exponential backoff (500ms → 1s → 2s → 4s → 8s) and jitter</item>
    ///   <item>60-second total timeout</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static ResiliencePipeline Aggressive(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(60)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    /// <summary>
    /// Creates a conservative resilience pipeline designed for workloads where retries are costly.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>
    /// A <see cref="ResiliencePipeline"/> configured with:
    /// <list type="bullet">
    ///   <item>1 retry attempt after a 5-second wait</item>
    ///   <item>120-second total timeout</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static ResiliencePipeline Conservative(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(120)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    /// <summary>
    /// Creates a typed resilience pipeline configured with retry and timeout strategies.
    /// </summary>
    /// <typeparam name="T">The result type produced by operations executed through the pipeline.</typeparam>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline{T}"/> configured with retry and timeout settings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    [UnconditionalSuppressMessage("Trimming", "IL2091",
        Justification = "Polly's typed generic pipeline builder (AddRetry<TResult>) requires DynamicallyAccessedMemberTypes.All on TResult. " +
                        "TResult in this context represents a Dapper query result type, not an AOT-critical type boundary. " +
                        "Applications using NativeAOT should use the untyped ResiliencePipeline overload. ADR-006.")]
    public static ResiliencePipeline<T> Standard<T>(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder<T>
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<T>().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    /// <summary>
    /// Creates a typed resilience pipeline combining retry, circuit breaker, and timeout strategies.
    /// </summary>
    /// <typeparam name="T">The result type produced by operations executed through the pipeline.</typeparam>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="failureRatio">The failure ratio (from 0.0 to 1.0) that causes the circuit to open. Defaults to 0.5.</param>
    /// <param name="samplingDuration">The time window over which failure ratios are evaluated. Defaults to 10 seconds.</param>
    /// <param name="minimumThroughput">The minimum throughput required within the sampling window before the circuit evaluates. Defaults to 10.</param>
    /// <param name="breakDuration">The duration the circuit remains open before transitioning to a half-open state. Defaults to 30 seconds.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline{T}"/> configured with retry, circuit breaker, and timeout settings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    [UnconditionalSuppressMessage("Trimming", "IL2091",
        Justification = "Polly's typed generic pipeline builder (AddRetry<TResult>, AddCircuitBreaker<TResult>) requires DynamicallyAccessedMemberTypes.All on TResult. " +
                        "TResult in this context represents a Dapper query result type, not an AOT-critical type boundary. " +
                        "Applications using NativeAOT should use the untyped ResiliencePipeline overload. ADR-006.")]
    public static ResiliencePipeline<T> StandardWithCircuitBreaker<T>(
        ISqlTransientErrorDetector detector,
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return new ResiliencePipelineBuilder<T>
        {
            TimeProvider = timeProvider ?? TimeProvider.System
        }
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<T>().Handle<Exception>(detector.IsTransient)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = failureRatio,
                SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(10),
                MinimumThroughput = minimumThroughput,
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<T>().Handle<Exception>(detector.IsTransient)
            })
            .Build();
    }

    // ─── Convenience provider shortcuts ────────────────────────────────────────

    /// <summary>
    /// Creates a standard resilience pipeline pre-configured for Microsoft SQL Server and Azure SQL transient errors.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> pre-configured for SQL Server.</returns>
    public static ResiliencePipeline ForSqlServer(TimeProvider? timeProvider = null)
        => Standard(SqlServerTransientErrorDetector.Default, timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline with circuit breaker pre-configured for Microsoft SQL Server and Azure SQL.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> with circuit breaker pre-configured for SQL Server.</returns>
    public static ResiliencePipeline ForSqlServerWithCircuitBreaker(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreaker(SqlServerTransientErrorDetector.Default, timeProvider: timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline pre-configured for PostgreSQL and CockroachDB transient errors.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> pre-configured for PostgreSQL.</returns>
    public static ResiliencePipeline ForPostgreSql(TimeProvider? timeProvider = null)
        => Standard(PostgreSqlTransientErrorDetector.Default, timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline with circuit breaker pre-configured for PostgreSQL and CockroachDB.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> with circuit breaker pre-configured for PostgreSQL.</returns>
    public static ResiliencePipeline ForPostgreSqlWithCircuitBreaker(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreaker(PostgreSqlTransientErrorDetector.Default, timeProvider: timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline pre-configured for MySQL and MariaDB transient errors.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> pre-configured for MySQL.</returns>
    public static ResiliencePipeline ForMySql(TimeProvider? timeProvider = null)
        => Standard(MySqlTransientErrorDetector.Default, timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline with circuit breaker pre-configured for MySQL and MariaDB.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> with circuit breaker pre-configured for MySQL.</returns>
    public static ResiliencePipeline ForMySqlWithCircuitBreaker(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreaker(MySqlTransientErrorDetector.Default, timeProvider: timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline pre-configured for SQLite transient errors.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> pre-configured for SQLite.</returns>
    public static ResiliencePipeline ForSqlite(TimeProvider? timeProvider = null)
        => Standard(SqliteTransientErrorDetector.Default, timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline with circuit breaker pre-configured for SQLite.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> with circuit breaker pre-configured for SQLite.</returns>
    public static ResiliencePipeline ForSqliteWithCircuitBreaker(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreaker(SqliteTransientErrorDetector.Default, timeProvider: timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline pre-configured for Oracle Database transient errors.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> pre-configured for Oracle Database.</returns>
    public static ResiliencePipeline ForOracle(TimeProvider? timeProvider = null)
        => Standard(OracleTransientErrorDetector.Default, timeProvider);

    /// <summary>
    /// Creates a standard resilience pipeline with circuit breaker pre-configured for Oracle Database.
    /// </summary>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>A <see cref="ResiliencePipeline"/> with circuit breaker pre-configured for Oracle Database.</returns>
    public static ResiliencePipeline ForOracleWithCircuitBreaker(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreaker(OracleTransientErrorDetector.Default, timeProvider: timeProvider);

    // ─── Canonical EricksonLopez.Resilience overloads ───────────────────────────
    // These methods return IResiliencePipeline from EricksonLopez.Resilience.Abstractions,
    // wrapping the Polly pipeline via PollyResiliencePipeline adapter.
    // DapperExtensions classifies the error context (ISqlTransientErrorDetector);
    // EricksonLopez.Resilience orchestrates the retry/circuit-breaker policy.

    /// <summary>
    /// Creates a standard <see cref="IResiliencePipeline"/> configured with retry and timeout strategies.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="pipelineName">The logical name assigned to the pipeline instance.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>An <see cref="IResiliencePipeline"/> wrapping the standard Polly pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static IResiliencePipeline StandardPipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-standard",
        TimeProvider? timeProvider = null)
        => new PollyResiliencePipeline(pipelineName, Standard(detector, timeProvider));

    /// <summary>
    /// Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="pipelineName">The logical name assigned to the pipeline instance.</param>
    /// <param name="failureRatio">The failure ratio (from 0.0 to 1.0) that causes the circuit to open. Defaults to 0.5.</param>
    /// <param name="samplingDuration">The time window over which failure ratios are evaluated. Defaults to 10 seconds.</param>
    /// <param name="minimumThroughput">The minimum throughput required within the sampling window. Defaults to 10.</param>
    /// <param name="breakDuration">The duration the circuit remains open before transitioning to half-open. Defaults to 30 seconds.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>An <see cref="IResiliencePipeline"/> wrapping the circuit-breaker pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static IResiliencePipeline StandardWithCircuitBreakerPipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-standard-cb",
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null)
        => new PollyResiliencePipeline(pipelineName, StandardWithCircuitBreaker(detector, failureRatio, samplingDuration, minimumThroughput, breakDuration, timeProvider));

    /// <summary>
    /// Creates an aggressive <see cref="IResiliencePipeline"/> optimized for high-availability scenarios.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="pipelineName">The logical name assigned to the pipeline instance.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>An <see cref="IResiliencePipeline"/> wrapping the aggressive Polly pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static IResiliencePipeline AggressivePipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-aggressive",
        TimeProvider? timeProvider = null)
        => new PollyResiliencePipeline(pipelineName, Aggressive(detector, timeProvider));

    /// <summary>
    /// Creates a conservative <see cref="IResiliencePipeline"/> for workloads where retries are costly.
    /// </summary>
    /// <param name="detector">The transient error detector used to evaluate database exceptions.</param>
    /// <param name="pipelineName">The logical name assigned to the pipeline instance.</param>
    /// <param name="timeProvider">The optional custom time provider for testing and time virtualization.</param>
    /// <returns>An <see cref="IResiliencePipeline"/> wrapping the conservative Polly pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static IResiliencePipeline ConservativePipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-conservative",
        TimeProvider? timeProvider = null)
        => new PollyResiliencePipeline(pipelineName, Conservative(detector, timeProvider));

    // ─── Provider-specific canonical shortcuts ───────────────────────────────

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> pre-configured for SQL Server.</summary>
    public static IResiliencePipeline ForSqlServerPipeline(TimeProvider? timeProvider = null)
        => StandardPipeline(SqlServerTransientErrorDetector.Default, "sql-sqlserver-standard", timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker for SQL Server.</summary>
    public static IResiliencePipeline ForSqlServerWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreakerPipeline(SqlServerTransientErrorDetector.Default, "sql-sqlserver-cb", timeProvider: timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> pre-configured for PostgreSQL.</summary>
    public static IResiliencePipeline ForPostgreSqlPipeline(TimeProvider? timeProvider = null)
        => StandardPipeline(PostgreSqlTransientErrorDetector.Default, "sql-postgresql-standard", timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker for PostgreSQL.</summary>
    public static IResiliencePipeline ForPostgreSqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreakerPipeline(PostgreSqlTransientErrorDetector.Default, "sql-postgresql-cb", timeProvider: timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> pre-configured for MySQL.</summary>
    public static IResiliencePipeline ForMySqlPipeline(TimeProvider? timeProvider = null)
        => StandardPipeline(MySqlTransientErrorDetector.Default, "sql-mysql-standard", timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker for MySQL.</summary>
    public static IResiliencePipeline ForMySqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreakerPipeline(MySqlTransientErrorDetector.Default, "sql-mysql-cb", timeProvider: timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> pre-configured for SQLite.</summary>
    public static IResiliencePipeline ForSqlitePipeline(TimeProvider? timeProvider = null)
        => StandardPipeline(SqliteTransientErrorDetector.Default, "sql-sqlite-standard", timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker for SQLite.</summary>
    public static IResiliencePipeline ForSqliteWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreakerPipeline(SqliteTransientErrorDetector.Default, "sql-sqlite-cb", timeProvider: timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> pre-configured for Oracle Database.</summary>
    public static IResiliencePipeline ForOraclePipeline(TimeProvider? timeProvider = null)
        => StandardPipeline(OracleTransientErrorDetector.Default, "sql-oracle-standard", timeProvider);

    /// <summary>Creates a standard <see cref="IResiliencePipeline"/> with circuit breaker for Oracle Database.</summary>
    public static IResiliencePipeline ForOracleWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)
        => StandardWithCircuitBreakerPipeline(OracleTransientErrorDetector.Default, "sql-oracle-cb", timeProvider: timeProvider);
}



