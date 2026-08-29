# ADR-006: Native AOT and Trimming Compliance Enforcement

## Status
Accepted

## Context
`EricksonLopez.DapperExtensions` positions itself as a modern, high-throughput, Native AOT-friendly library for .NET. However, having `<EnableTrimAnalyzer>false</EnableTrimAnalyzer>` prevented MSBuild from warning about unsupported reflection invocations at build time.

## Decision
1. Explicitly enable `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` in `Directory.Build.props` across all build targets.
2. Annotate dynamic access sites (such as `PostgreSqlTransientErrorDetector`) with `[UnconditionalSuppressMessage]` specifying explicit architectural rationale for decoupled ADO.NET exception inspection.
3. Design and recommend `IDataReaderMapper<T>` and source-generated factories for `MultiMapBuilder<TReturn>` to achieve 0-reflection Native AOT paths.

## Consequences
- Continuous verification of trimming compatibility during `dotnet build`.
- Guaranteed reliability in trimmed, containerized, and Native AOT deployed .NET microservices.
