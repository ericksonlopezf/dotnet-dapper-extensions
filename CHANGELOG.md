# Changelog

All notable changes to this project will be documented in this file.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) · Versioning: [SemVer](https://semver.org/)

## [Unreleased]

## [2.0.0] - 2026-09-24

### Breaking Changes
- **`IUnitOfWork.CreateSavepointAsync` Strict `DbTransaction` Enforcement**: Removed the silent fallback `NoOpSavepoint`. When the underlying transaction does not inherit from `System.Data.Common.DbTransaction` (such as in-memory test doubles or custom mocks implementing raw `IDbTransaction`), `CreateSavepointAsync` now throws a `NotSupportedException` instead of returning a no-op savepoint.
  - **Impact**: Any custom test double or non-ADO.NET provider implementing `IDbTransaction` directly will fail at runtime with `NotSupportedException`.
  - **Migration**: Custom test doubles and unit tests executing savepoint logic must inherit from `System.Data.Common.DbTransaction` (e.g., using `TestAdoTransaction` from `EricksonLopez.DapperExtensions.Testing.Common` or an in-memory SQLite transaction via `Microsoft.Data.Sqlite`).
- **PostgreSQL `BulkExtensions` Method Signatures (Binary Incompatibility)**: Added `CancellationToken cancellationToken = default` as a parameter to all four public bulk extension methods in `EricksonLopez.DapperExtensions.PostgreSql.BulkExtensions` (`BulkInsertAsync`, `BulkUpsertAsync`, `BulkDeleteAsync`, `BulkUpdateAsync`).
  - **Impact**: Precompiled binary consumers referencing prior signatures without recompilation will crash at runtime with `MissingMethodException`. Source code utilizing method group conversions or delegates with the 5-parameter signature will fail to compile.
  - **Migration**: Recompile consuming projects against the updated package. Update any delegate signatures (`Func<DbConnection, string, NpgsqlParameter[], DbTransaction?, int?, Task<int>>`) to include the 6th `CancellationToken` parameter.
- **Strict Keyset Cursor Identifier Regex Validation**: `QueryCursorPagedAsync<T>` across all 6 provider dialect packages (`PostgreSql`, `SqlServer`, `MySql`, `MariaDb`, `Oracle`, `Sqlite`) now enforces strict column name validation against `^[a-zA-Z0-9_\[\]\""\.]+$`. If `cursorColumn` contains characters outside this pattern, an `ArgumentException` is thrown.
  - **Impact**: Column expressions utilizing database backticks (such as MySQL/MariaDB `` `id` ``), calculated functions (e.g., `COALESCE(...)`), table aliases with spaces, or SQL expressions will throw `ArgumentException: "Invalid cursor column name."` at runtime.
  - **Migration**: Pass clean identifiers, double-quoted identifiers (`"id"`), or bracketed identifiers (`[id]`). Avoid backticks or complex SQL expressions in `cursorColumn`.
- **Deterministic Connection Lifecycle Management in PostgreSQL and SQL Server Bulk Operations**: `BulkInsertAsync`, `BulkUpsertAsync`, `BulkDeleteAsync`, and `BulkUpdateAsync` in PostgreSQL and `BulkInsertAsync`, `BulkDeleteAsync`, and `BulkUpdateAsync` in SQL Server now detect whether the connection was closed upon invocation. If closed, the connection is deterministically closed in a `finally` block or via Dapper internal scoping upon completion, rather than remaining open.
  - **Impact**: Consuming code that passed a closed connection and subsequently executed dependent commands assuming the connection was left in an `Open` state will now encounter an `InvalidOperationException: "The Connection's current state is closed"`.
  - **Migration**: Callers that require the database connection to remain open across multiple sequential operations must open the connection explicitly before invoking bulk extensions (`await connection.OpenAsync()`).
- **ISavepoint Standalone Contract**: Extracted `ISavepoint` from nested transaction implementations into a standalone interface contract with dedicated `RollbackAsync` and `ReleaseAsync` execution semantics.
  - **Migration**: Custom unit of work or transaction implementations must implement the standalone interface `EricksonLopez.DapperExtensions.UnitOfWork.ISavepoint`.

### Added
- **Unbuffered Async Streaming (ADR-019)**: Added `DapperStreamingExtensions.StreamAsync<T>` in `EricksonLopez.DapperExtensions.Streaming` providing unbuffered `IAsyncEnumerable<T>` streaming with $O(1)$ memory profile for large result sets.
- **Showcase Level 11**: Added `Level 11: Comprehensive Public API Coverage` (`Level11_ComprehensiveApiCoverageDemo.cs`) with companion documentation (`docs/showcase/level-11-comprehensive-api-coverage.md`) covering all 14 resilience pipelines, 6 dialect registrars, streaming, and grouped multi-map.
- **ADR-018**: Ecosystem Demarcation — EricksonLopez.DapperExtensions vs EricksonLopez.SqlBuilder Boundary.
- **ADR-019**: Async Streaming via DapperStreamingExtensions / IAsyncEnumerable<T>.
- **Ecosystem Resilience Integration (ADR-017)**: Added overloads in `SqlResilienceExtensions` and `SavepointResilienceExtensions` accepting `EricksonLopez.Resilience.IResiliencePipeline` to align with the core ecosystem resilience authority.
- **Ecosystem Resilience Pipeline Presets**: Added factory methods (`StandardPipeline`, `StandardWithCircuitBreakerPipeline`, `AggressivePipeline`, `ConservativePipeline`, `ForSqlServerPipeline`, `ForPostgreSqlPipeline`, `ForMySqlPipeline`, `ForSqlitePipeline`, `ForOraclePipeline`) in `SqlResilienceDefaults`.
- **`EricksonLopez.DapperExtensions.HealthChecks`**: Database health check probe (`DapperHealthCheck`) supporting custom probe queries, timeout constraints, failure status configuration, and injectable `TimeProvider` for deterministic testing.
- **`EricksonLopez.DapperExtensions.OpenTelemetry`**: Distributed observability package providing OpenTelemetry activity tracing (`ActivitySource`) and runtime execution meters (`Meter`) with automatic latency, status, and tag enrichment.
- **`EricksonLopez.DapperExtensions.SourceGenerators`**: Roslyn Incremental Generator (`SqlEntityGenerator`) for compile-time, reflection-free generation of static mapping methods (`ReadFromDataReader`, `GetMultiMapReaderFactory`) for `[SqlEntity]`-annotated models, guaranteeing Native AOT compatibility. The generated methods are automatically discovered by the library; users do not need to implement specific mapping interfaces.
- **Cursor-Based Pagination**: `QueryCursorPagedAsync<T>` extension method added to all dialect providers (PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite) with bidirectional keyset pagination (`Before`/`After` cursors), composite filters, and dynamic limit handling.
- **Savepoint Isolation**: Extracted `ISavepoint` to a standalone interface contract with dedicated `RollbackAsync` and `ReleaseAsync` execution semantics.
- **Multi-Targeting**: Official compilation and testing matrices expanded to multi-target `.NET 8.0`, `.NET 9.0`, and `.NET 10.0`.
- **Automated Mutation Testing Gate**: Added CI verification script (`scripts/verify-mutation-gate.js`) and GitHub Actions integration enforcing the ≥95% mutation testing quality gate.
- **Exhaustive Testing & Mutation Hardening**: Comprehensive test suites (337 core unit tests, 79 PG, 87 SQL Server, 74 MySQL, 73 MariaDB, 72 Oracle, 81 SQLite, 18 HealthChecks, 14 OpenTelemetry, 22 SourceGen) achieving **≥95% Mutation Score threshold** (98% measured) via Stryker.NET.
- **Official Executable Showcase** (`samples/EricksonLopez.DapperExtensions.Showcase`): Multi-level progressive learning and reference project covering Levels 00 to 11 (Conceptual, Quick Start, Full Configuration, Real-world Pagination/CRUD, Unit of Work & Multi-Map, Bulk Operations, Polly v8 Resilience & Savepoint-Aware Retry, Native AOT Zero-Reflection, Custom Extensibility, OpenTelemetry & Health Checks, Enterprise Outbox & Sagas, Comprehensive API Coverage).
- **Comprehensive Documentation Suite**: Full `/docs/` directory with quickstart, getting-started, API reference, architecture, best-practices, cookbook, performance-guide, troubleshooting, migration-guide, FAQ, CI/CD guide, and NuGet packages reference.
- **ADR-010**: OpenTelemetry Observability Package and Semantic Conventions.
- **ADR-011**: HealthChecks Package and Dialect Probe Architecture.
- **ADR-012**: Cursor-Based (Keyset) Pagination Strategy.
- **ADR-013**: Source Generator for Zero-Reflection Native AOT IDataReaderMapper.
- **ADR-014**: Savepoint-Aware Resilience Retry.
- **ADR-017**: Ecosystem Convergence, Resilience, and UoW Transaction Boundary.

### Changed
- **Test Suite Modularization**: Extracted reusable in-memory ADO.NET fakes and test doubles into `EricksonLopez.DapperExtensions.Testing.Common`, and modularized resilience and unit of work test suites into dedicated projects (`EricksonLopez.DapperExtensions.Tests.Resilience`, `EricksonLopez.DapperExtensions.Tests.UnitOfWork`).
- **Strict Single-Type-Per-File Architecture**: Refactored `BulkParameters<T>`, `BulkBuilder<T>`, `BulkDataTableBuilder<T>`, and dialect `TypeHandlerRegistrar` classes into dedicated `.T.cs` and separate registrar files.
- **Zero Warnings as Errors**: Enforced `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` across all packable packages with full XML documentation on all public members.
- **Strict Clean Architecture**: Enforced single type per file across all production libraries.
- **Consistent Licensing**: Prepended standardized MIT License headers across all source files.

> **Note:** Per ADR-017 (Ecosystem Convergence), the Polly `ResiliencePipeline` overloads in `SqlResilienceExtensions` and `SavepointResilienceExtensions` are **not** marked `[Obsolete]`. They are retained as first-class compatibility APIs. The canonical preferred API is `IResiliencePipeline` (via `*Pipeline()` factory methods in `SqlResilienceDefaults`). See ADR-017 for the full rationale.

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

