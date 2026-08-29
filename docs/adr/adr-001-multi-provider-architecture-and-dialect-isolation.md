# ADR-001: Multi-Provider Architecture and Dialect Isolation

## Status
Accepted

## Context
`EricksonLopez.DapperExtensions` provides high-performance extensions for Dapper across multiple relational database engines (PostgreSQL, MySQL, MariaDB, Oracle, SQLite, and SQL Server).

## Decision
1. Core provider-agnostic features (Unit of Work, MultiMapBuilder, Polly resilience extensions) reside in the base package `EricksonLopez.DapperExtensions`.
2. Database-specific capabilities (JSONB handlers, bulk operations, dialect-specific paginated SQL generation) are isolated in dedicated provider packages:
   - `EricksonLopez.DapperExtensions.PostgreSql`
   - `EricksonLopez.DapperExtensions.MySql`
   - `EricksonLopez.DapperExtensions.MariaDb`
   - `EricksonLopez.DapperExtensions.Oracle`
   - `EricksonLopez.DapperExtensions.Sqlite`
   - `EricksonLopez.DapperExtensions.SqlServer`

## Consequences
- Consumers only install the package matching their database engine, keeping dependencies minimal.
- Dialect optimizations do not contaminate cross-provider abstractions.
