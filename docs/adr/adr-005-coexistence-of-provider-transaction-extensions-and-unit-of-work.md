# ADR-005: Coexistence of Provider TransactionExtensions and Core UnitOfWork

## Status
Accepted

## Context
Across all 6 database dialect provider packages (`PostgreSQL`, `SqlServer`, `MySql`, `MariaDB`, `Oracle`, `Sqlite`), `TransactionExtensions.ExecuteInTransactionAsync` methods provide lightweight transaction scoping directly on `DbConnection`.

Simultaneously, the core library `EricksonLopez.DapperExtensions` provides `IUnitOfWork` and `UnitOfWorkExtensions.WithUnitOfWorkAsync`, which delivers:
1. Support for `IDbConnection` without requiring concrete `DbConnection` casting.
2. Full savepoint capabilities via `ISavepoint` (`CreateSavepointAsync`, `RollbackAsync`, `ReleaseAsync`).
3. Automatic commit on success and deterministic rollback on exception via `IAsyncDisposable`.
4. Full isolation level customization.

## Decision
1. Maintain both APIs fully active and first-class without deprecation (`[Obsolete]`):
   - `TransactionExtensions.ExecuteInTransactionAsync`: For lightweight transactional scripts and direct `DbConnection` consumers.
   - `UnitOfWorkExtensions.WithUnitOfWorkAsync` / `BeginUnitOfWorkAsync`: For domain-driven design, Clean Architecture, and advanced nested savepoints.
2. Provide consistent error handling, cancellation token support, and automatic connection management across both APIs.

## Consequences
- 100% binary and source compatibility for existing consumers.
- No obsolete warnings during builds.
- Clear choice between lightweight transactions and full Unit of Work with Savepoints.
