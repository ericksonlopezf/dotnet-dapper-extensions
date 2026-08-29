// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.MySql.Bulk;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.MySql.Tests.Unit;

public sealed class BulkExtensionsTests
{
    private sealed record Item(int Id, string Name);

    [Fact]
    public async Task BulkInsertAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var parameters = new DynamicParameters();

        var act = () => connection.BulkInsertAsync("INSERT INTO tbl VALUES (1)", parameters);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkInsertAsync_WhenSqlIsNullOrWhiteSpace_ReturnsZero(string? invalidSql)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        var result = await connection.BulkInsertAsync(invalidSql, parameters);

        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WhenParametersNull_ReturnsZero()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var result = await connection.BulkInsertAsync("INSERT INTO tbl VALUES (1)", null);
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithValidSqlAndParameters_ExecutesSuccessfully()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER, name TEXT);");

        var items = new[]
        {
            new Item(1, "Item 1"),
            new Item(2, "Item 2")
        };

        var (sql, parameters) = BulkBuilder.From(items)
            .Table("items")
            .Column("id", i => i.Id)
            .Column("name", i => i.Name)
            .Build();

        using var cts = new CancellationTokenSource();
        var rows = await connection.BulkInsertAsync(sql, parameters, cancellationToken: cts.Token);
        rows.Should().Be(2);

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM items");
        count.Should().Be(2);
    }

    [Fact]
    public async Task BulkInsertAsync_WithTransactionAndTimeout_ExecutesInsideTransaction()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER, name TEXT);");

        using var transaction = connection.BeginTransaction();
        var parameters = new DynamicParameters();
        parameters.Add("id", 10);
        parameters.Add("name", "Transacted Item");

        var rows = await connection.BulkInsertAsync(
            "INSERT INTO items (id, name) VALUES (@id, @name)",
            parameters,
            transaction: transaction,
            commandTimeout: 30);

        rows.Should().Be(1);
        transaction.Commit();

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM items");
        count.Should().Be(1);
    }

    [Fact]
    public async Task BulkUpsertAsync_DelegatesToBulkInsertAsync()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER, name TEXT);");

        var parameters = new DynamicParameters();
        parameters.Add("id", 42);
        parameters.Add("name", "Upserted");

        var rows = await connection.BulkUpsertAsync("INSERT INTO items (id, name) VALUES (@id, @name)", parameters);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task BulkDeleteAsync_DelegatesToBulkInsertAsync()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER, name TEXT);");
        await connection.ExecuteAsync("INSERT INTO items (id, name) VALUES (1, 'To Delete');");

        var parameters = new DynamicParameters();
        parameters.Add("id", 1);

        var rows = await connection.BulkDeleteAsync("DELETE FROM items WHERE id = @id", parameters);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task BulkUpdateAsync_DelegatesToBulkInsertAsync()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER, name TEXT);");
        await connection.ExecuteAsync("INSERT INTO items (id, name) VALUES (1, 'Old Name');");

        var parameters = new DynamicParameters();
        parameters.Add("id", 1);
        parameters.Add("name", "New Name");

        var rows = await connection.BulkUpdateAsync("UPDATE items SET name = @name WHERE id = @id", parameters);
        rows.Should().Be(1);
    }
}
