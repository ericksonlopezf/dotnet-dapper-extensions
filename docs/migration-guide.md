# Migration Guide — EricksonLopez.DapperExtensions

Step-by-step guide for migrating from **Vanilla Dapper**, **Entity Framework Core**, or **v1.x** to **EricksonLopez.DapperExtensions v2.0.0**.

---

## 1. Migrating from Vanilla Dapper

### Transaction Management

#### Before (Vanilla Dapper):
```csharp
using var transaction = connection.BeginTransaction();
try
{
    await connection.ExecuteAsync(sql1, p1, transaction);
    await connection.ExecuteAsync(sql2, p2, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

#### After (EricksonLopez.DapperExtensions):
```csharp
await connection.WithUnitOfWorkAsync(async (uow, ct) =>
{
    await connection.ExecuteAsync(sql1, p1, uow.Transaction);
    await connection.ExecuteAsync(sql2, p2, uow.Transaction);
}, cancellationToken: ct);
```

---

### Multi-Entity Joins & 1:N Grouping

#### Before (Vanilla Dapper Manual Dictionary):
```csharp
var lookup = new Dictionary<long, Order>();
await connection.QueryAsync<Order, OrderItem, Order>(sql, (order, item) =>
{
    if (!lookup.TryGetValue(order.Id, out var existing))
    {
        existing = order;
        lookup.Add(existing.Id, existing);
    }
    if (item != null) existing.Items.Add(item);
    return existing;
}, splitOn: "item_id");
var result = lookup.Values;
```

#### After (EricksonLopez.DapperExtensions):
```csharp
var result = await MultiMapBuilder<Order>
    .Query(orderQuery)
    .Map<OrderItem>("item_id", (order, item) =>
    {
        if (item != null) order.Items.Add(item);
        return order;
    })
    .QueryGroupedAsync(connection, compiler, o => o.Id, cancellationToken: ct);
```

---

## 2. Migrating from Entity Framework Core

| EF Core Concept | EricksonLopez.DapperExtensions Equivalent | Key Benefit |
|---|---|---|
| `DbContext.Database.BeginTransactionAsync` | `connection.BeginUnitOfWorkAsync()` | Async-first, zero tracking overhead |
| `EF.Functions.Like`, LINQ Queries | Raw SQL with compile-time constants | 100% control over execution plans |
| `DbContext.BulkInsert` (EF Extensions) | Dialect-native `BulkInsertAsync` (`UNNEST`, `SqlBulkCopy`) | Zero third-party licensing fees, native speed |
| `AsNoTracking()` Queries | Native Dapper queries | Zero entity change tracker memory overhead |
| `SaveChangesAsync()` | Explicit `uow.CommitAsync()` | Deterministic SQL execution timing |

---

## 3. Upgrading from v1.x to v2.0.0 (Release 2026-09-24)

### 3.1. Architectural & Multi-Targeting Features
- **Target Frameworks**: Multi-targeting expanded to `.NET 8.0`, `.NET 9.0`, and `.NET 10.0`.
- **Decoupled Pagination**: Provider packages depend directly on `EricksonLopez.Pagination.Abstractions`.
- **Standalone `ISavepoint`**: Savepoints now implement `ISavepoint` with explicit `RollbackAsync` and `ReleaseAsync` methods.
- **New Observability Packages**: Opt-in to `EricksonLopez.DapperExtensions.OpenTelemetry` and `EricksonLopez.DapperExtensions.HealthChecks` for distributed tracing and health probes.
- **Unbuffered Async Streaming**: `DapperStreamingExtensions.StreamAsync<T>` for memory-efficient `IAsyncEnumerable<T>` streaming.

### 3.2. Strict `DbTransaction` Enforcement in `CreateSavepointAsync`
- **What changed**: `IUnitOfWork.CreateSavepointAsync` throws `NotSupportedException` if the underlying transaction is not an ADO.NET `DbTransaction` (no more silent `NoOpSavepoint` fallback).
- **Migration**: Ensure custom test doubles inherit from `System.Data.Common.DbTransaction` (e.g., using `TestAdoTransaction` from `EricksonLopez.DapperExtensions.Testing.Common`).

### 3.3. PostgreSQL BulkExtensions `CancellationToken` Parameter
- **What changed**: All four public bulk extension methods in `EricksonLopez.DapperExtensions.PostgreSql.BulkExtensions` require a `CancellationToken`.
- **Migration**: Recompile consuming projects against v2.0.0 and update delegates to accept `CancellationToken`.

### 3.4. Keyset Cursor Column Name Regex Validation
- **What changed**: `QueryCursorPagedAsync<T>` enforces strict identifier validation (`^[a-zA-Z0-9_\[\]\""\.]+$`).
- **Migration**: Provide clean column identifiers or bracketed/quoted names; avoid SQL expressions or backticks.

### 3.5. Deterministic Connection Lifecycle
- **What changed**: Bulk methods that receive an initially closed connection now deterministically close it upon completion.
- **Migration**: Callers requiring the connection to stay open must open it explicitly prior to invoking bulk extensions (`await connection.OpenAsync()`).
