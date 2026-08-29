# ADR-017: Ecosystem Convergence — IResiliencePipeline Authority and IUnitOfWork/ITransactionManager Boundary

## Status
Accepted

## Context

The `EricksonLopez` ecosystem comprises multiple specialized frameworks that must interact cleanly without
coupling or authority duplication. This ADR formalizes two convergence decisions for `EricksonLopez.DapperExtensions`:

### 1. Resilience Authority Convergence

`EricksonLopez.DapperExtensions` previously exposed `Polly.ResiliencePipeline` (from `Microsoft.Extensions.Resilience`)
directly in its public API surface (`SqlResilienceExtensions`, `SavepointResilienceExtensions`, `SqlResilienceDefaults`).
This made Polly a first-class citizen of DapperExtensions' API contract, violating the ecosystem's convergence principle:

> **ONE CAPABILITY → ONE OWNER**. `EricksonLopez.Resilience` is the sole authority for resilience policy orchestration.
> Polly must remain an L4 infrastructure implementation detail, invisible to Application and even Infrastructure consumers
> of higher-level abstractions.

The `EricksonLopez.Resilience.Abstractions` package provides `IResiliencePipeline` — the ecosystem-standard
resilience pipeline contract backed by BCL types only (`ValueTask`, `CancellationToken`, `ResilienceContext`).
The `EricksonLopez.Resilience.Polly` package provides `PollyResiliencePipeline`, an adapter that implements
`IResiliencePipeline` backed by a compiled Polly v8 `ResiliencePipeline` — keeping Polly isolated as L4.

### 2. IUnitOfWork / ITransactionManager Boundary

`EricksonLopez.DapperExtensions` provides `IUnitOfWork` — the application/domain boundary contract for a
set of atomic database operations. `EricksonLopez.Transaction` provides `ITransactionManager` — the infrastructure
coordinator for ambient transaction propagation, savepoints, and commit-ambiguity handling.

These are NOT the same contract:

| Concern | Owner |
|---|---|
| Application/domain transaction boundary | `IUnitOfWork` (DapperExtensions) |
| Infrastructure transaction orchestration | `ITransactionManager` (EricksonLopez.Transaction) |

The two frameworks operate at different layers and are **complementary, not overlapping**:

```
Application
    │
    ▼
IUnitOfWork  (Application boundary — "these writes are atomic")
    │
    ▼
Infrastructure
    │
    ├── DapperExtensions.UnitOfWork  (direct Dapper path)
    │
    └── EricksonLopez.Transaction   (advanced ambient propagation)
            │
            ▼
        ITransactionManager
```

## Decision

### Resilience
1. Add `EricksonLopez.Resilience.Abstractions` and `EricksonLopez.Resilience.Polly` as dependencies of `EricksonLopez.DapperExtensions`.
2. Add `IResiliencePipeline`-based canonical overloads to `SqlResilienceExtensions` and `SavepointResilienceExtensions`.
3. Add `IResiliencePipeline`-returning factory methods (`StandardPipeline`, `AggressivePipeline`, `ConservativePipeline`, `ForSqlServerPipeline`, etc.) to `SqlResilienceDefaults`.
4. Retain complementary `Polly.ResiliencePipeline`-accepting overloads without deprecation (`[Obsolete]`), enabling seamless integration for consumers using Polly v8 directly while establishing `IResiliencePipeline` as the primary ecosystem contract (in accordance with ADR-005 zero-obsolete policy).

The `ISqlTransientErrorDetector` interface remains the DapperExtensions classification concern — it classifies the
database-specific error context. `EricksonLopez.Resilience` decides and orchestrates the retry/circuit-breaker policy.

### IUnitOfWork / ITransactionManager
5. `IUnitOfWork` in `EricksonLopez.DapperExtensions.UnitOfWork` remains the Application-layer unit-of-work contract for Dapper-based persistence.
6. `ITransactionManager` in `EricksonLopez.Transaction.Abstractions` remains the Infrastructure-layer ambient coordinator.
7. A formal dependency of DapperExtensions on `EricksonLopez.Transaction.Abstractions` is **not added** in this ADR because `EricksonLopez.Transaction.Abstractions` currently targets `net10.0` only while DapperExtensions multi-targets `net8.0;net9.0;net10.0`. When `Transaction.Abstractions` extends its target frameworks, an `ITransactionContext → IUnitOfWork` adapter extension should be added to `EricksonLopez.DapperExtensions.DependencyInjection`.
8. The separation is formalized and documented in `IUnitOfWork.cs` XML documentation, referencing `ITransactionManager` as the infrastructure-level coordinator.

## Consequences

### Positive
- `IResiliencePipeline` (EL canonical) is now the preferred resilience contract for all DapperExtensions consumers.
- `ISqlTransientErrorDetector` implements the correct separation: DapperExtensions classifies, Resilience decides.
- Zero obsolete warnings or deprecations introduced across the codebase.
- The `IUnitOfWork` / `ITransactionManager` boundary is formally documented and non-overlapping.

### Negative
- `EricksonLopez.Resilience.Polly` is now a transitive dependency of DapperExtensions when using project references.

### Neutral
- All existing tests using `ResiliencePipeline` (Polly) continue to compile and run (soft deprecation).
- `ITransactionContext → IUnitOfWork` adapter deferred to a future release pending multi-target alignment.

## Canonical API — Migration Guide

**Before (deprecated):**
```csharp
// SqlResilienceDefaults returns Polly ResiliencePipeline
ResiliencePipeline pipeline = SqlResilienceDefaults.ForPostgreSql();

// SqlResilienceExtensions accepts Polly ResiliencePipeline
await connection.QueryWithResilienceAsync<Order>(query, pipeline);
```

**After (canonical):**
```csharp
// SqlResilienceDefaults.ForPostgreSqlPipeline() returns IResiliencePipeline (EL)
IResiliencePipeline pipeline = SqlResilienceDefaults.ForPostgreSqlPipeline();

// SqlResilienceExtensions accepts IResiliencePipeline (EL)
await connection.QueryWithResilienceAsync<Order>(query, pipeline);
```

## References

- ADR-014 (this repo): Savepoint-Aware Resilience Retry.
- ADR-016 (this repo): Resilience Pipeline Scope — Wrap Unit of Work, Not Individual Commands.
- ADR-036 (EricksonLopez.Mediator): Deprecation of `EricksonLopez.Mediator.Polly` in Favor of `EricksonLopez.Resilience.Mediator`.
- `EricksonLopez.Resilience.Abstractions` — `IResiliencePipeline`, `IResilienceExecutor`.
- `EricksonLopez.Resilience.Polly` — `PollyResiliencePipeline` adapter.
- Ecosystem Convergence Plan — Sections 2.2, 3, 4, and 5.
