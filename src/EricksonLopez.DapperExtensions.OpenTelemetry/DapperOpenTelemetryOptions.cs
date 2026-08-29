// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.DapperExtensions.OpenTelemetry;

/// <summary>
/// Represents configuration options for OpenTelemetry instrumentation in Dapper extensions.
/// </summary>
public sealed class DapperOpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether raw SQL statements should be included in activity tags. Defaults to <see langword="true"/>.
    /// </summary>
    public bool CaptureSqlStatements { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics recording is enabled. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum length of captured SQL statements before truncation. Defaults to 4096 characters.
    /// </summary>
    public int MaxStatementLength { get; set; } = 4096;
}
