// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.DapperExtensions.Testing.Common;

/// <summary>
/// Reusable test base for verifying dialect-native TransactionExtensions across database providers.
/// Uses pure in-memory ADO.NET fakes (TestAdoConnection, TestAdoTransaction) with zero dynamic mocking overhead.
/// </summary>
public abstract class TransactionExtensionsTestsBase
{
    protected abstract Task ExecuteAsync(DbConnection connection, Func<DbTransaction, Task> operation, CancellationToken cancellationToken = default);
    protected abstract Task<TResult> ExecuteAsync<TResult>(DbConnection connection, Func<DbTransaction, Task<TResult>> operation, CancellationToken cancellationToken = default);

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await ExecuteAsync(connection, async trx => await Task.Yield());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        Func<DbTransaction, Task> operation = null!;
        var act = async () => await ExecuteAsync(connection, operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionClosed_OpensCommitsAndCloses()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Closed);
        var cts = new CancellationTokenSource();
        DbTransaction? capturedTrx = null;

        await ExecuteAsync(connection, async trx =>
        {
            await Task.Yield();
            capturedTrx = trx;
        }, cts.Token);

        connection.Spy.WasOpenAsyncCalled.Should().BeTrue();
        connection.Spy.CloseCount.Should().Be(1);
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasCommitAsyncCalled.Should().BeTrue();
        capturedTrx.Should().BeSameAs(connection.Spy.LastTransaction);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionOpen_DoesNotOpenOrClose()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Open);

        await ExecuteAsync(connection, async trx => await Task.Yield());

        connection.Spy.WasOpenAsyncCalled.Should().BeFalse();
        connection.Spy.CloseCount.Should().Be(0);
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasCommitAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackClosesAndRethrows()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Closed);

        var act = async () => await ExecuteAsync(connection, async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Operation failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Operation failure");
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasRollbackAsyncCalled.Should().BeTrue();
        connection.Spy.LastTransaction!.WasCommitAsyncCalled.Should().BeFalse();
        connection.Spy.CloseCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_WhenConnectionOpen_RollsBackAndRethrows()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Open);

        var act = async () => await ExecuteAsync(connection, async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Operation failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Operation failure");
        connection.Spy.WasOpenAsyncCalled.Should().BeFalse();
        connection.Spy.CloseCount.Should().Be(0);
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasRollbackAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await ExecuteAsync<int>(connection, async trx =>
        {
            await Task.Yield();
            return 1;
        });
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenOperationNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        Func<DbTransaction, Task<int>> operation = null!;
        var act = async () => await ExecuteAsync(connection, operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionClosed_OpensCommitsReturnsAndCloses()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Closed);
        var cts = new CancellationTokenSource();

        var result = await ExecuteAsync(connection, async trx =>
        {
            await Task.Yield();
            return "SUCCESS";
        }, cts.Token);

        result.Should().Be("SUCCESS");
        connection.Spy.WasOpenAsyncCalled.Should().BeTrue();
        connection.Spy.CloseCount.Should().Be(1);
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasCommitAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionOpen_DoesNotOpenOrClose()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Open);

        var result = await ExecuteAsync(connection, async trx =>
        {
            await Task.Yield();
            return 99;
        });

        result.Should().Be(99);
        connection.Spy.WasOpenAsyncCalled.Should().BeFalse();
        connection.Spy.CloseCount.Should().Be(0);
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasCommitAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenOperationThrows_RollsBackClosesAndRethrows()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Closed);

        var act = async () => await ExecuteAsync<int>(connection, async trx =>
        {
            await Task.Yield();
            throw new ArgumentOutOfRangeException("error");
        });

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasRollbackAsyncCalled.Should().BeTrue();
        connection.Spy.CloseCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldThrowIfRollbackFails()
    {
        using var connection = new TestAdoConnection(initialState: ConnectionState.Open);
        connection.TransactionFactory = (conn, iso) =>
        {
            var tx = new TestAdoTransaction(conn, iso);
            tx.OnRollbackAsync = ct => Task.FromException(new Exception("Rollback failed"));
            return tx;
        };

        var cts = new CancellationTokenSource();
        var act = async () => await ExecuteAsync<int>(connection, async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<Exception>().WithMessage("Rollback failed");
        connection.Spy.WasOpenAsyncCalled.Should().BeFalse();
        connection.Spy.LastTransaction.Should().NotBeNull();
        connection.Spy.LastTransaction!.WasRollbackAsyncCalled.Should().BeTrue();
    }
}
