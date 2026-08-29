# Level 00: Conceptual Architecture & Mental Model

## 1. Overview & Philosophy
`EricksonLopez.DapperExtensions` is designed around the architectural principle:
> **"Raw SQL, Managed Infrastructure"**

While Dapper provides unmatched developer control over SQL queries and row materialization, enterprise production workloads demand managed infrastructure:
1. **Asynchronous Transaction Boundaries**: Complete lifecycle management via `IUnitOfWork` and `IAsyncDisposable`.
2. **Resilience Scoping (ADR-016)**: Polly v8 pipelines that wrap entire transactional units, preventing corrupted state from inner statement retries.
3. **Dialect-Native Bulk Operations**: High-throughput ingestion using PostgreSQL `UNNEST`, SQL Server `SqlBulkCopy`, and parameterized multi-row batching.
4. **1:N Relational Mapping & Deduplication**: Fluent `MultiMapBuilder<TReturn>` eliminating boilerplate dictionaries.
5. **Zero-Reflection Native AOT Compliance**: Source-generated `IDataReaderMapper<T>` via `[SqlEntity]`.
6. **Built-in Observability & Probes**: OpenTelemetry `ActivitySource`/`Meter` and ASP.NET Core `IHealthCheck` probes.

---

## 2. Architecture Diagram

```mermaid
graph TD
    App[Application Layer] --> Res[Polly v8 Resilience Pipeline]
    Res --> UoW[IUnitOfWork Transaction Boundary]
    UoW --> Extensions[EricksonLopez.DapperExtensions Core]
    Extensions --> Dialects{Target Dialect}
    Dialects -->|PostgreSQL| Pg[UNNEST Array Bulk & JSONB]
    Dialects -->|SQL Server| Ms[SqlBulkCopy & JSON]
    Dialects -->|MySQL / MariaDB| My[Batch VALUES & JSON]
    Dialects -->|Oracle| Ora[BulkBuilder & JSON]
    Dialects -->|SQLite| Lite[In-Memory / File Batching]
    Extensions --> Telemetry[OpenTelemetry & HealthChecks]
    Extensions --> AOT[Roslyn Source Generator [SqlEntity]]
```

---

## 3. High-Level Comparison

| Capability | Vanilla Dapper | Dapper.Contrib / Extensions | Entity Framework Core | EricksonLopez.DapperExtensions |
|---|---|---|---|---|
| **SQL Transparency** | 100% Raw SQL | Partial / Magic Methods | Abstraction (LINQ) | **100% Raw SQL** |
| **Runtime Overhead** | Minimal | Minimal | Moderate / High | **Zero-Allocation Buffers** |
| **Native AOT Compliance** | ⚠️ Partial (Reflection Emit) | ❌ No | ⚠️ Complex / Partial | ✅ **100% Native AOT ([SqlEntity])** |
| **Unit of Work & Savepoints** | Manual ADO.NET boilerplate | ❌ No | Implicit (`DbContext`) | ✅ **First-Class `IUnitOfWork` & `ISavepoint`** |
| **Polly v8 Resilience** | Manual integration | ❌ No | Partial (Execution Strategies) | ✅ **Built-in Dialect Detectors + ADR-016** |
| **High-Throughput Bulk** | O(N) iterative loop | ❌ No | Third-Party extensions | ✅ **PostgreSQL UNNEST / SqlBulkCopy** |
| **1:N Aggregate Deduplication**| Manual Dictionary code | ❌ No | Automatic (`Include`) | ✅ **Fluent `MultiMapBuilder<TReturn>`** |
| **Observability & Probes** | Manual | ❌ No | EF Interceptors | ✅ **Native OpenTelemetry & HealthChecks** |

---

## 4. Source Code Reference
- Executable Showcase: [`samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level00_Conceptual/ConceptualOverview.cs`](../../samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level00_Conceptual/ConceptualOverview.cs)
