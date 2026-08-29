// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides transient error detection for SQLite databases.
/// </summary>
/// <remarks>
/// <para>
/// SQLite uses numeric error codes. Transient codes include:
/// </para>
/// <list type="bullet">
///   <item>5  — SQLITE_BUSY: The database file is locked by another connection.</item>
///   <item>6  — SQLITE_LOCKED: A table in the database is locked.</item>
///   <item>261 — SQLITE_BUSY_RECOVERY: Another connection is recovering the WAL file.</item>
///   <item>262 — SQLITE_LOCKED_SHAREDCACHE: Conflict in a shared cache.</item>
/// </list>
/// <para>
/// For connections using <c>Microsoft.Data.Sqlite</c>, the numeric error code is exposed via
/// the <c>SqliteException.SqliteErrorCode</c> property, accessed here via reflection to avoid
/// a hard package dependency.
/// </para>
/// </remarks>
public sealed class SqliteTransientErrorDetector : ISqlTransientErrorDetector
{
    private static readonly int[] _transientErrorCodes = new[]
    {
        5,   // SQLITE_BUSY
        6,   // SQLITE_LOCKED
        261, // SQLITE_BUSY_RECOVERY
        262, // SQLITE_LOCKED_SHAREDCACHE
    };

    private static readonly string[] _transientMessageFragments = new[]
    {
        "database is locked",
        "unable to open database",
        "disk I/O error",
        "database disk image is malformed",
        "SQLITE_BUSY",
        "SQLITE_LOCKED",
    };

    private static readonly SqliteTransientErrorDetector _default = new();

    /// <summary>
    /// Gets the default singleton instance of the <see cref="SqliteTransientErrorDetector"/> class.
    /// </summary>
    public static SqliteTransientErrorDetector Default => _default;

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        if (exception == null) return false;

        // Walk the exception chain looking for a SqliteException
        var ex = exception;
        while (ex != null)
        {
            if (ex is System.Data.Common.DbException dbEx)
            {
                if (dbEx.IsTransient)
                    return true;

                if (Array.IndexOf(_transientErrorCodes, dbEx.ErrorCode) >= 0)
                    return true;

                if (dbEx.Data["SqliteErrorCode"] is int sqliteCode && Array.IndexOf(_transientErrorCodes, sqliteCode) >= 0)
                    return true;
            }

            ex = ex.InnerException;
        }

        // Fall back to message-based detection
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
