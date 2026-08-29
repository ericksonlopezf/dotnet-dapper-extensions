# Architectural Decision Record: REJECT-011
## Rejection of Full Dynamic LINQ Expression Interpreters in Micro-ORMs

### Status
**REJECTED (Permanent Directorial Invariant)**

### Context
Proposals were made to write a full dynamic LINQ expression parser in `EricksonLopez.DapperExtensions` to mimic an ORM query engine.

### Decision
Permanently rejected. Complex query translation is the responsibility of `EricksonLopez.Specification` and `EricksonLopez.SqlBuilder`. `EricksonLopez.DapperExtensions` focuses strictly on high-performance raw SQL execution, bulk batch operations, and dialect-specific extensions (e.g. PostgreSQL UNNEST).

### Consequences
- Zero duplication of SQL building abstractions.
- Predictable execution plans and maximum Dapper throughput.
