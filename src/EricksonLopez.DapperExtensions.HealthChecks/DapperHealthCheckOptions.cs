// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.DapperExtensions.HealthChecks;

/// <summary>
/// Represents configuration options for database connectivity health checks.
/// </summary>
public sealed class DapperHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the SQL probe query used to verify database connectivity and readiness. Defaults to <c>SELECT 1;</c>.
    /// </summary>
    public string CommandText { get; set; } = "SELECT 1;";

    /// <summary>
    /// Gets or sets the latency threshold beyond which the database response is flagged as <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded"/>. Defaults to 500 milliseconds.
    /// </summary>
    public TimeSpan DegradedThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the timeout duration for the health check query execution. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
