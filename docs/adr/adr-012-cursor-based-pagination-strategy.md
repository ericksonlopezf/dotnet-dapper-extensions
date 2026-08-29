# ADR-012: Cursor-Based (Keyset) Pagination Strategy

## Status
Accepted

## Context
Standard offset pagination (`OFFSET n LIMIT m`) suffers from quadratic performance degradation on large tables ($O(n)$ scanning of bypassed rows). Keyset / cursor-based pagination uses indexed column comparisons (`column > @cursor`) to achieve constant $O(1)$ lookup performance regardless of page depth.

## Decision
1. Standardize on the canonical ecosystem contracts `ICursorPagedList<T>` and `CursorPaginationParameters` from `EricksonLopez.Pagination.Abstractions`, and `CursorPagedList<T>` from `EricksonLopez.Pagination`.
2. Implement `QueryCursorPagedAsync<T>` across all 6 dialect providers (PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite).
3. Strategy: Fetch `PageSize + 1` rows in a single round-trip to compute `HasNextPage` / `HasPreviousPage` without issuing a costly `COUNT(*)` query.

## Consequences
- Single, unified pagination model across the entire ecosystem without duplication.
- Constant time pagination performance on arbitrarily large database tables.
- Consistent API across all 6 relational engines.
