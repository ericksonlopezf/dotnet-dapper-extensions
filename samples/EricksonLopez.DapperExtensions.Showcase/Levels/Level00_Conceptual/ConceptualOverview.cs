// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level00_Conceptual;

/// <summary>
/// Level 0 — Conceptual: Philosophy, architecture, comparisons, and trade-offs.
/// </summary>
public static class ConceptualOverview
{
    public static Task RunAsync()
    {
        ConsoleHelper.PrintHeader(0, "Conceptual Overview", "Core foundations, architectural philosophy, and comparison with the .NET ecosystem");

        ConsoleHelper.PrintStep("1. What is EricksonLopez.DapperExtensions?");
        ConsoleHelper.PrintInfo("Definition", "High-performance, Native AOT-ready infrastructure extensions built on top of Dapper.");
        ConsoleHelper.PrintInfo("Philosophy", "\"Raw SQL, Managed Infrastructure\" — Full control over native SQL combined with robust infrastructure management.");

        ConsoleHelper.PrintStep("2. Core Problems Solved");
        ConsoleHelper.PrintInfo("Async Transactions", "Guarantees deterministic rollback via IUnitOfWork and IAsyncDisposable lifecycle.");
        ConsoleHelper.PrintInfo("Safe Resilience (ADR-016)", "Eliminates transactional data corruption caused by retries inside open transactions by wrapping the entire Unit of Work.");
        ConsoleHelper.PrintInfo("High-Throughput Bulk", "Provides native bulk operations (PostgreSQL UNNEST, SQL Server SqlBulkCopy, multi-row batching).");
        ConsoleHelper.PrintInfo("1:N Relational Mapping", "MultiMapBuilder with primary-key root deduplication and zero-reflection Native AOT support.");
        ConsoleHelper.PrintInfo("Standardized Pagination", "Single-roundtrip ICountedPagedList and ICursorPagedList metadata models.");

        ConsoleHelper.PrintStep("3. Architectural Comparison");
        Console.WriteLine("""
    +------------------------------+--------------------+----------------------------+----------------------+
    | Feature                      | Raw Dapper         | EricksonLopez.DapperExt    | Entity Framework Core|
    +------------------------------+--------------------+----------------------------+----------------------+
    | SQL Control                  | 100% Manual        | 100% Manual (Raw SQL)      | Abstracted (LINQ)    |
    | Performance Overhead         | Minimal            | Minimal (Zero-Allocation)  | Moderate / High      |
    | Native AOT & Trimming        | Partial            | Full ([SqlEntity] Mapper)  | Partial / Complex    |
    | Unit of Work & Savepoints    | Manual Boilerplate | Integrated (IUnitOfWork)   | Integrated (DbContext)|
    | Polly v8 Resilience          | Not Integrated     | Dialect Detectors + ADR-016| Partial (ExecutionSt)|
    | Bulk Insert / Upsert         | O(N) Loop          | Dialect-Native (UNNEST..)  | Paid Package / Ext   |
    | Multi-Map 1:N Deduplication  | Manual Dictionaries| Fluent MultiMapBuilder     | Automatic (Include)  |
    +------------------------------+--------------------+----------------------------+----------------------+
""");

        ConsoleHelper.PrintSuccess("Level 0 completed successfully.");
        return Task.CompletedTask;
    }
}
