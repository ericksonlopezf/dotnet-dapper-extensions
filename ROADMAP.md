# Ecosystem Roadmap — EricksonLopez.DapperExtensions

## 1. Overview & Vision

**EricksonLopez.DapperExtensions** is a high-performance, Native AOT-ready infrastructure suite for **Dapper** across modern relational databases (**PostgreSQL**, **SQL Server**, **MySQL**, **MariaDB**, **Oracle**, and **SQLite**).

The roadmap tracks implemented milestones, quality verification metrics, and ongoing long-term goals.

---

## 2. Framework Quality Metrics & Acceptance Gates

The framework enforces strict engineering standards across all 11 packages:

| Metric | Target | Current Status | Acceptance Criteria |
|---|:---:|:---:|:---:|
| **Line Coverage** | $\ge 100\%$ | ✅ 100% | Verified via Coverlet / Codecov |
| **Branch Coverage** | $\ge 90\%$ | ✅ Compliant | Zero untested execution branches in core logic |
| **Method Coverage** | $100\%$ | ✅ 100% | Full API signature invocation |
| **Mutation Score (Stryker)** | $\ge 95\%$ (Break) | ✅ $100\%$ High | Break gate $\ge 95\%$, Low $\ge 98\%$, High $100\%$ |
| **Warnings as Errors** | Zero Warnings | ✅ Compliant | `TreatWarningsAsErrors=true`, `WarningLevel=5` |
| **Native AOT Compliance** | Zero Warnings | ✅ Compliant | `EnableTrimAnalyzer=true`, Roslyn Source Generators |

---

## 3. Milestones & Delivery Status

### Phase 1: Core Foundation & PostgreSQL Provider (Completed — v1.0.0)
- [x] High-throughput PostgreSQL `UNNEST` bulk operations (`BulkInsertAsync`, `BulkUpsertAsync`).
- [x] Offset-based paginated queries returning `ICountedPagedList<T>`.
- [x] Scoped transaction execution helper (`ExecuteInTransactionAsync`).
- [x] JSONB Type Handler using `System.Text.Json`.
- [x] Integration testing with Testcontainers.PostgreSql and BenchmarkDotNet benchmarks.

### Phase 2: Multi-Provider Dialect Parity & Ecosystem Expansion (Completed — v1.2.0)
- [x] `EricksonLopez.DapperExtensions.DependencyInjection` with `AddDapperExtensions` for `IServiceCollection`.
- [x] Polly v8 resilience pipelines with SQLSTATE / ORA transient error detectors and circuit breakers.
- [x] Multi-Map fluent builder (`MultiMapBuilder<T>`) with 1:N root entity deduplication.
- [x] Standard type handlers (`DateOnlyTypeHandler`, `TimeOnlyTypeHandler`, `StringEnumTypeHandler<T>`).
- [x] Dialect isolation across SQL Server, MySQL, MariaDB, Oracle, and SQLite.
- [x] Global trimming analysis enabled (`EnableTrimAnalyzer=true`, ADR-006).
- [x] True end-to-end `CancellationToken` flow in resilience pipelines (ADR-004).

### Phase 3: Enterprise Diagnostics, Health Probes & Native AOT (Completed — v2.0.0)
- [x] `EricksonLopez.DapperExtensions.HealthChecks` dialect health probes (`DapperHealthCheck`).
- [x] `EricksonLopez.DapperExtensions.OpenTelemetry` instrumentation (`ActivitySource` & `Meter`).
- [x] `EricksonLopez.DapperExtensions.SourceGenerators` Roslyn incremental generator for zero-reflection `IDataReaderMapper<T>`.
- [x] Universal keyset/cursor pagination (`QueryCursorPagedAsync<T>`).
- [x] Standalone `ISavepoint` contract with savepoint-aware retry policy (ADR-014).
- [x] Multi-targeting across `.NET 8.0`, `.NET 9.0`, and `.NET 10.0`.
- [x] Consolidated Stryker mutation testing quality gate with GitHub commit status.
- [x] Multi-level executable showcase project (`samples/EricksonLopez.DapperExtensions.Showcase`, Levels 00-10).

---

## 4. Future Invariants & Non-Goals

To preserve the foundational principles of the library, the following items are explicitly out of scope:

1. **No Full ORM / Change Tracker**: The library will never implement an in-memory change tracker or entity graph state manager (Dapper and raw SQL remain the single source of truth).
2. **No Custom Dynamic LINQ Interpreters**: Rejected under [REJECT-011](docs/adr/reject-011-custom-expression-tree-interpreters-in-dapper.md) to preserve deterministic SQL predictability and Native AOT zero-reflection guarantees.
3. **No Heavy Reflection in Hot Paths**: All object mapping and execution pipelines must remain Native AOT compliant.
