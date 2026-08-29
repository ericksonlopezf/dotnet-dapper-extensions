# ADR-010: OpenTelemetry Observability Package and Semantic Conventions

## Status
Accepted

## Context
Production workloads require unified observability (distributed tracing and metrics) for database queries, execution latencies, and high-throughput bulk operations. Bundling `OpenTelemetry.Api` into the core `EricksonLopez.DapperExtensions` package would force unwanted runtime dependencies on lightweight or embedded scenarios.

## Decision
1. Deliver OpenTelemetry instrumentation in a dedicated package: `EricksonLopez.DapperExtensions.OpenTelemetry`.
2. Expose standard diagnostic instruments:
   - `ActivitySource`: `"EricksonLopez.DapperExtensions"` with semantic convention tags (`db.system`, `db.statement`, `db.operation`, `db.name`, `db.rows_affected`, `error.type`).
   - `Meter`: `"EricksonLopez.DapperExtensions"` emitting duration histograms (`db.client.commands.duration`), command counters, bulk rows affected, and resilience retries.
3. Provide `AddDapperOpenTelemetry(this IServiceCollection, Action<DapperOpenTelemetryOptions>?)` for seamless ASP.NET Core integration.

## Consequences
- Zero dependency footprint on the core database library.
- Production-grade observability conforming to OpenTelemetry database semantic conventions.
