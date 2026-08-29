// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Pipelines;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Resilience;

/// <summary>
/// Validates the canonical <see cref="IResiliencePipeline"/>-based overloads of <see cref="SqlResilienceExtensions"/>.
/// These tests verify the ecosystem-aligned API introduced as part of the DapperExtensions resilience convergence (ADR-017).
/// </summary>
public sealed class SqlResilienceExtensionsEcosystemTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private readonly IResiliencePipeline _pipeline = new PassthroughResiliencePipeline("test-passthrough");

    private static SqlResult ScalarQuery => new("SELECT 1 AS Val", new Dictionary<string, object?>());
    private static SqlResult InsertQuery => new("INSERT INTO items (name) VALUES (@name)", new Dictionary<string, object?> { ["name"] = "test" });
    private static SqlResult SelectItemsQuery => new("SELECT name FROM items", new Dictionary<string, object?>());
    private static SqlResult SelectFirstQuery => new("SELECT 1 AS Val UNION ALL SELECT 2 AS Val", new Dictionary<string, object?>());

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

    // ─── Guard assertions — canonical IResiliencePipeline overloads ──────────

    [Fact]
    public async Task ExecuteWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).ExecuteWithResilienceAsync(InsertQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.ExecuteWithResilienceAsync(InsertQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryWithResilienceAsync<int>(ScalarQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryWithResilienceAsync<int>(ScalarQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QuerySingleWithResilienceAsync<int>(ScalarQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QuerySingleWithResilienceAsync<int>(ScalarQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QuerySingleOrDefaultWithResilienceAsync<int>(ScalarQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QuerySingleOrDefaultWithResilienceAsync<int>(ScalarQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryFirstWithResilienceAsync<int>(SelectFirstQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryFirstWithResilienceAsync<int>(SelectFirstQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).QueryFirstOrDefaultWithResilienceAsync<int>(SelectFirstQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.QueryFirstOrDefaultWithResilienceAsync<int>(SelectFirstQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_EcosystemPipeline_NullConnection_ThrowsArgumentNullException()
    {
        var act = async () => await ((IDbConnection)null!).ExecuteScalarWithResilienceAsync<int>(ScalarQuery, _pipeline);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_EcosystemPipeline_NullPipeline_ThrowsArgumentNullException()
    {
        var act = async () => await _connection.ExecuteScalarWithResilienceAsync<int>(ScalarQuery, (IResiliencePipeline)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("pipeline");
    }

    // ─── Functional tests — canonical IResiliencePipeline overloads ──────────

    [Fact]
    public async Task ExecuteWithResilienceAsync_EcosystemPipeline_WhenSuccessful_ReturnsRowCount()
    {
        var rowsAffected = await _connection.ExecuteWithResilienceAsync(InsertQuery, _pipeline);
        rowsAffected.Should().Be(1);
    }

    [Fact]
    public async Task QueryWithResilienceAsync_EcosystemPipeline_WhenSuccessful_ReturnsResults()
    {
        await _connection.ExecuteWithResilienceAsync(InsertQuery, _pipeline);
        var result = await _connection.QueryWithResilienceAsync<string>(SelectItemsQuery, _pipeline);
        result.Should().ContainSingle().Which.Should().Be("test");
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_EcosystemPipeline_WhenSuccessful_ReturnsValue()
    {
        var result = await _connection.QuerySingleWithResilienceAsync<int>(ScalarQuery, _pipeline);
        result.Should().Be(1);
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_EcosystemPipeline_WhenMatches_ReturnsValue()
    {
        var result = await _connection.QuerySingleOrDefaultWithResilienceAsync<int>(ScalarQuery, _pipeline);
        result.Should().Be(1);
    }

    [Fact]
    public async Task QuerySingleOrDefaultWithResilienceAsync_EcosystemPipeline_WhenEmpty_ReturnsDefault()
    {
        var emptyQuery = new SqlResult("SELECT 1 AS Val WHERE 1=0", new Dictionary<string, object?>());
        var result = await _connection.QuerySingleOrDefaultWithResilienceAsync<int>(emptyQuery, _pipeline);
        result.Should().Be(0);
    }

    [Fact]
    public async Task QueryFirstWithResilienceAsync_EcosystemPipeline_WhenSuccessful_ReturnsFirstRow()
    {
        var result = await _connection.QueryFirstWithResilienceAsync<int>(SelectFirstQuery, _pipeline);
        result.Should().Be(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_EcosystemPipeline_WhenMatches_ReturnsFirstRow()
    {
        var result = await _connection.QueryFirstOrDefaultWithResilienceAsync<int>(SelectFirstQuery, _pipeline);
        result.Should().Be(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultWithResilienceAsync_EcosystemPipeline_WhenEmpty_ReturnsDefault()
    {
        var emptyQuery = new SqlResult("SELECT 1 AS Val WHERE 1=0", new Dictionary<string, object?>());
        var result = await _connection.QueryFirstOrDefaultWithResilienceAsync<int>(emptyQuery, _pipeline);
        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_EcosystemPipeline_WhenSuccessful_ReturnsScalar()
    {
        var result = await _connection.ExecuteScalarWithResilienceAsync<int>(ScalarQuery, _pipeline);
        result.Should().Be(1);
    }

    [Fact]
    public async Task EcosystemPipeline_WithTransaction_ExecutesSuccessfully()
    {
        using var tx = _connection.BeginTransaction();
        var insertTx = new SqlResult("INSERT INTO items (name) VALUES (@name)", new Dictionary<string, object?> { ["name"] = "tx-item" });
        var rows = await _connection.ExecuteWithResilienceAsync(insertTx, _pipeline, transaction: tx);
        rows.Should().Be(1);

        var queryTx = new SqlResult("SELECT name FROM items WHERE name = @name", new Dictionary<string, object?> { ["name"] = "tx-item" });
        var items = await _connection.QueryWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        items.Should().ContainSingle();

        var single = await _connection.QuerySingleWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        single.Should().Be("tx-item");

        var singleOrDef = await _connection.QuerySingleOrDefaultWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        singleOrDef.Should().Be("tx-item");

        var first = await _connection.QueryFirstWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        first.Should().Be("tx-item");

        var firstOrDef = await _connection.QueryFirstOrDefaultWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        firstOrDef.Should().Be("tx-item");

        var scalar = await _connection.ExecuteScalarWithResilienceAsync<string>(queryTx, _pipeline, transaction: tx);
        scalar.Should().Be("tx-item");

        tx.Rollback();
    }

    [Fact]
    public async Task CancellationToken_EcosystemPipeline_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _connection.QueryWithResilienceAsync<int>(ScalarQuery, _pipeline, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
