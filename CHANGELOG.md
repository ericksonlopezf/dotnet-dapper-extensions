# Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) · Versioning: [SemVer](https://semver.org/)

## [Unreleased]

## [2.0.0] — 2026-08-29

### Breaking Changes
- **ISavepoint Standalone Contract**: Extracted `ISavepoint` from nested transaction implementations into a standalone interface contract with dedicated `RollbackAsync` and `ReleaseAsync` execution semantics.
  - **Migration**: Custom unit of work or transaction implementations must implement the standalone interface `EricksonLopez.DapperExtensions.UnitOfWork.ISavepoint`.

### Added
- **Ecosystem Resilience Integration (ADR-017)**: Added overloads in `SqlResilienceExtensions` and `SavepointResilienceExtensions` accepting `EricksonLopez.Resilience.IResiliencePipeline` to align with the core ecosystem resilience authority.
- **Ecosystem Resilience Pipeline Presets**: Added factory methods (`StandardPipeline`, `StandardWithCircuitBreakerPipeline`, `AggressivePipeline`, `ConservativePipeline`, `ForSqlServerPipeline`, `ForPostgreSqlPipeline`, `ForMySqlPipeline`, `ForSqlitePipeline`, `ForOraclePipeline`) in `SqlResilienceDefaults`.
- **`EricksonLopez.DapperExtensions.HealthChecks`**: Database health check probe (`DapperHealthCheck`) supporting custom probe queries, timeout constraints, failure status configuration, and injectable `TimeProvider` for deterministic testing.
- **`EricksonLopez.DapperExtensions.OpenTelemetry`**: Distributed observability package providing OpenTelemetry activity tracing (`ActivitySource`) and runtime execution meters (`Meter`) with automatic latency, status, and tag enrichment.
- **`EricksonLopez.DapperExtensions.SourceGenerators`**: Roslyn Incremental Generator (`SqlEntityGenerator`) for compile-time, reflection-free generation of `IDataReaderMapper<T>` implementations for `[SqlEntity]`-annotated models, guaranteeing Native AOT compatibility.
- **Cursor-Based Pagination**: `QueryCursorPagedAsync<T>` extension method added to all dialect providers (PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite) with bidirectional keyset pagination (`Before`/`After` cursors), composite filters, and dynamic limit handling.
- **Savepoint Isolation**: Extracted `ISavepoint` to a standalone interface contract with dedicated `RollbackAsync` and `ReleaseAsync` execution semantics.
- **Multi-Targeting**: Official compilation and testing matrices expanded to multi-target `.NET 8.0`, `.NET 9.0`, and `.NET 10.0`.
- **Automated Mutation Testing Gate**: Added CI verification script (`scripts/verify-mutation-gate.js`) and GitHub Actions integration enforcing the 100% mutation testing quality gate.
- **Exhaustive Testing & Mutation Hardening**: Comprehensive test suites (337 core unit tests, 79 PG, 87 SQL Server, 74 MySQL, 73 MariaDB, 72 Oracle, 81 SQLite, 18 HealthChecks, 14 OpenTelemetry, 22 SourceGen) achieving **100% Mutation Score** via Stryker.NET.
- **Official Executable Showcase** (`samples/EricksonLopez.DapperExtensions.Showcase`): Multi-level progressive learning and reference project covering Levels 00 to 10 (Conceptual, Quick Start, Full Configuration, Real-world Pagination/CRUD, Unit of Work & Multi-Map, Bulk Operations, Polly v8 Resilience & Savepoint-Aware Retry, Native AOT Zero-Reflection, Custom Extensibility, OpenTelemetry & Health Checks, Enterprise Outbox & Sagas).
- **Comprehensive Documentation Suite**: Full `/docs/` directory with quickstart, getting-started, API reference, architecture, best-practices, cookbook, performance-guide, troubleshooting, migration-guide, FAQ, CI/CD guide, and NuGet packages reference.
- **ADR-010**: OpenTelemetry Observability Package and Semantic Conventions.
- **ADR-011**: HealthChecks Package and Dialect Probe Architecture.
- **ADR-012**: Cursor-Based (Keyset) Pagination Strategy.
- **ADR-013**: Source Generator for Zero-Reflection Native AOT IDataReaderMapper.
- **ADR-014**: Savepoint-Aware Resilience Retry.
- **ADR-017**: Ecosystem Convergence, Resilience, and UoW Transaction Boundary.

### Changed
- **Strict Single-Type-Per-File Architecture**: Refactored `BulkParameters<T>`, `BulkBuilder<T>`, `BulkDataTableBuilder<T>`, and dialect `TypeHandlerRegistrar` classes into dedicated `.T.cs` and separate registrar files.
- **Zero Warnings as Errors**: Enforced `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` across all packable packages with full XML documentation on all public members.
- **Strict Clean Architecture**: Enforced single type per file across all production libraries.
- **Consistent Licensing**: Prepended standardized MIT License headers across all source files.

### Deprecated
- **Polly v8 Resilience Overloads**: All extension methods in `SqlResilienceExtensions` and `SavepointResilienceExtensions` accepting `Polly.ResiliencePipeline` directly are now marked `[Obsolete]`.
  - **Migration**: Update callers to use overloads accepting `EricksonLopez.Resilience.IResiliencePipeline` created via `SqlResilienceDefaults.*Pipeline()` or configured through dependency injection. The Polly overloads will be removed in v3.0.0.

## [1.2.0] — 2026-08-21

### Breaking Changes
- **Namespace Renaming (`PostgreSQL` → `PostgreSql`)**: Renamed namespace `EricksonLopez.DapperExtensions.PostgreSQL.*` to `EricksonLopez.DapperExtensions.PostgreSql.*` to conform to standard .NET framework naming conventions.
  - **Migration**: Replace `using EricksonLopez.DapperExtensions.PostgreSQL;` with `using EricksonLopez.DapperExtensions.PostgreSql;` across consuming files.
- **Assembly & Namespace Casing (`MariaDB` → `MariaDb`)**: Renamed package, assembly, and namespace `EricksonLopez.DapperExtensions.MariaDB` to `EricksonLopez.DapperExtensions.MariaDb`.
  - **Migration**: Update package references in `.csproj` to `EricksonLopez.DapperExtensions.MariaDb` and namespace imports to `using EricksonLopez.DapperExtensions.MariaDb;`.
- **Removal of Provider Marker Extension Methods**: Removed stub extension methods `CheckMySqlExtensions`, `CheckOracleExtensions`, `CheckSqlServerExtensions`, and `CheckSqliteExtensions`.
  - **Migration**: Remove calls to `connection.Check*Extensions()`. Use functional dialect extensions (`BulkExtensions`, `PagedQueryExtensions`, `TransactionExtensions`) directly.
- **Decoupled Pagination Dependency**: Replaced dependency on `EricksonLopez.Pagination` (full) with `EricksonLopez.Pagination.Abstractions` across all 6 provider dialect packages. Added zero-dependency `PagedList<T>` implementing `ICountedPagedList<T>`.
  - **Migration**: Projects consuming types from the full `EricksonLopez.Pagination` package must add an explicit direct package reference.
- **Pagination Guard Clauses**: `PagedQueryExtensions.QueryPagedAsync` and `QueryPagedMultipleAsync` now throw `ArgumentOutOfRangeException` when `Page < 1` or `PageSize < 1`, preventing silent SQL OFFSET/LIMIT runtime failures.
  - **Migration**: Ensure page number is $\ge 1$ and page size is $\ge 1$ before calling pagination extensions.
- **CancellationToken Propagation in Resilience Pipelines (ADR-004)**: All methods in `SqlResilienceExtensions` now construct Dapper `CommandDefinition` instances with `cancellationToken: ct` inside the delegate closure, ensuring database commands are aborted on cancellation.
  - **Migration**: Pass a valid `CancellationToken` to queries executed inside resilience pipelines to benefit from true cancellation.

### Added
- **New Package `EricksonLopez.DapperExtensions.DependencyInjection`**: Seamless registration of Dapper type handlers and transient error detector singletons into `IServiceCollection` for ASP.NET Core and .NET Generic Host via `AddDapperExtensions()`.
- **Circuit Breaker Resilience Support**: Added `StandardWithCircuitBreaker` and provider presets (`ForPostgreSqlWithCircuitBreaker`, `ForSqlServerWithCircuitBreaker`, `ForMySqlWithCircuitBreaker`, `ForSqliteWithCircuitBreaker`, `ForOracleWithCircuitBreaker`) in `SqlResilienceDefaults` for cascading failure mitigation.
- **Bulk Delete & Bulk Update (PostgreSQL)**: Native `BulkDeleteAsync` and `BulkUpdateAsync` on `DbConnection` using UNNEST for single-round-trip, array-based batch operations.
- **Bulk Delete & Bulk Update (SQL Server)**: `BulkDeleteAsync` and `BulkUpdateAsync` on `DbConnection` using parameterized SQL with cancellation token support.
- **Multi-Map 1:N Grouping & Root Deduplication**: `QueryGroupedAsync<TKey>` and `QueryGroupedFirstOrDefaultAsync<TKey>` in `MultiMapBuilder<TReturn>` for hydrating relational joins without root entity duplication (dictionary-based deduplication via key selector).
- **Standard Type Handlers**: `DateOnlyTypeHandler`, `TimeOnlyTypeHandler`, and `StringEnumTypeHandler<TEnum>` with centralized startup registration via `DapperTypeHandlerRegistrar.RegisterStandardHandlers()` and `RegisterStringEnumHandler<TEnum>()`.
- **ADR-004**: CancellationToken Propagation in Resilience Pipelines.
- **ADR-005**: Coexistence of Provider TransactionExtensions and Core UnitOfWork.
- **ADR-006**: Native AOT and Trimming Compliance Enforcement.
- **ADR-007**: Multi-Map Root Deduplication and 1-to-N Grouping.
- **ADR-008**: Standard Type Handlers and Dependency Injection Boundary.
- **ADR-009**: Multi-Provider Bulk Operation Strategy.
- **ADR-016**: Resilience Pipeline Scope — Wrap Unit of Work, Not Individual Commands.
- `PackageTags` and `PackageIcon` metadata across all NuGet packages.

### Changed
- **EnableTrimAnalyzer = true** (ADR-006): Trim analyzer is now enabled globally. Dynamic access sites in `MultiMapBuilder<TReturn>` and `SqlResilienceDefaults` are annotated with `[UnconditionalSuppressMessage]` with explicit architectural justification. JSON type handlers (`JsonTypeHandler<T>`, `JsonbTypeHandler<T>`) now correctly propagate `[RequiresUnreferencedCode]` to callers.
- **Active Coexistence of Transaction Extensions** (ADR-005): Maintained both `TransactionExtensions.ExecuteInTransactionAsync` and `UnitOfWorkExtensions.WithUnitOfWorkAsync` as first-class, fully active APIs without deprecation.
- **ADR README**: Fixed all absolute file paths to relative paths for correct GitHub rendering.

### Fixed
- **Removed dead code**: `FindPostgreSqlException` method in `PostgreSqlTransientErrorDetector` (unused reflection via `GetType().FullName`) removed. The detector now relies exclusively on `DbException.SqlState` (ADO.NET 5+) and message-based fallback.

## [1.1.2] — 2026-08-21

> Note: Merged into v1.2.0 release. No separate NuGet package was published for v1.1.2.

### Fixed
- **CancellationToken not propagated in `SqlResilienceExtensions`**: Token now flows to the underlying Dapper `CommandDefinition`, preventing orphan server-side query executions on cancellation.
- **Pagination without validation**: Added `ArgumentOutOfRangeException` guards for `Page < 1` and `PageSize < 1` in all `PagedQueryExtensions`.

## [1.0.0] — 2025-10-01

### Added
- `BulkParameters<T>` — fluent builder for PostgreSQL UNNEST array parameters
- `BulkExtensions.BulkInsertAsync` — single round-trip bulk INSERT via UNNEST
- `BulkExtensions.BulkUpsertAsync` — bulk INSERT ... ON CONFLICT DO UPDATE via UNNEST
- `PagedQueryExtensions.QueryPagedAsync` — paginated query with parallel count, returns `PagedList<T>`
- `PagedQueryExtensions.QueryPagedMultipleAsync` — paginated query in a single round-trip via QueryMultiple
- `TransactionExtensions.ExecuteInTransactionAsync` — void and T-returning overloads with auto commit/rollback
- `JsonbTypeHandler<T>` — Dapper type handler for JSONB columns using System.Text.Json
- `NpgsqlTypeHandlerRegistrar` — startup helper for registering JSONB handlers
- Unit tests (8) — BulkParameters builder, guard clauses, array extraction
- Integration tests (5) — BulkInsert 100 rows, BulkUpsert ON CONFLICT, paginated query, transaction commit/rollback
- BenchmarkDotNet benchmarks — row-by-row vs UNNEST at 100/1K/10K rows
- ADR-001: Multi-Provider Architecture and Dialect Isolation
- ADR-002: UNNEST bulk strategy decision with benchmark data
- ADR-003: Decoupled Pagination Abstractions and ICountedPagedList Contract
- GitHub Actions: ci.yml (unit + integration jobs) + publish.yml (NuGet on tag)

[Unreleased]: https://github.com/ericksonlopezf/dotnet-dapper-extensions/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ericksonlopezf/dotnet-dapper-extensions/compare/v1.2.0...v2.0.0
[1.2.0]: https://github.com/ericksonlopezf/dotnet-dapper-extensions/compare/v1.0.0...v1.2.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-dapper-extensions/releases/tag/v1.0.0

