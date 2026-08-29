# ADR-016: Resilience Pipeline Scope — Wrap Unit of Work, Not Individual Commands

## Status
Accepted

## Context
When combining transactional operations with resilience pipelines, there are two possible scoping strategies:

**Strategy A — Retry individual commands inside an active transaction (WRONG):**
```csharp
await using var uow = await connection.BeginUnitOfWorkAsync();

// BAD: Retrying inside an active transaction corrupts the transaction state.
// If the first attempt partially executes, the retry runs against a dirty transaction.
IResiliencePipeline resilientPipeline = SqlResilienceDefaults.ForPostgreSqlPipeline();
await connection.ExecuteWithResilienceAsync(insertSql, query, resilientPipeline);
```

**Strategy B — Wrap the entire Unit of Work in the resilience pipeline (CORRECT):**
```csharp
IResiliencePipeline resilientPipeline = SqlResilienceDefaults.ForPostgreSqlPipeline();

await resilientPipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(cancellationToken: ct);
    await connection.ExecuteAsync(insertOrderSql, orderParams, uow.Transaction);
    await connection.ExecuteAsync(insertLinesSql, linesParams, uow.Transaction);
    await uow.CommitAsync(ct);
}, cancellationToken);
```

In Strategy B, if a transient error occurs, the entire `UnitOfWork` is disposed (triggering automatic rollback via `DisposeAsync`), the connection is re-established, and the entire atomic unit is retried from scratch — which is the only correct behavior.

## Decision

**All retry policies must wrap the entire transactional unit, never individual statements inside an active transaction.**

Specifically:
1. Do not apply `SqlResilienceExtensions` methods (`ExecuteWithResilienceAsync`, `QueryWithResilienceAsync`, etc.) to individual commands within a `BeginUnitOfWorkAsync` / `WithUnitOfWorkAsync` scope.
2. When retrying transactional operations, use `pipeline.ExecuteAsync(ct => { ... BeginUnitOfWorkAsync ... CommitAsync }, ct)` pattern.
3. The `WithUnitOfWorkAsync` helper can be used inside the pipeline delegate:
   ```csharp
   await pipeline.ExecuteAsync(async ct =>
       await connection.WithUnitOfWorkAsync(async (uow, ct) => { ... }, ct),
   cancellationToken);
   ```

## CancellationToken Note

As of v1.2.0, `CancellationToken` correctly flows through the pipeline `ct` parameter to the underlying `CommandDefinition` in all `SqlResilienceExtensions` methods (ADR-004). This ensures true end-to-end cancellation: canceling the token will abort both the pipeline AND the active database command on the server, preventing orphan server-side executions.

## Ecosystem Convergence Note (ADR-017)

As of the convergence milestone documented in ADR-017, the preferred pipeline API is `IResiliencePipeline`
from `EricksonLopez.Resilience.Abstractions`, obtained via `SqlResilienceDefaults.For*Pipeline()` methods.
The `Polly.ResiliencePipeline` overloads remain for backward compatibility but are deprecated. See ADR-017.

## Consequences

- Transient-error retry operates at the correct atomic unit boundary: the full transaction.
- Automatic rollback via `IAsyncDisposable` ensures no dirty state leaks between retry attempts.
- CancellationToken propagation ensures server-side query abortion on cancellation, reducing database server resource consumption.
- The `ISqlTransientErrorDetector` implementations detect serialization failures and deadlocks (SQLSTATE 40001, 40P01) which are natural candidates for UoW-level retry.
