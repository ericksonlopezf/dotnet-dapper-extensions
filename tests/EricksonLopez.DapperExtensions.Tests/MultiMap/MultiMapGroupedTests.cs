// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.MultiMap;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.MultiMap;

public sealed class MultiMapGroupedTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private readonly SqliteCompiler _compiler = new();

    public sealed class OrderEntity
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public OrderItemEntity? SingleItem { get; set; }
        public List<OrderItemEntity> Items { get; set; } = new();
    }

    public sealed class OrderItemEntity
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE orders (id INTEGER PRIMARY KEY, order_number TEXT NOT NULL);
            CREATE TABLE order_items (id INTEGER PRIMARY KEY, order_id INTEGER NOT NULL, product_name TEXT NOT NULL, price REAL NOT NULL);
            
            INSERT INTO orders (id, order_number) VALUES (1, 'ORD-001'), (2, 'ORD-002');
            INSERT INTO order_items (id, order_id, product_name, price) VALUES 
                (101, 1, 'Product A', 19.99),
                (102, 1, 'Product B', 29.99),
                (103, 2, 'Product C', 99.00);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task QueryAsync_DapperPath_MapsEntities()
    {
        var query = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            WHERE o.id = 1
            ORDER BY oi.id
            """);

        using var tx = _connection.BeginTransaction();
        var results = (await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.SingleItem = item;
                return order;
            })
            .QueryAsync(_connection, _compiler, transaction: tx, commandTimeout: 30))
            .ToList();

        results.Should().HaveCount(2);
        results[0].Id.Should().Be(1);
        results[0].SingleItem.Should().NotBeNull();
        results[0].SingleItem!.ProductName.Should().Be("Product A");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_DapperPath_ReturnsFirstOrNull()
    {
        var query = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            WHERE o.id = 1
            """);

        var result = await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.SingleItem = item;
                return order;
            })
            .QueryFirstOrDefaultAsync(_connection, _compiler);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);

        var emptyQuery = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            WHERE o.id = 999
            """);

        var emptyResult = await MultiMapBuilder<OrderEntity>
            .Query(emptyQuery)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.SingleItem = item;
                return order;
            })
            .QueryFirstOrDefaultAsync(_connection, _compiler);

        emptyResult.Should().BeNull();
    }

    [Fact]
    public async Task QueryGroupedAsync_DeduplicatesRoots_AndGroupsChildren()
    {
        var query = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            ORDER BY o.id, oi.id
            """);

        using var tx = _connection.BeginTransaction();
        var orders = (await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.Items.Add(item);
                return order;
            })
            .QueryGroupedAsync(_connection, _compiler, o => o.Id, transaction: tx, commandTimeout: 30))
            .ToList();

        orders.Should().HaveCount(2);

        var order1 = orders.First(o => o.Id == 1);
        order1.OrderNumber.Should().Be("ORD-001");
        order1.Items.Should().HaveCount(2);
        order1.Items[0].ProductName.Should().Be("Product A");
        order1.Items[1].ProductName.Should().Be("Product B");

        var order2 = orders.First(o => o.Id == 2);
        order2.OrderNumber.Should().Be("ORD-002");
        order2.Items.Should().HaveCount(1);
        order2.Items[0].ProductName.Should().Be("Product C");
    }

    [Fact]
    public async Task QueryGroupedFirstOrDefaultAsync_ReturnsFirstGroupedRoot_OrNull()
    {
        var query = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            WHERE o.id = 1
            ORDER BY oi.id
            """);

        var order = await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.Items.Add(item);
                return order;
            })
            .QueryGroupedFirstOrDefaultAsync(_connection, _compiler, o => o.Id);

        order.Should().NotBeNull();
        order!.Id.Should().Be(1);
        order.Items.Should().HaveCount(2);

        var emptyQuery = Sql.Raw("""
            SELECT 
                o.id AS Id, o.order_number AS OrderNumber,
                oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            WHERE o.id = 999
            """);

        var emptyOrder = await MultiMapBuilder<OrderEntity>
            .Query(emptyQuery)
            .Map<OrderItemEntity>("Id", (order, item) =>
            {
                order.Items.Add(item);
                return order;
            })
            .QueryGroupedFirstOrDefaultAsync(_connection, _compiler, o => o.Id);

        emptyOrder.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_WithoutMappings_ThrowsInvalidOperationException()
    {
        var query = Sql.Raw("SELECT 1");

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .QueryAsync(_connection, _compiler);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one entity mapping must be registered*");
    }

    [Fact]
    public async Task QueryGroupedAsync_WithoutMappings_ThrowsInvalidOperationException()
    {
        var query = Sql.Raw("SELECT 1");

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .QueryGroupedAsync(_connection, _compiler, o => o.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one entity mapping must be registered*");
    }

    // ─── CancellationToken Cancellation Tests ───────────────────────────

    [Fact]
    public async Task QueryAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        var query = Sql.Raw("""
            SELECT o.id AS Id, o.order_number AS OrderNumber, oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            """);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) => order)
            .QueryAsync(_connection, _compiler, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryGroupedAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        var query = Sql.Raw("""
            SELECT o.id AS Id, o.order_number AS OrderNumber, oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            """);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) => order)
            .QueryGroupedAsync(_connection, _compiler, o => o.Id, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        var query = Sql.Raw("""
            SELECT o.id AS Id, o.order_number AS OrderNumber, oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            """);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) => order)
            .QueryFirstOrDefaultAsync(_connection, _compiler, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryGroupedFirstOrDefaultAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        var query = Sql.Raw("""
            SELECT o.id AS Id, o.order_number AS OrderNumber, oi.id AS Id, oi.product_name AS ProductName, oi.price AS Price
            FROM orders o
            JOIN order_items oi ON o.id = oi.order_id
            """);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await MultiMapBuilder<OrderEntity>
            .Query(query)
            .Map<OrderItemEntity>("Id", (order, item) => order)
            .QueryGroupedFirstOrDefaultAsync(_connection, _compiler, o => o.Id, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
