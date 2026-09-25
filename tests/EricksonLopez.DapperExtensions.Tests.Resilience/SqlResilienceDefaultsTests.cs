// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Testing.Common;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace EricksonLopez.DapperExtensions.Resilience.UnitTests;

public class SqlResilienceDefaultsTests
{
    private readonly ISqlTransientErrorDetector _detector = SqlServerTransientErrorDetector.Default;
    private readonly FakeTimeProvider _timeProvider = new();

    // ─── Null argument checks ───────────────────────────────────────────

    [Fact]
    public void Standard_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.Standard(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void Aggressive_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.Aggressive(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void Conservative_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.Conservative(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void Standard_Generic_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.Standard<int>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void StandardWithCircuitBreaker_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.StandardWithCircuitBreaker(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void StandardWithCircuitBreaker_Generic_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.StandardWithCircuitBreaker<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    // ─── Pipeline Instantiation ─────────────────────────────────────────

    // ─── Pipeline Strategy Options Invariants (Mutation Safety Gates) ──

    [Fact]
    public void CreateStandardRetryOptions_ConfiguresCorrectParametersWithJitter()
    {
        var options = SqlResilienceDefaults.CreateStandardRetryOptions(_detector);
        options.MaxRetryAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(1));
        options.BackoffType.Should().Be(DelayBackoffType.Exponential);
        options.UseJitter.Should().BeTrue();
        options.ShouldHandle.Should().NotBeNull();
    }

    [Fact]
    public void CreateStandardTimeoutOptions_Configures30Seconds()
    {
        var options = SqlResilienceDefaults.CreateStandardTimeoutOptions();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CreateAggressiveRetryOptions_Configures5AttemptsWith500MsDelayAndJitter()
    {
        var options = SqlResilienceDefaults.CreateAggressiveRetryOptions(_detector);
        options.MaxRetryAttempts.Should().Be(5);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.BackoffType.Should().Be(DelayBackoffType.Exponential);
        options.UseJitter.Should().BeTrue();
        options.ShouldHandle.Should().NotBeNull();
    }

    [Fact]
    public void CreateAggressiveTimeoutOptions_Configures60Seconds()
    {
        var options = SqlResilienceDefaults.CreateAggressiveTimeoutOptions();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void CreateConservativeRetryOptions_Configures1AttemptWith5SecondsDelayAndNoJitter()
    {
        var options = SqlResilienceDefaults.CreateConservativeRetryOptions(_detector);
        options.MaxRetryAttempts.Should().Be(1);
        options.Delay.Should().Be(TimeSpan.FromSeconds(5));
        options.BackoffType.Should().Be(DelayBackoffType.Constant);
        options.UseJitter.Should().BeFalse();
        options.ShouldHandle.Should().NotBeNull();
    }

    [Fact]
    public void CreateConservativeTimeoutOptions_Configures120Seconds()
    {
        var options = SqlResilienceDefaults.CreateConservativeTimeoutOptions();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void CreateCircuitBreakerOptions_ConfiguresSpecifiedParameters()
    {
        var options = SqlResilienceDefaults.CreateCircuitBreakerOptions(
            _detector,
            failureRatio: 0.7,
            samplingDuration: TimeSpan.FromSeconds(5),
            minimumThroughput: 20,
            breakDuration: TimeSpan.FromSeconds(15));

        options.FailureRatio.Should().Be(0.7);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(5));
        options.MinimumThroughput.Should().Be(20);
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(15));
        options.ShouldHandle.Should().NotBeNull();
    }

    [Fact]
    public void CreateCircuitBreakerOptions_WithNullOptionalParameters_UsesDefaults()
    {
        var options = SqlResilienceDefaults.CreateCircuitBreakerOptions(
            _detector,
            failureRatio: 0.5,
            samplingDuration: null,
            minimumThroughput: 10,
            breakDuration: null);

        options.FailureRatio.Should().Be(0.5);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(10));
        options.MinimumThroughput.Should().Be(10);
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CreateStandardRetryOptions_Generic_ConfiguresCorrectParametersWithJitter()
    {
        var options = SqlResilienceDefaults.CreateStandardRetryOptions<int>(_detector);
        options.MaxRetryAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(1));
        options.BackoffType.Should().Be(DelayBackoffType.Exponential);
        options.UseJitter.Should().BeTrue();
        options.ShouldHandle.Should().NotBeNull();
    }

    [Fact]
    public void CreateCircuitBreakerOptions_Generic_ConfiguresSpecifiedParameters()
    {
        var options = SqlResilienceDefaults.CreateCircuitBreakerOptions<string>(
            _detector,
            failureRatio: 0.8,
            samplingDuration: TimeSpan.FromSeconds(8),
            minimumThroughput: 15,
            breakDuration: TimeSpan.FromSeconds(25));

        options.FailureRatio.Should().Be(0.8);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(8));
        options.MinimumThroughput.Should().Be(15);
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(25));
        options.ShouldHandle.Should().NotBeNull();
    }

    // ─── Execution, Retries, Timeouts & Options ─────────────────────────

    [Fact]
    public async Task Standard_Pipeline_ExecutesSuccessfully()
    {
        var pipeline = SqlResilienceDefaults.Standard(_detector, _timeProvider);
        var executed = false;

        await pipeline.ExecuteAsync(ct =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Standard_Generic_Pipeline_ExecutesAndReturnsResult()
    {
        var pipeline = SqlResilienceDefaults.Standard<int>(_detector, _timeProvider);

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(42), CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Standard_Pipeline_RetriesUpTo3Times_AndSucceedsOn4thAttempt()
    {
        var detector = SqlServerTransientErrorDetector.Default;
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard(detector, timeProvider);

        var callCount = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            if (callCount <= 3)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        // Advance through exponential delays
        for (int i = 0; i < 5; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        await task;
        callCount.Should().Be(4);
    }

    [Fact]
    public async Task Standard_Pipeline_ExhaustsRetries_After3Retries_AndThrows()
    {
        var detector = SqlServerTransientErrorDetector.Default;
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard(detector, timeProvider);

        var callCount = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            throw new TestDbException("deadlock", errorCode: 1205);
        }, CancellationToken.None).AsTask();

        for (int i = 0; i < 5; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        var act = async () => await task;
        await act.Should().ThrowAsync<TestDbException>();
        callCount.Should().Be(4); // 1 initial + 3 retries
    }

    [Fact]
    public async Task Standard_Generic_Pipeline_RetriesUpTo3Times_AndSucceedsOn4thAttempt()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard<string>(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts <= 3)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.FromResult("SUCCESS");
        }, CancellationToken.None).AsTask();

        for (int i = 0; i < 5; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        var result = await task;
        result.Should().Be("SUCCESS");
        attempts.Should().Be(4);
    }

    [Fact]
    public async Task Standard_Generic_Pipeline_ExhaustsRetries_After3Retries_AndThrows()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard<string>(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            return ValueTask.FromException<string>(new TestDbException("deadlock", errorCode: 1205));
        }, CancellationToken.None).AsTask();

        for (int i = 0; i < 5; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        var act = async () => await task;
        await act.Should().ThrowAsync<TestDbException>();
        attempts.Should().Be(4);
    }

    [Fact]
    public async Task Aggressive_Pipeline_RetriesUpTo5Times_AndSucceedsOn6thAttempt()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Aggressive(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts <= 5)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        for (int i = 0; i < 10; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        await task;
        attempts.Should().Be(6); // 1 initial + 5 retries
    }

    [Fact]
    public async Task Aggressive_Pipeline_ExhaustsRetries_After5Retries_AndThrows()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Aggressive(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            throw new TestDbException("deadlock", errorCode: 1205);
        }, CancellationToken.None).AsTask();

        for (int i = 0; i < 10; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        var act = async () => await task;
        await act.Should().ThrowAsync<TestDbException>();
        attempts.Should().Be(6);
    }

    [Fact]
    public async Task Conservative_Pipeline_RetriesExactly1Time_AndSucceedsOn2ndAttempt()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Conservative(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts <= 1)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        // Delay is 5 seconds constant without jitter
        timeProvider.Advance(TimeSpan.FromSeconds(6));

        await task;
        attempts.Should().Be(2); // 1 initial + 1 retry
    }

    [Fact]
    public async Task Conservative_Pipeline_DoesNotRetryMoreThanOnce()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Conservative(SqlServerTransientErrorDetector.Default, timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            throw new TestDbException("deadlock", errorCode: 1205);
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(10));

        var act = async () => await task;
        await act.Should().ThrowAsync<TestDbException>();
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task Standard_Pipeline_ThrowsTimeout_WhenExecutionExceeds30Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard(_detector, timeProvider);

        var tcs = new TaskCompletionSource<bool>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Aggressive_Pipeline_ThrowsTimeout_WhenExecutionExceeds60Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Aggressive(_detector, timeProvider);

        var tcs = new TaskCompletionSource<bool>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Aggressive_Pipeline_DoesNotTimeout_Before60Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Aggressive(_detector, timeProvider);

        var tcs = new TaskCompletionSource<string>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        // 45s is > 30s (default Polly timeout) but < 60s (Aggressive timeout)
        timeProvider.Advance(TimeSpan.FromSeconds(45));
        tcs.SetResult("SUCCESS_AT_45S");

        var result = await task;
        result.Should().Be("SUCCESS_AT_45S");
    }

    [Fact]
    public async Task Conservative_Pipeline_ThrowsTimeout_WhenExecutionExceeds120Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Conservative(_detector, timeProvider);

        var tcs = new TaskCompletionSource<bool>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(121));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Conservative_Pipeline_DoesNotTimeout_Before120Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Conservative(_detector, timeProvider);

        var tcs = new TaskCompletionSource<string>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        // 90s is > 30s and > 60s, but < 120s (Conservative timeout)
        timeProvider.Advance(TimeSpan.FromSeconds(90));
        tcs.SetResult("SUCCESS_AT_90S");

        var result = await task;
        result.Should().Be("SUCCESS_AT_90S");
    }

    [Fact]
    public async Task Standard_Generic_Pipeline_ThrowsTimeout_WhenExecutionExceeds30Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard<int>(_detector, timeProvider);

        var tcs = new TaskCompletionSource<int>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Pipeline_ThrowsTimeout_WhenExecutionExceeds30Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(_detector, timeProvider: timeProvider);

        var tcs = new TaskCompletionSource<bool>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Generic_ThrowsTimeout_WhenExecutionExceeds30Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(_detector, timeProvider: timeProvider);

        var tcs = new TaskCompletionSource<int>();

        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Pipeline_RetriesOnTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(SqlServerTransientErrorDetector.Default, timeProvider: timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Generic_RetriesOnTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(SqlServerTransientErrorDetector.Default, timeProvider: timeProvider);
        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.FromResult(100);
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var result = await task;

        result.Should().Be(100);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_OpensCircuitWhenFailureRatioExceeded_AndRecoversAfterBreakDuration()
    {
        var timeProvider = new FakeTimeProvider();
        // Use default samplingDuration (10s) and breakDuration (30s)
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 2,
            failureRatio: 0.5,
            timeProvider: timeProvider);

        // Cause 2 failures to exceed minimumThroughput and failureRatio
        for (int i = 0; i < 2; i++)
        {
            try
            {
                var t = pipeline.ExecuteAsync(ct => throw new TestDbException("deadlock", errorCode: 1205)).AsTask();
                timeProvider.Advance(TimeSpan.FromSeconds(10));
                await t;
            }
            catch
            {
                // Expected
            }
        }

        // Circuit is now Open -> Should throw BrokenCircuitException
        var act = async () => await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);
        await act.Should().ThrowAsync<BrokenCircuitException>();

        // Advance time past default breakDuration (30s) -> Enters Half-Open
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        // Successful execution closes circuit
        var successCalled = false;
        await pipeline.ExecuteAsync(ct =>
        {
            successCalled = true;
            return ValueTask.CompletedTask;
        });

        successCalled.Should().BeTrue();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Generic_OpensCircuitWhenFailureRatioExceeded_AndRecoversAfterBreakDuration()
    {
        var timeProvider = new FakeTimeProvider();
        // Use default samplingDuration (10s) and breakDuration (30s)
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 2,
            failureRatio: 0.5,
            timeProvider: timeProvider);

        for (int i = 0; i < 2; i++)
        {
            try
            {
                var t = pipeline.ExecuteAsync(ct => ValueTask.FromException<int>(new TestDbException("deadlock", errorCode: 1205))).AsTask();
                timeProvider.Advance(TimeSpan.FromSeconds(10));
                await t;
            }
            catch
            {
                // Expected
            }
        }

        var act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult(1));
        await act.Should().ThrowAsync<BrokenCircuitException>();

        // Advance time past default breakDuration (30s) -> Enters Half-Open
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(42));
        result.Should().Be(42);
    }

    [Fact]
    public async Task Aggressive_Pipeline_DoesNotTimeoutAt45Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Aggressive(_detector, timeProvider);

        var executed = false;
        await pipeline.ExecuteAsync(ct =>
        {
            timeProvider.Advance(TimeSpan.FromSeconds(45));
            executed = !ct.IsCancellationRequested;
            return ValueTask.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Conservative_Pipeline_DoesNotTimeoutAt60Seconds()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Conservative(_detector, timeProvider);

        var executed = false;
        await pipeline.ExecuteAsync(ct =>
        {
            timeProvider.Advance(TimeSpan.FromSeconds(60));
            executed = !ct.IsCancellationRequested;
            return ValueTask.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_CustomBreakDuration_RecoversAfterCustomDuration()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 2,
            failureRatio: 0.5,
            breakDuration: TimeSpan.FromSeconds(10),
            timeProvider: timeProvider);

        try
        {
            var t = pipeline.ExecuteAsync(ct => throw new TestDbException("deadlock", errorCode: 1205)).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(10));
            await t;
        }
        catch { }

        // Advance 5s -> still in break (break is 10s)
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var act = async () => await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);
        await act.Should().ThrowAsync<BrokenCircuitException>();

        // Advance 6s more (total 11s > 10s) -> half-open -> recovers!
        timeProvider.Advance(TimeSpan.FromSeconds(6));
        var success = false;
        await pipeline.ExecuteAsync(ct => { success = true; return ValueTask.CompletedTask; });
        success.Should().BeTrue();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Generic_CustomBreakDuration_RecoversAfterCustomDuration()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 2,
            failureRatio: 0.5,
            breakDuration: TimeSpan.FromSeconds(10),
            timeProvider: timeProvider);

        try
        {
            var t = pipeline.ExecuteAsync(ct => ValueTask.FromException<int>(new TestDbException("deadlock", errorCode: 1205))).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(10));
            await t;
        }
        catch { }

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult(1));
        await act.Should().ThrowAsync<BrokenCircuitException>();

        timeProvider.Advance(TimeSpan.FromSeconds(6));
        var res = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(42));
        res.Should().Be(42);
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_CustomSamplingDuration_DiscardsOldFailures()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 6,
            failureRatio: 0.5,
            samplingDuration: TimeSpan.FromSeconds(5),
            timeProvider: timeProvider);

        // 1st failure at t=0 produces 4 attempts (< 6 minThroughput)
        try
        {
            var t = pipeline.ExecuteAsync(ct => throw new TestDbException("deadlock", errorCode: 1205)).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await t;
        }
        catch { }

        // Advance 6s past the 5s sampling window so the failures expire
        timeProvider.Advance(TimeSpan.FromSeconds(6));

        // 2nd failure at t=8 produces 4 attempts in new window (< 6 minThroughput)
        // If window was 10s default: total failures = 8 >= 6 minThroughput, circuit would break!
        try
        {
            var t = pipeline.ExecuteAsync(ct => throw new TestDbException("deadlock", errorCode: 1205)).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await t;
        }
        catch { }

        // Circuit is closed and successful call works
        var success = false;
        await pipeline.ExecuteAsync(ct => { success = true; return ValueTask.CompletedTask; });
        success.Should().BeTrue();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_Generic_CustomSamplingDuration_DiscardsOldFailures()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(
            SqlServerTransientErrorDetector.Default,
            minimumThroughput: 6,
            failureRatio: 0.5,
            samplingDuration: TimeSpan.FromSeconds(5),
            timeProvider: timeProvider);

        try
        {
            var t = pipeline.ExecuteAsync(ct => ValueTask.FromException<int>(new TestDbException("deadlock", errorCode: 1205))).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await t;
        }
        catch { }

        timeProvider.Advance(TimeSpan.FromSeconds(6));

        try
        {
            var t = pipeline.ExecuteAsync(ct => ValueTask.FromException<int>(new TestDbException("deadlock", errorCode: 1205))).AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await t;
        }
        catch { }

        var res = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(123));
        res.Should().Be(123);
    }

    [Fact]
    public async Task Standard_Pipeline_DoesNotRetryOnPermanentError()
    {
        var detector = SqlServerTransientErrorDetector.Default;
        var pipeline = SqlResilienceDefaults.Standard(detector, _timeProvider);

        var callCount = 0;
        var act = async () => await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            throw new Exception("duplicate key violation");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        callCount.Should().Be(1);
    }

    // ─── Provider Shortcuts ─────────────────────────────────────────────

    [Fact]
    public async Task ForSqlServer_ExecutesAndRetriesOnSqlServerTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlServer(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForSqlServer();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForSqlServerWithCircuitBreaker_ExecutesAndRetriesOnSqlServerTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlServerWithCircuitBreaker(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1205);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForSqlServerWithCircuitBreaker();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForPostgreSql_ExecutesAndRetriesOnPostgreSqlTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForPostgreSql(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("serialization", sqlState: "40001");
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForPostgreSql();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForPostgreSqlWithCircuitBreaker_ExecutesAndRetriesOnPostgreSqlTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreaker(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("serialization", sqlState: "40001");
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreaker();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForMySql_ExecutesAndRetriesOnMySqlTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForMySql(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1213);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForMySql();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForMySqlWithCircuitBreaker_ExecutesAndRetriesOnMySqlTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForMySqlWithCircuitBreaker(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 1213);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForMySqlWithCircuitBreaker();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForSqlite_ExecutesAndRetriesOnSqliteTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlite(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("busy", errorCode: 5);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForSqlite();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForSqliteWithCircuitBreaker_ExecutesAndRetriesOnSqliteTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqliteWithCircuitBreaker(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("busy", errorCode: 5);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForSqliteWithCircuitBreaker();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForOracle_ExecutesAndRetriesOnOracleTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForOracle(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 60);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForOracle();
        defaultPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ForOracleWithCircuitBreaker_ExecutesAndRetriesOnOracleTransientError()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForOracleWithCircuitBreaker(timeProvider);
        pipeline.Should().NotBeNull();

        int attempts = 0;
        var task = pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TestDbException("deadlock", errorCode: 60);
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        await task;

        attempts.Should().Be(2);

        var defaultPipeline = SqlResilienceDefaults.ForOracleWithCircuitBreaker();
        defaultPipeline.Should().NotBeNull();
    }

    // ─── Canonical IResiliencePipeline Factory Methods Tests ───────────────────

    [Fact]
    public void StandardPipeline_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.StandardPipeline(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void StandardWithCircuitBreakerPipeline_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.StandardWithCircuitBreakerPipeline(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void AggressivePipeline_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.AggressivePipeline(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void ConservativePipeline_WithNullDetector_Throws()
    {
        var act = () => SqlResilienceDefaults.ConservativePipeline(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public async Task CanonicalPipelines_ExecuteSuccessfully()
    {
        var tp = new FakeTimeProvider();

        var standard = SqlResilienceDefaults.StandardPipeline(_detector, "custom-std", tp);
        standard.Should().NotBeNull();
        await standard.ExecuteAsync(ct => ValueTask.CompletedTask);

        var cb = SqlResilienceDefaults.StandardWithCircuitBreakerPipeline(_detector, "custom-cb", 0.5, TimeSpan.FromSeconds(10), 10, TimeSpan.FromSeconds(30), tp);
        cb.Should().NotBeNull();
        await cb.ExecuteAsync(ct => ValueTask.CompletedTask);

        var aggressive = SqlResilienceDefaults.AggressivePipeline(_detector, "custom-agg", tp);
        aggressive.Should().NotBeNull();
        await aggressive.ExecuteAsync(ct => ValueTask.CompletedTask);

        var conservative = SqlResilienceDefaults.ConservativePipeline(_detector, "custom-cons", tp);
        conservative.Should().NotBeNull();
        await conservative.ExecuteAsync(ct => ValueTask.CompletedTask);

        // Provider shortcuts
        var sqlServer = SqlResilienceDefaults.ForSqlServerPipeline(tp);
        sqlServer.Should().NotBeNull();
        await sqlServer.ExecuteAsync(ct => ValueTask.CompletedTask);

        var sqlServerCb = SqlResilienceDefaults.ForSqlServerWithCircuitBreakerPipeline(tp);
        sqlServerCb.Should().NotBeNull();
        await sqlServerCb.ExecuteAsync(ct => ValueTask.CompletedTask);

        var pg = SqlResilienceDefaults.ForPostgreSqlPipeline(tp);
        pg.Should().NotBeNull();
        await pg.ExecuteAsync(ct => ValueTask.CompletedTask);

        var pgCb = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreakerPipeline(tp);
        pgCb.Should().NotBeNull();
        await pgCb.ExecuteAsync(ct => ValueTask.CompletedTask);

        var mysql = SqlResilienceDefaults.ForMySqlPipeline(tp);
        mysql.Should().NotBeNull();
        await mysql.ExecuteAsync(ct => ValueTask.CompletedTask);

        var mysqlCb = SqlResilienceDefaults.ForMySqlWithCircuitBreakerPipeline(tp);
        mysqlCb.Should().NotBeNull();
        await mysqlCb.ExecuteAsync(ct => ValueTask.CompletedTask);

        var sqlite = SqlResilienceDefaults.ForSqlitePipeline(tp);
        sqlite.Should().NotBeNull();
        await sqlite.ExecuteAsync(ct => ValueTask.CompletedTask);

        var sqliteCb = SqlResilienceDefaults.ForSqliteWithCircuitBreakerPipeline(tp);
        sqliteCb.Should().NotBeNull();
        await sqliteCb.ExecuteAsync(ct => ValueTask.CompletedTask);

        var oracle = SqlResilienceDefaults.ForOraclePipeline(tp);
        oracle.Should().NotBeNull();
        await oracle.ExecuteAsync(ct => ValueTask.CompletedTask);

        var oracleCb = SqlResilienceDefaults.ForOracleWithCircuitBreakerPipeline(tp);
        oracleCb.Should().NotBeNull();
        await oracleCb.ExecuteAsync(ct => ValueTask.CompletedTask);

        // Parameterless versions
        SqlResilienceDefaults.ForSqlServerPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForSqlServerWithCircuitBreakerPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForPostgreSqlPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForPostgreSqlWithCircuitBreakerPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForMySqlPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForMySqlWithCircuitBreakerPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForSqlitePipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForSqliteWithCircuitBreakerPipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForOraclePipeline().Should().NotBeNull();
        SqlResilienceDefaults.ForOracleWithCircuitBreakerPipeline().Should().NotBeNull();
    }

    [Fact]
    public async Task ForSqlServerPipeline_WhenSqlTransientDeadlock_RetriesAndSucceeds()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlServerPipeline(tp);
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TestDbException("Deadlock victim", errorCode: 1205);
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ForPostgreSqlPipeline_WhenPostgresSerializationFailure_RetriesAndSucceeds()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForPostgreSqlPipeline(tp);
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TestDbException("Serialization failure", sqlState: "40001");
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ForMySqlPipeline_WhenMySqlDeadlock_RetriesAndSucceeds()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForMySqlPipeline(tp);
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TestDbException("Deadlock found", errorCode: 1213);
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ForSqlitePipeline_WhenSqliteBusy_RetriesAndSucceeds()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlitePipeline(tp);
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TestDbException("Database is locked", errorCode: 5);
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ForOraclePipeline_WhenOracleDeadlock_RetriesAndSucceeds()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForOraclePipeline(tp);
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TestDbException("Deadlock detected", errorCode: 60);
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ForSqlServerPipeline_WhenNonTransientException_FailsImmediatelyWithoutRetry()
    {
        var tp = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.ForSqlServerPipeline(tp);
        var attempts = 0;

        var act = async () => await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            throw new TestDbException("Invalid object name", errorCode: 208);
        });

        await act.Should().ThrowAsync<TestDbException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task StandardPipeline_WhenOperationExceeds30SecondsTimeout_ThrowsTimeoutRejectedException()
    {
        var tp = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Standard(SqlServerTransientErrorDetector.Default, timeProvider: tp);

        var task = pipeline.ExecuteAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60), tp, ct);
        }).AsTask();

        // Advance 29 seconds: still pending
        tp.Advance(TimeSpan.FromSeconds(29));
        task.IsCompleted.Should().BeFalse();

        // Advance past 30 seconds: triggers timeout
        tp.Advance(TimeSpan.FromSeconds(2));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Standard_Pipeline_WhenCancellationTokenCancelledDuringRetryDelay_AbortsAndThrowsOperationCanceledException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Standard(
            SqlServerTransientErrorDetector.Default,
            timeProvider: timeProvider);

        using var cts = new CancellationTokenSource();
        var attempts = 0;

        var task = pipeline.ExecuteAsync(async ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                // First attempt fails with transient error, triggering retry backoff delay
                throw new TestDbException("deadlock", errorCode: 1205);
            }
            await Task.CompletedTask;
        }, cts.Token).AsTask();

        // While backoff delay is waiting (not auto-triggered), cancel the token
        cts.Cancel();

        var act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify attempt 2 was never executed due to cancellation during retry delay
        attempts.Should().Be(1);
    }

    // ─── Timeout Strategy Tests ──────────────────────────────────────────────

    [Fact]
    public async Task Standard_Pipeline_WhenExecutionExceeds30Seconds_ThrowsTimeoutRejectedException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Standard(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<bool>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Standard_Generic_Pipeline_WhenExecutionExceeds30Seconds_ThrowsTimeoutRejectedException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Standard<string>(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<string>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task StandardWithCircuitBreaker_WhenExecutionExceeds30Seconds_ThrowsTimeoutRejectedException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.StandardWithCircuitBreaker(SqlServerTransientErrorDetector.Default, timeProvider: timeProvider);

        var tcs = new TaskCompletionSource<bool>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Aggressive_Pipeline_WhenExecutionExceeds60Seconds_ThrowsTimeoutRejectedException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Aggressive(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<bool>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Aggressive_Pipeline_WhenExecutionWithin60Seconds_DoesNotTimeOut()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Aggressive(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<string>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        // 45s is greater than default 30s but within Aggressive 60s
        timeProvider.Advance(TimeSpan.FromSeconds(45));
        tcs.TrySetResult("COMPLETED");

        var result = await task;
        result.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Conservative_Pipeline_WhenExecutionExceeds120Seconds_ThrowsTimeoutRejectedException()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Conservative(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<bool>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }, CancellationToken.None).AsTask();

        timeProvider.Advance(TimeSpan.FromSeconds(121));

        var act = async () => await task;
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Conservative_Pipeline_WhenExecutionWithin120Seconds_DoesNotTimeOut()
    {
        var timeProvider = new FakeTimeProvider { AutoTriggerDelays = false };
        var pipeline = SqlResilienceDefaults.Conservative(SqlServerTransientErrorDetector.Default, timeProvider);

        var tcs = new TaskCompletionSource<string>();
        var task = pipeline.ExecuteAsync(async ct =>
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }, CancellationToken.None).AsTask();

        // 90s is greater than 30s/60s but within Conservative 120s
        timeProvider.Advance(TimeSpan.FromSeconds(90));
        tcs.TrySetResult("CONSERVATIVE_OK");

        var result = await task;
        result.Should().Be("CONSERVATIVE_OK");
    }

    [Fact]
    public async Task Standard_Pipeline_SupportsHighConcurrencyWithoutStateCorruption()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard(_detector, timeProvider: timeProvider);
        const int concurrentOperations = 50;

        var tasks = Enumerable.Range(0, concurrentOperations).Select(async i =>
        {
            await Task.Yield();
            return await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Yield();
                return i * 2;
            }, CancellationToken.None);
        });

        var results = await Task.WhenAll(tasks);
        results.Length.Should().Be(concurrentOperations);
        for (int i = 0; i < concurrentOperations; i++)
        {
            results[i].Should().Be(i * 2);
        }
    }

    [Fact]
    public async Task Standard_Pipeline_UnderConcurrentRetries_ResolvesAllOperationsDeterministically()
    {
        var timeProvider = new FakeTimeProvider();
        var pipeline = SqlResilienceDefaults.Standard(_detector, timeProvider: timeProvider);
        const int concurrentOperations = 25;
        var attemptCounts = new int[concurrentOperations];

        var tasks = Enumerable.Range(0, concurrentOperations).Select(async i =>
        {
            await Task.Yield();
            return await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Yield();
                attemptCounts[i]++;
                if (attemptCounts[i] < 2)
                    throw new TestDbException("transient concurrent deadlock", errorCode: 1205);
                return $"Success_{i}";
            }, CancellationToken.None);
        });

        var resultsTask = Task.WhenAll(tasks);
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        var results = await resultsTask;

        results.Length.Should().Be(concurrentOperations);
        for (int i = 0; i < concurrentOperations; i++)
        {
            results[i].Should().Be($"Success_{i}");
            attemptCounts[i].Should().Be(2);
        }
    }
}
