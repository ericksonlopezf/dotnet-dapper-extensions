# API Reference — EricksonLopez.DapperExtensions

Formal Microsoft Learn-style technical documentation for all public classes, interfaces, records, structs, extension methods, and configuration options across the **EricksonLopez.DapperExtensions** ecosystem.

---

## 1. Public API Inventory

| Name | Namespace | Responsibility | Dependencies | Complexity | Showcase Reference |
|---|---|---|---|:---:|:---:|
| `SqlEntityAttribute` | `EricksonLopez.DapperExtensions` | Metadata attribute for Roslyn compile-time AOT generation | `System.Attribute` | Basic | Level 01, 07 |
| `IDataReaderMapper<T>` | `EricksonLopez.DapperExtensions.MultiMap` | Contract for zero-reflection AOT data reader hydration | `System.Data.IDataReader` | Advanced | Level 07, 08 |
| `MultiMapDescriptor` | `EricksonLopez.DapperExtensions.MultiMap` | Descriptor metadata for multi-entity hydration | `System.Type`, `IDataReader` | Advanced | Level 07 |
| `MultiMapBuilder<TReturn>` | `EricksonLopez.DapperExtensions.MultiMap` | Fluent builder for multi-entity mapping and 1:N root deduplication | `Dapper`, `IDbConnection` | Advanced | Level 04, 07 |
| `IUnitOfWork` | `EricksonLopez.DapperExtensions.UnitOfWork` | Unit of Work contract with async lifecycle and deterministic rollback | `IDbTransaction`, `IAsyncDisposable` | Intermediate | Level 04, 06, 10 |
| `ISavepoint` | `EricksonLopez.DapperExtensions.UnitOfWork` | Standalone savepoint representation for partial rollbacks | `System.Threading.Tasks` | Advanced | Level 04, 06 |
| `UnitOfWorkExtensions` | `EricksonLopez.DapperExtensions.UnitOfWork` | `BeginUnitOfWorkAsync` and `WithUnitOfWorkAsync` extensions | `IDbConnection`, `IUnitOfWork` | Basic | Level 04, 06, 10 |
| `ISqlTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient fault classification contract | `System.Exception` | Intermediate | Level 06, 08 |
| `PostgreSqlTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient error classifier for PostgreSQL (SQLSTATE) | `ISqlTransientErrorDetector` | Intermediate | Level 02, 06 |
| `SqlServerTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient error classifier for SQL Server (Error Numbers) | `ISqlTransientErrorDetector` | Intermediate | Level 02, 06 |
| `MySqlTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient error classifier for MySQL / MariaDB | `ISqlTransientErrorDetector` | Intermediate | Level 02, 06 |
| `OracleTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient error classifier for Oracle DB (ORA- codes) | `ISqlTransientErrorDetector` | Intermediate | Level 02, 06 |
| `SqliteTransientErrorDetector` | `EricksonLopez.DapperExtensions.Resilience` | Transient error classifier for SQLite (`SQLITE_BUSY`/`LOCKED`) | `ISqlTransientErrorDetector` | Intermediate | Level 02, 06 |
| `SqlResilienceDefaults` (Polly family) | `EricksonLopez.DapperExtensions.Resilience` | Factory returning `Polly.ResiliencePipeline`: `Standard()`, `Aggressive()`, `Conservative()`, `ForSqlServer()`, etc. (14 methods without suffix) | `Polly`, `ISqlTransientErrorDetector` | Intermediate | Level 06, 10, 11 |
| `SqlResilienceDefaults` (EL canonical family) | `EricksonLopez.DapperExtensions.Resilience` | Factory returning `IResiliencePipeline`: `StandardPipeline()`, `AggressivePipeline()`, `ForSqlServerPipeline()`, etc. (14 methods with `Pipeline` suffix) — ADR-017 | `EricksonLopez.Resilience`, `ISqlTransientErrorDetector` | Intermediate | Level 11 |
| `SqlResilienceExtensions` | `EricksonLopez.DapperExtensions.Resilience` | Resilient query and execution extension methods with two overload families: EL `IResiliencePipeline` (canonical, ADR-017) and Polly `ResiliencePipeline` (compat) | `IDbConnection`, `IResiliencePipeline`, `ResiliencePipeline` | Intermediate | Level 06, 11 |
| `SavepointResilienceExtensions` | `EricksonLopez.DapperExtensions.Resilience` | `ExecuteInSavepointWithRetryAsync` extensions (ADR-014) | `IUnitOfWork`, `ResiliencePipeline` | Advanced | Level 06 |
| `DapperTypeHandlerRegistrar` | `EricksonLopez.DapperExtensions.TypeHandlers` | Global Dapper type handler registration utility | `Dapper.SqlMapper` | Basic | Level 02 |
| `DateOnlyTypeHandler` | `EricksonLopez.DapperExtensions.TypeHandlers` | Type handler for `System.DateOnly` | `SqlMapper.TypeHandler<DateOnly>` | Basic | Level 01, 02 |
| `TimeOnlyTypeHandler` | `EricksonLopez.DapperExtensions.TypeHandlers` | Type handler for `System.TimeOnly` | `SqlMapper.TypeHandler<TimeOnly>` | Basic | Level 02 |
| `StringEnumTypeHandler<TEnum>` | `EricksonLopez.DapperExtensions.TypeHandlers` | Type handler for string-mapped enums | `SqlMapper.TypeHandler<TEnum>` | Basic | Level 02 |
| `DapperExtensionsOptions` | `EricksonLopez.DapperExtensions.DependencyInjection` | Configuration options for Dependency Injection | N/A | Basic | Level 01, 02 |
| `DapperExtensionsServiceCollectionExtensions` | `EricksonLopez.DapperExtensions.DependencyInjection` | `IServiceCollection` extensions (`AddDapperExtensions`, etc.) | `IServiceCollection` | Basic | Level 01, 02 |
| `DapperHealthCheckOptions` | `EricksonLopez.DapperExtensions.HealthChecks` | Configuration options for database health checks | N/A | Basic | Level 09 |
| `DapperHealthCheck` | `EricksonLopez.DapperExtensions.HealthChecks` | ASP.NET Core `IHealthCheck` implementation for database probes | `IHealthCheck`, `IDbConnection` | Intermediate | Level 09 |
| `DapperHealthChecksBuilderExtensions` | `EricksonLopez.DapperExtensions.HealthChecks` | `IHealthChecksBuilder` registration extensions | `IHealthChecksBuilder` | Basic | Level 09 |
| `DapperDiagnostics` | `EricksonLopez.DapperExtensions.OpenTelemetry` | Constants, `ActivitySource`, `Meter`, and 4 Metrics Instruments (`CommandDurationHistogram`, `CommandExecutionsCounter`, `BulkRowsCounter`, `ResilienceRetriesCounter`) plus 7 semantic tag constants | `System.Diagnostics` | Intermediate | Level 09, 11 |
| `DapperOpenTelemetryOptions` | `EricksonLopez.DapperExtensions.OpenTelemetry` | Configuration options: `CaptureSqlStatements` (default true), `EnableMetrics` (default true), `MaxStatementLength` (default 4096) | N/A | Basic | Level 09, 11 |
| `DapperOpenTelemetryServiceCollectionExtensions` | `EricksonLopez.DapperExtensions.OpenTelemetry` | `IServiceCollection` extension `AddDapperOpenTelemetry(services, configure?)` | `IServiceCollection` | Basic | Level 09, 11 |
| `OpenTelemetryDbConnectionExtensions` | `EricksonLopez.DapperExtensions.OpenTelemetry` | `QueryWithTelemetryAsync`, `ExecuteWithTelemetryAsync`, `TraceBulkOperationAsync` | `IDbConnection`, `ActivitySource` | Intermediate | Level 09 |
| `BulkParameters<T>` / `BulkParameters` | `EricksonLopez.DapperExtensions.PostgreSql.Bulk` | Typed PostgreSQL `UNNEST` array parameters builder | `NpgsqlTypes.NpgsqlDbType` | Advanced | Level 05 |
| `BulkDataTableBuilder<T>` / `BulkDataTableBuilder` | `EricksonLopez.DapperExtensions.SqlServer.Bulk` | Typed `DataTable` builder for SQL Server `SqlBulkCopy` | `System.Data.DataTable` | Advanced | Level 05 |
| `BulkBuilder<T>` / `BulkBuilder` | `EricksonLopez.DapperExtensions.(MySql/MariaDb/Oracle/Sqlite).Bulk` | Fluent batch command builder | `Dapper.DynamicParameters` | Intermediate | Level 05 |
| `PagedQueryExtensions` | `EricksonLopez.DapperExtensions.(Dialect).Pagination` | `QueryPagedAsync`, `QueryPagedMultipleAsync`, `QueryCursorPagedAsync` | `ICountedPagedList`, `ICursorPagedList` | Intermediate | Level 03 |
| `TransactionExtensions` | `EricksonLopez.DapperExtensions.(Dialect).Transactions` | `ExecuteInTransactionAsync`, `ExecuteInTransactionAsync<TResult>` | `System.Data.Common.DbTransaction` | Basic | Level 04 |
| `JsonTypeHandler<T>` / `JsonbTypeHandler<T>` | `EricksonLopez.DapperExtensions.(Dialect).TypeHandlers` | Dapper type handlers for JSON and JSONB columns | `System.Text.Json` | Intermediate | Level 02, 11 |
| `*TypeHandlerRegistrar` (6 Dialects) | `EricksonLopez.DapperExtensions.(Dialect).TypeHandlers` | Registration helpers for dialect JSON / JSONB handlers | `Dapper.SqlMapper` | Basic | Level 02, 11 |
| `DapperStreamingExtensions` | `EricksonLopez.DapperExtensions.Streaming` | `StreamAsync<T>(connection, sql, ...)` and `StreamAsync<T>(connection, CommandDefinition, ...)` — unbuffered `IAsyncEnumerable<T>` streaming. Reflection-based; not fully AOT-safe | `IDbConnection`, `Dapper` | Intermediate | Level 07, 11 |

---

## 2. Core Package (`EricksonLopez.DapperExtensions`)

### `SqlEntityAttribute`
Identifies an entity class or struct for compile-time mapper generation by `EricksonLopez.DapperExtensions.SourceGenerators`.

> **Generated output:** The source generator adds two **static** methods to the annotated `partial` class:
> - `public static T ReadFromDataReader(IDataReader reader)` — maps a row by ordinal.
> - `public static Func<IDataReader, object> GetMultiMapReaderFactory()` — factory for `MultiMapBuilder<T>`.
>
> These methods are **not** an implementation of `IDataReaderMapper<T>`. That interface is a separate, manually-implemented contract.

```csharp
namespace EricksonLopez.DapperExtensions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SqlEntityAttribute : Attribute
{
    public string? TableName { get; set; }
}
```

---

### `IDataReaderMapper<T>`
Provides a zero-allocation, reflection-free contract for hydrating instances of `T` directly from an active `IDataReader`.

```csharp
namespace EricksonLopez.DapperExtensions.MultiMap;

public interface IDataReaderMapper<T>
{
    T Map(IDataReader reader);
}
```

---

### `MultiMapBuilder<TReturn>`
Provides a fluent builder for configuring and executing multi-mapping queries with Native AOT support.

```csharp
namespace EricksonLopez.DapperExtensions.MultiMap;

public sealed class MultiMapBuilder<TReturn> where TReturn : class, new()
{
    public static MultiMapBuilder<TReturn> Query(ISqlQuery query);

    public MultiMapBuilder<TReturn> Map<T>(
        string splitOn,
        Func<TReturn, T, TReturn> combiner,
        Func<IDataReader, object>? parser = null);

    public MultiMapBuilder<TReturn> Map<T>(
        string splitOn,
        Action<TReturn, T> setter,
        Func<IDataReader, object>? parser = null);

    public string SplitOn { get; }
    public Type[] Types { get; }

    public Task<IEnumerable<TReturn>> QueryAsync(
        IDbConnection connection,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public Task<IEnumerable<TReturn>> QueryGroupedAsync<TKey>(
        IDbConnection connection,
        ISqlCompiler compiler,
        Func<TReturn, TKey> keySelector,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    public Task<TReturn?> QueryFirstOrDefaultAsync(
        IDbConnection connection,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public Task<TReturn?> QueryGroupedFirstOrDefaultAsync<TKey>(
        IDbConnection connection,
        ISqlCompiler compiler,
        Func<TReturn, TKey> keySelector,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull;
}
```

---

### `MultiMapDescriptor`
Describes how to split and hydrate an entity type in a multi-mapping query result. Provides an extensibility surface for dynamic introspection.

```csharp
namespace EricksonLopez.DapperExtensions.MultiMap;

public sealed class MultiMapDescriptor
{
    public Type EntityType { get; }
    public string TableName { get; }
    public string[] ColumnNames { get; }
    public Func<IDataReader, object> ReaderFactory { get; }

    public MultiMapDescriptor(
        Type entityType,
        string tableName,
        string[] columnNames,
        Func<IDataReader, object> readerFactory);
}
```

---

### `DapperStreamingExtensions`
Provides low-allocation unbuffered asynchronous row streaming (`IAsyncEnumerable<T>`) directly over an open database connection.

> [!NOTE]
> `StreamAsync<T>` relies on Dapper's internal reflection parser. For strict Native AOT environments, prefer hydrating via `IDataReaderMapper<T>` or `[SqlEntity]` generated `ReadFromDataReader`.

```csharp
namespace EricksonLopez.DapperExtensions.Streaming;

public static class DapperStreamingExtensions
{
    public static IAsyncEnumerable<T> StreamAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);
}
```

---

## 3. Unit of Work Subsystem (`EricksonLopez.DapperExtensions.UnitOfWork`)

### `IUnitOfWork` & `ISavepoint`
```csharp
namespace EricksonLopez.DapperExtensions.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbTransaction Transaction { get; }
    IsolationLevel IsolationLevel { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
}

public interface ISavepoint
{
    string Name { get; }
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
```

### `UnitOfWorkExtensions`
```csharp
namespace EricksonLopez.DapperExtensions.UnitOfWork;

public static class UnitOfWorkExtensions
{
    public static Task<IUnitOfWork> BeginUnitOfWorkAsync(
        this IDbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    public static Task<IUnitOfWork> BeginUnitOfWorkAsync(
        this DbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    public static Task WithUnitOfWorkAsync(
        this IDbConnection connection,
        Func<IUnitOfWork, CancellationToken, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    public static Task<TResult> WithUnitOfWorkAsync<TResult>(
        this IDbConnection connection,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
```

---

## 4. Resilience Subsystem (`EricksonLopez.DapperExtensions.Resilience`)

### `ISqlTransientErrorDetector`
```csharp
namespace EricksonLopez.DapperExtensions.Resilience;

public interface ISqlTransientErrorDetector
{
    bool IsTransient(Exception exception);
}
```

### Pre-Configured Detectors
- `PostgreSqlTransientErrorDetector.Default`
- `SqlServerTransientErrorDetector.Default`
- `MySqlTransientErrorDetector.Default`
- `SqliteTransientErrorDetector.Default`
- `OracleTransientErrorDetector.Default`

### `SqlResilienceDefaults`
```csharp
namespace EricksonLopez.DapperExtensions.Resilience;

public static class SqlResilienceDefaults
{
    public static ResiliencePipeline Standard(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null);
    public static ResiliencePipeline StandardWithCircuitBreaker(
        ISqlTransientErrorDetector detector,
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null);
    public static ResiliencePipeline Aggressive(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null);
    public static ResiliencePipeline Conservative(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null);

    public static ResiliencePipeline<T> Standard<T>(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null);
    public static ResiliencePipeline<T> StandardWithCircuitBreaker<T>(
        ISqlTransientErrorDetector detector,
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null);

    // Provider shortcuts (Polly overloads — compatible, not deprecated per ADR-017)
    public static ResiliencePipeline ForSqlServer(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForSqlServerWithCircuitBreaker(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForPostgreSql(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForPostgreSqlWithCircuitBreaker(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForMySql(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForMySqlWithCircuitBreaker(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForSqlite(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForSqliteWithCircuitBreaker(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForOracle(TimeProvider? timeProvider = null);
    public static ResiliencePipeline ForOracleWithCircuitBreaker(TimeProvider? timeProvider = null);

    // ─── Canonical IResiliencePipeline overloads (ADR-017 preferred API) ───────────
    // These return IResiliencePipeline from EricksonLopez.Resilience.Abstractions
    // and are the recommended API for all new consumers.
    public static IResiliencePipeline StandardPipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-standard",
        TimeProvider? timeProvider = null);
    public static IResiliencePipeline StandardWithCircuitBreakerPipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-standard-cb",
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null,
        TimeProvider? timeProvider = null);
    public static IResiliencePipeline AggressivePipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-aggressive",
        TimeProvider? timeProvider = null);
    public static IResiliencePipeline ConservativePipeline(
        ISqlTransientErrorDetector detector,
        string pipelineName = "sql-conservative",
        TimeProvider? timeProvider = null);

    // Provider shortcut IResiliencePipeline factory methods (ADR-017 canonical)
    public static IResiliencePipeline ForSqlServerPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForSqlServerWithCircuitBreakerPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForPostgreSqlPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForPostgreSqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForMySqlPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForMySqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForSqlitePipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForSqliteWithCircuitBreakerPipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForOraclePipeline(TimeProvider? timeProvider = null);
    public static IResiliencePipeline ForOracleWithCircuitBreakerPipeline(TimeProvider? timeProvider = null);
}
```

> **ADR-017 Note:** The `*Pipeline()` methods returning `IResiliencePipeline` are the **canonical preferred API**
> for all new consumers. The `ResiliencePipeline` (Polly) overloads are retained as first-class compatibility
> APIs and are **not** marked `[Obsolete]`.

### `SqlResilienceExtensions`
```csharp
namespace EricksonLopez.DapperExtensions.Resilience;

public static class SqlResilienceExtensions
{
    public static Task<int> ExecuteWithResilienceAsync(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<IEnumerable<T>> QueryWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T> QuerySingleWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> QuerySingleOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T> QueryFirstWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> QueryFirstOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> ExecuteScalarWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    // ─── Canonical IResiliencePipeline overloads (ADR-017 preferred API) ───────────
    public static Task<int> ExecuteWithResilienceAsync(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<IEnumerable<T>> QueryWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T> QuerySingleWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> QuerySingleOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T> QueryFirstWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> QueryFirstOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    public static Task<T?> ExecuteScalarWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
```

### `SavepointResilienceExtensions` (ADR-014)
```csharp
namespace EricksonLopez.DapperExtensions.Resilience;

public static class SavepointResilienceExtensions
{
    public static Task ExecuteInSavepointWithRetryAsync(
        this IUnitOfWork unitOfWork,
        ResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default);

    public static Task<TResult> ExecuteInSavepointWithRetryAsync<TResult>(
        this IUnitOfWork unitOfWork,
        ResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default);

    // ─── Canonical IResiliencePipeline overloads (ADR-017 preferred API) ───────────
    public static Task ExecuteInSavepointWithRetryAsync(
        this IUnitOfWork unitOfWork,
        IResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default);

    public static Task<TResult> ExecuteInSavepointWithRetryAsync<TResult>(
        this IUnitOfWork unitOfWork,
        IResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default);
}
```

---

## 4b. Streaming (`EricksonLopez.DapperExtensions.Streaming`)

### `DapperStreamingExtensions`

Provides unbuffered, memory-efficient `IAsyncEnumerable<T>` streaming for Dapper queries. Instead of buffering the full result set into a `List<T>`, rows are streamed off the wire sequentially.

> ⚠️ **Native AOT Compatibility:** `StreamAsync<T>` uses `IDataReader.GetRowParser<T>()` from Dapper internally,
> which is a **reflection-based** code path. This means it is **not fully Native AOT compatible** and may produce
> trimming warnings (`IL2026`, `IL3050`) in trimmed/AOT-published builds.
> For Native AOT, use `MultiMapBuilder<T>` with source-generated or manually implemented `IDataReaderMapper<T>` instead.
> See ADR-006 and ADR-019 for details.

```csharp
namespace EricksonLopez.DapperExtensions.Streaming;

public static class DapperStreamingExtensions
{
    // Overload 1: SQL string + parameters
    public static IAsyncEnumerable<T> StreamAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    // Overload 2: Compiled SqlResult (EricksonLopez.SqlBuilder.Abstractions)
    public static IAsyncEnumerable<T> StreamAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

**Usage example:**
```csharp
await foreach (var order in connection.StreamAsync<OrderDto>(
    "SELECT id, status FROM orders WHERE created_at > @Since",
    new { Since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)) },
    cancellationToken: cancellationToken))
{
    await processor.HandleAsync(order, cancellationToken);
}
```

---

## 5. Dependency Injection (`EricksonLopez.DapperExtensions.DependencyInjection`)


```csharp
namespace EricksonLopez.DapperExtensions.DependencyInjection;

public sealed class DapperExtensionsOptions
{
    public bool RegisterStandardTypeHandlers { get; set; } = true;
    public bool RegisterTransientErrorDetectors { get; set; } = true;
}

public static class DapperExtensionsServiceCollectionExtensions
{
    public static IServiceCollection AddDapperExtensions(
        this IServiceCollection services,
        Action<DapperExtensionsOptions>? configure = null);

    public static IServiceCollection AddDapperTypeHandlers(this IServiceCollection services);

    public static IServiceCollection AddDapperTransientErrorDetectors(this IServiceCollection services);
}
```

---

## 6. Observability (`EricksonLopez.DapperExtensions.OpenTelemetry`)

```csharp
namespace EricksonLopez.DapperExtensions.OpenTelemetry;

public static class DapperDiagnostics
{
    public const string SourceName = "EricksonLopez.DapperExtensions";
    public const string Version = "2.0.0";

    public static readonly ActivitySource ActivitySource;
    public static readonly Meter Meter;
    public static readonly Histogram<double> CommandDurationHistogram;
    public static readonly Counter<long> CommandExecutionsCounter;
    public static readonly Counter<long> BulkRowsCounter;
    public static readonly Counter<long> ResilienceRetriesCounter;

    public const string TagDbSystem = "db.system";
    public const string TagDbName = "db.name";
    public const string TagDbStatement = "db.statement";
    public const string TagDbOperation = "db.operation";
    public const string TagDbRowsAffected = "db.rows_affected";
    public const string TagServerAddress = "server.address";
    public const string TagErrorType = "error.type";
}

public sealed class DapperOpenTelemetryOptions
{
    public bool CaptureSqlStatements { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public int MaxStatementLength { get; set; } = 4096;
}

public static class DapperOpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddDapperOpenTelemetry(
        this IServiceCollection services,
        Action<DapperOpenTelemetryOptions>? configure = null);
}

public static class OpenTelemetryDbConnectionExtensions
{
    public static Task<int> ExecuteWithTelemetryAsync(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    public static Task<IEnumerable<T>> QueryWithTelemetryAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    public static Task<int> TraceBulkOperationAsync(
        this IDbConnection connection,
        string operationName,
        string targetTable,
        Func<CancellationToken, Task<int>> bulkAction,
        CancellationToken cancellationToken = default);
}
```

---

## 7. Health Checks (`EricksonLopez.DapperExtensions.HealthChecks`)

```csharp
namespace EricksonLopez.DapperExtensions.HealthChecks;

public sealed class DapperHealthCheckOptions
{
    public string CommandText { get; set; } = "SELECT 1;";
    public TimeSpan DegradedThreshold { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed class DapperHealthCheck : IHealthCheck
{
    public DapperHealthCheck(
        Func<CancellationToken, Task<IDbConnection>> connectionFactory,
        DapperHealthCheckOptions? options = null,
        TimeProvider? timeProvider = null);

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default);
}

public static class DapperHealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);

    public static IHealthChecksBuilder AddPostgreSqlDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);

    public static IHealthChecksBuilder AddSqlServerDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);

    public static IHealthChecksBuilder AddOracleDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);

    public static IHealthChecksBuilder AddMySqlDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);

    public static IHealthChecksBuilder AddSqliteDapperHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> connectionFactory,
        Action<DapperHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null);
}
```

---

## 8. Provider Bulk Operations

### PostgreSQL (`EricksonLopez.DapperExtensions.PostgreSql.Bulk`)
```csharp
namespace EricksonLopez.DapperExtensions.PostgreSql.Bulk;

public static class BulkParameters
{
    public static BulkParameters<T> From<T>(IEnumerable<T> items);
}

public sealed class BulkParameters<T>
{
    public int Count { get; }
    public BulkParameters<T> Add<TValue>(string parameterName, Func<T, TValue> selector, NpgsqlDbType dbType);
    public NpgsqlParameter[] Build();
}

public static class BulkExtensions
{
    public static Task<int> BulkInsertAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkUpsertAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkDeleteAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkUpdateAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

### SQL Server (`EricksonLopez.DapperExtensions.SqlServer.Bulk`)
```csharp
namespace EricksonLopez.DapperExtensions.SqlServer.Bulk;

public static class BulkDataTableBuilder
{
    public static BulkDataTableBuilder<T> From<T>(IEnumerable<T> items);
}

public sealed class BulkDataTableBuilder<T>
{
    public int Count { get; }
    public BulkDataTableBuilder<T> Column<TValue>(string columnName, Func<T, TValue> selector);
    public DataTable Build();
}

public static class BulkExtensions
{
    public static Task<int> BulkInsertAsync(
        this DbConnection connection,
        string destinationTableName,
        DataTable dataTable,
        DbTransaction? transaction = null,
        int batchSize = 0,
        int bulkCopyTimeout = 30,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkUpdateAsync(
        this DbConnection connection,
        string sql,
        object? param = null,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkDeleteAsync(
        this DbConnection connection,
        string sql,
        object? param = null,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

### SQLite / MySQL / MariaDB / Oracle (`EricksonLopez.DapperExtensions.(Provider).Bulk`)
```csharp
namespace EricksonLopez.DapperExtensions.Sqlite.Bulk;

public static class BulkBuilder
{
    public static BulkBuilder<T> From<T>(IEnumerable<T> items);
}

public sealed class BulkBuilder<T>
{
    public int Count { get; }
    public BulkBuilder<T> Table(string tableName);
    public BulkBuilder<T> Column<TValue>(string columnName, Func<T, TValue> selector);
    public (string Sql, DynamicParameters Parameters) Build();
}

public static class BulkExtensions
{
    public static Task<int> BulkInsertAsync(
        this IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkUpsertAsync(
        this IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkUpdateAsync(
        this IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    public static Task<int> BulkDeleteAsync(
        this IDbConnection connection,
        string sql,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

---

## 9. Provider Pagination & Transactions

### Pagination (`PagedQueryExtensions`)
```csharp
public static class PagedQueryExtensions
{
    public static Task<ICountedPagedList<T>> QueryPagedAsync<T>(
        this IDbConnection connection,
        string sql,
        string countSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);

    public static Task<ICountedPagedList<T>> QueryPagedMultipleAsync<T>(
        this IDbConnection connection,
        string sql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);

    public static Task<ICursorPagedList<T>> QueryCursorPagedAsync<T>(
        this IDbConnection connection,
        string sql,
        string cursorColumn,
        CursorPaginationParameters parameters,
        Func<T, string> cursorSelector,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null);
}
```

### Transactions (`TransactionExtensions`)
```csharp
public static class TransactionExtensions
{
    public static Task ExecuteInTransactionAsync(
        this DbConnection connection,
        Func<DbTransaction, Task> operation,
        CancellationToken cancellationToken = default);

    public static Task<TResult> ExecuteInTransactionAsync<TResult>(
        this DbConnection connection,
        Func<DbTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
```

---

## 10. Type Handlers Subsystem (`EricksonLopez.DapperExtensions.TypeHandlers` & Dialects)

### `DapperTypeHandlerRegistrar`
Provides global registration utilities for standard modern .NET date/time types and string-serialized enums with Dapper's `SqlMapper`.

```csharp
namespace EricksonLopez.DapperExtensions.TypeHandlers;

public static class DapperTypeHandlerRegistrar
{
    public static void RegisterStandardHandlers();
    public static void RegisterStringEnumHandler<TEnum>() where TEnum : struct, Enum;
}
```

#### Methods

##### `RegisterStandardHandlers()`
Registers `DateOnlyTypeHandler.Default` and `TimeOnlyTypeHandler.Default` globally in `SqlMapper`.
- **Exceptions**: None.
- **Remarks**: Call once during application startup (or use `AddDapperExtensions()` which registers them automatically when `RegisterDefaultTypeHandlers = true`).

##### `RegisterStringEnumHandler<TEnum>()`
Registers `StringEnumTypeHandler<TEnum>.Default` globally in `SqlMapper` to persist and hydrate enum values as text strings.
- **Type Parameters**: `TEnum` - The enumeration struct type.
- **Exceptions**: None.

---

### `DateOnlyTypeHandler` & `TimeOnlyTypeHandler`
High-performance Dapper type handlers for modern .NET date and time primitives.

```csharp
namespace EricksonLopez.DapperExtensions.TypeHandlers;

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public static readonly DateOnlyTypeHandler Default;
    public override void SetValue(IDbDataParameter parameter, DateOnly value);
    public override DateOnly Parse(object value);
}

public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public static readonly TimeOnlyTypeHandler Default;
    public override void SetValue(IDbDataParameter parameter, TimeOnly value);
    public override TimeOnly Parse(object value);
}
```

---

### `StringEnumTypeHandler<TEnum>`
Generic Dapper type handler that serializes enum members as strings into `VARCHAR`/`TEXT` columns and parses string column values back to enum instances using case-insensitive parsing.

```csharp
namespace EricksonLopez.DapperExtensions.TypeHandlers;

public sealed class StringEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
{
    public static readonly StringEnumTypeHandler<TEnum> Default;
    public override void SetValue(IDbDataParameter parameter, TEnum value);
    public override TEnum Parse(object value);
}
```

---

### Dialect TypeHandler Registrars & JSON Handlers

Each dialect provides a dedicated registrar and `ITypeHandler` for mapping complex object graphs to JSON/JSONB database columns via `System.Text.Json`:

| Dialect | Registrar Type | Method | Handler Type | Target Column Type |
|---|---|---|---|---|
| **PostgreSQL** | `NpgsqlTypeHandlerRegistrar` | `RegisterJsonbHandler<T>()` | `JsonbTypeHandler<T>` | `jsonb` |
| **SQL Server** | `SqlServerTypeHandlerRegistrar` | `RegisterJsonHandler<T>()` | `JsonTypeHandler<T>` | `NVARCHAR(MAX)` |
| **MySQL** | `MySqlTypeHandlerRegistrar` | `RegisterJsonHandler<T>()` | `JsonTypeHandler<T>` | `json` |
| **MariaDB** | `MariaDbTypeHandlerRegistrar` | `RegisterJsonHandler<T>()` | `JsonTypeHandler<T>` | `longtext` / `json` |
| **Oracle** | `OracleTypeHandlerRegistrar` | `RegisterJsonHandler<T>()` | `JsonTypeHandler<T>` | `json` / `CLOB` |
| **SQLite** | `SqliteTypeHandlerRegistrar` | `RegisterJsonHandler<T>()` | `JsonTypeHandler<T>` | `TEXT` |

```csharp
// Example: Registering JSON handlers for custom document types
NpgsqlTypeHandlerRegistrar.RegisterJsonbHandler<UserProfileMetadata>();
SqlServerTypeHandlerRegistrar.RegisterJsonHandler<AuditLogPayload>();
SqliteTypeHandlerRegistrar.RegisterJsonHandler<OrderConfiguration>();
```

