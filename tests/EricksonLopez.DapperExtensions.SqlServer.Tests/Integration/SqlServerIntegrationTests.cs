// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.SqlServer.Bulk;
using EricksonLopez.DapperExtensions.SqlServer.Transactions;
using EricksonLopez.DapperExtensions.Testing.Common;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace EricksonLopez.DapperExtensions.SqlServer.Tests.Integration;

/// <summary>
/// Integration tests that spin up a real Microsoft SQL Server instance via Docker (Testcontainers).
/// Requires Docker Desktop or Docker Engine to be running.
public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(BulkTestProduct.Ddl.SqlServerProductsTable);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// </summary>
[Trait("Category", "Integration")]
public sealed class SqlServerIntegrationTests : IClassFixture<MsSqlFixture>, IAsyncLifetime
{
    private readonly MsSqlFixture _fixture;
    private SqlConnection _connection = null!;

    public SqlServerIntegrationTests(MsSqlFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _connection = new SqlConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("DELETE FROM products;");
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── BulkInsert Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_WithSqlBulkCopy_InsertsAllRows()
    {
        var products = BulkTestProduct.GenerateProducts(50);
        using var table = BulkTestProduct.CreateProductDataTable(products, "products");

        var rowsInserted = await _connection.BulkInsertAsync("products", table);
        rowsInserted.Should().Be(50);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products;");
        count.Should().Be(50);

        await _connection.ExecuteAsync("DELETE FROM products;");
    }

    // ─── Transaction Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsChangesOnSuccess()
    {
        var productId = Guid.NewGuid();

        await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (@Id, @Name, @Price, @IsActive);",
                new { Id = productId, Name = "TxProduct", Price = 49.99m, IsActive = true },
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
        var productId = Guid.NewGuid();

        var act = async () => await _connection.ExecuteInTransactionAsync(async trx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO products (id, name, price, is_active) VALUES (@Id, @Name, @Price, @IsActive);",
                new { Id = productId, Name = "RollbackProduct", Price = 19.99m, IsActive = true },
                trx);

            throw new InvalidOperationException("Simulated failure inside transaction");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE id = @Id;",
            new { Id = productId });
        count.Should().Be(0);
    }
}
