// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Oracle.Transactions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.Oracle.Tests.Unit;

public sealed class TransactionExtensionsTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_ClosedConnection_OpensCommitsAndClosesOnSuccess()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        bool executed = false;
        await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            trx.Should().Be(transaction);
            executed = true;
        }, cts.Token);

        executed.Should().BeTrue();
        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OpenConnection_DoesNotOpenOrClose()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
        });

        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackAndRethrowOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var act = async () => await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Simulation failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulation failure");
        await transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

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
    public async Task ExecuteInTransactionAsync_WithResult_ClosedConnection_OpensCommitsAndReturnsResult()
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
            trx.Should().Be(transaction);
            return "ORACLE_SUCCESS";
        }, cts.Token);

        result.Should().Be("ORACLE_SUCCESS");
        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_OpenConnection_DoesNotOpenOrClose()
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
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldRollbackAndRethrowOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(transaction));

        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            throw new TimeoutException("Database timeout");
        });

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("Database timeout");
        await transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await connection.Received(1).CloseAsync();
        await transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            return 1;
        });
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_WhenOperationNull_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<DbConnection>();
        Func<DbTransaction, Task<int>> operation = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }
}
