// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Polly;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Resilience;

public sealed class SqlResilienceExtensionsTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private readonly ResiliencePipeline _pipeline = SqlResilienceDefaults.Standard(SqliteTransientErrorDetector.Default);

    private static SqlResult FakeQuery => new("SELECT 1 AS Val", new Dictionary<string, object?>());

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT NOT NULL);";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── Guard assertions ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).ExecuteWithResilienceAsync(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.ExecuteWithResilienceAsync(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QuerySingleWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QuerySingleWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QuerySingleOrDefaultWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QuerySingleOrDefaultWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryFirstWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryFirstWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryFirstOrDefaultWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryFirstOrDefaultWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).ExecuteScalarWithResilienceAsync<int>(FakeQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.ExecuteScalarWithResilienceAsync<int>(FakeQuery, (ResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    // ─── Execution with in-memory SQLite ─────────────────────────────────

    [Fact]
    public async Task ExecuteWithResilienceAsync_ExecutesAndReturnsAffectedRows()
    {
        var query = new SqlResult("INSERT INTO items (id, name) VALUES (@id, @name);", new Dictionary<string, object?>
        {
            ["@id"] = 1,
            ["@name"] = "Item 1"
        });

        var affected = await _connection.ExecuteWithResilienceAsync(query, _pipeline);
        affected.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_InsideTransaction_ExecutesSuccessfully()
    {
        using var tx = _connection.BeginTransaction();
        var query = new SqlResult("INSERT INTO items (id, name) VALUES (@id, @name);", new Dictionary<string, object?>
        {
            ["@id"] = 2,
            ["@name"] = "Item 2"
        });

        var affected = await _connection.ExecuteWithResilienceAsync(query, _pipeline, transaction: tx);
        tx.Commit();

        affected.Should().Be(1);
    }

    [Fact]
    public async Task QueryWithResilienceAsync_ReturnsSequence()
    {
        var insert = new SqlResult("INSERT INTO items (id, name) VALUES (10, 'A'), (20, 'B');", new Dictionary<string, object?>());
        await _connection.ExecuteWithResilienceAsync(insert, _pipeline);

        var query = new SqlResult("SELECT id AS Id, name AS Name FROM items ORDER BY id;", new Dictionary<string, object?>());
        var results = (await _connection.QueryWithResilienceAsync<ItemDto>(query, _pipeline)).ToList();

        results.Should().HaveCount(2);
        results[0].Id.Should().Be(10);
        results[0].Name.Should().Be("A");
        results[1].Id.Should().Be(20);
        results[1].Name.Should().Be("B");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_ReturnsSingleItem()
    {
        var insert = new SqlResult("INSERT INTO items (id, name) VALUES (100, 'Single');", new Dictionary<string, object?>());
        await _connection.ExecuteWithResilienceAsync(insert, _pipeline);

        var query = new SqlResult("SELECT id AS Id, name AS Name FROM items WHERE id = 100;", new Dictionary<string, object?>());
        var result = await _connection.QuerySingleWithResilienceAsync<ItemDto>(query, _pipeline);

        result.Should().NotBeNull();
        result.Id.Should().Be(100);
        result.Name.Should().Be("Single");
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_ReturnsItemOrDefault()
    {
        var queryFound = new SqlResult("SELECT 42;", new Dictionary<string, object?>());
        var found = await _connection.QuerySingleOrDefaultWithResilienceAsync<int>(queryFound, _pipeline);
        found.Should().Be(42);

        var queryNotFound = new SqlResult("SELECT id FROM items WHERE id = -999;", new Dictionary<string, object?>());
        var notFound = await _connection.QuerySingleOrDefaultWithResilienceAsync<int?>(queryNotFound, _pipeline);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_ReturnsFirstItem()
    {
        var insert = new SqlResult("INSERT INTO items (id, name) VALUES (201, 'First'), (202, 'Second');", new Dictionary<string, object?>());
        await _connection.ExecuteWithResilienceAsync(insert, _pipeline);

        var query = new SqlResult("SELECT id AS Id, name AS Name FROM items WHERE id >= 201 ORDER BY id;", new Dictionary<string, object?>());
        var result = await _connection.QueryFirstWithResilienceAsync<ItemDto>(query, _pipeline);

        result.Should().NotBeNull();
        result.Id.Should().Be(201);
        result.Name.Should().Be("First");
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_ReturnsFirstItemOrDefault()
    {
        var queryFound = new SqlResult("SELECT 99;", new Dictionary<string, object?>());
        var found = await _connection.QueryFirstOrDefaultWithResilienceAsync<int>(queryFound, _pipeline);
        found.Should().Be(99);

        var queryNotFound = new SqlResult("SELECT id FROM items WHERE id = -999;", new Dictionary<string, object?>());
        var notFound = await _connection.QueryFirstOrDefaultWithResilienceAsync<int?>(queryNotFound, _pipeline);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_ReturnsScalarValue()
    {
        var insert = new SqlResult("INSERT INTO items (id, name) VALUES (301, 'A'), (302, 'B'), (303, 'C');", new Dictionary<string, object?>());
        await _connection.ExecuteWithResilienceAsync(insert, _pipeline);

        var countQuery = new SqlResult("SELECT COUNT(*) FROM items WHERE id >= 301;", new Dictionary<string, object?>());
        var count = await _connection.ExecuteScalarWithResilienceAsync<long>(countQuery, _pipeline);

        count.Should().Be(3);
    }

    [Fact]
    public async Task PollyPipeline_WithTransaction_ExecutesSuccessfully()
    {
        using var tx = _connection.BeginTransaction();
        var insertTx = new SqlResult("INSERT INTO items (name) VALUES (@name)", new Dictionary<string, object?> { ["name"] = "polly-tx-item" });
        var rows = await _connection.ExecuteWithResilienceAsync(insertTx, _pipeline, transaction: tx);
        rows.Should().Be(1);

        var queryTx = new SqlResult("SELECT name FROM items WHERE name = @name", new Dictionary<string, object?> { ["name"] = "polly-tx-item" });
        var items = await _connection.QueryWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        items.Should().ContainSingle();

        var single = await _connection.QuerySingleWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        single.Should().Be("polly-tx-item");

        var singleOrDef = await _connection.QuerySingleOrDefaultWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        singleOrDef.Should().Be("polly-tx-item");

        var first = await _connection.QueryFirstWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        first.Should().Be("polly-tx-item");

        var firstOrDef = await _connection.QueryFirstOrDefaultWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        firstOrDef.Should().Be("polly-tx-item");

        var scalar = await _connection.ExecuteScalarWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        scalar.Should().Be("polly-tx-item");

        tx.Rollback();
    }

    private sealed class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
