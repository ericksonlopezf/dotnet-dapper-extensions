# ADR-011: HealthChecks Package and Dialect Probe Architecture

## Status
Accepted

## Context
ASP.NET Core health check endpoints (`/healthz`, `/ready`) require reliable probes to verify database connectivity, responsiveness, and degraded thresholds without allocating heavy resources.

## Decision
1. Provide a standalone package `EricksonLopez.DapperExtensions.HealthChecks` implementing `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`.
2. Encapsulate probe query execution with:
   - Configurable timeout and cancellation.
   - Latency measurement reporting `Healthy`, `Degraded` (if latency exceeds threshold), or `Unhealthy` on error.
   - Provider-tailored shortcuts (`AddPostgreSqlDapperHealthCheck`, `AddSqlServerDapperHealthCheck`, `AddOracleDapperHealthCheck`, `AddMySqlDapperHealthCheck`, `AddSqliteDapperHealthCheck`) using dialect-accurate probes (such as `SELECT 1 FROM DUAL` on Oracle).

## Consequences
- Clean separation between core data access and ASP.NET Core diagnostic infrastructure.
- Zero-overhead readiness verification for cloud-native containers and Kubernetes liveness/readiness probes.
