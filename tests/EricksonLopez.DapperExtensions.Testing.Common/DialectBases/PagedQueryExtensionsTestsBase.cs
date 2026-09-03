// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.Testing.Common;

/// <summary>
/// Reusable test base for verifying dialect-native PagedQueryExtensions across database providers.
/// Encapsulates common test entities, ADO.NET data reader fakes, and standard validation tests.
/// </summary>
public abstract class PagedQueryExtensionsTestsBase
{
    public sealed record Customer(long Id, string Name, string Email);

    protected static DbDataReader CreateDefaultCustomerReader(IEnumerable<Customer> customers)
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

    protected static DbDataReader CreateMultipleCustomerReader(IEnumerable<Customer> customers, int totalCount)
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

    protected static List<Customer> GenerateCustomers(int count, int startId = 1)
    {
        return Enumerable.Range(startId, count)
            .Select(i => new Customer(i, $"Customer {i:D2}", $"customer{i}@example.com"))
            .ToList();
    }

    protected abstract string ExpectedOffsetClause(int offset, int pageSize);

    protected virtual string ExpectedCursorLimitClause(int limit)
        => $"LIMIT {limit}";

    protected virtual string CursorParameterPlaceholder(string parameterName)
        => $"@{parameterName}";

    protected abstract Task<ICountedPagedList<Customer>> ExecuteQueryPagedAsync(
        IDbConnection connection,
        string sql,
        string countSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);

    protected abstract Task<ICountedPagedList<Customer>> ExecuteQueryPagedMultipleAsync(
        IDbConnection connection,
        string combinedSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);

    protected abstract Task<ICursorPagedList<Customer>> ExecuteQueryCursorPagedAsync(
        IDbConnection connection,
        string sql,
        string cursorColumn,
        CursorPaginationParameters parameters,
        Func<Customer, string> cursorSelector,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);

    // ─── QueryPagedAsync Validation Tests ─────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => ExecuteQueryPagedAsync(
            connection, "SELECT * FROM customers", "SELECT COUNT(*) FROM customers", pagination);

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

        var act = () => ExecuteQueryPagedAsync(
            connection, invalidSql!, "SELECT COUNT(*) FROM customers", pagination);

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

        var act = () => ExecuteQueryPagedAsync(
            connection, "SELECT * FROM customers", invalidCountSql!, pagination);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("countSql");
    }

    // ─── QueryPagedMultipleAsync Validation Tests ─────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var pagination = PaginationParameters.Create(1, 10);

        var act = () => ExecuteQueryPagedMultipleAsync(connection, "SELECT 1", pagination);
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

        var act = () => ExecuteQueryPagedMultipleAsync(connection, invalidSql!, pagination);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    // ─── QueryCursorPagedAsync Validation Tests ───────────────────────────────

    [Fact]
    public async Task QueryCursorPagedAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => ExecuteQueryCursorPagedAsync(
            connection, "SELECT * FROM customers", "id", parameters, c => c.Id.ToString());

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

        var act = () => ExecuteQueryCursorPagedAsync(
            connection, invalidSql!, "id", parameters, c => c.Id.ToString());

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

        var act = () => ExecuteQueryCursorPagedAsync(
            connection, "SELECT * FROM customers", invalidCol!, parameters, c => c.Id.ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("cursorColumn");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_WhenCursorSelectorNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        var parameters = new CursorPaginationParameters { First = 10 };

        var act = () => ExecuteQueryCursorPagedAsync(
            connection, "SELECT * FROM customers", "id", parameters, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("cursorSelector");
    }

    // ─── QueryPagedAsync Execution Tests ──────────────────────────────────────

    [Fact]
    public async Task QueryPagedAsync_FirstPage_ReturnsExpectedItemsAndMetadata()
    {
        var customers = GenerateCustomers(10, startId: 1);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers),
            ScalarFactory = (sql, _) => 25
        };

        var pagination = PaginationParameters.Create(1, 10);
        var result = await ExecuteQueryPagedAsync(
            connection,
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
        var result = await ExecuteQueryPagedAsync(
            connection,
            "SELECT id, name, email FROM customers ORDER BY id",
            "SELECT COUNT(*) FROM customers",
            pagination);

        capturedSql.Should().Contain(ExpectedOffsetClause(10, 10));
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
        var result = await ExecuteQueryPagedAsync(
            connection,
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

        var result = await ExecuteQueryPagedAsync(
            connection,
            "SELECT id, name, email FROM customers WHERE id > @MinId",
            "SELECT COUNT(*) FROM customers WHERE id > @MinId",
            pagination,
            param: new { MinId = 10 },
            transaction: transaction,
            commandTimeout: 30);

        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(15);
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

        var multiSql = $"""
            SELECT id, name, email FROM customers ORDER BY id {ExpectedOffsetClause(0, 5)};
            SELECT COUNT(*) FROM customers;
            """;

        var result = await ExecuteQueryPagedMultipleAsync(connection, multiSql, pagination);

        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    // ─── QueryCursorPagedAsync Execution Tests ────────────────────────────────

    [Fact]
    public async Task QueryCursorPagedAsync_DefaultParameters_DefaultsToForwardOrdering()
    {
        var customers = GenerateCustomers(11, startId: 1);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters();

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id ASC");
        capturedSql.Should().Contain(ExpectedCursorLimitClause(11));
        result.Should().NotBeNull();
        result.Count.Should().Be(10);
        result[0].Id.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_FirstPageForward_ReturnsPageAndNextCursor()
    {
        var customers = GenerateCustomers(11, startId: 1);
        string? capturedSql = null;
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => { capturedSql = sql; return CreateDefaultCustomerReader(customers); }
        };
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain("ORDER BY id ASC");
        result.Count.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().Be("1");
        result.EndCursor.Should().Be("10");
    }

    [Fact]
    public async Task QueryCursorPagedAsync_ExactPageSizeItems_HasMoreIsFalse()
    {
        var customers = GenerateCustomers(10, startId: 1);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers)
        };
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain($"WHERE id > {CursorParameterPlaceholder("__cursorValue")}");
        capturedSql.Should().Contain("ORDER BY id ASC");
        capturedSql.Should().Contain(ExpectedCursorLimitClause(11));
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain($"WHERE id < {CursorParameterPlaceholder("__cursorValue")}");
        capturedSql.Should().Contain("ORDER BY id DESC");
        capturedSql.Should().Contain(ExpectedCursorLimitClause(6));
        result.Count.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryCursorPagedAsync_Backward_WhenFewerThanPageSize_HasNextPageIsTrueAndHasPreviousFalse()
    {
        var customers = GenerateCustomers(4, startId: 1);
        using var connection = new TestAdoConnection
        {
            ReaderFactory = (sql, _) => CreateDefaultCustomerReader(customers)
        };
        var parameters = new CursorPaginationParameters { Last = 5, Before = "5" };

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers WHERE active = 1",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain($"WHERE active = 1 AND id > {CursorParameterPlaceholder("__cursorValue")}");
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
            "SELECT id, name, email FROM customers WHERE active = 1",
            "id",
            parameters,
            c => c.Id.ToString());

        capturedSql.Should().Contain($"WHERE active = 1 AND id < {CursorParameterPlaceholder("__cursorValue")}");
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
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

        var result = await ExecuteQueryCursorPagedAsync(
            connection,
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
