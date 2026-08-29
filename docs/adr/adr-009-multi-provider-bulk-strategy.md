# ADR-009: Multi-Provider Bulk Operation Strategy

## Status
Accepted

## Context
`EricksonLopez.DapperExtensions` targets 6 relational database engines: PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, and SQLite. Each engine exposes fundamentally different mechanisms for performing high-throughput batch data operations. A single "universal" strategy cannot simultaneously optimize for performance, driver compatibility, parameter limits, and AOT safety across all providers.

The audit (2026-08-19) identified that MySQL and Oracle bulk operations are `PARTIAL PARITY` vs PostgreSQL UNNEST:
- Multi-row VALUES: Parameter count grows linearly with rows × columns, bounded by driver limits (`max_allowed_packet` in MySQL, parameter limit in Oracle).
- UNNEST (PostgreSQL): 1 array per column regardless of row count — no parameter explosion.

## Decision

Adopt a **dialect-native strategy per provider**, explicitly documented and not unified:

| Provider | Strategy | Mechanism | Scale Limit |
|---|---|---|---|
| **PostgreSQL** | UNNEST arrays | `NpgsqlParameter[]` with array-typed params + `SELECT * FROM UNNEST(...)` | Practically unlimited (server memory) |
| **SQL Server** | SqlBulkCopy | `SqlBulkCopy` bulk-load protocol; bypasses row locks | Batch-controlled; highly scalable |
| **MySQL** | Multi-row VALUES | Parameterized `INSERT INTO (...) VALUES (...),(...)` | Limited by `max_allowed_packet` (~4MB default) |
| **MariaDB** | Multi-row VALUES | Same as MySQL | Limited by `max_allowed_packet` |
| **Oracle** | INSERT ALL | `INSERT ALL INTO t VALUES (...) INTO t VALUES (...) SELECT 1 FROM DUAL` | ~1000 rows practical limit |
| **SQLite** | Batched VALUES (999 params/batch) | Automatic chunking respecting SQLite 999-parameter limit | Chunked automatically |

## Rationale

1. **PostgreSQL and SQL Server** are the primary targets for enterprise high-throughput scenarios. Their strategies (UNNEST, SqlBulkCopy) are production-grade and scale to millions of rows.
2. **MySQL/MariaDB/Oracle** receive multi-row VALUES because it is safe, driver-independent, and sufficient for moderate volumes (< 10K rows per call). Teams requiring higher throughput on these engines should use vendor tools (`LOAD DATA INFILE` for MySQL — intentionally excluded due to security and permission requirements per ADR; Oracle's Array Binding requires OCI driver specifics out of scope).
3. **SQLite** 999-parameter limit is a hard engine constraint. Automatic chunking is the only practical in-process solution without external tools.
4. **API consistency**: All providers expose `BulkInsertAsync`, `BulkUpsertAsync` (where supported), `BulkDeleteAsync`, and `BulkUpdateAsync` with the same signature contract, even though the underlying strategies differ.

## Consequences

- Consumers targeting MySQL/Oracle for large volumes (> 50K rows) must supplement with provider-native tooling or accept multi-batch strategies.
- The API surface is consistent across providers for DX; the internal implementation is provider-optimized.
- `PARTIAL PARITY` designation for MySQL/Oracle bulk is a deliberate, documented trade-off, not a defect.
- `LOAD DATA INFILE` (MySQL) and Oracle Array Binding remain intentionally outside scope per the audit's REJECT list (security, driver coupling).
