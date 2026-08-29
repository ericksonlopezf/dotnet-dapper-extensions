// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.UnitOfWork;
using NSubstitute;
using Polly;
using Polly.Retry;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Resilience;

public class SavepointResilienceTests
{
    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder().Build();

    // ─── Guard assertions: ExecuteInSavepointWithRetryAsync (void) ───────────────

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_NullUnitOfWork_ThrowsArgumentNullException()
    {
        var act = async () => await ((IUnitOfWork)null!).ExecuteInSavepointWithRetryAsync(_pipeline, (u, ct) => Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var act = async () => await uow.ExecuteInSavepointWithRetryAsync((ResiliencePipeline)null!, (u, ct) => Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_NullOperation_ThrowsArgumentNullException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var act = async () => await uow.ExecuteInSavepointWithRetryAsync(_pipeline, (Func<IUnitOfWork, CancellationToken, Task>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    // ─── Guard assertions: ExecuteInSavepointWithRetryAsync<TResult> ─────────────

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_NullUnitOfWork_ThrowsArgumentNullException()
    {
        var act = async () => await ((IUnitOfWork)null!).ExecuteInSavepointWithRetryAsync<int>(_pipeline, (u, ct) => Task.FromResult(1));
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_NullPipeline_ThrowsArgumentNullException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var act = async () => await uow.ExecuteInSavepointWithRetryAsync<int>((ResiliencePipeline)null!, (u, ct) => Task.FromResult(1));
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_NullOperation_ThrowsArgumentNullException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var act = async () => await uow.ExecuteInSavepointWithRetryAsync<int>(_pipeline, (Func<IUnitOfWork, CancellationToken, Task<int>>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    // ─── Happy Path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_DefaultSavepointName_CreatesExecutesAndReleases()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        string? capturedName = null;

        uow.CreateSavepointAsync(Arg.Do<string>(s => capturedName = s), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var executed = false;
        await uow.ExecuteInSavepointWithRetryAsync(_pipeline, async (currentUow, ct) =>
        {
            executed = true;
            await Task.CompletedTask;
        });

        executed.Should().BeTrue();
        capturedName.Should().NotBeNullOrWhiteSpace();
        capturedName!.Should().StartWith("SP_");
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
        await savepoint.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_ExplicitSavepointName_PassesNameToUow()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        string? capturedName = null;

        uow.CreateSavepointAsync(Arg.Do<string>(s => capturedName = s), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        await uow.ExecuteInSavepointWithRetryAsync(_pipeline, async (currentUow, ct) =>
        {
            await Task.CompletedTask;
        }, savepointName: "Custom_Savepoint_1");

        capturedName.Should().Be("Custom_Savepoint_1");
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_ReturnsResultAndReleases()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        string? capturedName = null;

        uow.CreateSavepointAsync(Arg.Do<string>(s => capturedName = s), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var result = await uow.ExecuteInSavepointWithRetryAsync<string>(_pipeline, async (currentUow, ct) =>
        {
            await Task.CompletedTask;
            return "SUCCESS";
        }, savepointName: "Generic_SP");

        result.Should().Be("SUCCESS");
        capturedName.Should().Be("Generic_SP");
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
        await savepoint.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_DefaultSavepointName_GeneratesName()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        string? capturedName = null;

        uow.CreateSavepointAsync(Arg.Do<string>(s => capturedName = s), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var result = await uow.ExecuteInSavepointWithRetryAsync<int>(_pipeline, async (currentUow, ct) =>
        {
            await Task.CompletedTask;
            return 123;
        });

        result.Should().Be(123);
        capturedName.Should().StartWith("SP_");
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
    }

    // ─── Failure and Rollback Path ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_OnException_RollsBackAndRethrows()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        uow.CreateSavepointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var act = async () => await uow.ExecuteInSavepointWithRetryAsync(_pipeline, async (currentUow, ct) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Operation failed");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Operation failed");
        await savepoint.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await savepoint.DidNotReceive().ReleaseAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_OnException_RollsBackAndRethrows()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        uow.CreateSavepointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var act = async () => await uow.ExecuteInSavepointWithRetryAsync<int>(_pipeline, async (currentUow, ct) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Generic operation failed");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Generic operation failed");
        await savepoint.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await savepoint.DidNotReceive().ReleaseAsync(Arg.Any<CancellationToken>());
    }

    // ─── Retry Integration ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_RollsBackOnTransientFailureAndRetries()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        uow.CreateSavepointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero
            })
            .Build();

        int attempts = 0;
        await uow.ExecuteInSavepointWithRetryAsync(pipeline, async (currentUow, ct) =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new InvalidOperationException("Transient issue");
            }
            await Task.CompletedTask;
        });

        attempts.Should().Be(2);
        await savepoint.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInSavepointWithRetryAsync_Generic_RollsBackOnTransientFailureAndRetries()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var savepoint = Substitute.For<ISavepoint>();
        uow.CreateSavepointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(savepoint));

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero
            })
            .Build();

        int attempts = 0;
        var result = await uow.ExecuteInSavepointWithRetryAsync<string>(pipeline, async (currentUow, ct) =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new InvalidOperationException("Transient issue");
            }
            await Task.CompletedTask;
            return "SUCCESS_AFTER_RETRY";
        });

        result.Should().Be("SUCCESS_AFTER_RETRY");
        attempts.Should().Be(2);
        await savepoint.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await savepoint.Received(1).ReleaseAsync(Arg.Any<CancellationToken>());
    }
}
