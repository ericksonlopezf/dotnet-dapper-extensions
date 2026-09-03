# Testing Architecture & Guide — EricksonLopez.DapperExtensions

This document outlines the testing architecture, design guidelines, shared test infrastructure, and execution procedures for the automated test suite of the `EricksonLopez.DapperExtensions` ecosystem.

---

## 1. QA Philosophy & Core Principles

The test suite strictly adheres to foundational software quality and reliability principles:

1. **Pragmatism > Numerical Coverage > Architectural Purism**:
   - Prioritize deterministic, reliable tests that validate production contracts without over-reliance on fragile dynamic mocks.
2. **Determinism & Total Isolation (FIRST Principles)**:
   - Unit tests execute 100% in-memory in under 3 seconds with zero network calls or external daemon requirements.
   - Virtualized time for resilience pipelines via `FakeTimeProvider`.
   - Zero mutable static state (`BulkInsertInternalAsync` with direct delegate injection instead of global mutable hooks).
3. **Exhaustive Multi-Targeting**:
   - Every test project compiles and executes natively against **.NET 8.0**, **.NET 9.0**, and **.NET 10.0**.
4. **Quality Gates**:
   - Line code coverage > 90% (ecosystem average > 94%).
   - Zero surviving mutants across critical infrastructure (Polly resilience, UnitOfWork, Pagination).
   - Native AOT compatibility verified via a dedicated smoke test project with zero trimming/AOT warnings (`TreatWarningsAsErrors=true`).

---

## 2. Test Projects Layout

```text
tests/
├── EricksonLopez.DapperExtensions.Testing.Common/      # Shared test infrastructure library
│   ├── DialectBases/
│   │   ├── PagedQueryExtensionsTestsBase.cs            # Abstract base test suite for pagination (ANSI/LIMIT)
│   │   └── TransactionExtensionsTestsBase.cs           # Abstract base test suite for transaction extensions
│   ├── BulkTestDataFactory.cs                          # Centralized product entities, data tables, and batch generators
│   ├── FakeTimeProvider.cs                             # Virtual time provider for deterministic Polly v8 tests
│   ├── TestAdoCommand.cs                               # In-memory DbCommand test double
│   ├── TestAdoConnection.cs                            # In-memory DbConnection test double with delegate hooks
│   ├── TestAdoDataReader.cs                            # Simulated DbDataReader for Dapper row mapping
│   ├── TestAdoParameter.cs                             # DbParameter and DbParameterCollection test double
│   ├── TestAdoTransaction.cs                           # DbTransaction spy verifying CommitAsync/RollbackAsync
│   └── TestDbException.cs                              # Concrete DbException test double
├── EricksonLopez.DapperExtensions.Tests/               # Core: Polly resilience, UnitOfWork, Streaming, TypeHandlers
├── EricksonLopez.DapperExtensions.OpenTelemetry.Tests/ # OpenTelemetry tracing, spans, and ActivitySource
├── EricksonLopez.DapperExtensions.SqlServer.Tests/     # SQL Server dialect + BulkCopy + Testcontainers
├── EricksonLopez.DapperExtensions.PostgreSql.Tests/   # PostgreSQL dialect + BulkCopy + Testcontainers
├── EricksonLopez.DapperExtensions.MySql.Tests/        # MySQL dialect + Bulk + Testcontainers
├── EricksonLopez.DapperExtensions.MariaDb.Tests/      # MariaDB dialect + Bulk + Testcontainers
├── EricksonLopez.DapperExtensions.Oracle.Tests/       # Oracle dialect + Bulk + Testcontainers
├── EricksonLopez.DapperExtensions.Sqlite.Tests/       # SQLite dialect + Keyset + In-Memory Integration
└── EricksonLopez.DapperExtensions.AotSmokeTest/       # Native AOT publish validation (IL trimming/AOT gates)
```

---

## 3. ADO.NET Test Doubles Infrastructure (`Testing.Common`)

Rather than maintaining fragile dynamic mock setups (NSubstitute/Moq) over internal ADO.NET hierarchies (`DbConnection`, `DbCommand`), the suite utilizes a suite of **pure ADO.NET fakes** centralized in `EricksonLopez.DapperExtensions.Testing.Common`:

- **`TestAdoConnection`**: Intercepts command creation, scalar execution, connection state transitions, and reader generation via delegates (`ReaderFactory`, `ScalarFactory`, `NonQueryFactory`).
- **`TestAdoCommand`** & **`TestAdoParameter`**: Strongly-typed fakes recording executed SQL statements, command timeouts, transaction isolation levels, and bound parameters.
- **`TestAdoDataReader`**: Cursor implementation for Dapper row mapping without requiring an external database.
- **`TestAdoTransaction`**: Transaction spy verifying deterministic invocations to `CommitAsync` and `RollbackAsync`.
- **`FakeTimeProvider`**: Virtualized time provider supporting instantaneous and manual time progression for Polly v8 pipelines and health checks.
- **`BulkTestDataFactory`**: Reusable data generator for bulk insert testing across dialects (`BulkTestProduct`).

### Centralized Reference via MSBuild (`tests/Directory.Build.props`)

All test projects automatically reference the shared testing infrastructure library via `tests/Directory.Build.props`:

```xml
<ItemGroup Condition="'$(MSBuildProjectName)' != 'EricksonLopez.DapperExtensions.AotSmokeTest' and '$(MSBuildProjectName)' != 'EricksonLopez.DapperExtensions.Testing.Common'">
  <ProjectReference Include="$(MSBuildThisFileDirectory)EricksonLopez.DapperExtensions.Testing.Common\EricksonLopez.DapperExtensions.Testing.Common.csproj" />
</ItemGroup>
```

---

## 4. Dialect Test Reusability (`DialectBases`)

To eliminate code duplication across the 6 supported database dialects, abstract test base classes are provided:

1. **`PagedQueryExtensionsTestsBase`**:
   - Centralizes the `Customer` record fixture, tabular data generators (`CreateDefaultCustomerReader`, `CreateMultipleCustomerReader`), argument validation, and SQL assertion mechanics (`QueryPagedAsync`, `QueryPagedMultipleAsync`, `QueryCursorPagedAsync`).
   - Each dialect project only defines its database-specific pagination syntax (`ExpectedOffsetClause`) and binds its namespace extensions.
2. **`TransactionExtensionsTestsBase`**:
   - Centralizes transaction lifecycle tests (commit, rollback, exception propagation).

---

## 5. Resilience Testing with Polly v8 & `FakeTimeProvider`

Resilience policy tests (`SqlResilienceDefaultsTests.cs`) leverage `FakeTimeProvider` to advance time deterministically:

- **Exponential Retries**: Executed synchronously without introducing real clock delays.
- **Circuit Breakers**: State transitions (`Closed` → `Open` → `HalfOpen` → `Closed`) driven by explicit time progression (`timeProvider.Advance(breakDuration)`).
- **Timeouts**: Boundary enforcement (30s Standard, 60s Aggressive, 120s Conservative) verifying `TimeoutRejectedException` upon exceeding thresholds without relying on wall-clock timers (`CancellationTokenSource(wallClock)`).

---

## 6. Integration Testing with Testcontainers

Integration tests validate real database engine behaviors for Bulk Operations, Keyset Pagination, and multi-statement transactions in isolated Docker containers via **Testcontainers**:

- **SQL Server**: `mcr.microsoft.com/mssql/server:2022-latest`
- **PostgreSQL**: `postgres:16-alpine`
- **MySQL**: `mysql:8.0`
- **MariaDB**: `mariadb:11`
- **Oracle**: `gvenzl/oracle-free:23-slim-faststart`
- **SQLite**: In-memory (`Data Source=:memory:`), requires no Docker engine and runs automatically during normal test runs (`Category=SqliteIntegration`).

### Fixture Lifecycle Optimization (`IClassFixture<T>`)

To maximize test execution speed and prevent redundant container spins:
- Each integration test class implements `IClassFixture<TFixture>`.
- The container spins up **once per test class** and provisions the initial DDL schema.
- Individual `[Fact]` methods only open a clean connection and execute `DELETE FROM products;`.

### Continuous Compilation & `.runsettings` Filter

All integration tests are continuously compiled during standard builds to guarantee syntax, type, and contract integrity. To preserve ultra-fast local test runs without a mandatory Docker daemon requirement, the solution enforces a root `.runsettings` file configured via `tests/Directory.Build.props`:

```xml
<RunConfiguration>
  <TestCaseFilter>Category!=Integration</TestCaseFilter>
</RunConfiguration>
```

---

## 7. Test Execution Commands

### 7.1. Unit Test Execution (Fast / Default)

```bash
# Run all unit tests across all supported .NET target frameworks (.runsettings filters out Docker integration tests)
dotnet test

# Run unit tests for a specific dialect
dotnet test tests/EricksonLopez.DapperExtensions.SqlServer.Tests/

# Run with cross-platform code coverage collection
dotnet test --collect:"XPlat Code Coverage"
```

### 7.2. Integration Test Execution (Requires Docker Engine)

```bash
# Run all integration tests across the entire solution
dotnet test --filter "Category=Integration"

# Run integration tests for a specific dialect (e.g., PostgreSQL)
dotnet test tests/EricksonLopez.DapperExtensions.PostgreSql.Tests/ --filter "Category=Integration"
```

### 7.3. Mutation Testing with Stryker.NET

```bash
# Install the global .NET Stryker tool (if not already installed)
dotnet tool install -g dotnet-stryker

# Run mutation analysis on core library
dotnet stryker -p EricksonLopez.DapperExtensions.Tests -s EricksonLopez.DapperExtensions.slnx
```

### 7.4. Native AOT Verification

```bash
# Publish the smoke test project in Native AOT mode (validates absence of IL2026, IL2091, IL3050 trimming warnings)
dotnet publish tests/EricksonLopez.DapperExtensions.AotSmokeTest/ -c Release
```
