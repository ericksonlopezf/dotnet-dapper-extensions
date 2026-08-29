# ADR-004: CancellationToken Propagation in Resilience Pipelines

## Status
Accepted

## Context
In asynchronous database operations wrapped with Polly v8 resilience pipelines, the resilience pipeline produces an execution context and passes a `CancellationToken` (`ct`) into the delegate execution closure.

Previously, `SqlResilienceExtensions` received `cancellationToken` at the method boundary and passed it to `pipeline.ExecuteAsync(...)`, but within the inner delegate closure, the Dapper command was invoked without passing `ct`. This created a "false parity" gap where canceling the outer token interrupted the Polly pipeline but left underlying ADO.NET socket / database command executions running on the database server.

## Decision
All methods in `SqlResilienceExtensions` must construct Dapper `CommandDefinition` instances passing `cancellationToken: ct` inside the delegate closure.

```csharp
return pipeline.ExecuteAsync(
    async ct =>
    {
        var command = new CommandDefinition(
            query.Sql,
            query.Parameters,
            transaction,
            commandTimeout: null,
            commandType: CommandType.Text,
            flags: CommandFlags.None,
            cancellationToken: ct);
        return await connection.ExecuteAsync(command).ConfigureAwait(false);
    },
    cancellationToken).AsTask();
```

## Consequences
- True end-to-end cancellation: cancellation signals propagate directly to the underlying `DbCommand.Execute...Async(ct)` calls.
- Avoids orphan query executions and reduces database server resource consumption upon client timeouts or request cancellations.
