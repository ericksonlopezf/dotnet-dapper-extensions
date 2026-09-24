# ADR-019: Async Streaming via DapperStreamingExtensions / IAsyncEnumerable<T>

## Status
Accepted

## Date
2026-09-04

## Context
EricksonLopez.DapperExtensions includes DapperStreamingExtensions, a public static class in the
EricksonLopez.DapperExtensions.Streaming namespace that provides two StreamAsync<T> overloads returning
IAsyncEnumerable<T>. This capability was cited in ADR-018 as a proprietary differentiator over generic
Dapper usage, but was never formally documented in a dedicated ADR.

Standard Dapper QueryAsync<T> buffers the entire result set into an in-memory List<T> before returning.
For large datasets (10K+ rows), this causes high LOH (Large Object Heap) allocations and GC Gen 2 pressure,
which is detrimental to high-throughput microservice workloads.

## Decision

1. Formally document DapperStreamingExtensions as a first-class public API in the
   EricksonLopez.DapperExtensions.Streaming namespace.

2. Provide two overloads:
   - StreamAsync<T>(IDbConnection, string sql, ...) -- raw SQL string + parameters.
   - StreamAsync<T>(IDbConnection, SqlResult query, ...) -- compiled EricksonLopez.SqlBuilder.Abstractions.SqlResult.

3. Native AOT Constraint: StreamAsync<T> uses Dapper's IDataReader.GetRowParser<T>() internally.
   This is a reflection-based code path. Therefore:
   - StreamAsync<T> is NOT fully Native AOT compatible.
   - It may produce trimming warnings (IL2026, IL3050) in trimmed or AOT-published builds.
   - For Native AOT environments, consumers must use MultiMapBuilder<T> with source-generated
     ([SqlEntity]) or manually-implemented IDataReaderMapper<T> parsers instead.

4. The API is designed for scenarios where:
   - Result sets are large (10K+ rows).
   - The consumer can process rows incrementally (pipeline/channel/processor pattern).
   - The application targets standard runtime (not Native AOT).

## Consequences

- DapperStreamingExtensions is documented in README.md, api-reference.md, and referenced in ADR-006s Known Exception section.
- Consumers targeting Native AOT receive a clear guidance path: use MultiMapBuilder<T> instead.
- The DapperStreamingExtensions public surface is stable and additive; no breaking changes to existing APIs.
- Future enhancement: consider adding an overload accepting a custom parser delegate to support AOT-safe streaming in a future minor release.
