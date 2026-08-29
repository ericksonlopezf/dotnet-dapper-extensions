# ADR-002: PostgreSQL UNNEST Bulk Operation Strategy

## Status
Accepted

## Context
High-throughput batch inserting and upserting in PostgreSQL often suffers from row-by-row overhead or complex `COPY` stream setup that is hard to combine with transactions and Dapper parameters.

## Decision
Use PostgreSQL's `UNNEST` function with strongly-typed arrays (`BulkParameters<T>`) as the primary bulk insert and upsert mechanism (`BulkInsertAsync` / `BulkUpsertAsync`).

## Rationale
- Executes as a single round-trip query.
- Operates within standard `IDbTransaction` and Dapper connections.
- Benchmarks show orders-of-magnitude throughput improvement over row-by-row execution while remaining fully Native AOT safe.

## Consequences
- Predictable execution plans and minimal round-trip latency.
