// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.MariaDb.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.MariaDb.Tests.Unit;

public sealed class PagedQueryExtensionsTests
{
    private sealed record Customer(long Id, string Name, string Email);

    private static async Task<SqliteConnection> CreateSeededDatabaseAsync(int count = 25)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE customers (id INTEGER PRIMARY KEY, name TEXT, email TEXT);");

        for (int i = 1; i <= count; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO customers (id, name, email) VALUES (@id, @name, @email);",
                new { id = i, name = $"Customer {i:D3}", email = $"cust{i}@example.com" });
        }

        return connection;
    }

    [Fact]
    public async Task QueryPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>("SELECT * FROM customers", "SELECT COUNT(*) FROM customers", pagination);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedAsync_WhenSqlIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>(invalidSql!, "SELECT COUNT(*) FROM customers", pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedAsync_WhenCountSqlIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidCountSql)
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>("SELECT * FROM customers", invalidCountSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("countSql");
    }

    [Fact]
    public void PaginationParameters_Create_WhenInvalid_ThrowsArgumentOutOfRangeException()
    {
        var actPage = () => PaginationParameters.Create(0, 10);
        actPage.Should().Throw<ArgumentOutOfRangeException>();

        var actPageSize = () => PaginationParameters.Create(1, 0);
        actPageSize.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task QueryPagedAsync_ReturnsCorrectPageAndCount()
    {
        using var connection = await CreateSeededDatabaseAsync(25);
        using var transaction = connection.BeginTransaction();

        var pagination = PaginationParameters.Create(2, 5);
        var result = await connection.QueryPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            countSql: "SELECT COUNT(*) FROM customers",
            pagination: pagination,
            param: null,
            transaction: transaction,
            commandTimeout: 30);

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalPages.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();

        result[0].Id.Should().Be(6);
        result[4].Id.Should().Be(10);
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>("SELECT 1; SELECT 1;", pagination);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedMultipleAsync_WhenSqlIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>(invalidSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_ReturnsItemsAndTotalCount()
    {
        using var connection = await CreateSeededDatabaseAsync(15);
        using var transaction = connection.BeginTransaction();

        var pagination = PaginationParameters.Create(1, 10);
        var multiSql = "SELECT id, name, email FROM customers LIMIT 10 OFFSET 0; SELECT COUNT(*) FROM customers;";

        var result = await connection.QueryPagedMultipleAsync<Customer>(
            sql: multiSql,
            pagination: pagination,
            param: null,
            transaction: transaction,
            commandTimeout: 30);

        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var parameters = new CursorPaginationParameters { First = 5 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryCursorPagedAsync_WhenSqlIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var parameters = new CursorPaginationParameters { First = 5 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            invalidSql!,
            "id",
            parameters,
            c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryCursorPagedAsync_WhenCursorColumnIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidCol)
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var parameters = new CursorPaginationParameters { First = 5 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers",
            invalidCol!,
            parameters,
            c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("cursorColumn");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenCursorSelectorNull_ThrowsArgumentNullException()
    {
        using var connection = await CreateSeededDatabaseAsync(1);
        var parameters = new CursorPaginationParameters { First = 5 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers",
            "id",
            parameters,
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("cursorSelector");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_FirstPage_ReturnsCorrectSliceAndFlags()
    {
        using var connection = await CreateSeededDatabaseAsync(20);

        var parameters = new CursorPaginationParameters { First = 5 };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[^1].Id.Should().Be(5);
        result.StartCursor.Should().Be("1");
        result.EndCursor.Should().Be("5");
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_WithExternalParam_PassesAndFiltersCorrectly()
    {
        using var connection = await CreateSeededDatabaseAsync(20);

        // Uses external @pattern parameter to verify dynamicParams.AddDynamicParams(param) executes
        var parameters = new CursorPaginationParameters { First = 5 };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers WHERE email LIKE @pattern",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString(),
            param: new { pattern = "%@example.com%" });

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Email.Should().Contain("@example.com");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_WithoutFirstOrLast_DefaultsToForwardAndPageSizeTen()
    {
        using var connection = await CreateSeededDatabaseAsync(20);

        // Neither First nor Last is specified -> isBackward must be false, pageSize defaults to 10
        var parameters = new CursorPaginationParameters { After = "5" };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result[0].Id.Should().Be(6);
        result[^1].Id.Should().Be(15);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_ExactPageSizeRemaining_HasMoreIsFalseAndPreservesAllItems()
    {
        // Seed exactly 5 items, request First = 5 -> query returns exactly 5 items (count == pageSize, count > pageSize is false)
        using var connection = await CreateSeededDatabaseAsync(5);

        var parameters = new CursorPaginationParameters { First = 5 };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(1);
        result[^1].Id.Should().Be(5);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_WithAfterAndWhereClause_AppendsAndConnector()
    {
        using var connection = await CreateSeededDatabaseAsync(20);

        // SQL already has WHERE clause -> should append AND id > @__cursorValue
        var parameters = new CursorPaginationParameters { First = 5, After = "10" };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers WHERE id > 0",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString(),
            param: new { unused = 1 });

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(11);
        result[^1].Id.Should().Be(15);
        result.StartCursor.Should().Be("11");
        result.EndCursor.Should().Be("15");
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardPagination_WithLastAndBefore_AppendsDescAndComparison()
    {
        using var connection = await CreateSeededDatabaseAsync(20);

        // Backward: last=5, before="10" -> should order by id DESC LIMIT 6, and reverse comparison
        var parameters = new CursorPaginationParameters { Last = 5, Before = "10" };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result[0].Id.Should().Be(9);
        result[^1].Id.Should().Be(5);
        result.StartCursor.Should().Be("9");
        result.EndCursor.Should().Be("5");
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardPagination_FirstPageWithBefore_HasNextPageIsTrueWhenHasMoreIsFalse()
    {
        // Database has 5 items (id 1..5), Last = 5, Before = "4".
        // Matching items are id 1, 2, 3 (3 items < 5).
        // Since count (3) <= pageSize (5), hasMore is false.
        // But since Before is not empty, hasNextPage must be true!
        using var connection = await CreateSeededDatabaseAsync(5);

        var parameters = new CursorPaginationParameters { Last = 5, Before = "4" };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result[0].Id.Should().Be(3);
        result[^1].Id.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenEmptyResult_ReturnsNullCursorsAndFalseFlags()
    {
        using var connection = await CreateSeededDatabaseAsync(0);

        var parameters = new CursorPaginationParameters { First = 5 };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
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

    [Fact]
    public async Task QueryCursorPagedAsync_WithExistingWhereClause_AppendsAndConnector()
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
            sql: "SELECT id, name, email FROM customers WHERE active = 1",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE active = 1 AND id > @__cursorValue");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardWithExistingWhereClause_AppendsAndConnector()
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
            sql: "SELECT id, name, email FROM customers WHERE active = 1",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE active = 1 AND id < @__cursorValue");
    }
}
