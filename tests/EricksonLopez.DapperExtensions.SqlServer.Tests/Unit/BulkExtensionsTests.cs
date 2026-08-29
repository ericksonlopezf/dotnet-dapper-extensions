// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.SqlServer.Bulk;
using EricksonLopez.DapperExtensions.Testing.Common;
using Microsoft.Data.SqlClient;
using Xunit;

namespace EricksonLopez.DapperExtensions.SqlServer.Tests.Unit;

public sealed class BulkExtensionsTests
{
    // ─── BulkInsertAsync Validation Tests ─────────────────────────────────────

    [Fact]
    public async Task BulkInsertAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        using var table = new DataTable();

        var act = () => connection.BulkInsertAsync("dbo.Products", table);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkInsertAsync_WhenDestinationTableNullOrWhiteSpace_ThrowsArgumentException(string? invalidTable)
    {
        using var connection = new TestAdoConnection();
        using var table = new DataTable();

        var act = () => connection.BulkInsertAsync(invalidTable!, table);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("destinationTableName");
    }

    [Fact]
    public async Task BulkInsertAsync_WhenDataTableNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();

        var act = () => connection.BulkInsertAsync("dbo.Products", null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("dataTable");
    }

    [Fact]
    public async Task BulkInsertAsync_WhenDataTableIsEmpty_ReturnsZeroImmediately()
    {
        using var connection = new TestAdoConnection(ConnectionState.Closed);
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var rowsWritten = await connection.BulkInsertAsync("dbo.Products", table);

        rowsWritten.Should().Be(0);
        connection.OpenCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WhenConnectionIsNotSqlConnection_ThrowsArgumentException()
    {
        using var connection = new TestAdoConnection(ConnectionState.Open);
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1);

        var act = () => connection.BulkInsertAsync("dbo.Products", table);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("connection")
            .WithMessage("*SqlConnection*");
    }

    [Fact]
    public async Task BulkInsertAsync_WithClosedSqlConnection_AttemptsConnectionOpenAndThrowsSqlException()
    {
        using var connection = new SqlConnection("Server=127.0.0.1,65432;Database=Fake;Connection Timeout=1;");
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1);

        var act = () => connection.BulkInsertAsync("dbo.Products", table);
        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WithOverriddenExecutor_AndOpenConnection_ExecutesWithoutReopening()
    {
        var originalExecutor = BulkExtensions.BulkCopyExecutor;
        try
        {
            SqlConnection? capturedConn = null;
            string? capturedTable = null;
            DataTable? capturedDt = null;
            SqlTransaction? capturedTx = null;
            int capturedBatch = -1;
            int capturedTimeout = -1;
            CancellationToken capturedCt = default;

            BulkExtensions.BulkCopyExecutor = (conn, table, dt, tx, batch, timeout, ct) =>
            {
                capturedConn = conn;
                capturedTable = table;
                capturedDt = dt;
                capturedTx = tx;
                capturedBatch = batch;
                capturedTimeout = timeout;
                capturedCt = ct;
                return Task.FromResult(dt.Rows.Count);
            };

            using var connection = new TestAdoConnection(ConnectionState.Open);
            using var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(10);
            table.Rows.Add(20);

            using var cts = new CancellationTokenSource();

            var rows = await connection.BulkInsertAsync(
                "dbo.CustomTable",
                table,
                transaction: null,
                batchSize: 100,
                bulkCopyTimeout: 60,
                cancellationToken: cts.Token);

            rows.Should().Be(2);
            connection.OpenCount.Should().Be(0);
            capturedTable.Should().Be("dbo.CustomTable");
            capturedBatch.Should().Be(100);
            capturedTimeout.Should().Be(60);
            capturedCt.Should().Be(cts.Token);
        }
        finally
        {
            BulkExtensions.BulkCopyExecutor = originalExecutor;
        }
    }

    [Fact]
    public async Task BulkInsertAsync_WithOverriddenExecutor_AndClosedConnection_OpensConnectionAndExecutes()
    {
        var originalExecutor = BulkExtensions.BulkCopyExecutor;
        try
        {
            BulkExtensions.BulkCopyExecutor = (conn, table, dt, tx, batch, timeout, ct) => Task.FromResult(dt.Rows.Count);

            using var connection = new TestAdoConnection(ConnectionState.Closed);
            using var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(10);

            var rows = await connection.BulkInsertAsync("dbo.CustomTable", table);

            rows.Should().Be(1);
            connection.OpenCount.Should().Be(1);
        }
        finally
        {
            BulkExtensions.BulkCopyExecutor = originalExecutor;
        }
    }

    [Fact]
    public async Task ExecuteSqlBulkCopyAsync_WithOverriddenWriter_ReturnsWriterResult()
    {
        var originalWriter = BulkExtensions.BulkCopyWriter;
        try
        {
            BulkExtensions.BulkCopyWriter = (bc, dt, ct) => Task.FromResult(99);

            using var connection = new SqlConnection("Server=127.0.0.1,65432;Database=Fake;Connection Timeout=1;");
            using var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(1);

            using var bulkCopy = BulkExtensions.CreateSqlBulkCopy(connection, null, "dbo.TestTable", table, 0, 30);
            var rows = await BulkExtensions.ExecuteSqlBulkCopyAsync(bulkCopy, table, default);

            rows.Should().Be(99);
        }
        finally
        {
            BulkExtensions.BulkCopyWriter = originalWriter;
        }
    }

    [Fact]
    public void CreateSqlBulkCopy_ConfiguresAllPropertiesCorrectly()
    {
        using var connection = new SqlConnection("Server=127.0.0.1,65432;Database=Fake;Connection Timeout=1;");
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));

        using var bulkCopy = BulkExtensions.CreateSqlBulkCopy(connection, null, "dbo.TestTable", table, 50, 45);

        bulkCopy.DestinationTableName.Should().Be("dbo.TestTable");
        bulkCopy.BatchSize.Should().Be(50);
        bulkCopy.BulkCopyTimeout.Should().Be(45);
        bulkCopy.ColumnMappings.Count.Should().Be(2);
        bulkCopy.ColumnMappings[0].SourceColumn.Should().Be("Id");
        bulkCopy.ColumnMappings[0].DestinationColumn.Should().Be("Id");
        bulkCopy.ColumnMappings[1].SourceColumn.Should().Be("Name");
        bulkCopy.ColumnMappings[1].DestinationColumn.Should().Be("Name");
    }

    [Fact]
    public async Task ExecuteSqlBulkCopyAsync_WithClosedConnection_ThrowsInvalidOperationException()
    {
        using var connection = new SqlConnection("Server=127.0.0.1,65432;Database=Fake;Connection Timeout=1;");
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1);

        var bulkCopy = BulkExtensions.CreateSqlBulkCopy(connection, null, "dbo.TestTable", table, 0, 30);
        var act = async () => await BulkExtensions.ExecuteSqlBulkCopyAsync(bulkCopy, table, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DefaultBulkCopyExecutor_WhenGivenValidDataTable_RunsPipelineAndThrowsInvalidOperationOnClosedConn()
    {
        using var connection = new SqlConnection("Server=127.0.0.1,65432;Database=Fake;Connection Timeout=1;");
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, "Test");

        var act = async () => await BulkExtensions.BulkCopyExecutor(connection, "dbo.TestTable", table, null, 50, 45, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── BulkDeleteAsync Tests ────────────────────────────────────────────────

    [Fact]
    public async Task BulkDeleteAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = () => connection.BulkDeleteAsync("DELETE FROM dbo.Products WHERE Id = 1");
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkDeleteAsync_WhenSqlNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new TestAdoConnection();
        var act = () => connection.BulkDeleteAsync(invalidSql!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task BulkDeleteAsync_WhenConnectionClosed_OpensConnectionAndExecutesSql()
    {
        using var connection = new TestAdoConnection(ConnectionState.Closed);
        var result = await connection.BulkDeleteAsync(
            "DELETE FROM dbo.Products WHERE Price > @MinPrice",
            new { MinPrice = 100m },
            commandTimeout: 45);

        result.Should().Be(42);
        connection.OpenCount.Should().Be(1);
        connection.LastCommandText.Should().Be("DELETE FROM dbo.Products WHERE Price > @MinPrice");
    }

    [Fact]
    public async Task BulkDeleteAsync_WhenConnectionOpen_DoesNotReOpen()
    {
        using var connection = new TestAdoConnection(ConnectionState.Open);
        var result = await connection.BulkDeleteAsync("DELETE FROM dbo.Products");

        result.Should().Be(42);
        connection.OpenCount.Should().Be(0);
    }

    // ─── BulkUpdateAsync Tests ────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = () => connection.BulkUpdateAsync("UPDATE dbo.Products SET Price = 0");
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkUpdateAsync_WhenSqlNullOrWhiteSpace_ThrowsArgumentException(string? invalidSql)
    {
        using var connection = new TestAdoConnection();
        var act = () => connection.BulkUpdateAsync(invalidSql!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task BulkUpdateAsync_WhenConnectionClosed_OpensConnectionAndExecutesSql()
    {
        using var connection = new TestAdoConnection(ConnectionState.Closed);
        var result = await connection.BulkUpdateAsync(
            "UPDATE dbo.Products SET Price = @Price WHERE CategoryId = @CategoryId",
            new { Price = 19.99m, CategoryId = 5 },
            commandTimeout: 60);

        result.Should().Be(42);
        connection.OpenCount.Should().Be(1);
        connection.LastCommandText.Should().Be("UPDATE dbo.Products SET Price = @Price WHERE CategoryId = @CategoryId");
    }

    [Fact]
    public async Task BulkUpdateAsync_WhenConnectionOpen_DoesNotReOpen()
    {
        using var connection = new TestAdoConnection(ConnectionState.Open);
        var result = await connection.BulkUpdateAsync("UPDATE dbo.Products SET IsActive = 1");

        result.Should().Be(42);
        connection.OpenCount.Should().Be(0);
    }
}
