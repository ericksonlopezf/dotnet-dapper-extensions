// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.SqlServer.Transactions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.SqlServer.Tests.Unit;

public sealed class TransactionExtensionsTests
{
    // ─── Non-Generic ExecuteInTransactionAsync Tests ──────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(async trx => await Task.Yield());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationNull_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<DbConnection>();
        Func<DbTransaction, Task> operation = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionClosed_OpensCommitsAndCloses()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            trx.Should().Be(transaction);
        }, cts.Token);

        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionOpen_DoesNotOpenOrClose()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        await connection.ExecuteInTransactionAsync(async trx => await Task.Yield());

        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackClosesAndRethrows()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var act = async () => await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Operation failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Operation failure");

        await transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    // ─── Generic ExecuteInTransactionAsync<TResult> Tests ─────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            return 1;
        });
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenOperationNull_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<DbConnection>();
        Func<DbTransaction, Task<int>> operation = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionClosed_OpensCommitsReturnsAndCloses()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var result = await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            return "SUCCESS";
        }, cts.Token);

        result.Should().Be("SUCCESS");
        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenConnectionOpen_DoesNotOpenOrClose()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var result = await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            return 99;
        });

        result.Should().Be(99);
        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncGeneric_WhenOperationThrows_RollsBackClosesAndRethrows()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            throw new ArgumentOutOfRangeException("error");
        });

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();

        await transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }
}
