// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace EricksonLopez.DapperExtensions.OpenTelemetry;

/// <summary>
/// Provides extension methods for instrumenting database operations with OpenTelemetry tracing and metrics.
/// </summary>
public static class OpenTelemetryDbConnectionExtensions
{
    /// <summary>
    /// Executes a SQL command instrumented with OpenTelemetry activity tracing and duration metrics.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL statement to execute.</param>
    /// <param name="param">The optional command parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="commandType">The optional command type.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<int> ExecuteWithTelemetryAsync(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var activity = StartActivity("Execute", sql, connection);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                transaction,
                commandTimeout,
                commandType,
                CommandFlags.None,
                cancellationToken);

            var rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

            stopwatch.Stop();
            RecordSuccess(activity, stopwatch.Elapsed.TotalMilliseconds, "Execute", rowsAffected);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordFailure(activity, stopwatch.Elapsed.TotalMilliseconds, "Execute", ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a SQL query instrumented with OpenTelemetry activity tracing and duration metrics.
    /// </summary>
    /// <typeparam name="T">The target entity type to map each row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="param">The optional query parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="commandType">The optional command type.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the sequence of mapped <typeparamref name="T"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<IEnumerable<T>> QueryWithTelemetryAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var activity = StartActivity("Query", sql, connection);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                transaction,
                commandTimeout,
                commandType,
                CommandFlags.Buffered,
                cancellationToken);

            var results = await connection.QueryAsync<T>(command).ConfigureAwait(false);

            stopwatch.Stop();
            RecordSuccess(activity, stopwatch.Elapsed.TotalMilliseconds, "Query", null);
            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordFailure(activity, stopwatch.Elapsed.TotalMilliseconds, "Query", ex);
            throw;
        }
    }

    /// <summary>
    /// Instruments a bulk operation delegate with OpenTelemetry activity tracing and records affected bulk rows.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="operationName">The name of the bulk operation being performed (e.g., BulkInsert, BulkUpdate).</param>
    /// <param name="targetTable">The name of the target database table.</param>
    /// <param name="bulkAction">The asynchronous bulk execution delegate returning the number of rows affected.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="bulkAction"/> is <see langword="null"/></exception>
    public static async Task<int> TraceBulkOperationAsync(
        this IDbConnection connection,
        string operationName,
        string targetTable,
        Func<CancellationToken, Task<int>> bulkAction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(bulkAction);

        using var activity = DapperDiagnostics.ActivitySource.StartActivity(
            name: $"Bulk {operationName} {targetTable}",
            kind: ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag(DapperDiagnostics.TagDbSystem, ResolveDbSystem(connection));
            activity.SetTag(DapperDiagnostics.TagDbOperation, operationName);
            activity.SetTag(DapperDiagnostics.TagDbName, connection.Database);
            activity.SetTag("db.table", targetTable);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rowsAffected = await bulkAction(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag(DapperDiagnostics.TagDbRowsAffected, rowsAffected);

            DapperDiagnostics.CommandDurationHistogram.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operationName));

            DapperDiagnostics.BulkRowsCounter.Add(
                rowsAffected,
                new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operationName));

            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordFailure(activity, stopwatch.Elapsed.TotalMilliseconds, operationName, ex);
            throw;
        }
    }

    private static Activity? StartActivity(string operation, string sql, IDbConnection connection)
    {
        var activity = DapperDiagnostics.ActivitySource.StartActivity(
            name: $"{operation} {connection.Database}",
            kind: ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag(DapperDiagnostics.TagDbSystem, ResolveDbSystem(connection));
            activity.SetTag(DapperDiagnostics.TagDbOperation, operation);
            activity.SetTag(DapperDiagnostics.TagDbName, connection.Database);
            activity.SetTag(DapperDiagnostics.TagDbStatement, sql);
        }

        return activity;
    }

    private static void RecordSuccess(Activity? activity, double durationMs, string operation, int? rowsAffected)
    {
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            if (rowsAffected.HasValue)
            {
                activity.SetTag(DapperDiagnostics.TagDbRowsAffected, rowsAffected.Value);
            }
        }

        DapperDiagnostics.CommandDurationHistogram.Record(
            durationMs,
            new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operation));

        DapperDiagnostics.CommandExecutionsCounter.Add(
            1,
            new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operation),
            new KeyValuePair<string, object?>("status", "ok"));
    }

    private static void RecordFailure(Activity? activity, double durationMs, string operation, Exception ex)
    {
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.SetTag(DapperDiagnostics.TagErrorType, ex.GetType().FullName);
            activity.AddException(ex);
        }

        DapperDiagnostics.CommandDurationHistogram.Record(
            durationMs,
            new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operation));

        DapperDiagnostics.CommandExecutionsCounter.Add(
            1,
            new KeyValuePair<string, object?>(DapperDiagnostics.TagDbOperation, operation),
            new KeyValuePair<string, object?>("status", "error"));
    }

    private static string ResolveDbSystem(IDbConnection connection)
    {
        var typeName = connection.GetType().Name.ToLowerInvariant();
        if (typeName.Contains("npgsql")) return "postgresql";
        if (typeName.Contains("sqlconnection")) return "mssql";
        if (typeName.Contains("mysql")) return "mysql";
        if (typeName.Contains("mariadb")) return "mariadb";
        if (typeName.Contains("sqlite")) return "sqlite";
        if (typeName.Contains("oracle")) return "oracle";
        return "other_sql";
    }
}
