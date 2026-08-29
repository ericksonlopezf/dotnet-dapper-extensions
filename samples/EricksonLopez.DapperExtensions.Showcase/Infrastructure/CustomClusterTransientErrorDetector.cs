// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.DapperExtensions.Resilience;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Custom transient error detector for specialized high-availability database clusters.
/// </summary>
public sealed class CustomClusterTransientErrorDetector : ISqlTransientErrorDetector
{
    public bool IsTransient(Exception exception)
    {
        if (exception == null) return false;

        var message = exception.Message;
        return message.Contains("node restart in progress", StringComparison.OrdinalIgnoreCase)
            || message.Contains("read replica lag exceeded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("cluster topology changing", StringComparison.OrdinalIgnoreCase);
    }
}
