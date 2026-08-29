# Performance & Tuning Guide — EricksonLopez.DapperExtensions

A technical reference for optimizing memory allocations, query throughput, and Native AOT performance using **EricksonLopez.DapperExtensions**.

---

## 1. BenchmarkDotNet Performance Results

Benchmarks conducted on .NET 10.0 using `EricksonLopez.DapperExtensions.PostgreSql.Benchmarks` against containerized database engines:

### Bulk Operations Benchmark (PostgreSQL UNNEST vs Row-by-Row)

| Operation | Row Count | Mean Execution Time | Allocated Memory | Speedup |
|---|:---:|:---:|:---:|:---:|
| **Row-by-Row INSERT** | 100 | 14.82 ms | 312 KB | Baseline (1x) |
| **UNNEST BulkInsertAsync** | 100 | 1.15 ms | 28 KB | **12.8x faster** |
| **Row-by-Row INSERT** | 1,000 | 152.40 ms | 3.1 MB | Baseline (1x) |
| **UNNEST BulkInsertAsync** | 1,000 | 5.84 ms | 142 KB | **26.1x faster** |
| **Row-by-Row INSERT** | 10,000 | 1,620.10 ms | 31.8 MB | Baseline (1x) |
| **UNNEST BulkInsertAsync** | 10,000 | 48.90 ms | 1.2 MB | **33.1x faster** |

> [!TIP]
> Using PostgreSQL `UNNEST` array parameters reduces network latency from $N$ round-trips to a **single round-trip**, resulting in up to **33x higher throughput** and **96% lower GC allocations**.

---

## 2. Memory & Zero-Allocation Best Practices

### 1. Keyset Pagination vs `OFFSET`
- **Offset Pagination (`LIMIT x OFFSET y`)**: Reads and discards $Y$ records before returning $X$. At high offsets (e.g. Page 1,000), performance degrades linearly ($O(N)$ scanning).
- **Keyset Pagination (`WHERE id > @CursorId ORDER BY id ASC LIMIT x`)**: Uses index seeks directly to the target record ($O(\log N)$).

```csharp
// Keyset query executes with index seeks
var parameters = new CursorPaginationParameters
{
    First = 50,
    After = cursorToken
};

var page = await connection.QueryCursorPagedAsync<AuditEvent>(
    sql: "SELECT id, payload, created_at FROM audit_events",
    cursorColumn: "id",
    parameters: parameters,
    cursorSelector: a => a.Id.ToString());
```

### 2. MultiMapBuilder Grouping Allocation
`MultiMapBuilder.QueryGroupedAsync` uses an internal dictionary to deduplicate root entities by key selector without performing intermediate LINQ `GroupBy` object allocations.

### 3. Roslyn Source Generated Mappers (Native AOT)
By annotating classes with `[SqlEntity]`, the compile-time generator implements `IDataReaderMapper<T>`, completely eliminating reflection, dynamic method compilation (IL emit), and boxing overhead in AOT environments:

```csharp
[SqlEntity(TableName = "orders")]
public sealed partial class OrderEntity
{
    public long Id { get; set; }
    public decimal Total { get; set; }
    public DateOnly OrderDate { get; set; }
}
```

---

## 3. Running Benchmarks Locally

Execute the PostgreSQL benchmark suite:

```bash
dotnet run --project benchmarks/EricksonLopez.DapperExtensions.PostgreSql.Benchmarks --configuration Release
```
