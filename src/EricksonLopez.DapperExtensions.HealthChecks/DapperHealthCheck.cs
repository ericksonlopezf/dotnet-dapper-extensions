// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EricksonLopez.DapperExtensions.HealthChecks;

/// <summary>
/// Provides a database health check that validates connectivity by executing a lightweight query via Dapper.
/// </summary>
public sealed class DapperHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task<IDbConnection>> _connectionFactory;
    private readonly DapperHealthCheckOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperHealthCheck"/> class.
    /// </summary>
    /// <param name="connectionFactory">The asynchronous factory that supplies an open or openable database connection.</param>
    /// <param name="options">The optional health check configuration parameters.</param>
    /// <param name="timeProvider">The optional time provider for time virtualization.</param>
    /// <exception cref="ArgumentNullException"><paramref name="connectionFactory"/> is <see langword="null"/></exception>
    public DapperHealthCheck(
        Func<CancellationToken, Task<IDbConnection>> connectionFactory,
        DapperHealthCheckOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options ?? new DapperHealthCheckOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);

        var startTimestamp = _timeProvider.GetTimestamp();
        IDbConnection? connection = null;

        try
        {
            connection = await _connectionFactory(cts.Token).ConfigureAwait(false);

            if (connection.State != ConnectionState.Open)
            {
                if (connection is DbConnection dbConn)
                {
                    await dbConn.OpenAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    connection.Open();
                }
            }

            var command = new CommandDefinition(
                _options.CommandText,
                commandTimeout: (int)Math.Ceiling(_options.Timeout.TotalSeconds),
                cancellationToken: cts.Token);

            await connection.ExecuteScalarAsync(command).ConfigureAwait(false);

            var latency = _timeProvider.GetElapsedTime(startTimestamp);
            var data = new Dictionary<string, object>
            {
                ["latency_ms"] = latency.TotalMilliseconds,
                ["database"] = connection.Database ?? string.Empty
            };

            if (latency >= _options.DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    description: $"Database response latency ({latency.TotalMilliseconds:F1}ms) exceeded degraded threshold ({_options.DegradedThreshold.TotalMilliseconds:F1}ms).",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                description: $"Database probe succeeded in {latency.TotalMilliseconds:F1}ms.",
                data: data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                description: $"Database health probe timed out after {_options.Timeout.TotalSeconds}s.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: $"Database health probe failed: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["latency_ms"] = _timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds
                });
        }
        finally
        {
            if (connection is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                connection?.Dispose();
            }
        }
    }
}
