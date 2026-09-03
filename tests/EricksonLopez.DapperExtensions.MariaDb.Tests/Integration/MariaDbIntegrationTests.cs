// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.MariaDb.Bulk;
using EricksonLopez.DapperExtensions.MariaDb.Transactions;
using EricksonLopez.DapperExtensions.Testing.Common;
using MySqlConnector;
using Testcontainers.MariaDb;
using Xunit;

namespace EricksonLopez.DapperExtensions.MariaDb.Tests.Integration;

/// <summary>
/// Integration tests that spin up a real MariaDB instance via Docker (Testcontainers).
/// Requires Docker Desktop or Docker Engine to be running.
/// </summary>
public sealed class MariaDbFixture : IAsyncLifetime
{
    private readonly MariaDbContainer _container = new MariaDbBuilder()
        .WithImage("mariadb:11")
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(BulkTestProduct.Ddl.MariaDbProductsTable);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// </summary>
[Trait("Category", "Integration")]
public sealed class MariaDbIntegrationTests : IClassFixture<MariaDbFixture>, IAsyncLifetime
{
    private readonly MariaDbFixture _fixture;
    private MySqlConnection _connection = null!;

    public MariaDbIntegrationTests(MariaDbFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _connection = new MySqlConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("DELETE FROM products;");
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
    public async Task BulkInsertAsync_MultiRowValues_InsertsAllRows()
    {
        var products = GenerateProducts(50).ToList();

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("is_active", p => p.IsActive ? 1 : 0)
            .Build();

        var rowsAffected = await _connection.BulkInsertAsync(sql, parameters);
        rowsAffected.Should().Be(50);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products;");
        count.Should().Be(50);

        await _connection.ExecuteAsync("DELETE FROM products;");
    }

    // ─── Transaction Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsChangesOnSuccess()
    {
        var productId = Guid.NewGuid().ToString();

        await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (@Id, @Name, @Price, @IsActive);",
                new { Id = productId, Name = "TxProduct", Price = 59.99m, IsActive = 1 },
                trx);
        });

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = @Id;",
            new { Id = productId });
        count.Should().Be(1);

        await _connection.ExecuteAsync("DELETE FROM products WHERE id = @Id;", new { Id = productId });
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBackChangesOnException()
    {
        var productId = Guid.NewGuid().ToString();

        var act = async () => await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (@Id, @Name, @Price, @IsActive);",
                new { Id = productId, Name = "RollbackProduct", Price = 19.99m, IsActive = 1 },
                trx);

            throw new InvalidOperationException("Simulated error inside MariaDB transaction");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = @Id;",
            new { Id = productId });
        count.Should().Be(0);
    }
}
