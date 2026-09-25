// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data.Common;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

/// <summary>
/// Reusable test exception simulating database exceptions with transient, SQLSTATE, and error code metadata.
/// </summary>
public sealed class TestDbException : DbException
{
    private readonly bool _isTransient;
    private readonly string? _sqlState;

    public TestDbException(string message, int errorCode = 0, bool isTransient = false, string? sqlState = null, Exception? innerException = null)
        : base(message, innerException)
    {
        HResult = errorCode;
        _isTransient = isTransient;
        _sqlState = sqlState;
    }

    public override bool IsTransient => _isTransient;
    public override string? SqlState => _sqlState;
    public override int ErrorCode => HResult;
}
