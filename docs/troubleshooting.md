# Troubleshooting Guide — EricksonLopez.DapperExtensions

Common diagnostic scenarios, error codes, and practical remedies when working with **EricksonLopez.DapperExtensions**.

---

## 1. Relational Error Codes & Diagnostics

### PostgreSQL: `25P02: current transaction is aborted, commands ignored until end of transaction block`
- **Cause:** An error occurred in a previous statement within the current transaction block, causing PostgreSQL to mark the entire transaction as aborted. Subsequent commands fail immediately with `25P02`.
- **Remedy (ADR-016):** Do not retry commands inside an open transaction. Wrap the entire Unit of Work within the Polly resilience pipeline, or use [ADR-014](adr/adr-014-savepoint-aware-resilience-retry.md) savepoint isolation:
```csharp
// Use savepoint-isolated retry to prevent 25P02 poisoning
await uow.ExecuteInSavepointWithRetryAsync(
    pipeline: pipeline,
    operation: async (u, ct) =>
    {
        await connection.ExecuteAsync(sql, param, u.Transaction);
    },
    savepointName: "SP_STEP");
```

---

### SQL Server: `Error 1205: Transaction was deadlocked on lock resources with another process and has been chosen as the deadlock victim`
- **Cause:** High-concurrency conflict where two transactions hold resources needed by each other.
- **Remedy:** Use `SqlServerTransientErrorDetector` with `SqlResilienceDefaults.ForSqlServer()` to automatically catch Error 1205 and retry the entire transaction with exponential jitter delay:
```csharp
var pipeline = SqlResilienceDefaults.ForSqlServer();
await pipeline.ExecuteAsync(async ct =>
{
    await connection.WithUnitOfWorkAsync(async (uow, token) =>
    {
        await connection.ExecuteAsync(sql, param, uow.Transaction);
    }, cancellationToken: ct);
});
```

---

### SQLite: `SQLite Error 5: 'database is locked'` or `SQLite Error 6: 'database table is locked'`
- **Cause:** SQLite does not support concurrent write operations across multiple threads or connection handles in default journaling modes.
- **Remedy:** Ensure `SqliteTransientErrorDetector` is registered and WAL (Write-Ahead Logging) mode is enabled on the database (`PRAGMA journal_mode = WAL;`).

---

## 2. Native AOT & Trimming Diagnostics

### Warning: `IL2026: Using member which has 'RequiresUnreferencedCodeAttribute'`
- **Cause:** Direct dynamic deserialization via reflection inside custom type handlers or un-annotated entity models.
- **Remedy:**
  1. Add `[SqlEntity]` to your domain entity classes to enable compile-time `IDataReaderMapper<T>` code generation via `EricksonLopez.DapperExtensions.SourceGenerators`.
  2. For JSON type handlers (`JsonTypeHandler<T>`, `JsonbTypeHandler<T>`), supply a `JsonSerializerContext` from `System.Text.Json.Serialization` to enable compile-time JSON serialization without reflection.

---

## 3. Configuration & Startup Issues

### Missing `DateOnly` / `TimeOnly` Type Handlers
- **Symptom:** `InvalidCastException` or Dapper exception when reading `DateOnly` from `date` columns.
- **Remedy:** Ensure `AddDapperExtensions()` is invoked in `Program.cs` or call `DapperTypeHandlerRegistrar.RegisterStandardHandlers()` directly at application startup.
