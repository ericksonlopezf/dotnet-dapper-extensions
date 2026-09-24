// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.PostgreSql.Bulk;
using EricksonLopez.DapperExtensions.PostgreSql.Pagination;
using EricksonLopez.DapperExtensions.PostgreSql.Transactions;
using EricksonLopez.DapperExtensions.Testing.Common;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using Npgsql;
using NpgsqlTypes;
using Testcontainers.PostgreSql;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Integration;

/// <summary>
/// Integration tests that spin up a real PostgreSQL instance via Docker (Testcontainers).
/// Requires Docker Desktop or Docker Engine to be running.
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(BulkTestProduct.Ddl.PostgreSqlProductsTable);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// </summary>
[Trait("Category", "Integration")]
public sealed class PostgreSqlIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private NpgsqlConnection _connection = null!;

    public PostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _connection = new NpgsqlConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("DELETE FROM products;");
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── BulkInsert tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_ShouldInsertAllRows()
    {
        var products = BulkTestProduct.GenerateProducts(100);

        var parameters = BulkParameters.From(products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Add("IsActives", p => p.IsActive, NpgsqlDbType.Boolean)
            .Build();

        var rowsAffected = await _connection.BulkInsertAsync(
            """
            INSERT INTO products (id, name, price, is_active)
            SELECT * FROM UNNEST(@Ids, @Names, @Prices, @IsActives)
            """,
            parameters);

        rowsAffected.Should().Be(100);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
        count.Should().Be(100);

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    [Fact]
    public async Task BulkUpsertAsync_ShouldUpdateOnConflict()
    {
        // Insert initial product
        var id = Guid.NewGuid();
        await _connection.ExecuteAsync(
            "INSERT INTO products (id, name, price) VALUES (@Id, @Name, @Price)",
            new { Id = id, Name = "Original", Price = 10m });
        var items = new[] { new { Id = 1, Tag = (string?)null }, new { Id = 2, Tag = (string?)"active" } };

        // Upsert with updated name/price
        var products = new[] { new BulkTestProduct(id, "Updated", 99m, true) };
        var parameters = BulkParameters.From(products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Build();

        await _connection.BulkUpsertAsync(
            """
            INSERT INTO products (id, name, price)
            SELECT * FROM UNNEST(@Ids, @Names, @Prices)
            ON CONFLICT (id) DO UPDATE
                SET name  = EXCLUDED.name,
                    price = EXCLUDED.price
            """,
            parameters);

        var updated = await _connection.QuerySingleAsync<BulkTestProduct>(
            "SELECT id, name, price, is_active AS IsActive FROM products WHERE id = @Id",
            new { Id = id });

        updated.Name.Should().Be("Updated");
        updated.Price.Should().Be(99m);

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    // ─── Pagination tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_ShouldReturnCorrectPage()
    {
        // Insert 25 products
        var products = BulkTestProduct.GenerateProducts(25);
        var parameters = BulkParameters.From(products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Add("IsActives", p => p.IsActive, NpgsqlDbType.Boolean)
            .Build();

        await _connection.BulkInsertAsync(
            "INSERT INTO products (id, name, price, is_active) SELECT * FROM UNNEST(@Ids, @Names, @Prices, @IsActives)",
            parameters);

        var pagination = PaginationParameters.Create(page: 2, pageSize: 10);

        var page = await _connection.QueryPagedAsync<BulkTestProduct>(
            sql: "SELECT id, name, price, is_active AS IsActive FROM products ORDER BY name",
            countSql: "SELECT COUNT(*) FROM products",
            pagination: pagination);

        page.Page.Should().Be(2);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(25);
        page.TotalPages.Should().Be(3);
        page.Should().HaveCount(10);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeTrue();

        await _connection.ExecuteAsync("DELETE FROM products");
    }

    // ─── Transaction tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
        var id = Guid.NewGuid();

        await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price) VALUES (@Id, @Name, @Price)",
                new { Id = id, Name = "TxProduct", Price = 1m },
                trx);
        });

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = @Id", new { Id = id });

        count.Should().Be(1);
        await _connection.ExecuteAsync("DELETE FROM products");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnException()
    {
        var id = Guid.NewGuid();

        var act = async () => await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price) VALUES (@Id, @Name, @Price)",
                new { Id = id, Name = "WillRollback", Price = 1m },
                trx);

            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = @Id", new { Id = id });

        count.Should().Be(0); // Rolled back
    }
}






