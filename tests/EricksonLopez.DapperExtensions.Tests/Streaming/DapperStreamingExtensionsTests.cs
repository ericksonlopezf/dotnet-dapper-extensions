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

    public void Dispose()
    {
        _connection.Dispose();
    }
}
