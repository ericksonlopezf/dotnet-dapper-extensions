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
using EricksonLopez.DapperExtensions.PostgreSql.Pagination;
using EricksonLopez.DapperExtensions.Testing.Common;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

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

    private static DbDataReader CreateMultipleCustomerReader(IEnumerable<Customer> customers, int totalCount)
    {
        var dataSet = new DataSet();
        var table1 = dataSet.Tables.Add("Customers");
        table1.Columns.Add("Id", typeof(long));
        table1.Columns.Add("Name", typeof(string));
        table1.Columns.Add("Email", typeof(string));
        foreach (var c in customers)
        {
            table1.Rows.Add(c.Id, c.Name, c.Email);
        }

        var table2 = dataSet.Tables.Add("TotalCount");
        table2.Columns.Add("Count", typeof(int));
        table2.Rows.Add(totalCount);

        return dataSet.CreateDataReader();
    }

    private static List<Customer> GenerateCustomers(int count, int startId = 1)
    {
        return Enumerable.Range(startId, count)
            .Select(i => new Customer(i, $"Customer {i:D2}", $"customer{i}@example.com"))
            .ToList();
    }

#pragma warning restore CS8765, CS8767, CS8766, CS8769

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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedAsync<Customer>(
            "SELECT * FROM customers", invalidCountSql!, pagination);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("countSql");
    }

    // ─── QueryPagedAsync Execution Tests ──────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_FirstPage_ReturnsExpectedItemsAndMetadata()
    {
        var customers = GenerateCustomers(10, startId: 1);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); },
            ScalarFactory = (sql, _) => 25
        };

        var pagination = PaginationParameters.Create(1, 10);
        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "SELECT COUNT(*) FROM customers",
            pagination);

        capturedSql.Should().Contain("LIMIT 10 OFFSET 0");
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
    public async Task QueryPagedAsync_SecondPage_FormatsOffsetClauseCorrectly()
    {
        var customers = GenerateCustomers(10, startId: 11);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); },
            ScalarFactory = (sql, _) => 25
        };

        var pagination = PaginationParameters.Create(2, 10);
        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers ORDER BY id",
            "SELECT COUNT(*) FROM customers",
            pagination);

        capturedSql.Should().Contain("LIMIT 10 OFFSET 10");
        result.Page.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryPagedAsync_LastPage_HasNextPageIsFalse()
    {
        var customers = GenerateCustomers(5, startId: 21);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers),
            ScalarFactory = (sql, _) => 25
        };

        var pagination = PaginationParameters.Create(3, 10);
        var result = await connection.QueryPagedAsync<Customer>(
            "SELECT id, name, email FROM customers ORDER BY id",
            "SELECT COUNT(*) FROM customers",
            pagination);

        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryPagedAsync_WithParametersAndTransaction_ExecutesSuccessfully()
    {
        var customers = GenerateCustomers(5, startId: 11);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers),
            ScalarFactory = (sql, _) => 15
        };
        var transaction = Substitute.For<DbTransaction>();
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
        using var connection = new TestAdoConnection();
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => connection.QueryPagedMultipleAsync<Customer>(invalidSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    // ─── QueryPagedMultipleAsync Execution Tests ──────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_ReturnsItemsAndTotalCount()
    {
        var customers = GenerateCustomers(5, startId: 1);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateMultipleCustomerReader(customers, totalCount: 15)
        };
        var pagination = PaginationParameters.Create(1, 5);

        var multiSql = """
            SELECT id, name, email FROM customers ORDER BY id LIMIT 5 OFFSET 0;
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
        var customers = GenerateCustomers(5, startId: 1);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateMultipleCustomerReader(customers, totalCount: 10)
        };
        var transaction = Substitute.For<DbTransaction>();
        var pagination = PaginationParameters.Create(1, 5);

        var multiSql = """
            SELECT id, name, email FROM customers WHERE active = true ORDER BY id LIMIT 5 OFFSET 0;
            SELECT COUNT(*) FROM customers WHERE active = true;
            """;

        var result = await connection.QueryPagedMultipleAsync<Customer>(
            multiSql, pagination, param: new { active = true }, transaction: transaction, commandTimeout: 45);

        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(10);
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
        using var connection = new TestAdoConnection();
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
        using var connection = new TestAdoConnection();
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers", invalidCol!, parameters, c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("cursorColumn");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenCursorSelectorNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => connection.QueryCursorPagedAsync<Customer>(
            "SELECT * FROM customers", "id", parameters, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("cursorSelector");
    }

    // ─── QueryCursorPagedAsync Execution Tests ────────────────────────────────

    [Fact]
    public async Task QueryCursorPagedAsync_DefaultParameters_DefaultsToForwardOrdering()
    {
        var customers = GenerateCustomers(11, startId: 1); // 11 items returned for default pageSize 10
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters(); // Neither First nor Last set

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id ASC LIMIT 11");
        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result[0].Id.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_FirstPageForward_ReturnsPageAndNextCursor()
    {
        var customers = GenerateCustomers(11, startId: 1); // 11 items
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id ASC LIMIT 11");
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().Be("1");
        result.EndCursor.Should().Be("10");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ExactPageSizeItems_HasMoreIsFalse()
    {
        var customers = GenerateCustomers(10, startId: 1); // Exactly 10 items
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers)
        };
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
        var customers = GenerateCustomers(11, startId: 11);
        string? capturedSql = null;
        DbParameterCollection? capturedParams = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, p) => { capturedSql = sql; capturedParams = p; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { First = 10, After = "10" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE id > @__cursorValue");
        capturedSql.Should().Contain("ORDER BY id ASC LIMIT 11");
        capturedParams.Should().NotBeNull();
        capturedParams!.Contains("__cursorValue").Should().BeTrue();
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.StartCursor.Should().Be("11");
        result.EndCursor.Should().Be("20");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_LastPageForward_WithAfterCursor_HasNoMoreNext()
    {
        var customers = GenerateCustomers(5, startId: 21);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers)
        };
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
        var customers = GenerateCustomers(6, startId: 15);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { Last = 5, Before = "21" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE id < @__cursorValue");
        capturedSql.Should().Contain("ORDER BY id DESC LIMIT 6");
        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Backward_WhenFewerThanPageSize_HasNextPageIsTrueAndHasPreviousFalse()
    {
        var customers = GenerateCustomers(4, startId: 1); // 4 items (less than pageSize 5)
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers)
        };
        var parameters = new CursorPaginationParameters { Last = 5, Before = "5" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        result.Count.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WithExistingWhereClause_AppendsAndConnector()
    {
        var customers = GenerateCustomers(6, startId: 11);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { First = 5, After = "10" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE active = true",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE active = true AND id > @__cursorValue");
        result.Count.Should().Be(5);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_BackwardWithExistingWhereClause_AppendsAndConnector()
    {
        var customers = GenerateCustomers(6, startId: 11);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { Last = 5, Before = "20" };

        var result = await connection.QueryCursorPagedAsync<Customer>(
            "SELECT id, name, email FROM customers WHERE active = true",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("WHERE active = true AND id < @__cursorValue");
        result.Count.Should().Be(5);
    }

    [Fact]
    public async Task QueryCursorPagedAsync_EmptyResults_ReturnsNullCursors()
    {
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader([])
        };
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
        var customers = GenerateCustomers(6, startId: 1);
        DbParameterCollection? capturedParams = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, p) => { capturedParams = p; return CreateDefaultCustomerReader(customers); }
        };
        var transaction = Substitute.For<DbTransaction>();
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
        capturedParams.Should().NotBeNull();
        capturedParams!.Contains("Pattern").Should().BeTrue();
    }

}
