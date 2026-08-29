// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides transient error detection for MySQL and MariaDB databases.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>1213 — ER_LOCK_DEADLOCK (deadlock found)</item>
///   <item>1205 — ER_LOCK_WAIT_TIMEOUT (lock wait timeout exceeded)</item>
///   <item>2006 — CR_SERVER_GONE_ERROR (MySQL server has gone away)</item>
///   <item>2013 — CR_SERVER_LOST (lost connection to server)</item>
///   <item>1158 — ER_NET_READ_ERROR (network read error)</item>
///   <item>1159 — ER_NET_READ_INTERRUPTED (connection read interrupted)</item>
///   <item>1160 — ER_NET_ERROR_ON_WRITE (network write error)</item>
///   <item>1161 — ER_NET_WRITE_INTERRUPTED (connection write interrupted)</item>
/// </list>
/// </remarks>
public sealed class MySqlTransientErrorDetector : ISqlTransientErrorDetector
{
    private static readonly int[] _transientErrorNumbers = new[]
    {
        1213, // ER_LOCK_DEADLOCK
        1205, // ER_LOCK_WAIT_TIMEOUT
        2006, // CR_SERVER_GONE_ERROR
        2013, // CR_SERVER_LOST
        1158, // ER_NET_READ_ERROR
        1159, // ER_NET_READ_INTERRUPTED
        1160, // ER_NET_ERROR_ON_WRITE
        1161, // ER_NET_WRITE_INTERRUPTED
        3024, // ER_QUERY_TIMEOUT
    };

    private static readonly MySqlTransientErrorDetector _default = new();

    /// <summary>
    /// Gets the default singleton instance of the <see cref="MySqlTransientErrorDetector"/> class.
    /// </summary>
    public static MySqlTransientErrorDetector Default => _default;

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

                if (System.Array.IndexOf(_transientErrorNumbers, dbEx.ErrorCode) >= 0)
                    return true;

                if (dbEx.Data["ServerErrorStatus"] is int serverStatus && System.Array.IndexOf(_transientErrorNumbers, serverStatus) >= 0)
                    return true;
            }

            ex = ex.InnerException;
        }

        return IsTransientMessage(exception.Message);
    }

    private static bool IsTransientMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        return message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("gone away", StringComparison.OrdinalIgnoreCase)
            || message.Contains("lost connection", StringComparison.OrdinalIgnoreCase);
    }
}


