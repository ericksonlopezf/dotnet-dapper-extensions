// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.MultiMap;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.MultiMap;

// ─── Domain fixtures ──────────────────────────────────────────────────────────

public sealed class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public Customer? Customer { get; set; }
    public Product? Product { get; set; }
}

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Product
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
}

public sealed class Address
{
    public int Id { get; set; }
    public string Line1 { get; set; } = "";
}

public sealed class AotCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public static Func<IDataReader, object> GetMultiMapReaderFactory()
    {
        return reader => new AotCustomer
        {
            Id = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            Name = reader.GetString(reader.GetOrdinal("CustomerName"))
        };
    }
}

public sealed class AotOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public AotCustomer? Customer { get; set; }
    public List<AotOrderItem> Items { get; set; } = new();

    public static Func<IDataReader, object> GetMultiMapReaderFactory()
    {
        return reader => new AotOrder
        {
            Id = reader.GetInt32(reader.GetOrdinal("OrderId")),
            OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber"))
        };
    }
}

public sealed class AotOrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";

    public static Func<IDataReader, object> GetMultiMapReaderFactory()
    {
        return reader => new AotOrderItem
        {
            Id = reader.GetInt32(reader.GetOrdinal("ItemId")),
            ProductName = reader.GetString(reader.GetOrdinal("ProductName"))
        };
    }
}

public sealed class OrderMapper : IDataReaderMapper<Order>
{
    public Order Map(IDataReader reader)
    {
        return new Order
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Total = reader.GetDecimal(reader.GetOrdinal("total"))
        };
    }
}

// ─── Tests ───────────────────────────────────────────────────────────────────

public sealed class MultiMapBuilderTests : IAsyncLifetime
{
    private static readonly ISqlQuery _fakeQuery = Sql.Raw("SELECT 1");
    private SqliteConnection _connection = null!;
    private readonly SqliteCompiler _compiler = new();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE aot_orders (OrderId INTEGER PRIMARY KEY, OrderNumber TEXT NOT NULL);
            CREATE TABLE aot_customers (CustomerId INTEGER PRIMARY KEY, CustomerName TEXT NOT NULL);
            CREATE TABLE aot_items (ItemId INTEGER PRIMARY KEY, OrderId INTEGER NOT NULL, ProductName TEXT NOT NULL);

            INSERT INTO aot_customers (CustomerId, CustomerName) VALUES (10, 'Alice');
            INSERT INTO aot_orders (OrderId, OrderNumber) VALUES (1, 'ORD-AOT-001'), (2, 'ORD-AOT-002');
            INSERT INTO aot_items (ItemId, OrderId, ProductName) VALUES (100, 1, 'Keyboard'), (101, 1, 'Mouse');
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ─── Query() factory ────────────────────────────────────────────────

    [Fact]
    public void Query_WithNullQuery_ThrowsArgumentNullException()
    {
        var act = () => MultiMapBuilder<Order>.Query(null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("query");
    }

    [Fact]
    public void Query_WithValidQuery_ReturnsBuilderInstance()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        builder.Should().NotBeNull();
    }

    // ─── Map() — fluent registration ────────────────────────────────────

    [Fact]
    public void Map_WithNullSplitOn_ThrowsArgumentException()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        var act = () => builder.Map<Customer>(null!, (order, c) => { order.Customer = c; return order; });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Map_WithEmptySplitOn_ThrowsArgumentException()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        var act = () => builder.Map<Customer>("", (order, c) => { order.Customer = c; return order; });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Map_WithNullCombiner_ThrowsArgumentNullException()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        var act = () => builder.Map<Customer>("customer_id", (Func<Order, Customer, Order>)null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("combiner");
    }

    [Fact]
    public void Map_WithActionSetter_NullSetter_ThrowsArgumentNullException()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        var act = () => builder.Map<Customer>("customer_id", (Action<Order, Customer>)null!);
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("setter");
    }

    [Fact]
    public void Map_WithActionSetter_ExecutesAndReturnsRoot()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => o.Customer = c);

        builder.Should().NotBeNull();
        builder.SplitOn.Should().Be("customer_id");
    }

    [Fact]
    public void Map_DiscoversStaticGetMultiMapReaderFactory_WhenParserNull()
    {
        var builder = MultiMapBuilder<AotOrder>.Query(_fakeQuery)
            .Map<AotCustomer>("CustomerId", (o, c) => { o.Customer = c; return o; });

        builder.Should().NotBeNull();
    }

    [Fact]
    public void Map_CombinerHandlesEmptyPartsGracefully()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; });

        var combinersField = typeof(MultiMapBuilder<Order>).GetField("_combiners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var combiners = (System.Collections.Generic.List<Func<object[], Order, Order>>)combinersField.GetValue(builder)!;

        var order = new Order { Id = 1 };
        var result = combiners[0](Array.Empty<object>(), order);

        result.Should().BeSameAs(order);
    }

    // ─── SplitOn property ───────────────────────────────────────────────

    [Fact]
    public void SplitOn_NoMappings_ReturnsEmptyString()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        builder.SplitOn.Should().BeEmpty();
    }

    [Fact]
    public void SplitOn_OneMappingRegistered_ReturnsSingleColumn()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; });

        builder.SplitOn.Should().Be("customer_id");
    }

    [Fact]
    public void SplitOn_TwoMappingsRegistered_ReturnsCommaSeparated()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; })
            .Map<Product>("product_id", (o, p) => { o.Product = p; return o; });

        builder.SplitOn.Should().Be("customer_id,product_id");
    }

    [Fact]
    public void SplitOn_EightMappings_ReturnsAllCommaSeparated()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("c_id", (o, c) => { o.Customer = c; return o; })
            .Map<Product>("p_id", (o, p) => { o.Product = p; return o; })
            .Map<Address>("a1_id", (o, _) => o)
            .Map<Address>("a2_id", (o, _) => o)
            .Map<Address>("a3_id", (o, _) => o)
            .Map<Address>("a4_id", (o, _) => o)
            .Map<Address>("a5_id", (o, _) => o)
            .Map<Address>("a6_id", (o, _) => o);

        builder.SplitOn.Should().Be("c_id,p_id,a1_id,a2_id,a3_id,a4_id,a5_id,a6_id");
    }

    // ─── Types property ─────────────────────────────────────────────────

    [Fact]
    public void Types_NoMappings_ContainsOnlyRootType()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery);
        builder.Types.Should().Equal(typeof(Order));
    }

    [Fact]
    public void Types_TwoMappings_HasCorrectTypeOrder()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; })
            .Map<Product>("product_id", (o, p) => { o.Product = p; return o; });

        builder.Types.Should().Equal(typeof(Order), typeof(Customer), typeof(Product));
    }

    // ─── QueryAsync & QueryFirstOrDefaultAsync (Dapper & AOT paths) ────

    [Fact]
    public async Task QueryAsync_Guards_CheckNullArguments()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; });

        var actNullConn = async () => await builder.QueryAsync(null!, _compiler);
        await actNullConn.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var actNullComp = async () => await builder.QueryAsync(_connection, null!);
        await actNullComp.Should().ThrowAsync<ArgumentNullException>().WithParameterName("compiler");
    }

    [Fact]
    public async Task QueryAsync_AotPath_ExecutesManualParsers()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, c.CustomerId, c.CustomerName
            FROM aot_orders o
            CROSS JOIN aot_customers c
            WHERE o.OrderId = 1;
            """);

        var results = (await MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) => { o.Customer = c; return o; })
            .QueryAsync(_connection, _compiler))
            .ToList();

        results.Should().HaveCount(1);
        results[0].Id.Should().Be(1);
        results[0].OrderNumber.Should().Be("ORD-AOT-001");
        results[0].Customer.Should().NotBeNull();
        results[0].Customer!.Id.Should().Be(10);
        results[0].Customer!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task QueryAsync_AotPath_ThrowsWhenCancelledInsideReaderLoop()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, c.CustomerId, c.CustomerName
            FROM aot_orders o
            CROSS JOIN aot_customers c;
            """);

        using var cts = new CancellationTokenSource();
        var builder = MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) =>
            {
                cts.Cancel(); // cancel on 1st row so 2nd row iteration hits cancellationToken.ThrowIfCancellationRequested()
                o.Customer = c;
                return o;
            });

        var act = async () => await builder.QueryAsync(_connection, _compiler, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryGroupedAsync_AotPath_ThrowsWhenCancelledInsideReaderLoop()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, i.ItemId, i.ProductName
            FROM aot_orders o
            JOIN aot_items i ON o.OrderId = i.OrderId;
            """);

        using var cts = new CancellationTokenSource();
        var builder = MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotOrderItem>("ItemId", (o, i) =>
            {
                cts.Cancel(); // cancel on 1st row so 2nd row iteration hits cancellationToken.ThrowIfCancellationRequested()
                o.Items.Add(i);
                return o;
            });

        var act = async () => await builder.QueryGroupedAsync(_connection, _compiler, o => o.Id, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryAsync_AotPath_RootMissingFactory_ThrowsInvalidOperationException()
    {
        var query = Sql.Raw("SELECT 1 AS id, 'Alice' AS Name, 2 AS CustomerId, 'Bob' AS CustomerName;");

        // Order does not have GetMultiMapReaderFactory, but we pass custom parser to Map so _mappings has no nulls
        var builder = MultiMapBuilder<Order>.Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) => o, _ => new AotCustomer());

        var act = async () => await builder.QueryAsync(_connection, _compiler);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Root type Order is missing GetMultiMapReaderFactory()*");
    }

    [Fact]
    public async Task QueryAsync_WithActionSetter_InvokesSetterAndHydratesEntity()
    {
        var query = Sql.Raw("""
            SELECT 1 AS Id, 100.0 AS Total, 10 AS Id, 'Alice' AS Name;
            """);

        var results = (await MultiMapBuilder<Order>
            .Query(query)
            .Map<Customer>("Id", (o, c) => o.Customer = c)
            .QueryAsync(_connection, _compiler))
            .ToList();

        results.Should().HaveCount(1);
        results[0].Customer.Should().NotBeNull();
        results[0].Customer!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task QueryAsync_MixedMappings_FallsBackToDapper()
    {
        var query = Sql.Raw("""
            SELECT 1 AS Id, 100.0 AS Total, 10 AS CustomerId, 'Alice' AS CustomerName, 20 AS ProductId, 'PROD-1' AS Sku;
            """);

        var results = (await MultiMapBuilder<Order>
            .Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) => o, _ => new AotCustomer { Id = 10, Name = "Alice" })
            .Map<Product>("ProductId", (o, p) => { o.Product = p; return o; }, parser: null)
            .QueryAsync(_connection, _compiler))
            .ToList();

        results.Should().HaveCount(1);
        results[0].Product.Should().NotBeNull();
        results[0].Product!.Sku.Should().Be("PROD-1");
    }

    [Fact]
    public async Task QueryGroupedAsync_MixedMappings_FallsBackToDapper()
    {
        var query = Sql.Raw("""
            SELECT 1 AS Id, 100.0 AS Total, 10 AS CustomerId, 'Alice' AS CustomerName, 20 AS ProductId, 'PROD-1' AS Sku;
            """);

        var results = (await MultiMapBuilder<Order>
            .Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) => o, _ => new AotCustomer { Id = 10, Name = "Alice" })
            .Map<Product>("ProductId", (o, p) => { o.Product = p; return o; }, parser: null)
            .QueryGroupedAsync(_connection, _compiler, o => o.Id))
            .ToList();

        results.Should().HaveCount(1);
        results[0].Product.Should().NotBeNull();
        results[0].Product!.Sku.Should().Be("PROD-1");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ReturnsFirstResult_OrNull()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, c.CustomerId, c.CustomerName
            FROM aot_orders o
            CROSS JOIN aot_customers c
            WHERE o.OrderId = 1;
            """);

        var result = await MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotCustomer>("CustomerId", (o, c) => { o.Customer = c; return o; })
            .QueryFirstOrDefaultAsync(_connection, _compiler);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);

        var emptyQuery = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, c.CustomerId, c.CustomerName
            FROM aot_orders o
            CROSS JOIN aot_customers c
            WHERE o.OrderId = 999;
            """);

        var emptyResult = await MultiMapBuilder<AotOrder>
            .Query(emptyQuery)
            .Map<AotCustomer>("CustomerId", (o, c) => { o.Customer = c; return o; })
            .QueryFirstOrDefaultAsync(_connection, _compiler);

        emptyResult.Should().BeNull();
    }

    // ─── QueryGroupedAsync AOT path ─────────────────────────────────────

    [Fact]
    public async Task QueryGroupedAsync_Guards_CheckNullArguments()
    {
        var builder = MultiMapBuilder<Order>.Query(_fakeQuery)
            .Map<Customer>("customer_id", (o, c) => { o.Customer = c; return o; });

        var actNullConn = async () => await builder.QueryGroupedAsync(null!, _compiler, o => o.Id);
        await actNullConn.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var actNullComp = async () => await builder.QueryGroupedAsync(_connection, null!, o => o.Id);
        await actNullComp.Should().ThrowAsync<ArgumentNullException>().WithParameterName("compiler");

        var actNullKey = async () => await builder.QueryGroupedAsync<int>(_connection, _compiler, null!);
        await actNullKey.Should().ThrowAsync<ArgumentNullException>().WithParameterName("keySelector");
    }

    [Fact]
    public async Task QueryGroupedAsync_AotPath_GroupsChildren()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, i.ItemId, i.ProductName
            FROM aot_orders o
            JOIN aot_items i ON o.OrderId = i.OrderId
            ORDER BY o.OrderId, i.ItemId;
            """);

        var orders = (await MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotOrderItem>("ItemId", (order, item) =>
            {
                order.Items.Add(item);
                return order;
            })
            .QueryGroupedAsync(_connection, _compiler, o => o.Id))
            .ToList();

        orders.Should().HaveCount(1);
        orders[0].Id.Should().Be(1);
        orders[0].Items.Should().HaveCount(2);
        orders[0].Items[0].ProductName.Should().Be("Keyboard");
        orders[0].Items[1].ProductName.Should().Be("Mouse");
    }

    [Fact]
    public async Task QueryGroupedAsync_AotPath_RootMissingFactory_ThrowsInvalidOperationException()
    {
        var query = Sql.Raw("SELECT 1 AS id, 'Alice' AS Name, 2 AS ItemId, 'Bob' AS ProductName;");

        var builder = MultiMapBuilder<Order>.Query(query)
            .Map<AotOrderItem>("ItemId", (o, i) => o, _ => new AotOrderItem());

        var act = async () => await builder.QueryGroupedAsync(_connection, _compiler, o => o.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Root type Order is missing GetMultiMapReaderFactory()*");
    }

    [Fact]
    public async Task QueryGroupedFirstOrDefaultAsync_AotPath_ReturnsFirstOrNull()
    {
        var query = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, i.ItemId, i.ProductName
            FROM aot_orders o
            JOIN aot_items i ON o.OrderId = i.OrderId
            WHERE o.OrderId = 1;
            """);

        var order = await MultiMapBuilder<AotOrder>
            .Query(query)
            .Map<AotOrderItem>("ItemId", (o, i) => { o.Items.Add(i); return o; })
            .QueryGroupedFirstOrDefaultAsync(_connection, _compiler, o => o.Id);

        order.Should().NotBeNull();
        order!.Id.Should().Be(1);
        order.Items.Should().HaveCount(2);

        var emptyQuery = Sql.Raw("""
            SELECT o.OrderId, o.OrderNumber, i.ItemId, i.ProductName
            FROM aot_orders o
            JOIN aot_items i ON o.OrderId = i.OrderId
            WHERE o.OrderId = 999;
            """);

        var emptyOrder = await MultiMapBuilder<AotOrder>
            .Query(emptyQuery)
            .Map<AotOrderItem>("ItemId", (o, i) => { o.Items.Add(i); return o; })
            .QueryGroupedFirstOrDefaultAsync(_connection, _compiler, o => o.Id);

        emptyOrder.Should().BeNull();
    }

    // ─── AotMultiMapReader Direct Tests ─────────────────────────────────

    [Fact]
    public async Task AotMultiMapReader_QueryAotAsync_ReadsAndSplitsProperly()
    {
        var mappings = new List<(Type Type, string SplitOn, Func<IDataReader, object> Parser)>
        {
            (typeof(AotOrder), "OrderId", reader => new AotOrder
            {
                Id = reader.GetInt32(reader.GetOrdinal("OrderId")),
                OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber"))
            }),
            (typeof(AotCustomer), "CustomerId", reader => new AotCustomer
            {
                Id = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                Name = reader.GetString(reader.GetOrdinal("CustomerName"))
            })
        };

        var combiners = new Func<object[], AotOrder, AotOrder>[]
        {
            (parts, root) =>
            {
                root.Customer = parts[0] as AotCustomer;
                return root;
            }
        };

        var sql = """
            SELECT o.OrderId, o.OrderNumber, c.CustomerId, c.CustomerName
            FROM aot_orders o
            CROSS JOIN aot_customers c
            WHERE o.OrderId = 1;
            """;

        var results = (await AotMultiMapReader.QueryAotAsync<AotOrder>(
            _connection,
            sql,
            param: null,
            transaction: null,
            commandTimeout: null,
            commandType: null,
            mappings,
            combiners))
            .ToList();

        results.Should().HaveCount(1);
        results[0].Id.Should().Be(1);
        results[0].OrderNumber.Should().Be("ORD-AOT-001");
        results[0].Customer.Should().NotBeNull();
        results[0].Customer!.Id.Should().Be(10);
        results[0].Customer!.Name.Should().Be("Alice");
    }

    // ─── MultiMapDescriptor & IDataReaderMapper Tests ───────────────────

    [Fact]
    public void MultiMapDescriptor_InitializesAllProperties()
    {
        var type = typeof(Order);
        var columns = new[] { "id", "total" };
        Func<IDataReader, object> factory = _ => new Order();

        var descriptor = new MultiMapDescriptor(type, "orders", columns, factory);

        descriptor.EntityType.Should().Be(type);
        descriptor.TableName.Should().Be("orders");
        descriptor.ColumnNames.Should().Equal(columns);
        descriptor.ReaderFactory.Should().BeSameAs(factory);
    }

    [Fact]
    public async Task IDataReaderMapper_CanMapFromDataReader()
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT 10 AS id, 99.5 AS total;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var readSuccess = await reader.ReadAsync();
        readSuccess.Should().BeTrue();

        IDataReaderMapper<Order> mapper = new OrderMapper();
        var mapped = mapper.Map(reader);

        mapped.Should().NotBeNull();
        mapped.Id.Should().Be(10);
        mapped.Total.Should().Be(99.5m);
    }
}
