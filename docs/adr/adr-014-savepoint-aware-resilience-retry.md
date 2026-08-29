# ADR-014: Savepoint-Aware Resilience Retry

## Status
Accepted

## Context
In standard relational databases (especially PostgreSQL), executing a statement that fails inside an open transaction aborts the entire transaction block (`current transaction is aborted, commands ignored until end of transaction block`). Retrying statements directly inside an active transaction without rolling back intermediate failed commands leaves the transaction corrupted.

## Decision
1. Introduce `ExecuteInSavepointWithRetryAsync` extension methods on `IUnitOfWork`.
2. Encapsulate retry cycles within nested savepoints:
   - Create savepoint before invocation.
   - Upon encountering transient exceptions caught by Polly, roll back to the savepoint before yielding to the resilience pipeline for retry.
   - Upon successful execution, release or retain the savepoint for eventual commit.

## Consequences
- Safe, deterministic retries of partial units of work within broader domain transactions.
- Zero risk of unrecoverable aborted transaction states on PostgreSQL and SQL Server.
