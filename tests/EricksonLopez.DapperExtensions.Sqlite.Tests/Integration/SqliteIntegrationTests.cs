// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Sqlite.Bulk;
using EricksonLopez.DapperExtensions.Sqlite.Transactions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Sqlite.Tests.Integration;

/// <summary>
/// Integration tests using an in-memory SQLite database (no Docker required).
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqliteIntegrationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await _connection.ExecuteAsync("""
            CREATE TABLE products (
                id      INTEGER NOT NULL PRIMARY KEY,
                name    TEXT    NOT NULL,
                price   REAL    NOT NULL,
                active  INTEGER NOT NULL DEFAULT 1
            )
            """);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class ProductRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool Active { get; set; }

        public ProductRow() { }

        public ProductRow(long id, string name, double price, bool active)
        {
            Id = id;
            Name = name;
            Price = price;
            Active = active;
        }
    }

    private static IEnumerable<ProductRow> GenerateProducts(int count)
        => Enumerable.Range(1, count).Select(i =>
            new ProductRow(i, $"Product {i}", i * 9.99, i % 2 == 0));

    // ─── BulkInsert tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_ShouldInsertAllRows()
    {
        var products = GenerateProducts(20).ToList();

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("active", p => p.Active ? 1 : 0)
            .Build();

        var rowsAffected = await _connection.BulkInsertAsync(sql, parameters);

        rowsAffected.Should().Be(20);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
        count.Should().Be(20);

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    [Fact]
    public async Task BulkUpsertAsync_ShouldInsertOrReplace()
    {
        // Insert initial
        await _connection.ExecuteAsync(
            "INSERT INTO products (id, name, price) VALUES (1, 'Original', 10.0)");

        // Upsert with OR REPLACE
        var products = new[] { new ProductRow(1, "Updated", 99.0, true) };

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Build();

        // SQLite uses INSERT OR REPLACE
        var upsertSql = sql!.Replace("INSERT INTO", "INSERT OR REPLACE INTO");

        await _connection.BulkUpsertAsync(upsertSql, parameters);

        var updated = await _connection.QuerySingleAsync<ProductRow>(
            "SELECT id, name, price, active FROM products WHERE id = 1");

        updated.Name.Should().Be("Updated");
        updated.Price.Should().Be(99.0);

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    // ─── Transaction tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
        await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price) VALUES (100, 'TxProduct', 1.0)",
                transaction: trx);
        });

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = 100");

        count.Should().Be(1);
        await _connection.ExecuteAsync("DELETE FROM products");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnException()
    {
        var act = async () => await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price) VALUES (200, 'WillRollback', 1.0)",
                transaction: trx);

            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = 200");

        count.Should().Be(0); // Rolled back
    }
}
