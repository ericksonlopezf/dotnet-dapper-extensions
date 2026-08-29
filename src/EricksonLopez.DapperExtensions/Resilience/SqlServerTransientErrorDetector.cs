// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides transient error detection for Microsoft SQL Server databases.
/// </summary>
/// <remarks>
/// Transient error codes per Microsoft documentation:
/// <list type="bullet">
///   <item>1205 — Deadlock victim</item>
///   <item>1222 — Lock timeout exceeded</item>
///   <item>233 — Connection initialization error</item>
///   <item>64 — Connection terminated during handshake</item>
///   <item>4060 — Database not available (AlwaysOn failover)</item>
///   <item>40143 — Service encountered a transient error</item>
///   <item>40197 — Service error (retry shortly)</item>
///   <item>40501 — Service busy (retry in 10 seconds)</item>
///   <item>40613 — Database not currently available</item>
///   <item>49918 — Cannot process request (not enough resources)</item>
/// </list>
/// </remarks>
public sealed class SqlServerTransientErrorDetector : ISqlTransientErrorDetector
{
    private static readonly int[] _transientErrorNumbers = new[]
    {
        1205,  // Deadlock victim
        1222,  // Lock timeout exceeded
        233,   // Connection initialization error
        64,    // Connection terminated during handshake
        4060,  // Database not available
        40143, // Transient error
        40197, // Service error
        40501, // Service busy
        40613, // Database currently unavailable
        49918, // Not enough resources
        10928, // Resource pool limit
        10929, // Resource pool requests limit
        10053, // Transport-level error
        10054, // Connection forcibly closed
        10060, // Connection timeout
    };

    private static readonly SqlServerTransientErrorDetector _default = new();

    /// <summary>
    /// Gets the default singleton instance of the <see cref="SqlServerTransientErrorDetector"/> class.
    /// </summary>
    public static SqlServerTransientErrorDetector Default => _default;

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

                if (dbEx.Data["Number"] is int num && System.Array.IndexOf(_transientErrorNumbers, num) >= 0)
                    return true;
            }

            ex = ex.InnerException;
        }

        // Connection-level exceptions
        return IsTransientMessage(exception.Message);
    }

    private static bool IsTransientMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        return message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
            || message.Contains("transient", StringComparison.OrdinalIgnoreCase);
    }
}


