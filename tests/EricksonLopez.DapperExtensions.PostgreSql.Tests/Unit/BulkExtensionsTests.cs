// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.PostgreSql.Bulk;
using EricksonLopez.SqlBuilder;
using Npgsql;
using NpgsqlTypes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

public sealed class BulkExtensionsTests
{
    [Fact]
    public async Task BulkInsertAsync_WhenParametersEmpty_ShouldReturnZeroAndNotExecute()
    {
        var connection = Substitute.For<DbConnection>();
        var result = await connection.BulkInsertAsync("INSERT", Array.Empty<NpgsqlParameter>());

        result.Should().Be(0);
        connection.DidNotReceive().CreateCommand();
    }

    [Fact]
    public async Task BulkInsertAsync_ShouldExecuteCommandAndReturnAffectedRows()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parameters = Substitute.For<DbParameterCollection>();

        connection.State.Returns(ConnectionState.Closed);
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        command.ExecuteNonQueryAsync().Returns(42);

        var npgsqlParams = new[] { new NpgsqlParameter("test", NpgsqlDbType.Text) };
        var result = await connection.BulkInsertAsync("INSERT SQL", npgsqlParams);

        result.Should().Be(42);
        command.CommandText.Should().Be("INSERT SQL");
        command.CommandTimeout.Should().Be(30); // Default timeout
        await connection.Received(1).OpenAsync();
        parameters.Received(1).AddRange(npgsqlParams);
    }

    [Fact]
    public async Task BulkInsertAsync_WithCustomTimeout_ShouldSetCommandTimeout()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parameters = Substitute.For<DbParameterCollection>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);

        var npgsqlParams = new[] { new NpgsqlParameter("test", NpgsqlDbType.Text) };
        await connection.BulkInsertAsync("INSERT SQL", npgsqlParams, commandTimeout: 45);

        command.CommandTimeout.Should().Be(45);
    }

    [Fact]
    public async Task BulkUpsertAsync_ShouldCallInsertInternally()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parameters = Substitute.For<DbParameterCollection>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        command.ExecuteNonQueryAsync().Returns(10);

        var npgsqlParams = new[] { new NpgsqlParameter("test", NpgsqlDbType.Text) };
        var result = await connection.BulkUpsertAsync("UPSERT SQL", npgsqlParams, commandTimeout: 60);

        result.Should().Be(10);
        command.CommandText.Should().Be("UPSERT SQL");
        command.CommandTimeout.Should().Be(60);
        await connection.DidNotReceive().OpenAsync();
    }

    [Fact]
    public async Task BulkInsertAsync_WhenConnectionIsOpen_ShouldNotOpenOrCloseConnection()
    {
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);
        var command = Substitute.For<DbCommand>();
        var parametersCollection = Substitute.For<DbParameterCollection>();
        command.Parameters.Returns(parametersCollection);
        connection.CreateCommand().Returns(command);
        command.ExecuteNonQueryAsync(Arg.Any<CancellationToken>()).Returns(5);

        var parameters = new[] { new NpgsqlParameter("test", 1) };
        var transaction = Substitute.For<DbTransaction>();
        var result = await connection.BulkInsertAsync("UPSERT SQL", parameters, transaction);

        result.Should().Be(5);
        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
        await connection.DidNotReceive().CloseAsync();
    }

    [Fact]
    public async Task BulkInsertAsync_WithTransaction_ShouldSetTransactionOnCommand()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parametersCollection = Substitute.For<DbParameterCollection>();
        command.Parameters.Returns(parametersCollection);
        connection.CreateCommand().Returns(command);
        command.ExecuteNonQueryAsync().Returns(10);
        connection.State.Returns(ConnectionState.Open);

        var parameters = new[] { new NpgsqlParameter("test", 1) };
        var transaction = Substitute.For<DbTransaction>();

        await connection.BulkInsertAsync("UPSERT SQL", parameters, transaction);

        command.Transaction.Should().Be(transaction);
    }

    [Fact]
    public async Task BulkInsertAsync_WhenConnectionNull_ShouldThrow()
    {
        DbConnection connection = null!;
        var act = async () => await connection.BulkInsertAsync("sql", Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task BulkInsertAsync_WhenSqlNullOrWhiteSpace_ShouldThrow()
    {
        var connection = Substitute.For<DbConnection>();
        var act = async () => await connection.BulkInsertAsync("", Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task BulkInsertAsync_WhenParametersNull_ShouldThrow()
    {
        var connection = Substitute.For<DbConnection>();
        var act = async () => await connection.BulkInsertAsync("INSERT", null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("parameters");
    }

    [Fact]
    public async Task BulkDeleteAsync_ShouldCallInsertInternallyAndReturnAffectedRows()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parameters = Substitute.For<DbParameterCollection>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        command.ExecuteNonQueryAsync().Returns(15);

        var npgsqlParams = new[] { new NpgsqlParameter("test", NpgsqlDbType.Array | NpgsqlDbType.Uuid) };
        var result = await connection.BulkDeleteAsync("DELETE SQL", npgsqlParams, commandTimeout: 30);

        result.Should().Be(15);
        command.CommandText.Should().Be("DELETE SQL");
    }

    [Fact]
    public async Task BulkUpdateAsync_ShouldCallInsertInternallyAndReturnAffectedRows()
    {
        var connection = Substitute.For<DbConnection>();
        var command = Substitute.For<DbCommand>();
        var parameters = Substitute.For<DbParameterCollection>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        command.ExecuteNonQueryAsync().Returns(25);

        var npgsqlParams = new[] { new NpgsqlParameter("test", NpgsqlDbType.Array | NpgsqlDbType.Text) };
        var result = await connection.BulkUpdateAsync("UPDATE SQL", npgsqlParams, commandTimeout: 30);

        result.Should().Be(25);
        command.CommandText.Should().Be("UPDATE SQL");
    }
}







