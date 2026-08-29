// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides transient error detection for PostgreSQL databases.
/// </summary>
/// <remarks>
/// Uses PostgreSQL SQLSTATE codes per the PostgreSQL documentation.
/// <list type="bullet">
///   <item>40001 — serialization_failure (REPEATABLE READ / SERIALIZABLE conflicts)</item>
///   <item>40P01 — deadlock_detected</item>
///   <item>08006 — connection_failure</item>
///   <item>08001 — sqlclient_unable_to_establish_sqlconnection</item>
///   <item>08004 — sqlserver_rejected_establishment_of_sqlconnection</item>
///   <item>57P01 — admin_shutdown (server restarting)</item>
///   <item>57P02 — crash_shutdown</item>
///   <item>57P03 — cannot_connect_now (startup in progress)</item>
/// </list>
/// </remarks>
public sealed class PostgreSqlTransientErrorDetector : ISqlTransientErrorDetector
{
    private static readonly string[] _transientSqlStates = new[]
    {
        "40001", // serialization_failure
        "40P01", // deadlock_detected
        "08006", // connection_failure
        "08001", // sqlclient_unable_to_establish_sqlconnection
        "08004", // sqlserver_rejected_establishment_of_sqlconnection
        "57P01", // admin_shutdown
        "57P02", // crash_shutdown
        "57P03", // cannot_connect_now
        "53300", // too_many_connections
        "53400", // configuration_limit_exceeded
    };

    private static readonly PostgreSqlTransientErrorDetector _default = new();

    /// <summary>
    /// Gets the default singleton instance of the <see cref="PostgreSqlTransientErrorDetector"/> class.
    /// </summary>
    public static PostgreSqlTransientErrorDetector Default => _default;

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        if (exception == null) return false;

        var ex = exception;
        while (ex != null)
        {
            if (ex is System.Data.Common.DbException dbEx)
            {
                if (dbEx.IsTransient)
                    return true;

                if (dbEx.SqlState is string sqlState && System.Array.IndexOf(_transientSqlStates, sqlState) >= 0)
                    return true;

                if (dbEx.Data["SqlState"] is string dataSqlState && System.Array.IndexOf(_transientSqlStates, dataSqlState) >= 0)
                    return true;
            }

            ex = ex.InnerException;
        }

        return IsTransientMessage(exception.Message);
    }

    private static bool IsTransientMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        return message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
            || message.Contains("serialization", StringComparison.OrdinalIgnoreCase);
    }
}


