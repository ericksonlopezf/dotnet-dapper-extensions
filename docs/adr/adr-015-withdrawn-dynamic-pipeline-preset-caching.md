# ADR-015: [WITHDRAWN] Dynamic Pipeline Preset Caching

## Status
Withdrawn

## Date
2026-09-04

## Context
This ADR number was reserved during an early draft of the resilience convergence work
(later formalized in ADR-016 and ADR-017). The draft explored caching pre-built Polly
`ResiliencePipeline` instances in a static `ConcurrentDictionary` keyed by
`ISqlTransientErrorDetector` identity, to avoid re-building equivalent pipelines per call site.

## Decision
After prototyping, the approach was withdrawn for the following reasons:

1. **Complexity vs. benefit:** Polly v8 `ResiliencePipelineBuilder.Build()` is fast and
   allocation-light. The caching layer added observable complexity without measurable
   latency improvements in benchmark runs.

2. **Superseded by `IResiliencePipeline` factory methods:** The canonical `For*Pipeline()`
   factory methods introduced in ADR-017 (e.g., `ForPostgreSqlPipeline()`) return
   `IResiliencePipeline` wrappers that consumers can cache at their own discretion (e.g.,
   in a static field or DI singleton). This gives consumers control over the cache lifetime
   without forcing library-level caching semantics.

3. **ADR-017 supersedes the scope:** The ecosystem convergence decision in ADR-017 fully
   addresses pipeline creation, lifecycle, and preferred API patterns.

## Consequences
- No implementation was shipped under ADR-015.
- The ADR number is retained as a `Withdrawn` tombstone to preserve the sequential gap
  documentation and avoid ambiguity in future ADR numbering.
- See ADR-016 and ADR-017 for the active resilience pipeline scope decisions.
