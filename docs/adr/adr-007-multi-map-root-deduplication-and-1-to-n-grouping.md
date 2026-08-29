# ADR-007: Multi-Map Root Deduplication and 1-to-N Grouping

## Status
Accepted

## Context
When performing relational SQL `JOIN` queries across 1:N entity relationships (e.g. `Order` JOIN `OrderItem`), standard row-by-row mapping returns multiple rows with identical root entity primary keys. Without a key-based deduplication mechanism, standard multi-map execution produces multiple distinct root entity instances, each containing only a subset of child entities.

## Decision
Provide first-class `QueryGroupedAsync<TKey>` and `QueryGroupedFirstOrDefaultAsync<TKey>` methods in `MultiMapBuilder<TReturn>`:
1. Accepts a `Func<TReturn, TKey> keySelector` delegate.
2. Maintains an internal `Dictionary<TKey, TReturn>` during both AOT reader execution and Dapper fallback execution.
3. Automatically retrieves or registers the unique root instance and folds child entity mappings into the existing root instance.
4. Returns `lookup.Values` as a collection of deduplicated root entities with populated child relationships.

## Consequences
- Clean, high-performance resolution of 1:N relational hierarchies with zero manual dictionary boilerplate in application services.
- Supported in both Native AOT reader mode and Dapper dynamic mapping mode.
