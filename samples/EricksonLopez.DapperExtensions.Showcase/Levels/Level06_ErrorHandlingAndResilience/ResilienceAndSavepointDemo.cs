// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.UnitOfWork;
using EricksonLopez.SqlBuilder.Abstractions;
using Polly;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level06_ErrorHandlingAndResilience;

/// <summary>
/// Level 6 — Error Handling & Resilience: Polly v8 pipelines, Transient Error Detectors, and ADR-014/ADR-016 invariants.
/// </summary>
public static class ResilienceAndSavepointDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(6, "Error Handling & Resilience", "Polly v8 pipelines, SQLSTATE error detection, and Retry policies with UoW and Savepoints");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        ConsoleHelper.PrintStep("1. Transient Error Detector Evaluation");
        var sqliteDetector = SqliteTransientErrorDetector.Default;
        var pgDetector = PostgreSqlTransientErrorDetector.Default;
        var sqlServerDetector = SqlServerTransientErrorDetector.Default;

        var busyException = new InvalidOperationException("database is locked (SQLITE_BUSY)");
        var syntaxException = new InvalidOperationException("near 'SELEC': syntax error");

        ConsoleHelper.PrintInfo("Is 'database is locked' transient?", sqliteDetector.IsTransient(busyException));
        ConsoleHelper.PrintInfo("Is 'syntax error' transient?", sqliteDetector.IsTransient(syntaxException));

        ConsoleHelper.PrintStep("2. Creating Pre-Configured Resilience Pipelines");
        var standardPipeline = SqlResilienceDefaults.ForSqlite();
        var circuitBreakerPipeline = SqlResilienceDefaults.ForSqliteWithCircuitBreaker();

        ConsoleHelper.PrintSuccess("Standard and CircuitBreaker pipelines constructed successfully.");

        ConsoleHelper.PrintStep("3. ADR-016 Invariant: Resilience Wrapping the Entire Unit of Work");
        // ADR-016 Rule: Resilience must wrap the entire transactional lifecycle (Begin -> Execute -> Commit).
        await standardPipeline.ExecuteAsync(async ct =>
        {
            await using var uow = await connection.BeginUnitOfWorkAsync(cancellationToken: ct).ConfigureAwait(false);

            const string updateStockSql = "UPDATE products SET stock_quantity = stock_quantity - 1 WHERE id = 1;";
            await connection.ExecuteAsync(updateStockSql, transaction: uow.Transaction).ConfigureAwait(false);

            await uow.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConsoleHelper.PrintSuccess("Resilient transaction executed in compliance with ADR-016.");

        ConsoleHelper.PrintStep("4. ADR-014 Invariant: Retries inside Open Transaction with Savepoints (ExecuteInSavepointWithRetryAsync)");
        await using (var uow = await connection.BeginUnitOfWorkAsync().ConfigureAwait(false))
        {
            int attemptCounter = 0;

            await uow.ExecuteInSavepointWithRetryAsync(
                standardPipeline,
                async (innerUow, ct) =>
                {
                    attemptCounter++;
                    ConsoleHelper.PrintInfo("Execution Attempt in Savepoint", attemptCounter);

                    if (attemptCounter == 1)
                    {
                        // Simulate a transient recoverable failure on first attempt
                        throw new InvalidOperationException("database is locked");
                    }

                    const string insertAuditSql = """
                        INSERT INTO outbox_messages (id, message_type, payload, status, created_at)
                        VALUES ('MSG-001', 'OrderCreatedEvent', '{"orderId":1}', 'Pending', '2026-08-26');
                        """;

                    await connection.ExecuteAsync(insertAuditSql, transaction: innerUow.Transaction).ConfigureAwait(false);
                },
                savepointName: "SP_AUDIT_RETRY").ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
            ConsoleHelper.PrintSuccess("Savepoint retry executed and committed after automatic recovery.");
        }

        ConsoleHelper.PrintStep("5. ExecuteInSavepointWithRetryAsync<TResult> — Returning a Typed Result");
        await using (var uow2 = await connection.BeginUnitOfWorkAsync().ConfigureAwait(false))
        {
            int attempt = 0;
            var stockCount = await uow2.ExecuteInSavepointWithRetryAsync<int>(
                standardPipeline,
                async (innerUow, ct) =>
                {
                    attempt++;
                    if (attempt == 1)
                        throw new InvalidOperationException("database is locked"); // simulated failure

                    return await connection.ExecuteScalarAsync<int>(
                        "SELECT stock_quantity FROM products WHERE id = 1;",
                        transaction: innerUow.Transaction).ConfigureAwait(false);
                },
                savepointName: "SP_TYPED_RETRY").ConfigureAwait(false);

            await uow2.CommitAsync().ConfigureAwait(false);
            ConsoleHelper.PrintInfo("Stock Quantity returned by ExecuteInSavepointWithRetryAsync<TResult>", stockCount);
        }

        ConsoleHelper.PrintStep("6. SqlResilienceExtensions — Pattern with Direct SqlResult");
        var queryResult = new SqlResult(
            "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id;",
            new Dictionary<string, object?> { ["id"] = 1L });

        var productFromResilientQuery = await connection.QueryFirstOrDefaultWithResilienceAsync<Product>(
            queryResult,
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Product via QueryFirstOrDefaultWithResilienceAsync<T>",
            productFromResilientQuery?.Name ?? "null");

        var countFromScalar = await connection.ExecuteScalarWithResilienceAsync<int>(
            new SqlResult("SELECT COUNT(*) FROM products;", new Dictionary<string, object?>()),
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("COUNT via ExecuteScalarWithResilienceAsync<T>", countFromScalar);

        var productsViaResilience = await connection.QueryWithResilienceAsync<Product>(
            new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE is_active = 1;",
                          new Dictionary<string, object?>()),
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Rows via QueryWithResilienceAsync<T>",
            System.Linq.Enumerable.Count(productsViaResilience));

        ConsoleHelper.PrintSuccess("SqlResilienceExtensions demonstrated with direct SqlResult.");

        ConsoleHelper.PrintStep("7. Advanced Pipelines: Aggressive() and Conservative()");
        var aggressivePipeline = SqlResilienceDefaults.Aggressive(SqliteTransientErrorDetector.Default);
        var conservativePipeline = SqlResilienceDefaults.Conservative(SqliteTransientErrorDetector.Default);

        var aggressiveProduct = await aggressivePipeline.ExecuteAsync(async ct =>
            await connection.QueryFirstOrDefaultAsync<Product>(
                "SELECT id, sku, name FROM products WHERE id = 1;").ConfigureAwait(false)
        ).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Aggressive Pipeline — Type", aggressivePipeline.GetType().Name);
        ConsoleHelper.PrintInfo("Aggressive — Product Name", aggressiveProduct?.Name ?? "null");
        ConsoleHelper.PrintInfo("Conservative Pipeline — Type", conservativePipeline.GetType().Name);
        ConsoleHelper.PrintSuccess("Aggressive() and Conservative() pipelines built successfully.");

        ConsoleHelper.PrintStep("8. Typed Pipelines: Standard<T>() and StandardWithCircuitBreaker<T>()");
        var typedPipeline = SqlResilienceDefaults.Standard<Product>(SqliteTransientErrorDetector.Default);
        var typedCbPipeline = SqlResilienceDefaults.StandardWithCircuitBreaker<int>(SqliteTransientErrorDetector.Default);

        var typedProduct = await typedPipeline.ExecuteAsync(async ct =>
        {
            return await connection.QueryFirstOrDefaultAsync<Product>(
                "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = 2;")
                .ConfigureAwait(false) ?? new Product { Name = "(not found)" };
        }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("ResiliencePipeline<Product> result", typedProduct.Name);

        var typedCount = await typedCbPipeline.ExecuteAsync(async ct =>
        {
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders;").ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("ResiliencePipeline<int> result — Total Orders", typedCount);

        ConsoleHelper.PrintStep("9. Provider Shortcuts: ForXxx() and ForXxxWithCircuitBreaker()");
        var sqlServerPipeline = SqlResilienceDefaults.ForSqlServer();
        var sqlServerCbPipeline = SqlResilienceDefaults.ForSqlServerWithCircuitBreaker();
        var pgPipeline = SqlResilienceDefaults.ForPostgreSql();
        var pgCbPipeline = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreaker();
        var mysqlPipeline = SqlResilienceDefaults.ForMySql();
        var mysqlCbPipeline = SqlResilienceDefaults.ForMySqlWithCircuitBreaker();
        var oraclePipeline = SqlResilienceDefaults.ForOracle();
        var oracleCbPipeline = SqlResilienceDefaults.ForOracleWithCircuitBreaker();
        var sqlitePipeline = SqlResilienceDefaults.ForSqlite();
        var sqliteCbPipeline = SqlResilienceDefaults.ForSqliteWithCircuitBreaker();

        ConsoleHelper.PrintInfo("ForSqlServer() pipeline", sqlServerPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqlServerWithCircuitBreaker() pipeline", sqlServerCbPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForPostgreSql() pipeline", pgPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForPostgreSqlWithCircuitBreaker() pipeline", pgCbPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForMySql() pipeline", mysqlPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForMySqlWithCircuitBreaker() pipeline", mysqlCbPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForOracle() pipeline", oraclePipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForOracleWithCircuitBreaker() pipeline", oracleCbPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqlite() pipeline", sqlitePipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqliteWithCircuitBreaker() pipeline", sqliteCbPipeline.GetType().Name);
        ConsoleHelper.PrintSuccess("All 10 provider shortcut methods constructed successfully.");

        ConsoleHelper.PrintStep("10. OracleTransientErrorDetector — Transient Error Evaluation");
        var oracleDetector = OracleTransientErrorDetector.Default;
        var oracleNetworkEx = new InvalidOperationException("ORA-12541: TNS:no listener");
        var oracleDeadlockEx = new InvalidOperationException("ORA-00060: deadlock detected while waiting for resource");
        var oracleSyntaxEx = new InvalidOperationException("ORA-00942: table or view does not exist");

        ConsoleHelper.PrintInfo("OracleDetector — Is 'TNS:no listener' transient?", oracleDetector.IsTransient(oracleNetworkEx));
        ConsoleHelper.PrintInfo("OracleDetector — Is 'deadlock detected' transient?", oracleDetector.IsTransient(oracleDeadlockEx));
        ConsoleHelper.PrintInfo("OracleDetector — Is 'table or view does not exist' transient?", oracleDetector.IsTransient(oracleSyntaxEx));
        ConsoleHelper.PrintSuccess("OracleTransientErrorDetector evaluated successfully.");

        ConsoleHelper.PrintStep("11. SqlResilienceExtensions — Full Overload Coverage: QuerySingle, QuerySingleOrDefault, and QueryFirst");
        var singleProduct = await connection.QuerySingleWithResilienceAsync<Product>(
            new SqlResult(
                "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id;",
                new Dictionary<string, object?> { ["id"] = 1L }),
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("QuerySingleWithResilienceAsync<Product> — Name", singleProduct.Name);
        ConsoleHelper.PrintInfo("QuerySingleWithResilienceAsync<Product> — Price", singleProduct.Price.ToString("C", CultureInfo.InvariantCulture));

        var maybeProduct = await connection.QuerySingleOrDefaultWithResilienceAsync<Product>(
            new SqlResult(
                "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id;",
                new Dictionary<string, object?> { ["id"] = 9999L }),
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("QuerySingleOrDefaultWithResilienceAsync<Product> (id=9999 non-existent)", maybeProduct?.Name ?? "null — product not found");

        var firstProduct = await connection.QueryFirstWithResilienceAsync<Product>(
            new SqlResult(
                "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products ORDER BY id ASC;",
                new Dictionary<string, object?>()),
            standardPipeline).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("QueryFirstWithResilienceAsync<Product> — First Product", firstProduct.Name);
        ConsoleHelper.PrintSuccess("All 6 public overloads of SqlResilienceExtensions demonstrated successfully.");

        ConsoleHelper.PrintSuccess("Level 6 completed successfully.");
    }
}
