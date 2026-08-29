// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.MultiMap;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.Sqlite.Transactions;
using EricksonLopez.DapperExtensions.TypeHandlers;
using EricksonLopez.DapperExtensions.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level04_AdvancedIntegration;

/// <summary>
/// Level 4 — Advanced Integration: Unit of Work, Transactional Savepoints, and 1:N Relational Multi-Mapping.
/// </summary>
public static class UnitOfWorkAndMultiMapDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(4, "Advanced Integration", "Unit of Work with IAsyncDisposable, nested Savepoints, and 1:N Relational Mapping");

        // Ensure TypeHandlers are registered for DateOnly and enums regardless of level execution order.
        DapperTypeHandlerRegistrar.RegisterStandardHandlers();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<OrderStatus>();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<PaymentMethod>();

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        ConsoleHelper.PrintStep("1. Functional Transactional Scoping with WithUnitOfWorkAsync");
        await connection.WithUnitOfWorkAsync(async (uow, ct) =>
        {
            const string insertOrderSql = """
                INSERT INTO orders (customer_id, order_number, status, payment_method, total_amount, order_date)
                VALUES (1, 'ORD-UOW-001', 'Processing', 'CreditCard', 120.00, '2026-08-26');
                """;

            await connection.ExecuteAsync(insertOrderSql, transaction: uow.Transaction).ConfigureAwait(false);
            ConsoleHelper.PrintInfo("UoW Transaction", "Order inserted in transactional scope");

            const string insertItemSql = """
                INSERT INTO order_items (order_id, product_id, product_name, quantity, unit_price)
                VALUES (3, 1, 'Cloud Native Architecture Guide', 2, 60.00);
                """;

            await connection.ExecuteAsync(insertItemSql, transaction: uow.Transaction).ConfigureAwait(false);
            ConsoleHelper.PrintInfo("UoW Transaction", "Order item inserted");
        }).ConfigureAwait(false);

        ConsoleHelper.PrintSuccess("WithUnitOfWorkAsync automatically committed changes (CommitAsync).");

        ConsoleHelper.PrintStep("2. Manual Unit of Work with Savepoints and Partial Rollback");
        await using (var uow = await connection.BeginUnitOfWorkAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false))
        {
            // 1. Insert Order Header
            const string insertHeaderSql = """
                INSERT INTO orders (customer_id, order_number, status, payment_method, total_amount, order_date)
                VALUES (2, 'ORD-SP-001', 'Draft', 'PayPal', 200.00, '2026-08-26');
                """;
            await connection.ExecuteAsync(insertHeaderSql, transaction: uow.Transaction).ConfigureAwait(false);
            ConsoleHelper.PrintInfo("Savepoint Demo", "Header 'ORD-SP-001' inserted.");

            // 2. Create Savepoint before tentative operation
            var savepoint = await uow.CreateSavepointAsync("SP_TENTATIVE_ITEMS").ConfigureAwait(false);
            ConsoleHelper.PrintInfo("Savepoint Created", savepoint.Name);

            try
            {
                // Simulated business failure
                const string invalidItemSql = "INSERT INTO order_items (order_id, product_id, product_name, quantity, unit_price) VALUES (9999, 9999, 'Non-existent Product', 1, 50.00);";
                await connection.ExecuteAsync(invalidItemSql, transaction: uow.Transaction).ConfigureAwait(false);

                // Simulate validation failure
                bool validationFailed = true;
                if (validationFailed)
                {
                    throw new InvalidOperationException("Insufficient inventory for tentative item.");
                }

                await savepoint.ReleaseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintWarning($"Tentative block failure: {ex.Message}");
                ConsoleHelper.PrintStep("Rolling back strictly to savepoint 'SP_TENTATIVE_ITEMS'...");
                await savepoint.RollbackAsync().ConfigureAwait(false);
                ConsoleHelper.PrintSuccess("Partial rollback completed. Outer header transaction remains intact.");
            }

            // Commit outer transaction (header is committed, failed item discarded)
            await uow.CommitAsync().ConfigureAwait(false);
            ConsoleHelper.PrintSuccess("Unit of Work committed successfully while preserving valid transactional state.");
        }

        ConsoleHelper.PrintStep("3. Provider Transaction Extensions (ExecuteInTransactionAsync)");
        var orderCount = await connection.ExecuteInTransactionAsync(async trx =>
        {
            return await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM orders;",
                transaction: trx).ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Total Committed Orders", orderCount);

        ConsoleHelper.PrintStep("4. 1:N Relational Mapping with Root Deduplication (Dapper SplitOn)");
        const string splitSql = """
            SELECT 
                o.id AS Id, o.customer_id AS CustomerId, o.order_number AS OrderNumber,
                o.status AS Status, o.payment_method AS PaymentMethod, o.total_amount AS TotalAmount,
                i.id AS Id, i.order_id AS OrderId, i.product_id AS ProductId,
                i.product_name AS ProductName, i.quantity AS Quantity, i.unit_price AS UnitPrice
            FROM orders o
            LEFT JOIN order_items i ON o.id = i.order_id
            WHERE o.id = 1;
            """;

        var orderDictionary = new Dictionary<long, Order>();

        await connection.QueryAsync<Order, OrderItem, Order>(
            splitSql,
            (orderRoot, orderItem) =>
            {
                if (!orderDictionary.TryGetValue(orderRoot.Id, out var existingOrder))
                {
                    existingOrder = orderRoot;
                    orderDictionary.Add(existingOrder.Id, existingOrder);
                }

                if (orderItem != null && orderItem.Id > 0)
                {
                    existingOrder.Items.Add(orderItem);
                }

                return existingOrder;
            },
            splitOn: "Id").ConfigureAwait(false);

        foreach (var ord in orderDictionary.Values)
        {
            ConsoleHelper.PrintInfo($"Order [{ord.OrderNumber}]", $"Total Items: {ord.Items.Count}");
            foreach (var line in ord.Items)
            {
                ConsoleHelper.PrintInfo("  - Line Item", $"{line.ProductName} (Qty: {line.Quantity}, Price: {line.UnitPrice.ToString("C", CultureInfo.InvariantCulture)})");
            }
        }

        ConsoleHelper.PrintStep("5. WithUnitOfWorkAsync<TResult> — Returning a Typed Result");
        var orderCountInUoW = await connection.WithUnitOfWorkAsync<int>(async (uow, ct) =>
        {
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM orders;",
                transaction: uow.Transaction).ConfigureAwait(false);
            ConsoleHelper.PrintInfo("WithUnitOfWorkAsync<TResult> — In-scope count", count);
            return count;
        }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Result returned by WithUnitOfWorkAsync<TResult>", orderCountInUoW);
        ConsoleHelper.PrintSuccess("WithUnitOfWorkAsync<TResult> completed and transaction committed automatically.");

        ConsoleHelper.PrintStep("6. MultiMapBuilder<TReturn> — Builder Configuration (Map combiner, Map setter, SplitOn, Types)");
        var builderWithCombiner = MultiMapBuilder<Order>.Query(new RawSqlQuery(
            "SELECT o.id AS Id, o.customer_id AS CustomerId, o.order_number AS OrderNumber, o.status AS Status, " +
            "o.payment_method AS PaymentMethod, o.total_amount AS TotalAmount, o.order_date AS OrderDate, " +
            "i.id AS Id, i.order_id AS OrderId, i.product_id AS ProductId, " +
            "i.product_name AS ProductName, i.quantity AS Quantity, i.unit_price AS UnitPrice " +
            "FROM orders o LEFT JOIN order_items i ON o.id = i.order_id",
            new Dictionary<string, object?>()))
            .Map<OrderItem>("Id", (order, item) =>
            {
                if (item != null && item.Id > 0)
                    order.Items.Add(item);
                return order;
            });

        ConsoleHelper.PrintInfo("MultiMapBuilder<Order>.SplitOn", builderWithCombiner.SplitOn);
        ConsoleHelper.PrintInfo("MultiMapBuilder<Order>.Types[0] (Root)", builderWithCombiner.Types[0].Name);
        ConsoleHelper.PrintInfo("MultiMapBuilder<Order>.Types[1] (Related)", builderWithCombiner.Types[1].Name);

        var builderWithSetter = MultiMapBuilder<Order>.Query(new RawSqlQuery(
            "SELECT o.id, o.customer_id AS CustomerId, o.order_number AS OrderNumber, " +
            "o.status AS Status, o.payment_method AS PaymentMethod, o.total_amount AS TotalAmount, " +
            "o.order_date AS OrderDate FROM orders o WHERE o.id = 1",
            new Dictionary<string, object?>()))
            .Map<Order>("Id",
                setter: (rootOrder, relatedOrder) =>
                {
                    rootOrder.TotalAmount += relatedOrder.TotalAmount;
                });

        ConsoleHelper.PrintInfo("Builder with setter — Types Count", builderWithSetter.Types.Length);
        ConsoleHelper.PrintSuccess("MultiMapBuilder<TReturn> configured with Map(combiner) and Map(setter).");

        ConsoleHelper.PrintStep("7. ExecuteInTransactionAsync<TResult> — Typed Result Variant");
        var productsInTransaction = await connection.ExecuteInTransactionAsync<int>(async trx =>
        {
            const string tempInsertSql = """
                INSERT OR IGNORE INTO products (sku, name, price, stock_quantity, is_active, release_date, daily_restock_time)
                VALUES ('PROD-TYPED-TRX', 'Typed Transaction Demo', 19.99, 10, 1, '2026-08-26', '12:00:00');
                """;
            await connection.ExecuteAsync(tempInsertSql, transaction: trx).ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products;", transaction: trx)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("ExecuteInTransactionAsync<TResult> — Total Products after Insert", productsInTransaction);
        ConsoleHelper.PrintSuccess("ExecuteInTransactionAsync<TResult> executed and transaction committed automatically.");

        ConsoleHelper.PrintSuccess("Level 4 completed successfully.");
    }
}
