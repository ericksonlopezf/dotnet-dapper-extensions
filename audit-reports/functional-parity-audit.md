# Functional Parity & Ecosystem Architecture Audit — EricksonLopez.DapperExtensions

> **Report Version:** 2.0.0 (Roadmap 100% Implemented & Validated)  
> **Ecosystem Version:** `EricksonLopez.DapperExtensions v2.0.0` (11 packages)  
> **Packages in Scope:** Core + 6 Database Dialect Providers + DependencyInjection + OpenTelemetry + HealthChecks + SourceGenerators  
> **Methodology:** Source inspection, unit testing, containerized integration testing, BenchmarkDotNet benchmarks, and Native AOT verification.

---

## 1. Executive Summary

**EricksonLopez.DapperExtensions** is a modular .NET library ecosystem built on top of Dapper. It extends Dapper's capabilities with **async transactional lifetimes** (Unit of Work + Savepoints with retry awareness), **dialect-native bulk operations** (UNNEST, SqlBulkCopy, multi-row VALUES), **standardized pagination** (Offset and Keyset/Cursor models), **transient fault resilience** (Polly v8), **distributed observability** (OpenTelemetry ActivitySource and Meter), **health checks**, and **Roslyn Incremental Source Generators** for Native AOT zero-reflection hydration.

### Key Audit Findings
- **Dialect Parity**: Complete feature parity across 6 relational database engines (**PostgreSQL**, **SQL Server**, **MySQL**, **MariaDB**, **Oracle**, and **SQLite**).
- **Architecture Invariants**: Zero-allocation hot paths, Native AOT trimming compliance enforced via analyzers, and strict dialect isolation.
- **Resilience Policy**: Full conformance to [ADR-016](../docs/adr/adr-016-resilience-pipeline-scope-wrap-unit-of-work.md) (wrapping Unit of Work rather than individual commands) and [ADR-014](../docs/adr/adr-014-savepoint-aware-resilience-retry.md) (savepoint-isolated retries).
- **Quality Gates**: 100% Mutation Score verified via Stryker.NET across all 11 production packages.

---

## 2. Analyzed Packages & Target Frameworks

| Package | Version | Status | Target Frameworks | Responsibilities |
|---|:---:|:---:|---|---|
| `EricksonLopez.DapperExtensions` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | Core abstractions, `IUnitOfWork`, `ISavepoint`, `MultiMapBuilder`, Polly v8 pipelines, TypeHandlers, Cursor models |
| `EricksonLopez.DapperExtensions.DependencyInjection` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | `IServiceCollection` extensions (`AddDapperExtensions`) |
| `EricksonLopez.DapperExtensions.HealthChecks` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | Relational database connectivity probes |
| `EricksonLopez.DapperExtensions.OpenTelemetry` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | OpenTelemetry tracing (`ActivitySource`) and runtime metrics (`Meter`) |
| `EricksonLopez.DapperExtensions.SourceGenerators` | 2.0.0 | Active | `netstandard2.0` | Roslyn Incremental Source Generator for Native AOT `IDataReaderMapper<T>` |
| `EricksonLopez.DapperExtensions.PostgreSql` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | UNNEST bulk insert/upsert/delete/update, JSONB handler, keyset & offset pagination |
| `EricksonLopez.DapperExtensions.SqlServer` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | `SqlBulkCopy` integration, batch operations, JSON handler, `OFFSET...FETCH` & keyset pagination |
| `EricksonLopez.DapperExtensions.MySql` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | Multi-row batching, JSON handler, `LIMIT...OFFSET` & keyset pagination |
| `EricksonLopez.DapperExtensions.MariaDb` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | Multi-row batching, JSON handler, `LIMIT...OFFSET` & keyset pagination |
| `EricksonLopez.DapperExtensions.Oracle` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | `INSERT ALL` bulk builder, JSON handler, `OFFSET...FETCH` & keyset pagination |
| `EricksonLopez.DapperExtensions.Sqlite` | 2.0.0 | Active | `net8.0;net9.0;net10.0` | Parameter-bounded batching, JSON handler, offset & keyset pagination |

---

## 3. Capability Audit Matrix

| Architectural Area | PostgreSQL | SQL Server | MySQL | MariaDB | Oracle | SQLite |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Bulk Insert** | ✅ `UNNEST` | ✅ `SqlBulkCopy` | ✅ `VALUES` Batch | ✅ `VALUES` Batch | ✅ `INSERT ALL` | ✅ `VALUES` Batch |
| **Bulk Upsert** | ✅ `ON CONFLICT` | ✅ `MERGE` | ✅ `ON DUPLICATE` | ✅ `ON DUPLICATE` | ✅ `MERGE` | ✅ `ON CONFLICT` |
| **Bulk Update** | ✅ `FROM UNNEST` | ✅ Batch Update | ✅ Batch Update | ✅ Batch Update | ✅ Batch Update | ✅ Batch Update |
| **Bulk Delete** | ✅ `ANY(@Ids)` | ✅ Parameterized | ✅ Parameterized | ✅ Parameterized | ✅ Parameterized | ✅ Parameterized |
| **Offset Pagination** | ✅ `LIMIT/OFFSET` | ✅ `OFFSET/FETCH` | ✅ `LIMIT/OFFSET` | ✅ `LIMIT/OFFSET` | ✅ `OFFSET/FETCH` | ✅ `LIMIT/OFFSET` |
| **Keyset Pagination** | ✅ Bi-directional | ✅ Bi-directional | ✅ Bi-directional | ✅ Bi-directional | ✅ Bi-directional | ✅ Bi-directional |
| **Savepoints** | ✅ `SAVEPOINT` | ✅ `SAVE TRANSACTION`| ✅ `SAVEPOINT` | ✅ `SAVEPOINT` | ✅ `SAVEPOINT` | ✅ `SAVEPOINT` |
| **Transient Error Detector** | ✅ SQLSTATE | ✅ Error Codes | ✅ Error Codes | ✅ Error Codes | ✅ ORA Codes | ✅ SQLite Error Codes |
| **JSON Type Handler** | ✅ `JsonbTypeHandler` | ✅ `JsonTypeHandler`| ✅ `JsonTypeHandler` | ✅ `JsonTypeHandler` | ✅ `JsonTypeHandler` | ✅ `JsonTypeHandler` |
| **Health Check Probe** | ✅ Native Probe | ✅ Native Probe | ✅ Native Probe | ✅ Native Probe | ✅ Native Probe | ✅ Native Probe |

---

## 4. Architectural Decisions & Quality Conclusions

1. **Dialect Segregation**: Each database provider is segregated into its own assembly to ensure zero transitive bloat (e.g., consumers targeting PostgreSQL do not reference Oracle or SQL Server drivers).
2. **Roslyn Source Generation**: Compile-time code generation guarantees zero runtime reflection overhead during data reader hydration, satisfying Native AOT requirements.
3. **Resilience Boundaries**: Scoped transactional retry eliminates transaction poisoning by enforcing clean savepoint rollbacks prior to retry attempts.
