// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Oracle.Bulk;
using EricksonLopez.DapperExtensions.Oracle.Transactions;
using EricksonLopez.DapperExtensions.Testing.Common;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Xunit;

namespace EricksonLopez.DapperExtensions.Oracle.Tests.Integration;

/// <summary>
/// Integration tests that spin up a real Oracle Database instance via Docker (Testcontainers).
/// Requires Docker Desktop or Docker Engine to be running.
/// </summary>
public sealed class OracleFixture : IAsyncLifetime
{
    private readonly OracleContainer _container = new OracleBuilder()
        .WithImage("gvenzl/oracle-free:23-slim-faststart")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new OracleConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(BulkTestProduct.Ddl.OracleProductsTable);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// </summary>
[Trait("Category", "Integration")]
public sealed class OracleIntegrationTests : IClassFixture<OracleFixture>, IAsyncLifetime
{
    private readonly OracleFixture _fixture;
    private OracleConnection _connection = null!;

    public OracleIntegrationTests(OracleFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _connection = new OracleConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("DELETE FROM products");
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed record ProductRow(string Id, string Name, decimal Price, bool IsActive);

    private static IEnumerable<ProductRow> GenerateProducts(int count)
        => Enumerable.Range(1, count).Select(i =>
            new ProductRow(Guid.NewGuid().ToString(), $"Product {i}", i * 9.99m, i % 2 == 0));

    // ─── BulkInsert Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_InsertAll_InsertsAllRows()
    {
        var products = GenerateProducts(20).ToList();

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("is_active", p => p.IsActive ? 1 : 0)
            .Build();

        var rowsAffected = await _connection.BulkInsertAsync(sql, parameters);
        rowsAffected.Should().Be(20);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
        count.Should().Be(20);

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    // ─── Transaction Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsChangesOnSuccess()
    {
        var productId = Guid.NewGuid().ToString();

        await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (:Id, :Name, :Price, :IsActive)",
                new { Id = productId, Name = "TxProduct", Price = 79.99m, IsActive = 1 },
                trx);
        });

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = :Id",
            new { Id = productId });
        count.Should().Be(1);

        await _connection.ExecuteAsync("DELETE FROM products WHERE id = :Id", new { Id = productId });
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBackChangesOnException()
    {
        var productId = Guid.NewGuid().ToString();

        var act = async () => await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (:Id, :Name, :Price, :IsActive)",
                new { Id = productId, Name = "RollbackProduct", Price = 19.99m, IsActive = 1 },
                trx);

            throw new InvalidOperationException("Simulated error inside Oracle transaction");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = :Id",
            new { Id = productId });
        count.Should().Be(0);
    }
}
