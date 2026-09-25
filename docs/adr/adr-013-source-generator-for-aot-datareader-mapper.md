# ADR-013: Source Generator for Zero-Reflection Native AOT IDataReaderMapper

## Status
Accepted

## Date
2026-09-04

## Context
High-throughput Native AOT applications avoid reflection and runtime code emission (`Reflection.Emit`). While `MultiMapBuilder<TReturn>` supports manual parsers via `IDataReaderMapper<T>`, hand-writing mapping methods for multiple entities is error-prone and tedious.

## Decision
1. Deliver Roslyn incremental generator `EricksonLopez.DapperExtensions.SourceGenerators`.
2. Inspect types annotated with `[SqlEntity]`.
3. Generate zero-reflection `ReadFromDataReader(IDataReader reader)` and static factory method `GetMultiMapReaderFactory()` mapping properties directly by ordinal index.

## Consequences
- Guaranteed 100% Native AOT compatibility with zero reflection warnings.
- Maximum possible throughput matching hand-written ADO.NET hydration code.

## Implementation Clarification: Static Methods vs. `IDataReaderMapper<T>`

The Roslyn generator adds two **static methods** to the annotated `partial` class:
- `public static T ReadFromDataReader(IDataReader reader)` — maps a single row.
- `public static Func<IDataReader, object> GetMultiMapReaderFactory()` — returns a factory for `MultiMapBuilder<T>`.

These generated methods satisfy the `MultiMapBuilder<T>` AOT path automatically. They do **not** make the
class implement the `IDataReaderMapper<T>` interface. `IDataReaderMapper<T>` is a **separate, manually
implemented** interface for scenarios requiring a named, injectable mapper object. Both mechanisms are
complementary and can be used together.

