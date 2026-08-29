# NuGet Packages & Ecosystem Guide — EricksonLopez.DapperExtensions

A technical reference for all 11 published packages within the **EricksonLopez.DapperExtensions** ecosystem, including Target Frameworks, Central Package Management (CPM) dependencies, compatibility matrices, benchmarks, and sample projects.

---

## 1. Package Inventory & Capabilities

| Package Name | Package ID | Target Frameworks | Description |
|---|---|---|---|
| **Core** | `EricksonLopez.DapperExtensions` | `net8.0;net9.0;net10.0` | Core abstractions, `IUnitOfWork`, Savepoints, MultiMapBuilder, Polly v8 resilience pipelines, TypeHandlers, and Keyset/Cursor models. |
| **Dependency Injection** | `EricksonLopez.DapperExtensions.DependencyInjection` | `net8.0;net9.0;net10.0` | `IServiceCollection` extension methods (`AddDapperExtensions`) for ASP.NET Core and .NET Generic Host. |
| **Health Checks** | `EricksonLopez.DapperExtensions.HealthChecks` | `net8.0;net9.0;net10.0` | Database connectivity health check probes (`DapperHealthCheck`) for relational database engines. |
| **OpenTelemetry** | `EricksonLopez.DapperExtensions.OpenTelemetry` | `net8.0;net9.0;net10.0` | Distributed tracing (`ActivitySource`) and execution latency metrics (`Meter`) for Dapper operations. |
| **Source Generators** | `EricksonLopez.DapperExtensions.SourceGenerators` | `netstandard2.0` | Roslyn Incremental Generator for compile-time generation of zero-reflection `IDataReaderMapper<T>` implementations. |
| **PostgreSQL** | `EricksonLopez.DapperExtensions.PostgreSql` | `net8.0;net9.0;net10.0` | High-performance PostgreSQL extensions: UNNEST bulk insert/upsert/delete/update, JSONB handler, keyset/offset pagination. |
| **SQL Server** | `EricksonLopez.DapperExtensions.SqlServer` | `net8.0;net9.0;net10.0` | High-performance SQL Server extensions: `SqlBulkCopy` integration, JSON type handler, `OFFSET...FETCH` & keyset pagination. |
| **MySQL** | `EricksonLopez.DapperExtensions.MySql` | `net8.0;net9.0;net10.0` | High-performance MySQL extensions: multi-row batch insert/upsert/delete/update, JSON handler, keyset & offset pagination. |
| **MariaDB** | `EricksonLopez.DapperExtensions.MariaDb` | `net8.0;net9.0;net10.0` | High-performance MariaDB extensions: multi-row batch insert/upsert/delete/update, JSON handler, keyset & offset pagination. |
| **Oracle** | `EricksonLopez.DapperExtensions.Oracle` | `net8.0;net9.0;net10.0` | High-performance Oracle extensions: `INSERT ALL` bulk builder, JSON handler, `OFFSET...FETCH` & keyset pagination. |
| **SQLite** | `EricksonLopez.DapperExtensions.Sqlite` | `net8.0;net9.0;net10.0` | High-performance SQLite extensions: parameter-bounded batch insert/update/delete, JSON handler, keyset & offset pagination. |

---

## 2. Ecosystem Dependency Graph

```mermaid
graph TD
    Core["EricksonLopez.DapperExtensions<br/>(Core Library)"]
    DI["EricksonLopez.DapperExtensions.DependencyInjection"]
    HC["EricksonLopez.DapperExtensions.HealthChecks"]
    OTel["EricksonLopez.DapperExtensions.OpenTelemetry"]
    SG["EricksonLopez.DapperExtensions.SourceGenerators"]
    
    PG["EricksonLopez.DapperExtensions.PostgreSql"]
    MSSQL["EricksonLopez.DapperExtensions.SqlServer"]
    MySQL["EricksonLopez.DapperExtensions.MySql"]
    MariaDB["EricksonLopez.DapperExtensions.MariaDb"]
    Oracle["EricksonLopez.DapperExtensions.Oracle"]
    Sqlite["EricksonLopez.DapperExtensions.Sqlite"]

    DI --> Core
    HC --> Core
    OTel --> Core
    
    PG --> Core
    MSSQL --> Core
    MySQL --> Core
    MariaDB --> Core
    Oracle --> Core
    Sqlite --> Core
```

---

## 3. Central Package Management (CPM) Dependencies

Configured centrally in `Directory.Packages.props`:

| Dependency | Pinned Version | Consumed By |
|---|:---:|---|
| `Dapper` | `2.1.79` | Core, All Dialect Providers |
| `Microsoft.Extensions.Resilience` | `10.9.0` | Core |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.11` | DI, OpenTelemetry, HealthChecks |
| `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` | `10.0.11` | HealthChecks |
| `OpenTelemetry.Api` | `1.18.0` | OpenTelemetry |
| `Npgsql` | `10.0.3` | PostgreSql |
| `Microsoft.Data.SqlClient` | `7.0.2` | SqlServer |
| `MySqlConnector` | `2.6.2` | MySql, MariaDb |
| `Oracle.ManagedDataAccess.Core` | `23.26.300` | Oracle |
| `Microsoft.Data.Sqlite` | `10.0.11` | Sqlite |
| `Microsoft.CodeAnalysis.CSharp` | `4.13.0` | SourceGenerators |
| `Microsoft.CodeAnalysis.Analyzers` | `3.11.0` | SourceGenerators |
| `BenchmarkDotNet` | `0.15.8` | Benchmarks |
| `xunit` | `2.9.3` | Test Suites |
| `Testcontainers.*` | `4.14.0` | Integration Test Suites |
| `AwesomeAssertions` | `9.6.0` | Test Suites |
| `NSubstitute` | `6.2.0` | Unit Test Suites |
| `coverlet.collector` | `10.0.1` | Test Suites |

---

## 4. Cross-Repository (Sibling) Dependencies

The following packages are produced by upstream sibling repositories and consumed exclusively as **NuGet packages** (`PackageReference`) with Central Package Management:

| External Package | Source Repository | Consumers | Purpose |
|---|---|---|---|
| `EricksonLopez.Pagination.Abstractions` | `dotnet-pagination` | Core, PostgreSql, SqlServer, MySql, MariaDb, Oracle, Sqlite | `ICountedPagedList<T>`, `ICursorPagedList<T>`, `PaginationParameters`, `CursorParameters` contracts |
| `EricksonLopez.Pagination` | `dotnet-pagination` | PostgreSql, SqlServer, MySql, MariaDb, Oracle, Sqlite | Default `PagedList<T>` and `CursorPagedList<T>` implementations |
| `EricksonLopez.Resilience.Abstractions` | `dotnet-resilience` | Core | Resilience pipeline abstractions |
| `EricksonLopez.Resilience.Polly` | `dotnet-resilience` | Core | Polly v8 pipeline adapter implementation |
| `EricksonLopez.SqlBuilder.Abstractions` | `dotnet-sql-builder` | Core | SQL building abstraction contracts |

---

## 5. Framework Compatibility Matrix

| Package | .NET 8.0 (LTS) | .NET 9.0 (STS) | .NET 10.0 (Current) | Native AOT |
|---|:---:|:---:|:---:|:---:|
| `EricksonLopez.DapperExtensions` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.DependencyInjection` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.HealthChecks` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible |
| `EricksonLopez.DapperExtensions.OpenTelemetry` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible |
| `EricksonLopez.DapperExtensions.SourceGenerators` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Native (Analyzer Host) |
| `EricksonLopez.DapperExtensions.PostgreSql` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.SqlServer` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.MySql` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.MariaDb` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.Oracle` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |
| `EricksonLopez.DapperExtensions.Sqlite` | ✅ Supported | ✅ Supported | ✅ Supported | ✅ Compatible\* |

\* **Native AOT compatibility is conditional.** Full AOT safety requires decorating entities with `[SqlEntity]` and referencing `EricksonLopez.DapperExtensions.SourceGenerators`. Without source-generated `IDataReaderMapper<T>` parsers, `MultiMapBuilder<TReturn>` falls back to Dapper's reflection-based mapping, which is **not** Native AOT compatible. See ADR-006 for the full analysis.

---

## 6. Benchmarks Suite

The benchmark project is located in `benchmarks/EricksonLopez.DapperExtensions.PostgreSql.Benchmarks`.

### How to Run:
```bash
dotnet run --project benchmarks/EricksonLopez.DapperExtensions.PostgreSql.Benchmarks --configuration Release
```

Included benchmark suites:
- `BulkInsertBenchmarks`: Compares row-by-row `INSERT` vs PostgreSQL `UNNEST` array batching across 100, 1,000, and 10,000 entities.
- `JsonbBenchmarks`: Measures `JsonbTypeHandler<T>` serialization and deserialization latency.
- `PagedQueryBenchmarks`: Measures single round-trip `QueryPagedMultipleAsync` vs two-query `QueryPagedAsync`.
- `TransactionBenchmarks`: Evaluates overhead of `WithUnitOfWorkAsync` scoping vs raw ADO.NET transactions.

---

## 7. Official Executable Showcase Reference

The project `samples/EricksonLopez.DapperExtensions.Showcase` demonstrates progressive implementations across 11 levels:

| Level | Topic | Description |
|---|---|---|
| **Level 00** | Conceptual Overview | Foundational introduction to "Raw SQL, Managed Infrastructure". |
| **Level 01** | Quick Start Demo | Rapid setup, DI injection, and basic entity mapping. |
| **Level 02** | Full Configuration | Custom type handlers (`DateOnly`, `TimeOnly`, enums) and transient detectors. |
| **Level 03** | Real-World CRUD & Pagination | Keyset and offset pagination patterns with counted metadata. |
| **Level 04** | Unit of Work & Multi-Map | Async transactional lifetimes, nested savepoints, and 1:N deduplication. |
| **Level 05** | Bulk Processing | PostgreSQL `UNNEST`, SQL Server `SqlBulkCopy`, and multi-row batching. |
| **Level 06** | Error Handling & Resilience | Polly v8 pipelines, circuit breakers, and ADR-014 savepoint-aware retries. |
| **Level 07** | Scalability & Native AOT | Zero-reflection hydration using `[SqlEntity]` and source generators. |
| **Level 08** | Customization | Implementing custom `ISqlTransientErrorDetector` and specialized type handlers. |
| **Level 09** | Observability & Health | OpenTelemetry tracing/metrics and ASP.NET Core database health probes. |
| **Level 10** | Enterprise Architecture | Transactional Outbox pattern and distributed Saga compensation. |

### How to Run the Showcase:
```bash
dotnet run --project samples/EricksonLopez.DapperExtensions.Showcase
```
