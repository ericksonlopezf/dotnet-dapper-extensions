// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EricksonLopez.DapperExtensions.OpenTelemetry;

/// <summary>
/// Provides diagnostic sources, meters, and semantic convention constants for OpenTelemetry instrumentation in Dapper extensions.
/// </summary>
public static class DapperDiagnostics
{
    /// <summary>
    /// The diagnostic source name for tracing and metrics.
    /// </summary>
    public const string SourceName = "EricksonLopez.DapperExtensions";

    /// <summary>
    /// The diagnostic instrumentation version.
    /// </summary>
    public const string Version = "2.0.0";

    /// <summary>
    /// Gets the shared <see cref="System.Diagnostics.ActivitySource"/> instance for distributed tracing.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, Version);

    /// <summary>
    /// Gets the shared <see cref="System.Diagnostics.Metrics.Meter"/> instance for database metrics.
    /// </summary>
    public static readonly Meter Meter = new(SourceName, Version);

    /// <summary>
    /// Gets the histogram instrument recording the execution duration of database commands in milliseconds.
    /// </summary>
    public static readonly Histogram<double> CommandDurationHistogram = Meter.CreateHistogram<double>(
        name: "db.client.commands.duration",
        unit: "ms",
        description: "Duration of database command executions in milliseconds");

    /// <summary>
    /// Gets the counter instrument tracking the total number of executed database commands.
    /// </summary>
    public static readonly Counter<long> CommandExecutionsCounter = Meter.CreateCounter<long>(
        name: "db.client.commands.count",
        unit: "{command}",
        description: "Total count of executed database commands");

    /// <summary>
    /// Gets the counter instrument tracking the total rows affected by high-throughput bulk operations.
    /// </summary>
    public static readonly Counter<long> BulkRowsCounter = Meter.CreateCounter<long>(
        name: "db.client.bulk.rows",
        unit: "{row}",
        description: "Total rows affected by bulk operations");

    /// <summary>
    /// Gets the counter instrument tracking resilience retry executions across pipelines.
    /// </summary>
    public static readonly Counter<long> ResilienceRetriesCounter = Meter.CreateCounter<long>(
        name: "db.client.resilience.retries",
        unit: "{retry}",
        description: "Total resilience retries executed for database operations");

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the database system identifier.
    /// </summary>
    public const string TagDbSystem = "db.system";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the database name.
    /// </summary>
    public const string TagDbName = "db.name";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the executed database statement.
    /// </summary>
    public const string TagDbStatement = "db.statement";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the executed database operation.
    /// </summary>
    public const string TagDbOperation = "db.operation";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the number of rows affected.
    /// </summary>
    public const string TagDbRowsAffected = "db.rows_affected";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the database server address.
    /// </summary>
    public const string TagServerAddress = "server.address";

    /// <summary>
    /// The OpenTelemetry semantic convention attribute name for the error classification type.
    /// </summary>
    public const string TagErrorType = "error.type";
}
