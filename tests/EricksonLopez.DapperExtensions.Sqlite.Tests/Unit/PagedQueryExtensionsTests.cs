// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Sqlite.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Sqlite.Tests.Unit;

public sealed class PagedQueryExtensionsTests
{
    private sealed record Customer(long Id, string Name, string Email);

    private static async Task<SqliteConnection> CreateSeededDatabaseAsync(int count = 25)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT NOT NULL
            );
            """);

        for (int i = 1; i <= count; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO customers (id, name, email) VALUES (@Id, @Name, @Email)",
                new { Id = (long)i, Name = $"Customer {i:D2}", Email = $"customer{i}@example.com" });
        }

        return connection;
    }

    // ─── QueryPagedAsync Validation Tests ─────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>(
            "SELECT * FROM customers", "SELECT COUNT(*) FROM customers", pagination);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedAsync_WhenSqlInvalid_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>(
            invalidSql!, "SELECT COUNT(*) FROM customers", pagination);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedAsync_WhenCountSqlInvalid_ThrowsArgumentException(string? invalidCountSql)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>(
            "SELECT * FROM customers", invalidCountSql!, pagination);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("countSql");
    }

    [Fact]
    public void PaginationParameters_WhenPageLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => PaginationParameters.Create(0, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PaginationParameters_WhenPageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => PaginationParameters.Create(1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─── QueryPagedAsync Execution Tests ──────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_FirstPage_ReturnsExpectedItemsAndMetadata()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var pagination = PaginationParameters.Create(1, 10);

        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "SELECT COUNT(*) FROM customers",
            pagination);

        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result[0].Id.Should().Be(1);
        result[9].Id.Should().Be(10);
    }

    [Fact]
    public async Task QueryPagedAsync_SecondPage_ReturnsExpectedItemsAndMetadata()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var pagination = PaginationParameters.Create(2, 10);

        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "SELECT COUNT(*) FROM customers",
            pagination);

        result.Count.Should().Be(10);
        result.Page.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result[0].Id.Should().Be(11);
        result[9].Id.Should().Be(20);
    }

    [Fact]
    public async Task QueryPagedAsync_LastPage_ReturnsRemainingItems()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var pagination = PaginationParameters.Create(3, 10);

        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "SELECT COUNT(*) FROM customers",
            pagination);

        result.Count.Should().Be(5);
        result.Page.Should().Be(3);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
        result[0].Id.Should().Be(21);
        result[4].Id.Should().Be(25);
    }

    [Fact]
    public async Task QueryPagedAsync_WithParametersAndTransaction_ExecutesSuccessfully()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        using var transaction = connection.BeginTransaction();
        var pagination = PaginationParameters.Create(1, 5);

        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE id > @MinId",
            "SELECT COUNT(*) FROM customers WHERE id > @MinId",
            pagination,
            param: new { MinId = 10 },
            transaction: transaction,
            commandTimeout: 30);

        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(15);
        result[0].Id.Should().Be(11);
        transaction.Commit();
    }

    // ─── QueryPagedMultipleAsync Validation Tests ─────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>("SELECT 1", pagination);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedMultipleAsync_WhenSqlInvalid_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>(invalidSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    // ─── QueryPagedMultipleAsync Execution Tests ──────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_ReturnsItemsAndTotalCount()
    {
        using var connection = await CreateSeededDatabaseAsync(15);
        var pagination = PaginationParameters.Create(1, 5);

        var multiSql = """
            SELECT id, name, email FROM customers LIMIT 5 OFFSET 0;
            SELECT COUNT(*) FROM customers;
            """;

        var result = await connection.QueryPagedMultipleAsync<Customer>(multiSql, pagination);

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_WithParametersAndTransaction_ExecutesSuccessfully()
    {
        using var connection = await CreateSeededDatabaseAsync(15);
        using var transaction = connection.BeginTransaction();
        var pagination = PaginationParameters.Create(1, 5);

        var multiSql = """
            SELECT id, name, email FROM customers WHERE id > @MinId LIMIT 5 OFFSET 0;
            SELECT COUNT(*) FROM customers WHERE id > @MinId;
            """;

        var result = await connection.QueryPagedMultipleAsync<Customer>(
            multiSql,
            pagination,
            param: new { MinId = 5 },
            transaction: transaction,
            commandTimeout: 30);

        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(10);
        result[0].Id.Should().Be(6);
        transaction.Commit();
    }

    // ─── QueryCursorPagedAsync Validation Tests ───────────────────────────────

    [Fact]
    public async Task QueryCursorPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers", "id", parameters, c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryCursorPagedAsync_WhenSqlInvalid_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            invalidSql!, "id", parameters, c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryCursorPagedAsync_WhenCursorColumnInvalid_ThrowsArgumentException(string? invalidCol)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers", invalidCol!, parameters, c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("cursorColumn");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenCursorSelectorNull_ThrowsArgumentNullException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers", "id", parameters, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("cursorSelector");
    }

    // ─── QueryCursorPagedAsync Execution Tests ────────────────────────────────

    [Fact]
    public async Task QueryCursorPagedAsync_DefaultParameters_DefaultsToForwardOrdering()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters(); // Neither First nor Last set

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result[0].Id.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_FirstPageForward_ReturnsPageAndNextCursor()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().Be("1");
        result.EndCursor.Should().Be("10");
        result[0].Id.Should().Be(1);
        result[9].Id.Should().Be(10);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ExactPageSizeItems_HasMoreIsFalse()
    {
        using var connection = await CreateSeededDatabaseAsync(10);
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_NextPageForward_WithAfterCursor_ReturnsNextBatch()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { First = 10, After = "10" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.StartCursor.Should().Be("11");
        result.EndCursor.Should().Be("20");
        result[0].Id.Should().Be(11);
        result[9].Id.Should().Be(20);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_LastPageForward_WithAfterCursor_HasNoMoreNext()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { First = 10, After = "20" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
        result.StartCursor.Should().Be("21");
        result.EndCursor.Should().Be("25");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Backward_WithBeforeCursor_ReturnsPreviousBatch()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { Last = 5, Before = "21" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.StartCursor.Should().Be("20");
        result.EndCursor.Should().Be("16");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Backward_WhenFewerThanPageSize_HasNextPageIsTrueAndHasPreviousFalse()
    {
        using var connection = await CreateSeededDatabaseAsync(10);
        var parameters = new CursorPaginationParameters { Last = 5, Before = "5" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(4); // ids 4, 3, 2, 1
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WithExistingWhereClause_AppendsAndConnector()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { First = 5, After = "10" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE id > 5",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(5);
        result[0].Id.Should().Be(11);
        result[4].Id.Should().Be(15);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardWithExistingWhereClause_AppendsAndConnector()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        var parameters = new CursorPaginationParameters { Last = 5, Before = "20" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE id > 5",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(5);
        result[0].Id.Should().Be(19);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_EmptyResults_ReturnsNullCursors()
    {
        using var connection = await CreateSeededDatabaseAsync(5);
        var parameters = new CursorPaginationParameters { First = 10, After = "100" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Should().BeEmpty();
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WithParamAndTransaction_ExecutesSuccessfully()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        using var transaction = connection.BeginTransaction();
        var parameters = new CursorPaginationParameters { First = 5 };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE email LIKE @Pattern",
            "id",
            parameters,
            c => c.Id.ToString(),
            param: new { Pattern = "%@example.com" },
            transaction: transaction,
            commandTimeout: 30);

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        transaction.Commit();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Forward_GeneratesOrderAscSql()
    {
        string? capturedSql = null;
        using var connection = new EricksonLopez.DapperExtensions.Testing.Common.TestAdoConnection
        {
            ReaderFactory = (sql, _) =>
            {
                capturedSql = sql;
                return new System.Data.DataTableReader(new System.Data.DataTable());
            }
        };

        var parameters = new CursorPaginationParameters { First = 5, After = "10" };
        await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id ASC");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Backward_GeneratesOrderDescSql()
    {
        string? capturedSql = null;
        using var connection = new EricksonLopez.DapperExtensions.Testing.Common.TestAdoConnection
        {
            ReaderFactory = (sql, _) =>
            {
                capturedSql = sql;
                return new System.Data.DataTableReader(new System.Data.DataTable());
            }
        };

        var parameters = new CursorPaginationParameters { Last = 5, Before = "20" };
        await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id DESC");
    }
}
