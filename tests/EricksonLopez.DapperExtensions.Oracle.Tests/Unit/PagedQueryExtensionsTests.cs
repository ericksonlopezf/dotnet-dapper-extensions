// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Oracle.Pagination;
using EricksonLopez.DapperExtensions.Testing.Common;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.Oracle.Tests.Unit;

public sealed class PagedQueryExtensionsTests
{
    private sealed record Customer(long Id, string Name, string Email);

    private static DbDataReader CreateDefaultCustomerReader(IEnumerable<Customer> customers)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(long));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));

        foreach (var c in customers)
        {
            table.Rows.Add(c.Id, c.Name, c.Email);
        }

        return table.CreateDataReader();
    }

    // ─── Tests ───────────────────────────────────────────────────────────────

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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
        var executedSqls = new List<string>();

        var sampleCustomers = Enumerable.Range(6, 5)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (sql, _) =>
        {
            executedSqls.Add(sql);
            return CreateDefaultCustomerReader(sampleCustomers);
        };

        connection.ScalarFactory = (sql, _) =>
        {
            executedSqls.Add(sql);
            return 25;
        };

        var pagination = PaginationParameters.Create(2, 5);
        var result = await connection.QueryPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers",
            countSql: "SELECT COUNT(*) FROM customers",
            pagination: pagination,
            param: null,
            transaction: null,
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

        executedSqls.Should().Contain("SELECT id, name, email FROM customers OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY");
        executedSqls.Should().Contain("SELECT COUNT(*) FROM customers");
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>("SELECT 1 FROM DUAL; SELECT 1 FROM DUAL;", pagination);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryPagedMultipleAsync_WhenSqlIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new TestAdoConnection();
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>(invalidSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_ReturnsItemsAndTotalCount()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE customers (id INTEGER PRIMARY KEY, name TEXT, email TEXT);");
        for (int i = 1; i <= 15; i++)
        {
            await connection.ExecuteAsync("INSERT INTO customers VALUES (@id, @name, @email)",
                new { id = i, name = $"Customer {i}", email = $"c{i}@example.com" });
        }

        var pagination = PaginationParameters.Create(1, 10);
        var multiSql = "SELECT id, name, email FROM customers LIMIT 10 OFFSET 0; SELECT COUNT(*) FROM customers;";

        var result = await connection.QueryPagedMultipleAsync<Customer>(
            sql: multiSql,
            pagination: pagination);

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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();

        // 6 items returned for pageSize 5 (5 items + 1 extra indicating hasMore)
        var sampleCustomers = Enumerable.Range(1, 6)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (sql, _) =>
        {
            sql.Should().Be("SELECT id, name, email FROM customers ORDER BY id ASC OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY");
            return CreateDefaultCustomerReader(sampleCustomers);
        };

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
        using var connection = new TestAdoConnection();

        var sampleCustomers = Enumerable.Range(1, 5)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (sql, _) =>
        {
            sql.Should().Be("SELECT id, name, email FROM customers WHERE email LIKE :pattern ORDER BY id ASC OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY");
            return CreateDefaultCustomerReader(sampleCustomers);
        };

        var parameters = new CursorPaginationParameters { First = 5 };
        var result = await connection.QueryCursorPagedAsync<Customer>(
            sql: "SELECT id, name, email FROM customers WHERE email LIKE :pattern",
            cursorColumn: "id",
            parameters: parameters,
            cursorSelector: c => c.Id.ToString(),
            param: new { pattern = "%@example.com%" });

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        connection.LastParameters.Should().NotBeNull();
        connection.LastParameters!.Contains("pattern").Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_WithoutFirstOrLast_DefaultsToForwardAndPageSizeTen()
    {
        using var connection = new TestAdoConnection();

        var sampleCustomers = Enumerable.Range(6, 11)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (sql, _) =>
        {
            sql.Should().Be("SELECT id, name, email FROM customers WHERE id > :__cursorValue ORDER BY id ASC OFFSET 0 ROWS FETCH NEXT 11 ROWS ONLY");
            return CreateDefaultCustomerReader(sampleCustomers);
        };

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
        connection.LastParameters.Should().NotBeNull();
        connection.LastParameters!.Contains("__cursorValue").Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ForwardPagination_ExactPageSizeRemaining_HasMoreIsFalseAndPreservesAllItems()
    {
        using var connection = new TestAdoConnection();

        // Exactly 5 items returned for First = 5 -> hasMore is false, all 5 items preserved
        var sampleCustomers = Enumerable.Range(1, 5)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (_, _) => CreateDefaultCustomerReader(sampleCustomers);

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
        using var connection = new TestAdoConnection();

        var sampleCustomers = Enumerable.Range(11, 6)
            .Select(i => new Customer(i, $"Customer {i:D3}", $"cust{i}@example.com"))
            .ToList();

        connection.ReaderFactory = (sql, _) =>
        {
            sql.Should().Be("SELECT id, name, email FROM customers WHERE id > 0 AND id > :__cursorValue ORDER BY id ASC OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY");
            return CreateDefaultCustomerReader(sampleCustomers);
        };

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
        connection.LastParameters.Should().NotBeNull();
        connection.LastParameters!.Contains("__cursorValue").Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardPagination_WithLastAndBefore_AppendsDescAndComparison()
    {
        using var connection = new TestAdoConnection();

        var sampleCustomers = new List<Customer>
        {
            new(9, "Customer 009", "cust9@example.com"),
            new(8, "Customer 008", "cust8@example.com"),
            new(7, "Customer 007", "cust7@example.com"),
            new(6, "Customer 006", "cust6@example.com"),
            new(5, "Customer 005", "cust5@example.com"),
            new(4, "Customer 004", "cust4@example.com") // 6th item
        };

        connection.ReaderFactory = (sql, _) =>
        {
            sql.Should().Be("SELECT id, name, email FROM customers WHERE id < :__cursorValue ORDER BY id DESC OFFSET 0 ROWS FETCH NEXT 6 ROWS ONLY");
            return CreateDefaultCustomerReader(sampleCustomers);
        };

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
        connection.LastParameters.Should().NotBeNull();
        connection.LastParameters!.Contains("__cursorValue").Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardPagination_FirstPageWithBefore_HasNextPageIsTrueWhenHasMoreIsFalse()
    {
        using var connection = new TestAdoConnection();

        // 3 items (< 5) -> hasMore is false, but Before is set -> hasNextPage must be true
        var sampleCustomers = new List<Customer>
        {
            new(3, "Customer 003", "cust3@example.com"),
            new(2, "Customer 002", "cust2@example.com"),
            new(1, "Customer 001", "cust1@example.com")
        };

        connection.ReaderFactory = (_, _) => CreateDefaultCustomerReader(sampleCustomers);

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
        using var connection = new TestAdoConnection();
        connection.ReaderFactory = (_, _) => CreateDefaultCustomerReader([]);

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
}
