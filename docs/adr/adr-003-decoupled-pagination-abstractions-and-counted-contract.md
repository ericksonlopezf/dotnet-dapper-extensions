# ADR-003: Decoupled Pagination Abstractions and ICountedPagedList Contract

## Status
Superseded by Ecosystem Unification on `EricksonLopez.Pagination`

## Context
`EricksonLopez.DapperExtensions` provides database-specific provider packages (`PostgreSQL`, `MySql`, `MariaDB`, `Oracle`, `Sqlite`, `SqlServer`) that include paginated query helpers (`QueryPagedAsync<T>`, `QueryPagedMultipleAsync<T>`, and `QueryCursorPagedAsync<T>`).

## Decision
1. All pagination across the ecosystem is standardized on the official `EricksonLopez.Pagination` and `EricksonLopez.Pagination.Abstractions` packages.
2. `EricksonLopez.DapperExtensions` directly references `EricksonLopez.Pagination` (via project directory during active development), eliminating any duplicated pagination models or local wrapper classes.
3. Pagination returns canonical `ICountedPagedList<T>` (for counted offset pagination) and `ICursorPagedList<T>` (for keyset pagination) materialized via `PagedList<T>` / `CursorPagedList<T>`.

## Architectural & Technical Rationale

1. **Framework Design Guidelines Compliance**:
   - Methods that execute a count query (`countSql` or second result set) guarantee that `TotalCount` is known. Returning `ICountedPagedList<T>` accurately communicates this guarantee through the type system (`long TotalCount` vs nullable `long? TotalCount` on `IPagedList<T>`).
2. **Clean Architecture / Ports & Adapters**:
   - The Application Layer can consume `ICountedPagedList<T>` directly from repositories without knowing whether Dapper, EF Core, or a memory store satisfied the query.
3. **Native AOT & Trimming**:
   - The lightweight implementation has zero reflection, zero runtime options configuration, and zero heap overhead beyond the items list itself.

## Consequences

### Positive
- Strict dependency hygiene: `DapperExtensions` provider packages no longer transitively pull Microsoft extensions or LINQ engines.
- Consumers program against the pure contract `ICountedPagedList<T>`.
- 100% Native AOT trimming safe.

### Negative
- None.

## References
- Ecosystem Architectural Audit: `VIO-02`
- Clean Architecture Dependency Rule: Frameworks & Drivers $\rightarrow$ Interface Adapters $\rightarrow$ Application $\rightarrow$ Domain
