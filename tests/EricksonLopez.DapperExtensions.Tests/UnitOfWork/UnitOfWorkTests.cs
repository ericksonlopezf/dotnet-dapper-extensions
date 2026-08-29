// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.UnitOfWork;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;
using UowImpl = EricksonLopez.DapperExtensions.UnitOfWork.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Tests.UnitOfWork;

/// <summary>
/// Exhaustive unit and integration tests for IUnitOfWork, UnitOfWork, ISavepoint, and UnitOfWorkExtensions.
/// </summary>
[Collection("UnitOfWork")]
public class UnitOfWorkTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE counter (id INTEGER PRIMARY KEY, value INTEGER NOT NULL DEFAULT 0);" +
                          "INSERT INTO counter (id, value) VALUES (1, 0);";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async Task<int> ReadCounterAsync()
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM counter WHERE id = 1";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task IncrementCounterAsync(IDbTransaction tx)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE counter SET value = value + 1 WHERE id = 1";
        cmd.Transaction = tx as SqliteTransaction;
        await cmd.ExecuteNonQueryAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Core transaction tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BeginUnitOfWorkAsync_Returns_IUnitOfWork_WithTransaction()
    {
        await using var uow = await _connection.BeginUnitOfWorkAsync();

        uow.Should().NotBeNull();
        uow.Transaction.Should().NotBeNull();
        uow.IsolationLevel.Should().Be(IsolationLevel.Serializable); // SQLite default
    }

    [Fact]
    public async Task BeginUnitOfWorkAsync_WhenConnectionClosed_OpensAndBeginsTransaction()
    {
        using var closedConn = new SqliteConnection("Data Source=:memory:");
        closedConn.State.Should().Be(ConnectionState.Closed);

        await using var uow = await closedConn.BeginUnitOfWorkAsync();

        closedConn.State.Should().Be(ConnectionState.Open);
        uow.Should().NotBeNull();
        uow.Transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitAsync_Commits_DatabaseChanges()
    {
        var valueBefore = await ReadCounterAsync();

        await using (var uow = await _connection.BeginUnitOfWorkAsync())
        {
            await IncrementCounterAsync(uow.Transaction);
            await uow.CommitAsync();
        }

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore + 1);
    }

    [Fact]
    public async Task CommitAsync_SetsCommittedFlag_PreventingRollbackOnDisposal()
    {
        var mockTx = Substitute.For<IDbTransaction, IDisposable>();
        var uow = new UowImpl(mockTx);

        await uow.CommitAsync();
        await uow.DisposeAsync();

        mockTx.Received(1).Commit();
        mockTx.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task DisposeAsync_WithoutCommit_RollsBack_Changes()
    {
        var valueBefore = await ReadCounterAsync();

        await using (var uow = await _connection.BeginUnitOfWorkAsync())
        {
            await IncrementCounterAsync(uow.Transaction);
            // uow disposed without commit
        }

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore, "rollback should have undone the increment");
    }

    [Fact]
    public async Task ExplicitRollbackAsync_UndoesChanges()
    {
        var valueBefore = await ReadCounterAsync();

        await using var uow = await _connection.BeginUnitOfWorkAsync();
        await IncrementCounterAsync(uow.Transaction);
        await uow.RollbackAsync();

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var mockTx = Substitute.For<IDbTransaction, IDisposable>();
        var uow = new UowImpl(mockTx);

        await uow.DisposeAsync();
        await uow.DisposeAsync();

        mockTx.Received(1).Rollback();
        ((IDisposable)mockTx).Received(1).Dispose();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // WithUnitOfWorkAsync Overloads
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WithUnitOfWorkAsync_VoidOverload_CommitsOnSuccess()
    {
        var valueBefore = await ReadCounterAsync();

        using var cts = new CancellationTokenSource();
        await _connection.WithUnitOfWorkAsync(async (uow, ct) =>
        {
            ct.Should().Be(cts.Token);
            await IncrementCounterAsync(uow.Transaction);
        }, IsolationLevel.Serializable, cts.Token);

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore + 1);
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_VoidOverload_RollsBackOnException()
    {
        var valueBefore = await ReadCounterAsync();

        var act = async () => await _connection.WithUnitOfWorkAsync(async (uow, ct) =>
        {
            await IncrementCounterAsync(uow.Transaction);
            throw new InvalidOperationException("Simulated error");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated error");

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore, "rollback should have undone the increment");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_ReturningOverload_CommitsAndReturnsResult()
    {
        var valueBefore = await ReadCounterAsync();

        using var cts = new CancellationTokenSource();
        var result = await _connection.WithUnitOfWorkAsync(async (uow, ct) =>
        {
            ct.Should().Be(cts.Token);
            await IncrementCounterAsync(uow.Transaction);
            return 42;
        }, IsolationLevel.Serializable, cts.Token);

        result.Should().Be(42);
        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore + 1);
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_ReturningOverload_RollsBackOnException()
    {
        var valueBefore = await ReadCounterAsync();

        var act = async () => await _connection.WithUnitOfWorkAsync<int>(async (uow, ct) =>
        {
            await IncrementCounterAsync(uow.Transaction);
            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated failure");

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Savepoint Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSavepointAsync_RollbackToSavepoint_UndoesChangesAfterSavepoint()
    {
        var valueBefore = await ReadCounterAsync();

        await using var uow = await _connection.BeginUnitOfWorkAsync();

        // First increment — before savepoint S1
        await IncrementCounterAsync(uow.Transaction);
        var savepoint = await uow.CreateSavepointAsync("S1");
        savepoint.Name.Should().Be("S1");

        // Second increment — after savepoint S1
        await IncrementCounterAsync(uow.Transaction);
        await savepoint.RollbackAsync();

        // Savepoint release
        await savepoint.ReleaseAsync();

        // Commit remaining transaction
        await uow.CommitAsync();

        var valueAfter = await ReadCounterAsync();
        valueAfter.Should().Be(valueBefore + 1, "only the change before the savepoint should persist");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Guards, Validation and Mock Non-DbConnection/Non-DbTransaction paths
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnitOfWork_Constructor_NullTransaction_ThrowsArgumentNullException()
    {
        var act = () => new UowImpl(null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public async Task BeginUnitOfWorkAsync_NullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? nullConn = null;
        var act = async () => await nullConn!.BeginUnitOfWorkAsync();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_NullAction_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.WithUnitOfWorkAsync((Func<IUnitOfWork, CancellationToken, Task>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_NullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? nullConn = null;
        var act = async () => await nullConn!.WithUnitOfWorkAsync(async (uow, ct) => await Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_ReturningOverload_NullAction_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.WithUnitOfWorkAsync<int>((Func<IUnitOfWork, CancellationToken, Task<int>>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_ReturningOverload_NullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? nullConn = null;
        var act = async () => await nullConn!.WithUnitOfWorkAsync<int>(async (uow, ct) => await Task.FromResult(42));
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task CommitAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        var uow = await _connection.BeginUnitOfWorkAsync();
        await uow.DisposeAsync();
        var act = async () => await uow.CommitAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task RollbackAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        var uow = await _connection.BeginUnitOfWorkAsync();
        await uow.DisposeAsync();
        var act = async () => await uow.RollbackAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CreateSavepointAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        var uow = await _connection.BeginUnitOfWorkAsync();
        await uow.DisposeAsync();
        var act = async () => await uow.CreateSavepointAsync("S1");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSavepointAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        await using var uow = await _connection.BeginUnitOfWorkAsync();
        var act = async () => await uow.CreateSavepointAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(name))
            .WithMessage("Savepoint name must not be empty.*");
    }

    [Fact]
    public async Task NonDbTransaction_Commit_Rollback_Savepoint_Dispose_ExecutedProperly()
    {
        var mockTx = Substitute.For<IDbTransaction, IDisposable>();
        mockTx.IsolationLevel.Returns(IsolationLevel.ReadCommitted);

        var uow = new UowImpl(mockTx);
        uow.Transaction.Should().BeSameAs(mockTx);
        uow.IsolationLevel.Should().Be(IsolationLevel.ReadCommitted);

        // Commit on non-DbTransaction calls synchronous Commit
        await uow.CommitAsync();
        mockTx.Received(1).Commit();

        // Rollback on non-DbTransaction calls synchronous Rollback
        var uow2 = new UowImpl(mockTx);
        await uow2.RollbackAsync();
        mockTx.Received(1).Rollback();

        // CreateSavepoint on non-DbTransaction returns NoOpSavepoint
        var uow3 = new UowImpl(mockTx);
        var noOpSavepoint = await uow3.CreateSavepointAsync("NoOpSavepoint");
        noOpSavepoint.Should().NotBeNull();
        noOpSavepoint.Name.Should().Be("NoOpSavepoint");
        await noOpSavepoint.RollbackAsync();
        await noOpSavepoint.ReleaseAsync();

        // Dispose non-DbTransaction without commit triggers synchronous rollback and dispose
        var mockTxToRollback = Substitute.For<IDbTransaction, IDisposable>();
        var uow4 = new UowImpl(mockTxToRollback);
        await uow4.DisposeAsync();
        mockTxToRollback.Received(1).Rollback();
        ((IDisposable)mockTxToRollback).Received(1).Dispose();

        // Dispose swallowing exceptions when rollback fails
        var failingTx = Substitute.For<IDbTransaction, IDisposable>();
        failingTx.When(x => x.Rollback()).Do(_ => throw new InvalidOperationException("Connection broken"));
        var uow5 = new UowImpl(failingTx);
        var actDisposeFailing = async () => await uow5.DisposeAsync();
        await actDisposeFailing.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NonDbConnection_BeginUnitOfWorkAsync_HandlesOpenAndBeginTransaction()
    {
        var mockTx = Substitute.For<IDbTransaction, IDisposable>();
        var mockConn = Substitute.For<IDbConnection>();
        mockConn.State.Returns(ConnectionState.Closed, ConnectionState.Open);
        mockConn.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(mockTx);

        var uow = await mockConn.BeginUnitOfWorkAsync(IsolationLevel.RepeatableRead);

        mockConn.Received(1).Open();
        mockConn.Received(1).BeginTransaction(IsolationLevel.RepeatableRead);
        uow.Should().NotBeNull();
    }

    [Fact]
    public async Task UnitOfWorkExtensions_Guards_ThrowOnNullArguments()
    {
        DbConnection nullDbConn = null!;
        var actNullDb = async () => await nullDbConn.BeginUnitOfWorkAsync();
        await actNullDb.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        IDbConnection nullConn = null!;
        var actNullConn1 = async () => await nullConn.WithUnitOfWorkAsync((uow, ct) => Task.CompletedTask);
        await actNullConn1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var actNullConn2 = async () => await nullConn.WithUnitOfWorkAsync((uow, ct) => Task.FromResult(42));
        await actNullConn2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var actNullAction1 = async () => await _connection.WithUnitOfWorkAsync((Func<IUnitOfWork, CancellationToken, Task>)null!);
        await actNullAction1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");

        var actNullAction2 = async () => await _connection.WithUnitOfWorkAsync((Func<IUnitOfWork, CancellationToken, Task<int>>)null!);
        await actNullAction2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }
}
