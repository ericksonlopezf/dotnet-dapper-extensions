# ADR-013: Source Generator for Zero-Reflection Native AOT IDataReaderMapper

## Status
Accepted

## Context
High-throughput Native AOT applications avoid reflection and runtime code emission (`Reflection.Emit`). While `MultiMapBuilder<TReturn>` supports manual parsers via `IDataReaderMapper<T>`, hand-writing mapping methods for multiple entities is error-prone and tedious.

## Decision
1. Deliver Roslyn incremental generator `EricksonLopez.DapperExtensions.SourceGenerators`.
2. Inspect types annotated with `[SqlEntity]`.
3. Generate zero-reflection `ReadFromDataReader(IDataReader reader)` and static factory method `GetMultiMapReaderFactory()` mapping properties directly by ordinal index.

## Consequences
- Guaranteed 100% Native AOT compatibility with zero reflection warnings.
- Maximum possible throughput matching hand-written ADO.NET hydration code.
