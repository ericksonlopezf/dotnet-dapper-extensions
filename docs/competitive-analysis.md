# Competitive Analysis & Architecture Benchmarks

## 1. Executive Summary
This document outlines the comparative architectural analysis between `EricksonLopez.DapperExtensions` and prevailing .NET data access libraries, including vanilla Dapper, Dapper.Contrib, RepoDb, and Entity Framework Core.

---

## 2. Capability Matrix

| Feature | Vanilla Dapper | Dapper.Contrib | RepoDb | EF Core 8/9/10 | EricksonLopez.DapperExtensions |
|---|:---:|:---:|:---:|:---:|:---:|
| **Async Unit of Work & Savepoints** | ❌ Manual ADO.NET | ❌ No | ⚠️ Basic Transaction | ⚠️ Complex DbContext | ✅ **Native `IUnitOfWork` & `ISavepoint`** |
| **Savepoint-Isolated Retries (ADR-014)** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ **`ExecuteInSavepointWithRetryAsync`** |
| **PostgreSQL UNNEST Bulk Operations** | ❌ Manual SQL | ❌ Not supported | ⚠️ Partial | ❌ No | ✅ **Native Typed Array `UNNEST`** |
| **SQL Server Binary Bulk Copy** | ❌ Manual `SqlBulkCopy` | ❌ Not supported | ✅ `BulkInsert` | ⚠️ EF Bulk Extensions | ✅ **`BulkDataTableBuilder` + `SqlBulkCopy`** |
| **Oracle Multi-Row Batch Operations** | ❌ Manual ADO.NET | ❌ Not supported | ⚠️ Basic | ❌ No | ✅ **`BulkBuilder` (`INSERT ALL`)** |
| **MySQL / MariaDB / SQLite Batching** | ❌ Manual SQL | ❌ Not supported | ⚠️ Basic | ⚠️ Batching | ✅ **Parameter-Safe `BulkBuilder`** |
| **Polly v8 Resilience Integration (ADR-016)** | ❌ Manual | ❌ No | ❌ No | ⚠️ ExecutionStrategy | ✅ **Dialect-Aware Polly v8 Pipelines** |
| **1:N Root Entity Deduplication** | ⚠️ Manual Dictionary | ❌ No | ⚠️ Relational Map | ✅ Change Tracker (Heavy) | ✅ **Zero-Allocation `MultiMapBuilder`** |
| **Keyset / Cursor Pagination** | ❌ Manual SQL | ❌ No | ❌ No | ❌ No | ✅ **`QueryCursorPagedAsync<T>`** |
| **Native AOT & Trimming Support** | ⚠️ Partial | ❌ Reflection-bound | ⚠️ Trimming warnings | ⚠️ Conditional | ✅ **100% Smoke-Tested Native AOT** |
| **Roslyn Source Generator Hydration** | ❌ No | ❌ No | ❌ No | ⚠️ Compiled Models | ✅ **`SqlEntityGenerator` (`IDataReaderMapper`)** |
| **OpenTelemetry & Health Checks** | ❌ No | ❌ No | ❌ No | ⚠️ DiagnosticSource | ✅ **Dedicated OTel & HealthCheck Packages** |
| **Strong Name Assembly Signing (SNK)** | ⚠️ Unofficial | ⚠️ Varied | ❌ No | ✅ Signed | ✅ **Canonical Strong-Named Key** |
| **Stryker Mutation Tested ($\ge 95\%$)** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ **100% Package Matrix (Threshold: $\ge 95\%$)** |
| **Automated PR Benchmark Gate** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ **Active PR Benchmark Regression Gate** |

---

## 3. Allocation & Ingestion Latency Trade-offs

### PostgreSQL `UNNEST` vs Row-by-Row
Using PostgreSQL `UNNEST` enables single-round-trip batch streaming directly to the database engine wire protocol. By converting collections into typed arrays (`bigint[]`, `text[]`, `numeric[]`), client-side query parsing and execution planning are consolidated into a single operation, resulting in:
- **Up to 33.1x faster execution** for 10,000 entities.
- **96% lower GC allocations** (1.2 MB vs 31.8 MB).
- Complete isolation within standard ADO.NET transactions.

### SQL Server `SqlBulkCopy` vs Parameterized Batches
Streaming through `BulkDataTableBuilder` and `SqlBulkCopy` leverages TDS streaming binary bulk insert protocol directly into SQL Server tables, bypassing the T-SQL query parser and query compilation cache entirely for large ingestion volumes.

### Zero-Reflection Native AOT Hydration
Using `[SqlEntity]` with Roslyn Incremental Generators produces compile-time `IDataReaderMapper<T>` code, completely eliminating runtime reflection, `DynamicMethod` / IL emit compilation, and unboxing overhead during object graph mapping.
