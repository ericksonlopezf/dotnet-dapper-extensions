// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Streaming;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Streaming;

/// <summary>
/// Unit tests for <see cref="DapperStreamingExtensions"/>.
/// </summary>
public sealed class DapperStreamingExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DapperStreamingExtensionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE TestItems (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);";
        cmd.ExecuteNonQuery();

        using var insertCmd = _connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO TestItems (Id, Name) VALUES 
            (1, 'Alpha'),
            (2, 'Beta'),
            (3, 'Gamma'),
            (4, 'Delta'),
            (5, 'Epsilon');
            """;
        insertCmd.ExecuteNonQuery();
    }

    [Fact]
    public void StreamAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        IDbConnection nullConn = null!;
        var act = () => nullConn.StreamAsync<StreamingTestItem>("SELECT * FROM TestItems");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void StreamAsync_WithCommandDefinition_WithNullConnection_ThrowsArgumentNullException()
    {
        IDbConnection nullConn = null!;
        var cmd = new CommandDefinition("SELECT * FROM TestItems");
        var act = () => nullConn.StreamAsync<StreamingTestItem>(cmd);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void StreamAsync_WithNullSql_ThrowsArgumentNullException()
    {
        var act = () => _connection.StreamAsync<StreamingTestItem>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sql");
    }

    [Fact]
    public async Task StreamAsync_WithValidQuery_StreamsAllRowsCorrectly()
    {
        var items = new List<StreamingTestItem>();

        await foreach (var item in _connection.StreamAsync<StreamingTestItem>("SELECT Id, Name FROM TestItems ORDER BY Id ASC"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(5);
        items[0].Id.Should().Be(1);
        items[0].Name.Should().Be("Alpha");
        items[4].Id.Should().Be(5);
        items[4].Name.Should().Be("Epsilon");
    }

    [Fact]
    public async Task StreamAsync_WithParameters_StreamsFilteredRows()
    {
        var items = new List<StreamingTestItem>();

        await foreach (var item in _connection.StreamAsync<StreamingTestItem>(
            "SELECT Id, Name FROM TestItems WHERE Id > @MinId ORDER BY Id ASC",
            new { MinId = 2 }))
        {
            items.Add(item);
        }

        items.Should().HaveCount(3);
        items[0].Id.Should().Be(3);
        items[1].Id.Should().Be(4);
        items[2].Id.Should().Be(5);
    }

    [Fact]
    public async Task StreamAsync_WithCancellation_AbortsStream()
    {
        using var cts = new CancellationTokenSource();
        var items = new List<StreamingTestItem>();

        var act = async () =>
        {
            await foreach (var item in _connection.StreamAsync<StreamingTestItem>(
                "SELECT Id, Name FROM TestItems ORDER BY Id ASC",
                cancellationToken: cts.Token))
            {
                items.Add(item);
                if (items.Count == 2)
                {
                    cts.Cancel();
                }
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task StreamAsync_WithCommandDefinition_StreamsRowsCorrectly()
    {
        var cmd = new CommandDefinition(
            "SELECT Id, Name FROM TestItems WHERE Name = @Name",
            new { Name = "Gamma" });

        var items = new List<StreamingTestItem>();
        await foreach (var item in _connection.StreamAsync<StreamingTestItem>(cmd))
        {
            items.Add(item);
        }

        items.Should().HaveCount(1);
        items[0].Name.Should().Be("Gamma");
    }

    [Fact]
    public async Task StreamAsync_WhenConnectionClosed_OpensConnectionAutomaticallyAndStreams()
    {
        using var master = new SqliteConnection("Data Source=InMemoryStreamClosedTest;Mode=Memory;Cache=Shared");
        await master.OpenAsync();
        await master.ExecuteAsync("CREATE TABLE ClosedTest (Id INTEGER PRIMARY KEY, Val TEXT); INSERT INTO ClosedTest VALUES (1, 'Alpha');");

        using var closedConnection = new SqliteConnection("Data Source=InMemoryStreamClosedTest;Mode=Memory;Cache=Shared");
        closedConnection.State.Should().Be(ConnectionState.Closed);

        var items = new List<string>();
        await foreach (var item in closedConnection.StreamAsync<string>("SELECT Val FROM ClosedTest"))
        {
            items.Add(item);
        }

        closedConnection.State.Should().Be(ConnectionState.Open);
        items.Should().ContainSingle().Which.Should().Be("Alpha");
    }

    [Fact]
    public async Task StreamAsync_WithAlreadyCancelledToken_ThrowsImmediatelyWithoutReading()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () =>
        {
            await foreach (var _ in _connection.StreamAsync<StreamingTestItem>(
                "SELECT Id, Name FROM TestItems",
                cancellationToken: cts.Token))
            {
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_WithCommandDefinition_WhenCancelledMidStream_AbortsStream()
    {
        using var cts = new CancellationTokenSource();
        var cmd = new CommandDefinition(
            "SELECT Id, Name FROM TestItems ORDER BY Id ASC",
            cancellationToken: cts.Token);

        var items = new List<StreamingTestItem>();
        var act = async () =>
        {
            await foreach (var item in _connection.StreamAsync<StreamingTestItem>(cmd))
            {
                items.Add(item);
                if (items.Count == 1)
                {
                    cts.Cancel();
                }
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task StreamAsync_WhenConsumerThrowsDuringIteration_AbortsAndReleasesConnection()
    {
        var act = async () =>
        {
            await foreach (var item in _connection.StreamAsync<StreamingTestItem>(
                "SELECT Id, Name FROM TestItems ORDER BY Id ASC"))
            {
                if (item.Id == 2)
                {
                    throw new InvalidOperationException("Consumer loop aborted");
                }
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Consumer loop aborted");

        // Verify the connection is unblocked and reader was disposed in finally
        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestItems");
        count.Should().Be(5);
    }

    [Fact]
    public async Task StreamAsync_WithCommandDefinition_WhenExplicitCancellationTokenPassed_OverridesDefaultCommandToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cmd = new CommandDefinition("SELECT Id, Name FROM TestItems", cancellationToken: default);

        var act = async () =>
        {
            await foreach (var _ in _connection.StreamAsync<StreamingTestItem>(cmd, cancellationToken: cts.Token))
            {
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_WithNonDbConnection_OpensConnectionSynchronouslyAndStreams()
    {
        using var rawConn = new SqliteConnection("Data Source=sharedmem;Mode=Memory;Cache=Shared");
        rawConn.Open();
        using (var setupCmd = rawConn.CreateCommand())
        {
            setupCmd.CommandText = "CREATE TABLE TestItems (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL); INSERT INTO TestItems (Id, Name) VALUES (1, 'Alpha');";
            setupCmd.ExecuteNonQuery();
        }

        using var closedConn = new SqliteConnection("Data Source=sharedmem;Mode=Memory;Cache=Shared");
        var nonDbConn = new NonDbConnectionWrapper(closedConn);
        nonDbConn.State.Should().Be(ConnectionState.Closed);

        var items = new List<StreamingTestItem>();
        await foreach (var item in nonDbConn.StreamAsync<StreamingTestItem>("SELECT Id, Name FROM TestItems WHERE Id = 1"))
        {
            items.Add(item);
        }

        nonDbConn.WasOpened.Should().BeTrue();
        items.Should().ContainSingle().Which.Name.Should().Be("Alpha");
    }

#pragma warning disable CS8765, CS8767, CS8766, CS8769
    private sealed class NonDbConnectionWrapper : IDbConnection
    {
        private readonly IDbConnection _inner;
        public bool WasOpened { get; private set; }

        public NonDbConnectionWrapper(IDbConnection inner)
        {
            _inner = inner;
        }

        public ConnectionState State => _inner.State;
        public string ConnectionString { get => _inner.ConnectionString ?? ""; set => _inner.ConnectionString = value; }
        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database ?? "";

        public void Open()
        {
            WasOpened = true;
            _inner.Open();
        }

        public void Close() => _inner.Close();
        public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public IDbCommand CreateCommand() => _inner.CreateCommand();
        public void Dispose() => _inner.Dispose();
    }
#pragma warning restore CS8765, CS8767, CS8766, CS8769

    public void Dispose()
    {
        _connection.Dispose();
    }
}
