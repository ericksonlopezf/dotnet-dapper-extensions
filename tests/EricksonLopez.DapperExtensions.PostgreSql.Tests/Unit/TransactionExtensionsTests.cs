// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.PostgreSql.Transactions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

public sealed class TransactionExtensionsTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            trx.Should().Be(transaction);
        }, cts.Token);

        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackAndCloseConnectionOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var act = async () => await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).RollbackAsync(cts.Token);
        await connection.Received(1).CloseAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var act = async () => await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).RollbackAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldThrowIfRollbackFails()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        transaction.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new Exception("Rollback failed")));

        var cts = new CancellationTokenSource();
        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<Exception>().WithMessage("Rollback failed");

        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).RollbackAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldCommitAndReturn()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var result = await connection.ExecuteInTransactionAsync(async trx =>
        {
            await Task.Yield();
            return 42;
        }, cts.Token);

        result.Should().Be(42);
        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).CommitAsync(cts.Token);
        await connection.Received(1).CloseAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldRollbackAndCloseConnectionOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Closed);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await connection.Received(1).OpenAsync(cts.Token);
        await transaction.Received(1).RollbackAsync(cts.Token);
        await connection.Received(1).CloseAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_ShouldRollbackOnException()
    {
        var connection = Substitute.For<DbConnection>();
        var transaction = Substitute.For<DbTransaction>();

        connection.State.Returns(ConnectionState.Open);
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }, cts.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await transaction.Received(1).RollbackAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionNull_ShouldThrow()
    {
        DbConnection connection = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(async trx => await Task.Yield());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationNull_ShouldThrow()
    {
        var connection = Substitute.For<DbConnection>();
        Func<DbTransaction, Task> operation = null!;
        var act = async () => await connection.ExecuteInTransactionAsync(operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_WhenConnectionNull_ShouldThrow()
    {
        DbConnection connection = null!;
        var act = async () => await connection.ExecuteInTransactionAsync<int>(async trx => { await Task.Yield(); return 1; });
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_WhenOperationNull_ShouldThrow()
    {
        var connection = Substitute.For<DbConnection>();
        Func<DbTransaction, Task<int>> operation = null!;
        var act = async () => await connection.ExecuteInTransactionAsync<int>(operation);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionIsOpen_ShouldNotOpenOrCloseConnection()
    {
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);
        var transaction = Substitute.For<DbTransaction>();
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        await connection.ExecuteInTransactionAsync(async trx => await Task.Yield(), cts.Token);

        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).CommitAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_WhenConnectionIsOpen_ShouldNotOpenOrCloseConnection()
    {
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);
        var transaction = Substitute.For<DbTransaction>();
        connection.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<DbTransaction>(transaction));

        var cts = new CancellationTokenSource();
        var result = await connection.ExecuteInTransactionAsync(async trx => { await Task.Yield(); return 42; }, cts.Token);

        result.Should().Be(42);
        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
        await transaction.Received(1).CommitAsync(cts.Token);
    }
}





