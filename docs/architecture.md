# Architecture & Functional Map — EricksonLopez.DapperExtensions

## 1. Overview & Architectural Philosophy

**EricksonLopez.DapperExtensions** is built on the philosophy of **"Raw SQL, Managed Infrastructure"**. It provides enterprise-grade transactional boundaries, transient fault resilience, dialect-native bulk throughput, and Native AOT zero-reflection hydration while giving developers 100% control over SQL text, query semantics, and database execution plans.

```mermaid
graph TD
    App["Application / Web API / Worker Service"] --> DI["Dependency Injection Layer<br/>EricksonLopez.DapperExtensions.DependencyInjection"]
    DI --> Core["Core Library Layer<br/>EricksonLopez.DapperExtensions"]
    
    subgraph CoreComponents ["Core Abstractions & Runtime"]
        UoW["Unit of Work & Savepoints<br/>IUnitOfWork / ISavepoint"]
        Resilience["Polly v8 Resilience Pipelines<br/>SqlResilienceDefaults / Extensions"]
        TypeHandlers["Type Handler Subsystem<br/>DateOnly / TimeOnly / Enums"]
        MultiMap["Native AOT Multi-Map<br/>MultiMapBuilder / [SqlEntity]"]
    end
    
    Core --> CoreComponents
    
    subgraph Dialects ["Dialect-Native Infrastructure Providers"]
        PG["PostgreSql Provider<br/>UNNEST Bulk / JSONB / Keyset"]
        MSSQL["SqlServer Provider<br/>SqlBulkCopy / JSON / Keyset"]
        MySQL["MySql & MariaDb Providers<br/>Multi-Row VALUES / Keyset"]
        Oracle["Oracle Provider<br/>INSERT ALL / Keyset"]
        Sqlite["Sqlite Provider<br/>Bounded Batch / Keyset"]
    end
    
    CoreComponents --> Dialects
    
    subgraph ObservabilityHealth ["Enterprise Cross-Cutting"]
        OTel["OpenTelemetry Instrumentation<br/>ActivitySource & Metrics Meter"]
        HC["HealthChecks Subsystem<br/>Dialect Probes & Latency Metrics"]
    end
    
    CoreComponents --> ObservabilityHealth
    Dialects --> DB[("Relational Databases<br/>PostgreSQL / SQL Server / MySQL / MariaDB / Oracle / SQLite")]
```

---

## 2. Layer Transitions & Execution Lifecycle

From the moment an application request arrives until the transaction commits and connection disposes, the system follows a strictly defined layer lifecycle:

```mermaid
sequenceDiagram
    autonumber
    actor Caller as Service / Handler
    participant DI as DI Container
    participant Res as Polly v8 Pipeline
    participant UoW as IUnitOfWork Scope
    participant Interceptor as OpenTelemetry
    participant DB as Database Engine

    Caller->>DI: Resolve IDbConnection / Detectors
    Caller->>Res: ExecuteAsync(action, ct) (ADR-016)
    activate Res
    Res->>UoW: connection.BeginUnitOfWorkAsync(ct)
    activate UoW
    
    UoW->>DB: Open Connection & BEGIN Transaction
    UoW->>Interceptor: Start Activity ("dapper.unit_of_work")
    
    loop Domain Operations
        Caller->>UoW: ExecuteAsync / QueryAsync / BulkInsertAsync
        UoW->>DB: SQL Command with Transaction & CancellationToken
        DB-->>UoW: Result Set / Rows Affected
    end

    alt Transient Error in Nested Operation (ADR-014)
        Caller->>UoW: CreateSavepointAsync("SP_OP")
        UoW->>DB: SAVEPOINT SP_OP
        UoW-->>Caller: ISavepoint savepoint
        Caller->>DB: Attempt Transient-Prone SQL
        DB-->>Caller: Transient Failure (e.g. Deadlock / Lock Timeout)
        Caller->>savepoint: RollbackAsync()
        savepoint->>DB: ROLLBACK TO SAVEPOINT SP_OP
        Caller->>DB: Retry Operation inside Savepoint
    end

    Caller->>UoW: CommitAsync(ct)
    UoW->>DB: COMMIT Transaction
    UoW->>Interceptor: Record Duration & Success Tag
    deactivate UoW
    deactivate Res
    UoW->>DB: Dispose Transaction & Close Connection
```

---

## 3. Internal Project Dependency Graph

```mermaid
graph TD
    Core["EricksonLopez.DapperExtensions<br/>(Core Library)"]
    DI["EricksonLopez.DapperExtensions.DependencyInjection"]
    SG["EricksonLopez.DapperExtensions.SourceGenerators<br/>(Roslyn Analyzer — netstandard2.0)"]
    HC["EricksonLopez.DapperExtensions.HealthChecks"]
    OTel["EricksonLopez.DapperExtensions.OpenTelemetry"]
    
    PG["EricksonLopez.DapperExtensions.PostgreSql"]
    MSSQL["EricksonLopez.DapperExtensions.SqlServer"]
    MySQL["EricksonLopez.DapperExtensions.MySql"]
    MariaDB["EricksonLopez.DapperExtensions.MariaDb"]
    Oracle["EricksonLopez.DapperExtensions.Oracle"]
    Sqlite["EricksonLopez.DapperExtensions.Sqlite"]

    DI --> Core
    HC --> Core
    OTel --> Core
    
    PG --> Core
    MSSQL --> Core
    MySQL --> Core
    MariaDB --> Core
    Oracle --> Core
    Sqlite --> Core

    %% SourceGenerators is a Roslyn analyzer; consuming apps reference it alongside Core
    %% to enable [SqlEntity] compile-time ReadFromDataReader/GetMultiMapReaderFactory generation (ADR-013)
    %% Note: generates static methods on partial class, NOT IDataReaderMapper<T> implementations
    SG -.->|"consumes [SqlEntity] marker<br/>(compile-time only)"| Core
    
    Showcase["EricksonLopez.DapperExtensions.Showcase<br/>(Executable Samples)"]
    Showcase --> DI
    Showcase --> HC
    Showcase --> OTel
    Showcase --> PG
    Showcase --> MSSQL
    Showcase --> MySQL
    Showcase --> MariaDB
    Showcase --> Oracle
    Showcase --> Sqlite
```

---

## 4. External Ecosystem Dependencies (Cross-Repository)

The following packages are produced by upstream sibling repositories and consumed exclusively as **NuGet package references** (`PackageReference`) centrally managed via `Directory.Packages.props`:

```mermaid
graph LR
    Core["EricksonLopez.DapperExtensions"] --> PagAbs["EricksonLopez.Pagination.Abstractions"]
    Core --> ResAbs["EricksonLopez.Resilience.Abstractions"]
    Core --> ResPolly["EricksonLopez.Resilience.Polly"]
    Core --> SqlBuilderAbs["EricksonLopez.SqlBuilder.Abstractions"]

    PG["EricksonLopez.DapperExtensions.PostgreSql"] --> PagAbs
    PG --> Pag["EricksonLopez.Pagination"]
    MSSQL["EricksonLopez.DapperExtensions.SqlServer"] --> PagAbs
    MSSQL --> Pag
    MySQL["EricksonLopez.DapperExtensions.MySql"] --> PagAbs
    MySQL --> Pag
    MariaDB["EricksonLopez.DapperExtensions.MariaDb"] --> PagAbs
    MariaDB --> Pag
    Oracle["EricksonLopez.DapperExtensions.Oracle"] --> PagAbs
    Oracle --> Pag
    Sqlite["EricksonLopez.DapperExtensions.Sqlite"] --> PagAbs
    Sqlite --> Pag
```

| External Package | Source Repository | Purpose |
|---|---|---|
| `EricksonLopez.Pagination.Abstractions` | `dotnet-pagination` | `ICountedPagedList<T>`, `ICursorPagedList<T>`, `PaginationParameters`, `CursorParameters` contracts |
| `EricksonLopez.Pagination` | `dotnet-pagination` | Default `PagedList<T>` and `CursorPagedList<T>` implementations |
| `EricksonLopez.Resilience.Abstractions` | `dotnet-resilience` | Resilience pipeline abstractions |
| `EricksonLopez.Resilience.Polly` | `dotnet-resilience` | Polly v8 pipeline adapter implementation |
| `EricksonLopez.SqlBuilder.Abstractions` | `dotnet-sql-builder` | SQL building abstraction contracts used by the Core library |

> **Standalone Build**: Upstream ecosystem packages are resolved from NuGet feeds through Central Package Management (`Directory.Packages.props`). The repository builds standalone with zero local directory dependencies.

---

## 5. Architectural Decisions & Key Invariants

The architecture enforces core design invariants formalized through Architecture Decision Records:

1. **Dialect Segregation ([ADR-001](adr/adr-001-multi-provider-architecture-and-dialect-isolation.md))**: Each relational engine is isolated into its own package. No database driver bloat is forced onto consuming applications.
2. **Resilience Scope ([ADR-016](adr/adr-016-resilience-pipeline-scope-wrap-unit-of-work.md))**: Polly resilience pipelines must wrap the *entire* Unit of Work, never retrying individual ADO.NET commands inside an active transaction.
3. **Savepoint-Aware Retry ([ADR-014](adr/adr-014-savepoint-aware-resilience-retry.md))**: Partial failures within active transactions use named savepoints (`ISavepoint`) with dedicated rollback semantics to prevent transaction state poisoning.
4. **Zero Reflection in Hot Paths ([ADR-006](adr/adr-006-native-aot-and-trimming-compliance-enforcement.md), [ADR-013](adr/adr-013-source-generator-for-aot-datareader-mapper.md))**: Full Native AOT compatibility verified via trim analyzers (`EnableTrimAnalyzer=true`) and Roslyn Incremental Generators (`[SqlEntity]`).
5. **No Full ORM / Change Tracker Invariant ([REJECT-011](adr/reject-011-custom-expression-tree-interpreters-in-dapper.md))**: The library strictly avoids in-memory change tracking, dynamic LINQ trees, and artificial abstraction layers over raw SQL.
6. **Ecosystem Resilience Convergence ([ADR-017](adr/adr-017-ecosystem-convergence-resilience-and-uow-transaction-boundary.md))**: Standardized on `IResiliencePipeline` from `EricksonLopez.Resilience` as the canonical authority, while retaining Polly overloads for backward compatibility without `[Obsolete]`.
7. **Ecosystem Demarcation ([ADR-018](adr/adr-018-architectural-boundary-and-coexistence-with-sql-builder.md))**: Formalized the "ONE CAPABILITY → ONE OWNER" demarcation between `DapperExtensions` (data access runtime, UoW, UNNEST, streaming) and `SqlBuilder` (query AST compiler).
8. **Async Streaming ([ADR-019](adr/adr-019-async-streaming-dapper-streaming-extensions.md))**: Unbuffered `IAsyncEnumerable<T>` streaming with an $O(1)$ memory consumption profile for high-volume record streams.

---

## 6. Resilience Pipeline State Diagram

```mermaid
stateDiagram-v2
    [*] --> Idle: Pipeline created (SqlResilienceDefaults.ForXxx())

    Idle --> Executing: pipeline.ExecuteAsync(action, ct)

    Executing --> Success: Action completes without exception
    Success --> [*]: Return result

    Executing --> TransientError: ISqlTransientErrorDetector.IsTransient() = true
    TransientError --> RetryWait: Exponential backoff delay
    RetryWait --> Executing: Retry attempt (up to MaxRetries)
    RetryWait --> ExhaustedRetries: MaxRetries exceeded

    Executing --> PermanentError: IsTransient() = false
    PermanentError --> [*]: Exception propagates to caller

    ExhaustedRetries --> [*]: Exception propagates to caller

    Executing --> CircuitOpen: CircuitBreaker threshold exceeded (WithCircuitBreaker variants)
    CircuitOpen --> HalfOpen: BreakDuration elapsed
    HalfOpen --> Executing: Probe attempt
    HalfOpen --> CircuitOpen: Probe fails
```

---

## 7. Error Handling Flow Diagram

```mermaid
flowchart TD
    Start([SQL Operation Attempted]) --> Wrap{Wrapped in<br/>ResiliencePipeline?}

    Wrap -- Yes --> Execute[Execute Action]
    Wrap -- No --> DirectExec[Direct ADO.NET Execute]
    DirectExec --> DirectFail[Exception propagates immediately]

    Execute --> Result{Success?}
    Result -- Yes --> Done([Return Result])

    Result -- No --> Detect{ISqlTransientErrorDetector<br/>.IsTransient?}
    Detect --> No → Permanent --> Fail([Propagate Exception])

    Detect --> Yes → Transient --> InTx{Inside open<br/>IUnitOfWork?}
    InTx -- No --> Retry[Retry with backoff]
    Retry --> Execute

    InTx -- Yes → DANGER --> Savepoint{Using<br/>ISavepoint?}
    Savepoint -- Yes --> RollbackSP[savepoint.RollbackAsync]
    RollbackSP --> RetryOp[Retry sub-operation in savepoint]
    RetryOp --> Execute

    Savepoint -- No → POISONED --> PoisonedTx[Transaction state POISONED<br/>Retry would fail with SQLSTATE 25P02]
    PoisonedTx --> ForceRollback[Force uow.RollbackAsync]
    ForceRollback --> Fail
```

---

## 8. Processing Pipeline Diagram

```mermaid
flowchart LR
    Input[("Application Request")] --> DI[("DI Container<br/>IDbConnection · IResiliencePipeline<br/>ISqlTransientErrorDetector")]

    DI --> Pipeline["Polly v8 / IResiliencePipeline<br/>(SqlResilienceDefaults)"]
    Pipeline --> UoW["IUnitOfWork Scope<br/>(BeginUnitOfWorkAsync)"]

    UoW --> SQLExec["SQL Execution Layer<br/>Dapper / ADO.NET"]

    SQLExec --> OTel["OpenTelemetry Instrumentation<br/>ActivitySource · Meter"]
    SQLExec --> DB[("Relational Database")]

    DB --> Result["Result Set / Rows Affected"]
    Result --> MultiMap["Optional MultiMapBuilder&lt;T&gt;<br/>(1:N Aggregation)"]
    Result --> Stream["Optional StreamAsync&lt;T&gt;<br/>(Unbuffered IAsyncEnumerable)"]

    MultiMap --> Output[("Domain Objects / Aggregates")]
    Stream --> Output
    Result --> Output

    OTel --> Telemetry[("OTLP Exporter / Prometheus<br/>Distributed Traces · Latency Histograms")]
```
