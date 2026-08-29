# Cookbook: Production Recipes — EricksonLopez.DapperExtensions

A curated collection of production-ready recipes demonstrating common data access patterns using strictly the public APIs of the **EricksonLopez.DapperExtensions** ecosystem.

---

## 🍳 Table of Contents

1. [Recipe 1: Transactional Outbox Pattern with Unit of Work (ADR-016)](#recipe-1-transactional-outbox-pattern-with-unit-of-work-adr-016)
2. [Recipe 2: Multi-Entity Relational Aggregate Hydration (1:N Deduplication)](#recipe-2-multi-entity-relational-aggregate-hydration-1n-deduplication)
3. [Recipe 3: High-Throughput Bulk Ingestion in PostgreSQL (UNNEST)](#recipe-3-high-throughput-bulk-ingestion-in-postgresql-unnest)
4. [Recipe 4: Streaming Binary Bulk Copy in SQL Server (SqlBulkCopy)](#recipe-4-streaming-binary-bulk-copy-in-sql-server-sqlbulkcopy)
5. [Recipe 5: Multi-Row Parameterized Batching in SQLite, MySQL & MariaDB](#recipe-5-multi-row-parameterized-batching-in-sqlite-mysql--mariadb)
6. [Recipe 6: Keyset (Cursor-Based) High-Volume Pagination](#recipe-6-keyset-cursor-based-high-volume-pagination)
7. [Recipe 7: Single Round-Trip Multi-Query Pagination with Total Count](#recipe-7-single-round-trip-multi-query-pagination-with-total-count)
8. [Recipe 8: Partial Transaction Retries with Savepoints (ADR-014)](#recipe-8-partial-transaction-retries-with-savepoints-adr-014)
9. [Recipe 9: Zero-Reflection Native AOT Entity Hydration](#recipe-9-zero-reflection-native-aot-entity-hydration)
10. [Recipe 10: OpenTelemetry Distributed Tracing & Execution Metrics](#recipe-10-opentelemetry-distributed-tracing--execution-metrics)
11. [Recipe 11: Resilient Database Connectivity Health Checks for Kubernetes](#recipe-11-resilient-database-connectivity-health-checks-for-kubernetes)
12. [Recipe 12: Custom Value Object Type Handler & Error Detector](#recipe-12-custom-value-object-type-handler--error-detector)
13. [Recipe 13: Choosing the Right `SqlResilienceExtensions` Query Overload](#recipe-13-choosing-the-right-sqlresilienceextensions-query-overload)

---

## Recipe 1: Transactional Outbox Pattern with Unit of Work (ADR-016)

### Problem
Atomically persist a domain entity change and enqueue its corresponding integration event in a single database transaction, ensuring zero dual-write inconsistency and automatic rollback on failure.

### Solution
Wrap the business logic and event insertion inside `WithUnitOfWorkAsync` or within a Polly v8 `ResiliencePipeline` wrapping `BeginUnitOfWorkAsync` per ADR-016.

### Complete Code
```csharp
using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.UnitOfWork;

public sealed class OrderAppService
{
    private readonly IDbConnection _connection;

    public OrderAppService(IDbConnection connection) => _connection = connection;

    public async Task PlaceOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var pipeline = SqlResilienceDefaults.ForPostgreSql();

        // ADR-016: Polly resilience wraps the entire transactional unit
        await pipeline.ExecuteAsync(async ct =>
        {
            await using var uow = await _connection.BeginUnitOfWorkAsync(IsolationLevel.ReadCommitted, ct);

            // 1. Persist Domain Entity
            const string insertOrderSql = """
                INSERT INTO orders (id, customer_id, order_number, status, total_amount, order_date)
                VALUES (@Id, @CustomerId, @OrderNumber, @Status, @TotalAmount, @OrderDate);
                """;

            await _connection.ExecuteAsync(new CommandDefinition(
                insertOrderSql,
                new
                {
                    order.Id,
                    order.CustomerId,
                    order.OrderNumber,
                    Status = order.Status.ToString(),
                    order.TotalAmount,
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                },
                transaction: uow.Transaction,
                cancellationToken: ct));

            // 2. Persist Atomic Outbox Event
            const string insertOutboxSql = """
                INSERT INTO outbox_messages (id, message_type, payload, status, created_at)
                VALUES (@Id, @MessageType, @Payload, 'Pending', @CreatedAt);
                """;

            var outboxMessage = new
            {
                Id = Guid.NewGuid().ToString(),
                MessageType = "OrderPlacedDomainEvent",
                Payload = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.TotalAmount }),
                CreatedAt = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
            };

            await _connection.ExecuteAsync(new CommandDefinition(
                insertOutboxSql,
                outboxMessage,
                transaction: uow.Transaction,
                cancellationToken: ct));

            // Commit atomic transaction
            await uow.CommitAsync(ct);
        }, cancellationToken);
    }
}
```

### Explanation
`BeginUnitOfWorkAsync` opens a transaction and returns an `IUnitOfWork` implementing `IAsyncDisposable`. If an exception occurs before `CommitAsync` is reached, `DisposeAsync` rolls back the transaction deterministically.

### Best Practices
- Always pass `uow.Transaction` to every Dapper command inside the scope.
- Wrap the entire Unit of Work inside the Polly pipeline (ADR-016).

### Common Errors
- *Error:* Calling Polly retry inside `BeginUnitOfWorkAsync` without savepoints, causing poisoned transaction states.

---

## Recipe 2: Multi-Entity Relational Aggregate Hydration (1:N Deduplication)

### Problem
Query an aggregate root with nested child collections via SQL `LEFT JOIN` without generating duplicate root instances or allocating manual dictionary boilerplate.

### Solution
Use `MultiMapBuilder<TReturn>` or Dapper multi-mapping with aggregate deduplication.

### Complete Code
```csharp
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

public static async Task<IEnumerable<Order>> GetOrdersWithLinesAsync(
    IDbConnection connection, 
    long customerId, 
    CancellationToken ct = default)
{
    const string sql = """
        SELECT 
            o.id AS Id, o.customer_id AS CustomerId, o.order_number AS OrderNumber,
            o.status AS Status, o.total_amount AS TotalAmount,
            i.id AS Id, i.order_id AS OrderId, i.product_id AS ProductId,
            i.product_name AS ProductName, i.quantity AS Quantity, i.unit_price AS UnitPrice
        FROM orders o
        LEFT JOIN order_items i ON o.id = i.order_id
        WHERE o.customer_id = @CustomerId;
        """;

    var lookup = new Dictionary<long, Order>();

    await connection.QueryAsync<Order, OrderItem, Order>(
        new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: ct),
        (order, item) =>
        {
            if (!lookup.TryGetValue(order.Id, out var existingOrder))
            {
                existingOrder = order;
                lookup.Add(existingOrder.Id, existingOrder);
            }

            if (item != null && item.Id > 0)
            {
                existingOrder.Items.Add(item);
            }

            return existingOrder;
        },
        splitOn: "Id");

    return lookup.Values;
}
```

### Explanation
`splitOn: "Id"` marks the column boundary in the result set where Dapper stops mapping the `Order` root and begins mapping the `OrderItem` child.

### Best Practices
- Ensure primary keys and join keys are indexed.
- Use `LEFT JOIN` when aggregates might have zero child items.

---

## Recipe 3: High-Throughput Bulk Ingestion in PostgreSQL (UNNEST)

### Problem
Ingest 50,000+ records into PostgreSQL in sub-second time without issuing 50,000 round-trips.

### Solution
Use `BulkParameters.From<T>()` and `BulkExtensions.BulkInsertAsync` with PostgreSQL `UNNEST`.

### Complete Code
```csharp
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.PostgreSql.Bulk;
using NpgsqlTypes;

public static async Task BulkIngestProductsAsync(
    DbConnection connection, 
    IReadOnlyList<Product> products)
{
    var parameters = BulkParameters.From(products)
        .Add("Ids",        p => p.Id,            NpgsqlDbType.Bigint)
        .Add("Skus",       p => p.Sku,           NpgsqlDbType.Text)
        .Add("Names",      p => p.Name,          NpgsqlDbType.Text)
        .Add("Prices",     p => p.Price,         NpgsqlDbType.Numeric)
        .Add("StockQtys",  p => p.StockQuantity, NpgsqlDbType.Integer)
        .Build();

    const string sql = """
        INSERT INTO products (id, sku, name, price, stock_quantity)
        SELECT * FROM UNNEST(@Ids, @Skus, @Names, @Prices, @StockQtys)
        ON CONFLICT (id) DO UPDATE
        SET name = EXCLUDED.name,
            price = EXCLUDED.price,
            stock_quantity = EXCLUDED.stock_quantity;
        """;

    await connection.BulkUpsertAsync(sql, parameters);
}
```

### Explanation
PostgreSQL `UNNEST` treats array parameters as temporary column vectors, executing the entire insertion as a single atomic query.

---

## Recipe 4: Streaming Binary Bulk Copy in SQL Server (SqlBulkCopy)

### Problem
Stream large datasets into SQL Server with maximum IO throughput and minimal memory overhead.

### Solution
Use `BulkDataTableBuilder.From<T>()` and `BulkExtensions.BulkInsertAsync`.

### Complete Code
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.SqlServer.Bulk;
using Microsoft.Data.SqlClient;

public static async Task BulkCopyCustomersAsync(
    SqlConnection connection, 
    IReadOnlyList<Customer> customers)
{
    var dataTable = BulkDataTableBuilder.From(customers)
        .Column("id",        c => c.Id)
        .Column("email",     c => c.Email)
        .Column("full_name", c => c.FullName)
        .Column("tier",      c => c.Tier.ToString())
        .Build();

    await connection.BulkInsertAsync(
        destinationTableName: "dbo.customers", 
        dataTable: dataTable, 
        batchSize: 5000, 
        timeoutSeconds: 60);
}
```

---

## Recipe 5: Multi-Row Parameterized Batching in SQLite, MySQL & MariaDB

### Problem
Perform batch inserts in SQLite, MySQL, or MariaDB without constructing SQL strings manually.

### Solution
Use `BulkBuilder.From<T>()` and `BulkExtensions.BulkInsertAsync`.

### Complete Code
```csharp
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Sqlite.Bulk;

public static async Task BatchInsertProductsAsync(
    IDbConnection connection, 
    IReadOnlyList<Product> products)
{
    var (sql, parameters) = BulkBuilder.From(products)
        .Table("products")
        .Column("sku", p => p.Sku)
        .Column("name", p => p.Name)
        .Column("price", p => p.Price)
        .Column("stock_quantity", p => p.StockQuantity)
        .Column("is_active", p => p.IsActive ? 1 : 0)
        .Column("release_date", p => p.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        .Build();

    await connection.BulkInsertAsync(sql, parameters);
}
```

---

## Recipe 6: Keyset (Cursor-Based) High-Volume Pagination

### Problem
Paginate multi-million row datasets where `OFFSET` causes high disk IO and linear query degradation.

### Solution
Use `QueryCursorPagedAsync` with `CursorPaginationParameters`.

### Complete Code
```csharp
using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Sqlite.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

public static async Task<ICursorPagedList<Product>> GetProductPageByCursorAsync(
    IDbConnection connection, 
    string? afterCursor = null, 
    int pageSize = 20)
{
    var parameters = new CursorPaginationParameters
    {
        First = pageSize,
        After = afterCursor
    };

    return await connection.QueryCursorPagedAsync<Product>(
        sql: "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products",
        cursorColumn: "id",
        parameters: parameters,
        cursorSelector: p => p.Id.ToString(CultureInfo.InvariantCulture));
}
```

---

## Recipe 7: Single Round-Trip Multi-Query Pagination with Total Count

### Problem
Retrieve paginated items and total item count in a single database round-trip.

### Solution
Use `QueryPagedMultipleAsync`.

### Complete Code
```csharp
using System.Data;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Sqlite.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

public static async Task<ICountedPagedList<Product>> GetPagedProductsSingleRoundTripAsync(
    IDbConnection connection, 
    int page = 1, 
    int pageSize = 10)
{
    var paginationParams = PaginationParameters.Create(page, pageSize);
    var offset = (page - 1) * pageSize;

    var multiSql = $"""
        SELECT id, sku, name, price, stock_quantity AS StockQuantity, is_active AS IsActive, release_date AS ReleaseDate 
        FROM products 
        WHERE is_active = 1 
        LIMIT {pageSize} OFFSET {offset};

        SELECT COUNT(*) FROM products WHERE is_active = 1;
        """;

    return await connection.QueryPagedMultipleAsync<Product>(
        sql: multiSql,
        pagination: paginationParams);
}
```

---

## Recipe 8: Partial Transaction Retries with Savepoints (ADR-014)

### Problem
Safely retry an unreliable tentative operation within an open transaction without aborting the parent transaction if it fails initially.

### Solution
Use `ExecuteInSavepointWithRetryAsync` on `IUnitOfWork`.

### Complete Code
```csharp
using System.Data;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.UnitOfWork;

public static async Task ProcessOrderWithTentativeAuditAsync(
    IDbConnection connection, 
    Order order)
{
    var pipeline = SqlResilienceDefaults.ForSqlite();

    await using var uow = await connection.BeginUnitOfWorkAsync(IsolationLevel.ReadCommitted);

    // 1. Mandatory core domain operation
    await connection.ExecuteAsync("INSERT INTO orders ...", order, uow.Transaction);

    // 2. Tentative sub-operation wrapped in a savepoint retry scope (ADR-014)
    await uow.ExecuteInSavepointWithRetryAsync(
        pipeline: pipeline,
        operation: async (innerUow, ct) =>
        {
            await connection.ExecuteAsync(
                "INSERT INTO tentative_audit_log (order_id) VALUES (@Id);",
                new { order.Id },
                transaction: innerUow.Transaction);
        },
        savepointName: "SP_AUDIT_LOG");

    await uow.CommitAsync();
}
```

---

## Recipe 9: Zero-Reflection Native AOT Entity Hydration

### Problem
Deploy high-throughput microservices compiled with Native AOT without missing reflection metadata warnings or runtime exceptions.

### Solution
Annotate domain entities with `[SqlEntity]` and consume generated `ReadFromDataReader`.

### Complete Code
```csharp
using System;
using System.Data;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions;

[SqlEntity(TableName = "customers")]
public sealed partial class Customer
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public static async Task<Customer?> ReadCustomerAotAsync(IDbConnection connection, long id)
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT id, email, full_name AS FullName FROM customers WHERE id = 1;";
    using var reader = await (command as System.Data.Common.DbCommand)!.ExecuteReaderAsync();

    if (reader.Read())
    {
        // Source-generated zero-reflection mapper:
        return Customer.ReadFromDataReader(reader);
    }
    return null;
}
```

---

## Recipe 10: OpenTelemetry Distributed Tracing & Execution Metrics

### Problem
Export distributed trace spans and duration histograms for all database queries to an OpenTelemetry collector.

### Solution
Configure `AddDapperOpenTelemetry` and use `QueryWithTelemetryAsync` / `ExecuteWithTelemetryAsync`.

### Complete Code
```csharp
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// In Program.cs:
builder.Services.AddDapperOpenTelemetry(opts =>
{
    opts.CaptureSqlStatements = true;
    opts.EnableMetrics = true;
    opts.MaxStatementLength = 2048;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(DapperDiagnostics.SourceName).AddOtlpExporter())
    .WithMetrics(m => m.AddMeter(DapperDiagnostics.SourceName).AddOtlpExporter());

// In repository / data access code:
var products = await connection.QueryWithTelemetryAsync<Product>(
    sql: "SELECT id, sku, name, price FROM products WHERE is_active = 1;");
```

---

## Recipe 11: Resilient Database Connectivity Health Checks for Kubernetes

### Problem
Configure health check probes in ASP.NET Core that detect connectivity issues and query degradation without leaking connections.

### Solution
Use `AddPostgreSqlDapperHealthCheck`, `AddSqlServerDapperHealthCheck`, etc.

### Complete Code
```csharp
using System;
using EricksonLopez.DapperExtensions.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// In Program.cs:
builder.Services.AddHealthChecks()
    .AddPostgreSqlDapperHealthCheck(
        name: "postgres-main",
        connectionFactory: async (sp, ct) => await ResolveOpenConnectionAsync(sp, ct),
        configure: options =>
        {
            options.CommandText = "SELECT 1;";
            options.DegradedThreshold = TimeSpan.FromMilliseconds(250);
            options.Timeout = TimeSpan.FromSeconds(3);
        },
        failureStatus: HealthStatus.Unhealthy);
```

---

## Recipe 12: Custom Value Object Type Handler & Error Detector

### Problem
Persist domain value objects (e.g. `Money`) cleanly and detect custom database error codes in cloud clusters.

### Solution
Implement `SqlMapper.TypeHandler<T>` and `ISqlTransientErrorDetector`.

### Complete Code
```csharp
using System;
using System.Data;
using System.Globalization;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;

public readonly record struct Money(decimal Amount, string Currency);

public sealed class MoneyTypeHandler : SqlMapper.TypeHandler<Money>
{
    public static readonly MoneyTypeHandler Default = new();

    public override void SetValue(IDbDataParameter parameter, Money value)
    {
        parameter.DbType = DbType.Decimal;
        parameter.Value = value.Amount;
    }

    public override Money Parse(object value)
    {
        var amount = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return new Money(amount, "USD");
    }
}

public sealed class CustomClusterDetector : ISqlTransientErrorDetector
{
    public bool IsTransient(Exception exception)
    {
        if (exception == null) return false;
        return exception.Message.Contains("cluster topology changing", StringComparison.OrdinalIgnoreCase);
    }
}

// In Startup:
SqlMapper.AddTypeHandler(MoneyTypeHandler.Default);
var pipeline = SqlResilienceDefaults.Standard(new CustomClusterDetector());
```

---

## Recipe 13: Choosing the Right `SqlResilienceExtensions` Query Overload

### Problem
`SqlResilienceExtensions` exposes 6 query overloads for resilient execution. Picking the wrong one causes either silent data loss (wrong cardinality assumption) or unexpected `InvalidOperationException` at runtime.

### Solution
Match the overload to the **cardinality guarantee** of your query.

### Cardinality Semantics Table

| Method | When to Use | Throws If |
|---|---|---|
| `QueryWithResilienceAsync<T>` | Any number of rows expected | Never |
| `QueryFirstOrDefaultWithResilienceAsync<T>` | 0 or 1 row; null is valid | Never |
| `QueryFirstWithResilienceAsync<T>` | At least 1 row guaranteed; you want the first | 0 rows |
| `QuerySingleOrDefaultWithResilienceAsync<T>` | 0 or 1 row; null is valid; > 1 is a bug | > 1 row |
| `QuerySingleWithResilienceAsync<T>` | Exactly 1 row guaranteed (e.g., PK lookup) | 0 rows, or > 1 row |
| `ExecuteScalarWithResilienceAsync<T>` | Aggregate or single-value query | Never |

### Complete Code

```csharp
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.SqlBuilder.Abstractions;

var pipeline = SqlResilienceDefaults.ForSqlite();

// ─── QueryWithResilienceAsync<T> ─────────────────────────────────────────────
// Use for: list queries with 0..N expected rows
var allProducts = await connection.QueryWithResilienceAsync<Product>(
    new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products;",
                  new Dictionary<string, object?>()),
    pipeline);

// ─── QueryFirstOrDefaultWithResilienceAsync<T> ───────────────────────────────
// Use for: optional lookups where no result is a valid business state
var maybeProduct = await connection.QueryFirstOrDefaultWithResilienceAsync<Product>(
    new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id;",
                  new Dictionary<string, object?> { ["id"] = 9999L }),
    pipeline);
// maybeProduct will be null if id=9999 doesn't exist — no exception

// ─── QueryFirstWithResilienceAsync<T> ────────────────────────────────────────
// Use for: "get the latest / cheapest / most recent" — at least 1 row is guaranteed
var mostRecentProduct = await connection.QueryFirstWithResilienceAsync<Product>(
    new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products ORDER BY id DESC;",
                  new Dictionary<string, object?>()),
    pipeline);
// Throws InvalidOperationException if the table is empty

// ─── QuerySingleOrDefaultWithResilienceAsync<T> ──────────────────────────────
// Use for: unique constraint lookups where the row may not exist yet
var existingUser = await connection.QuerySingleOrDefaultWithResilienceAsync<Product>(
    new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE sku = @sku;",
                  new Dictionary<string, object?> { ["sku"] = "NONEXISTENT-SKU" }),
    pipeline);
// Throws InvalidOperationException if query returns > 1 row (data integrity violation)

// ─── QuerySingleWithResilienceAsync<T> ───────────────────────────────────────
// Use for: PK lookups or unique index queries that MUST find exactly one row
var product = await connection.QuerySingleWithResilienceAsync<Product>(
    new SqlResult("SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id;",
                  new Dictionary<string, object?> { ["id"] = 1L }),
    pipeline);
// Throws InvalidOperationException if 0 or > 1 rows — treat as invariant violation

// ─── ExecuteScalarWithResilienceAsync<T> ─────────────────────────────────────
// Use for: COUNT, SUM, MAX, MIN, or any single-value aggregate
var totalProducts = await connection.ExecuteScalarWithResilienceAsync<int>(
    new SqlResult("SELECT COUNT(*) FROM products;",
                  new Dictionary<string, object?>()),
    pipeline);
```

### Explanation
Each overload maps directly to a Dapper cardinality method (`QueryAsync`, `QueryFirstOrDefaultAsync`, etc.) and delegates the actual execution to the provided `ResiliencePipeline`. The pipeline handles retry, timeout, and circuit breaking transparently.

### Best Practices
- Use `QuerySingleWithResilienceAsync` for PK-based lookups to fail fast on data integrity violations.
- Prefer `QueryFirstOrDefaultWithResilienceAsync` over `QuerySingleOrDefaultWithResilienceAsync` when you truly don't care about duplicate rows.
- Always pass a `CancellationToken` in long-running or HTTP-request-scoped operations.
- Never pass an open `uow.Transaction` to resilience extensions that wrap the full Unit of Work — do that at the `ExecuteWithResilienceAsync` level only (ADR-016).

### Common Errors
- **Error**: Using `QuerySingleWithResilienceAsync` on a query that can return > 1 row (e.g., without a `WHERE` clause narrowing to unique key). Causes `InvalidOperationException: Sequence contains more than one element`.
- **Error**: Using `QueryFirstOrDefaultWithResilienceAsync` when a missing row should be treated as an error. The `null` is silently swallowed, producing `NullReferenceException` later.

### When to Use
When executing queries through a Polly v8 resilience pipeline and the result cardinality matters (i.e., always, when operating on production relational databases).

### When NOT to Use
When you are inside an **open transaction** and the entire Unit of Work is already wrapped in the resilience pipeline at the outer scope (ADR-016). In that case, use plain Dapper `connection.QueryAsync / QuerySingleAsync` with `transaction: uow.Transaction`.

---
