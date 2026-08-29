# Best Practices & Architectural Guidelines — EricksonLopez.DapperExtensions

This document outlines mandatory design rules, resilience scoping mandates, performance guidelines, and common anti-patterns when using **EricksonLopez.DapperExtensions**.

---

## 1. Core Architectural Rules

### Rule 1: Scoping Resilience Pipelines (ADR-016 Mandate)
> [!IMPORTANT]
> **Always wrap the entire Unit of Work within the Polly resilience pipeline — never retry individual SQL commands inside an open transaction.**

#### ❌ Anti-Pattern: Retrying inside an active transaction
```csharp
// DANGEROUS: If cmd2 fails with a transient error and retries,
// the server transaction may already be in an aborted/poisoned state (e.g., PostgreSQL 25P02).
await using var uow = await connection.BeginUnitOfWorkAsync();
await pipeline.ExecuteAsync(async ct =>
{
    await connection.ExecuteAsync(sql1, p1, uow.Transaction);
    await connection.ExecuteAsync(sql2, p2, uow.Transaction); // May fail and poison transaction
});
```

#### ✅ Best Practice: Wrapping the complete Unit of Work
```csharp
// CORRECT: On transient failure, the entire Unit of Work is disposed/rolled back
// and recreated cleanly on the next attempt.
await pipeline.ExecuteAsync(async ct =>
{
    await connection.WithUnitOfWorkAsync(async (uow, token) =>
    {
        await connection.ExecuteAsync(sql1, p1, uow.Transaction);
        await connection.ExecuteAsync(sql2, p2, uow.Transaction);
    }, cancellationToken: ct);
});
```

---

### Rule 2: Handling Nested Retries via Savepoints (ADR-014 Mandate)
When executing complex transactional flows (e.g. Sagas, Outbox publishing) where only a subset of operations should retry without rolling back preceding work, use **Savepoint-Aware Retry**:

```csharp
await using var uow = await connection.BeginUnitOfWorkAsync();

// Step 1: Mandatory root operation
await connection.ExecuteAsync(insertOrderSql, orderParams, uow.Transaction);

// Step 2: Optional / transient-prone sub-operation isolated in a savepoint (ADR-014)
await uow.ExecuteInSavepointWithRetryAsync(
    pipeline: SqlResilienceDefaults.ForPostgreSql(),
    operation: async (unitOfWork, ct) =>
    {
        await connection.ExecuteAsync(reserveInventorySql, invParams, unitOfWork.Transaction);
    },
    savepointName: "SP_INVENTORY_RESERVATION");

await uow.CommitAsync();
```

---

### Rule 3: Native AOT & Zero-Reflection Hydration (ADR-006 & ADR-013)
When targeting Native AOT (`PublishAot=true`), avoid runtime reflection mapping:

1. **Annotate Models**: Use `[SqlEntity]` on domain models.
2. **Roslyn Generator**: Let `EricksonLopez.DapperExtensions.SourceGenerators` emit compile-time `IDataReaderMapper<T>` implementations.
3. **Avoid Dynamic Expressions**: Dynamic SQL building should use strongly-typed builders or compile-time string constants rather than runtime expression tree visitors ([REJECT-011](adr/reject-011-custom-expression-tree-interpreters-in-dapper.md)).

```csharp
[SqlEntity(TableName = "customers")]
public sealed partial class CustomerEntity
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly BirthDate { get; init; }
}
```

---

### Rule 4: High-Throughput Bulk Sizing & Parameter Limits
Choose the right bulk strategy based on the target relational engine:

| Database Engine | Recommended Strategy | Optimal Batch Size | Notes |
|---|---|:---:|---|
| **PostgreSQL** | `BulkParameters` (`UNNEST`) | 5,000 – 20,000 | Single round-trip passing native typed arrays. 10-30x faster than row-by-row. |
| **SQL Server** | `BulkDataTableBuilder` (`SqlBulkCopy`) | 10,000 – 50,000 | Streaming binary protocol with zero SQL parsing overhead. |
| **MySQL / MariaDB** | `BulkBuilder` (multi-row `VALUES`) | 1,000 – 2,500 | Keep total parameters below `max_allowed_packet` limit. |
| **Oracle** | `BulkBuilder` (`INSERT ALL`) | 500 – 1,000 | Bounded by Oracle's 1,000-row limit in `INSERT ALL`. |
| **SQLite** | `BulkBuilder` (multi-row `VALUES`) | 500 – 999 | Enforces the 999 / 32,766 SQLite parameter limit. |

---

### Rule 5: Consistent CancellationToken Propagation (ADR-004)
Always forward the `CancellationToken` into all extension methods to prevent database connection leaks and orphan queries on cancellation:

```csharp
public async Task<ICountedPagedList<ProductDto>> GetCatalogAsync(
    PaginationParameters pagination, 
    CancellationToken cancellationToken)
{
    return await connection.QueryPagedAsync<ProductDto>(
        sql: "SELECT id, name, price FROM products",
        countSql: "SELECT COUNT(*) FROM products",
        pagination: pagination,
        cancellationToken: cancellationToken);
}
```

---

## 2. Summary of Anti-Patterns to Avoid

| Anti-Pattern | Reason / Risk | Recommended Alternative |
|---|---|---|
| Retrying individual commands inside an active transaction | Causes transaction state poisoning (SQLSTATE 25P02 in PG) | Wrap `IUnitOfWork` or use `ExecuteInSavepointWithRetryAsync` |
| Using reflection in hot-path loops | Trimming warnings and Native AOT runtime crashes | Use `[SqlEntity]` and source-generated `IDataReaderMapper<T>` |
| Relying on `OFFSET` for millions of rows | $O(N)$ linear index degradation on high page numbers | Use `QueryCursorPagedAsync<T>` (Keyset pagination) |
| Hardcoding non-transient retry policies | Retrying syntax errors or constraint violations wastes resources | Use `ISqlTransientErrorDetector` and `SqlResilienceDefaults` |
| Manual transaction commit without try/catch rollback | Unhandled exceptions leave connections and locks dangling | Use `WithUnitOfWorkAsync` for deterministic async cleanup |
