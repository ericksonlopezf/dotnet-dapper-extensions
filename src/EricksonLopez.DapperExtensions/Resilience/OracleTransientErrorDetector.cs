// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides transient error detection for Oracle databases.
/// </summary>
/// <remarks>
/// <para>
/// Oracle error codes that indicate transient conditions (per Oracle documentation):
/// </para>
/// <list type="bullet">
///   <item>ORA-00060 — Deadlock detected while waiting for resource.</item>
///   <item>ORA-08177 — Can't serialize access for this transaction (serialization failure).</item>
///   <item>ORA-03113 — End-of-file on communication channel (connection lost).</item>
///   <item>ORA-03114 — Not connected to ORACLE.</item>
///   <item>ORA-03135 — Connection lost contact (network).</item>
///   <item>ORA-12170 — Connect timeout occurred.</item>
///   <item>ORA-12541 — TNS: no listener (listener not available yet after failover).</item>
///   <item>ORA-12560 — TNS: protocol adapter error (connection reset).</item>
///   <item>ORA-12571 — TNS: packet writer failure.</item>
///   <item>ORA-00018 — Maximum number of sessions exceeded (resource exhaustion, transient).</item>
///   <item>ORA-00054 — Resource busy and acquire with NOWAIT specified (row-level lock).</item>
/// </list>
/// <para>
/// For <c>Oracle.ManagedDataAccess</c> / <c>Oracle.ManagedDataAccess.Client.OracleException</c>,
/// the numeric code is available via the <c>Number</c> property, accessed here via reflection
/// to avoid a hard dependency on the Oracle NuGet packages.
/// </para>
/// </remarks>
public sealed class OracleTransientErrorDetector : ISqlTransientErrorDetector
{
    private static readonly int[] _transientErrorNumbers = new[]
    {
        60,    // ORA-00060: Deadlock
        18,    // ORA-00018: Maximum sessions exceeded
        54,    // ORA-00054: Resource busy
        8177,  // ORA-08177: Serialization failure
        3113,  // ORA-03113: End-of-file on communication channel
        3114,  // ORA-03114: Not connected
        3135,  // ORA-03135: Connection lost
        12170, // ORA-12170: Connect timeout
        12541, // ORA-12541: No listener
        12560, // ORA-12560: Protocol adapter error
        12571, // ORA-12571: Packet writer failure
        4031,  // ORA-04031: Unable to allocate shared memory (transient resource)
    };

    private static readonly string[] _transientMessageFragments = new[]
    {
        "ORA-00060",
        "ORA-08177",
        "ORA-03113",
        "ORA-03114",
        "ORA-03135",
        "ORA-12170",
        "ORA-12541",
        "ORA-12560",
        "ORA-12571",
        "deadlock",
        "connection lost",
        "end-of-file",
        "timeout",
        "serialization failure",
    };

    private static readonly OracleTransientErrorDetector _default = new();

    /// <summary>
    /// Gets the default singleton instance of the <see cref="OracleTransientErrorDetector"/> class.
    /// </summary>
    public static OracleTransientErrorDetector Default => _default;

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

                if (Array.IndexOf(_transientErrorNumbers, dbEx.ErrorCode) >= 0)
                    return true;
            }

            ex = ex.InnerException;
        }

        return IsTransientMessage(exception.Message);
    }

    private static bool IsTransientMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return false;

        foreach (var fragment in _transientMessageFragments)
        {
            if (message.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}


