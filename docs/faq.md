# Frequently Asked Questions (FAQ) — EricksonLopez.DapperExtensions

Technical answers and architectural rationale for commonly asked questions about **EricksonLopez.DapperExtensions**.

---

## 1. General & Architecture

### Q: Why not just use Entity Framework Core?
**A:** EF Core is a full ORM with in-memory change tracking, expression tree translation, and identity mapping. While convenient for simple CRUD, high-throughput microservices, telemetry ingestion, and financial transaction systems frequently suffer from:
1. Overhead from entity tracking and object instantiation.
2. Inefficient or non-deterministic SQL generation for complex joins or batch updates.
3. Lack of first-class Native AOT support without complex reflection configurations.

**EricksonLopez.DapperExtensions** follows the **"Raw SQL, Managed Infrastructure"** philosophy: you write deterministic raw SQL, while the library manages transactions, transient retries, savepoints, bulk streaming, and Native AOT zero-reflection hydration.

---

### Q: Why are dynamic LINQ expression trees rejected (REJECT-011)?
**A:** As formalized in [REJECT-011](adr/reject-011-custom-expression-tree-interpreters-in-dapper.md), full dynamic LINQ-to-SQL interpreters introduce heavy runtime reflection, high memory allocation, and unpredictable SQL execution plans that conflict with high-performance micro-ORMs and Native AOT compilation.

---

## 2. Transactions & Resilience

### Q: Why should I wrap the Unit of Work instead of individual commands (ADR-016)?
**A:** Retrying a single failing SQL command inside an active transaction is dangerous. In relational engines like PostgreSQL, any statement error aborts the server-side transaction (SQLSTATE `25P02`). Retrying the command will fail immediately because the server transaction is in an unrecoverable state. Wrapping the *entire* Unit of Work ensures that on transient failure, the entire transaction is rolled back and re-executed cleanly from the beginning.

---

### Q: When should I use Savepoints instead of a root transaction retry (ADR-014)?
**A:** Use Savepoints when an outer transaction must remain intact while an inner, non-critical or retryable operation executes (e.g. reserving a secondary resource or invoking an external microservice with database journaling). [ADR-014](adr/adr-014-savepoint-aware-resilience-retry.md) provides `ExecuteInSavepointWithRetryAsync` to isolate and retry inner operations without poisoning the outer transaction.

---

## 3. Native AOT & Tooling

### Q: How does Native AOT support work?
**A:** The library enables the .NET Trim Analyzer (`EnableTrimAnalyzer=true`) across all assemblies. In addition, `EricksonLopez.DapperExtensions.SourceGenerators` analyzes classes decorated with `[SqlEntity]` and generates compile-time `IDataReaderMapper<T>` code, completely bypassing reflection.

---

### Q: What .NET versions are supported?
**A:** All production packages multi-target `.NET 8.0`, `.NET 9.0`, and `.NET 10.0` (with `EricksonLopez.DapperExtensions.SourceGenerators` targeting `netstandard2.0` to support Roslyn analyzer hosts).
