// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using Xunit;

namespace EricksonLopez.DapperExtensions.Testing.Common.DialectBases;

/// <summary>
/// Abstract base class for ISqlTransientErrorDetector tests to eliminate duplication across dialect tests.
/// </summary>
public abstract class TransientErrorDetectorTestsBase<TDetector> where TDetector : ISqlTransientErrorDetector
{
    protected abstract TDetector Sut { get; }

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        Sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        Sut.IsTransient(new Exception("")).Should().BeFalse();
        Sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        Sut.IsTransient(ex).Should().BeTrue();
    }
}
