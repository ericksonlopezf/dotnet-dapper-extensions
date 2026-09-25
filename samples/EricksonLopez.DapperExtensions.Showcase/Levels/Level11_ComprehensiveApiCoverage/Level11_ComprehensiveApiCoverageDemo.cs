// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.MariaDb.TypeHandlers;
using EricksonLopez.DapperExtensions.MultiMap;
using EricksonLopez.DapperExtensions.MySql.TypeHandlers;
using EricksonLopez.DapperExtensions.OpenTelemetry;
using EricksonLopez.DapperExtensions.Oracle.TypeHandlers;
using EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.Sqlite.TypeHandlers;
using EricksonLopez.DapperExtensions.SqlServer.TypeHandlers;
using EricksonLopez.DapperExtensions.Streaming;
using EricksonLopez.DapperExtensions.TypeHandlers;
using EricksonLopez.Resilience;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IL2026, IL3050

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level11_ComprehensiveApiCoverage;

// ─── Showcase-local helpers ────────────────────────────────────────────────────

/// <summary>Entity mapped to JSON columns in dialect-specific type handlers.</summary>
public sealed class ShowcaseJsonData
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

internal sealed class ShowcaseParameterManager : IParameterManager
{
    private readonly Dictionary<string, object?> _params = new();

    public string Add(object? value)
    {
        var name = $"@p{_params.Count}";
        _params[name] = value;
        return name;
    }

    public string AddNamed(string name, object? value)
    {
        _params[name] = value;
        return name;
    }

    public IReadOnlyDictionary<string, object?> GetParameters() => _params;
}

internal sealed class ShowcaseSqlCompiler : ISqlCompiler
{
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Showcase compiler")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Showcase compiler")]
    public SqlResult Compile(ISqlQuery query) => query.Build(this);

    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Showcase compiler")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Showcase compiler")]
    public SqlResult Compile(ISqlQuery query, IParameterManager? existingParameters) => query.Build(this);

    public string Escape(string identifier) => $"\"{identifier}\"";
    public string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
    public IParameterManager CreateParameterManager() => new ShowcaseParameterManager();
    public bool SupportsCapability(ProviderCapability capability) => true;
}

// ─── Level 11 ─────────────────────────────────────────────────────────────────

/// <summary>
/// Level 11: Comprehensive Public API Coverage Verification.
/// <para>
/// This level serves as the living verification matrix for all public surface areas of
/// <c>EricksonLopez.DapperExtensions</c> and its satellite packages. Every method invoked
/// here must correspond to an existing, documented public API — no mocks or imaginary overloads.
/// </para>
/// <para>
/// <strong>Key distinction: two pipeline families in <see cref="SqlResilienceDefaults"/>.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <strong>Polly family</strong> (<c>ResiliencePipeline</c>): <c>Standard()</c>, <c>Aggressive()</c>,
///     <c>Conservative()</c>, <c>ForSqlServer()</c>, etc. — used directly with Polly's
///     <c>pipeline.ExecuteAsync()</c> or the Polly-overloaded resilience extension methods.
///   </description></item>
///   <item><description>
///     <strong>EricksonLopez canonical family</strong> (<c>IResiliencePipeline</c>): <c>StandardPipeline()</c>,
///     <c>AggressivePipeline()</c>, <c>ConservativePipeline()</c>, <c>ForSqlServerPipeline()</c>, etc. —
///     returns <see cref="IResiliencePipeline"/> from <c>EricksonLopez.Resilience</c>, used with the
///     canonical EL overloads of <see cref="SqlResilienceExtensions"/> (ADR-017).
///   </description></item>
/// </list>
/// </summary>
public static class Level11_ComprehensiveApiCoverageDemo
{
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050")]
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(
            11,
            "Comprehensive API Coverage",
            "Living verification of the full public surface: Resilience (both families), TypeHandlers, " +
            "Streaming, MultiMapBuilder, OpenTelemetry options, and DI registration.");

        // ── SECTION A ─────────────────────────────────────────────────────────
        // SqlResilienceDefaults — Polly ResiliencePipeline family (10 provider shortcuts)
        // These return Polly.ResiliencePipeline and are used with Polly's ExecuteAsync or
        // the Polly-overload of SqlResilienceExtensions (SqlResult, ResiliencePipeline).
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("A. SqlResilienceDefaults — Polly ResiliencePipeline (provider shortcuts)");

        var pollyForSqlServer = SqlResilienceDefaults.ForSqlServer();
        var pollyForSqlServerCb = SqlResilienceDefaults.ForSqlServerWithCircuitBreaker();
        var pollyForPostgreSql = SqlResilienceDefaults.ForPostgreSql();
        var pollyForPostgreSqlCb = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreaker();
        var pollyForMySql = SqlResilienceDefaults.ForMySql();
        var pollyForMySqlCb = SqlResilienceDefaults.ForMySqlWithCircuitBreaker();
        var pollyForSqlite = SqlResilienceDefaults.ForSqlite();
        var pollyForSqliteCb = SqlResilienceDefaults.ForSqliteWithCircuitBreaker();
        var pollyForOracle = SqlResilienceDefaults.ForOracle();
        var pollyForOracleCb = SqlResilienceDefaults.ForOracleWithCircuitBreaker();

        ConsoleHelper.PrintInfo("ForSqlServer()           → Polly ResiliencePipeline", pollyForSqlServer.GetType().Name);
        ConsoleHelper.PrintInfo("ForPostgreSql()          → Polly ResiliencePipeline", pollyForPostgreSql.GetType().Name);
        ConsoleHelper.PrintInfo("ForMySql()               → Polly ResiliencePipeline", pollyForMySql.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqlite()              → Polly ResiliencePipeline", pollyForSqlite.GetType().Name);
        ConsoleHelper.PrintInfo("ForOracle()              → Polly ResiliencePipeline", pollyForOracle.GetType().Name);
        ConsoleHelper.PrintSuccess("10 Polly ResiliencePipeline provider shortcuts instantiated.");

        // ── SECTION B ─────────────────────────────────────────────────────────
        // SqlResilienceDefaults — Polly ResiliencePipeline generic family
        // Standard(), Aggressive(), Conservative() — accept a detector, return ResiliencePipeline.
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("B. SqlResilienceDefaults — Polly ResiliencePipeline (generic factory methods)");

        var sqliteDetector = SqliteTransientErrorDetector.Default;
        var pollyStandard = SqlResilienceDefaults.Standard(sqliteDetector);
        var pollyStandardCb = SqlResilienceDefaults.StandardWithCircuitBreaker(sqliteDetector);
        var pollyAggressive = SqlResilienceDefaults.Aggressive(sqliteDetector);
        var pollyConservative = SqlResilienceDefaults.Conservative(sqliteDetector);

        ConsoleHelper.PrintInfo("Standard(detector)                  → Polly ResiliencePipeline", pollyStandard.GetType().Name);
        ConsoleHelper.PrintInfo("StandardWithCircuitBreaker(detector) → Polly ResiliencePipeline", pollyStandardCb.GetType().Name);
        ConsoleHelper.PrintInfo("Aggressive(detector)                 → Polly ResiliencePipeline", pollyAggressive.GetType().Name);
        ConsoleHelper.PrintInfo("Conservative(detector)               → Polly ResiliencePipeline", pollyConservative.GetType().Name);
        ConsoleHelper.PrintSuccess("4 Polly ResiliencePipeline generic factory methods instantiated.");

        // ── SECTION C ─────────────────────────────────────────────────────────
        // SqlResilienceDefaults — EricksonLopez canonical IResiliencePipeline family (14 methods)
        // These return IResiliencePipeline (EricksonLopez.Resilience.Abstractions), wrapping
        // the Polly pipeline via PollyResiliencePipeline. Used with EL-canonical overloads of
        // SqlResilienceExtensions (ADR-017).
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("C. SqlResilienceDefaults — EL IResiliencePipeline family (\"Pipeline\" suffix, 14 methods)");

        IResiliencePipeline elStdPipeline = SqlResilienceDefaults.StandardPipeline(sqliteDetector);
        IResiliencePipeline elStdCbPipeline = SqlResilienceDefaults.StandardWithCircuitBreakerPipeline(sqliteDetector);
        IResiliencePipeline elAggPipeline = SqlResilienceDefaults.AggressivePipeline(sqliteDetector);
        IResiliencePipeline elConsPipeline = SqlResilienceDefaults.ConservativePipeline(sqliteDetector);

        IResiliencePipeline elSqlServerPipeline = SqlResilienceDefaults.ForSqlServerPipeline();
        IResiliencePipeline elSqlServerCbPipeline = SqlResilienceDefaults.ForSqlServerWithCircuitBreakerPipeline();
        IResiliencePipeline elPostgreSqlPipeline = SqlResilienceDefaults.ForPostgreSqlPipeline();
        IResiliencePipeline elPostgreSqlCbPipe = SqlResilienceDefaults.ForPostgreSqlWithCircuitBreakerPipeline();
        IResiliencePipeline elMySqlPipeline = SqlResilienceDefaults.ForMySqlPipeline();
        IResiliencePipeline elMySqlCbPipeline = SqlResilienceDefaults.ForMySqlWithCircuitBreakerPipeline();
        IResiliencePipeline elSqlitePipeline = SqlResilienceDefaults.ForSqlitePipeline();
        IResiliencePipeline elSqliteCbPipeline = SqlResilienceDefaults.ForSqliteWithCircuitBreakerPipeline();
        IResiliencePipeline elOraclePipeline = SqlResilienceDefaults.ForOraclePipeline();
        IResiliencePipeline elOracleCbPipeline = SqlResilienceDefaults.ForOracleWithCircuitBreakerPipeline();

        ConsoleHelper.PrintInfo("StandardPipeline()              → IResiliencePipeline", elStdPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("AggressivePipeline()            → IResiliencePipeline", elAggPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ConservativePipeline()          → IResiliencePipeline", elConsPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqlServerPipeline()          → IResiliencePipeline", elSqlServerPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForPostgreSqlPipeline()         → IResiliencePipeline", elPostgreSqlPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForMySqlPipeline()              → IResiliencePipeline", elMySqlPipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForSqlitePipeline()             → IResiliencePipeline", elSqlitePipeline.GetType().Name);
        ConsoleHelper.PrintInfo("ForOraclePipeline()             → IResiliencePipeline", elOraclePipeline.GetType().Name);
        ConsoleHelper.PrintSuccess("14 EL IResiliencePipeline factory methods instantiated.");

        // ── SECTION D ─────────────────────────────────────────────────────────
        // TypeHandler surface: DateOnlyTypeHandler, TimeOnlyTypeHandler, StringEnumTypeHandler
        // DapperTypeHandlerRegistrar: RegisterStandardHandlers, RegisterStringEnumHandler<T>
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("D. TypeHandlers: DateOnly, TimeOnly, StringEnum & Registrar");

        // DateOnlyTypeHandler
        var sqliteParamDate = new SqliteParameter();
        DateOnlyTypeHandler.Default.SetValue(sqliteParamDate, new DateOnly(2026, 9, 15));
        var parsedDate = DateOnlyTypeHandler.Default.Parse(new DateTime(2026, 9, 15));
        ConsoleHelper.PrintInfo("DateOnlyTypeHandler.SetValue → DbType", sqliteParamDate.DbType);
        ConsoleHelper.PrintInfo("DateOnlyTypeHandler.Parse    → DateOnly", parsedDate);

        // TimeOnlyTypeHandler
        var sqliteParamTime = new SqliteParameter();
        TimeOnlyTypeHandler.Default.SetValue(sqliteParamTime, new TimeOnly(8, 30, 0));
        var parsedTime = TimeOnlyTypeHandler.Default.Parse(new DateTime(2026, 9, 15, 8, 30, 0));
        ConsoleHelper.PrintInfo("TimeOnlyTypeHandler.SetValue → DbType", sqliteParamTime.DbType);
        ConsoleHelper.PrintInfo("TimeOnlyTypeHandler.Parse    → TimeOnly", parsedTime);

        // StringEnumTypeHandler
        var sqliteParamEnum = new SqliteParameter();
        StringEnumTypeHandler<OrderStatus>.Default.SetValue(sqliteParamEnum, OrderStatus.Processing);
        var parsedEnum = StringEnumTypeHandler<OrderStatus>.Default.Parse("Delivered");
        ConsoleHelper.PrintInfo("StringEnumTypeHandler.SetValue → DbType", sqliteParamEnum.DbType);
        ConsoleHelper.PrintInfo("StringEnumTypeHandler.Parse    → OrderStatus", parsedEnum);

        // DapperTypeHandlerRegistrar
        DapperTypeHandlerRegistrar.RegisterStandardHandlers();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<OrderStatus>();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<PaymentMethod>();
        ConsoleHelper.PrintSuccess("DapperTypeHandlerRegistrar: DateOnly, TimeOnly, OrderStatus, PaymentMethod registered.");

        // ── SECTION E ─────────────────────────────────────────────────────────
        // Dialect-specific JSON/JSONB TypeHandler Registrars
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("E. Dialect JSON TypeHandler Registrars (6 dialects)");

        NpgsqlTypeHandlerRegistrar.RegisterJsonbHandler<ShowcaseJsonData>();
        SqlServerTypeHandlerRegistrar.RegisterJsonHandler<ShowcaseJsonData>();
        MySqlTypeHandlerRegistrar.RegisterJsonHandler<ShowcaseJsonData>();
        MariaDbTypeHandlerRegistrar.RegisterJsonHandler<ShowcaseJsonData>();
        OracleTypeHandlerRegistrar.RegisterJsonHandler<ShowcaseJsonData>();
        SqliteTypeHandlerRegistrar.RegisterJsonHandler<ShowcaseJsonData>();

        ConsoleHelper.PrintSuccess("All 6 dialect JSON/JSONB type handler registrars applied.");

        // ── SECTION F ─────────────────────────────────────────────────────────
        // Database operations: StreamAsync, SqlResilienceExtensions (EL canonical overloads),
        // SqlResilienceExtensions (Polly overloads), MultiMapBuilder QueryGrouped
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("F. Database Operations: StreamAsync, SqlResilienceExtensions (both overload families), MultiMapBuilder");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        // StreamAsync<T> — unbuffered IAsyncEnumerable
        int streamCount = 0;
        await foreach (var item in connection.StreamAsync<Product>(
            "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products",
            cancellationToken: CancellationToken.None).ConfigureAwait(false))
        {
            streamCount++;
        }
        ConsoleHelper.PrintInfo("DapperStreamingExtensions.StreamAsync<Product> — rows streamed", streamCount);

        // ExecuteWithResilienceAsync — EL canonical IResiliencePipeline overload
        // SqlResilienceExtensions accepts SqlResult (compiled query), not ISqlQuery.
        var executeResult = new SqlResult(
            "UPDATE products SET stock_quantity = stock_quantity + 0 WHERE id = 1",
            new Dictionary<string, object?>());
        var affectedEl = await connection.ExecuteWithResilienceAsync(executeResult, elSqlitePipeline).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("ExecuteWithResilienceAsync (IResiliencePipeline) — rows affected", affectedEl);

        // ExecuteWithResilienceAsync — Polly ResiliencePipeline overload (distinct type)
        var affectedPolly = await connection.ExecuteWithResilienceAsync(executeResult, pollyForSqlite).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("ExecuteWithResilienceAsync (Polly ResiliencePipeline) — rows affected", affectedPolly);

        // QueryWithResilienceAsync<T> — EL canonical IResiliencePipeline overload
        var productsResult = new SqlResult(
            "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE is_active = 1",
            new Dictionary<string, object?>());
        var productsEl = await connection.QueryWithResilienceAsync<Product>(productsResult, elSqlitePipeline).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("QueryWithResilienceAsync<T> (IResiliencePipeline) — count", System.Linq.Enumerable.Count(productsEl));

        // QueryFirstOrDefaultWithResilienceAsync<T> — EL canonical
        var firstProductResult = new SqlResult(
            "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE id = @id",
            new Dictionary<string, object?> { ["id"] = 1L });
        var firstProduct = await connection.QueryFirstOrDefaultWithResilienceAsync<Product>(firstProductResult, elSqlitePipeline).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("QueryFirstOrDefaultWithResilienceAsync<T> — name", firstProduct?.Name ?? "null");

        // ExecuteScalarWithResilienceAsync<T> — EL canonical
        var countResult = new SqlResult(
            "SELECT COUNT(*) FROM products",
            new Dictionary<string, object?>());
        var countEl = await connection.ExecuteScalarWithResilienceAsync<int>(countResult, elSqlitePipeline).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("ExecuteScalarWithResilienceAsync<T> (IResiliencePipeline) — count", countEl);

        ConsoleHelper.PrintSuccess("SqlResilienceExtensions: both Polly and EL IResiliencePipeline overloads verified.");

        // MultiMapBuilder<TReturn> — QueryGroupedAsync & QueryGroupedFirstOrDefaultAsync
        // Note: QueryGroupedAsync uses the source-generated AOT ReadFromDataReader path.
        // OrderItem has only natively convertible columns (long, string, int, decimal),
        // making it suitable as the root entity for this API surface verification.
        var compiler = new ShowcaseSqlCompiler();
        var multiMapQuery = new RawSqlQuery(
            "SELECT i.id, i.order_id, i.product_id, i.product_name, i.quantity, i.unit_price " +
            "FROM order_items i",
            new Dictionary<string, object?>());

        // Use OrderItem as root; the Map<OrderItem> setter variant demonstrates the API.
        var multiMapBuilder = MultiMapBuilder<OrderItem>.Query(multiMapQuery)
            .Map<OrderItem>("id", setter: (root, related) =>
            {
                // Demonstrate the setter combiner: accumulate quantity across related rows.
                root.Quantity += related.Quantity;
            });

        var groupedList = await multiMapBuilder.QueryGroupedAsync(connection, compiler, i => i.OrderId).ConfigureAwait(false);
        var firstGrouped = await multiMapBuilder.QueryGroupedFirstOrDefaultAsync(connection, compiler, i => i.OrderId).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("MultiMapBuilder.QueryGroupedAsync — groups by OrderId", System.Linq.Enumerable.Count(groupedList));
        ConsoleHelper.PrintInfo("MultiMapBuilder.QueryGroupedFirstOrDefaultAsync — first OrderId", firstGrouped?.OrderId);
        ConsoleHelper.PrintSuccess("MultiMapBuilder<TReturn>: QueryGroupedAsync and QueryGroupedFirstOrDefaultAsync verified.");

        // ── SECTION G ─────────────────────────────────────────────────────────
        // OpenTelemetry: DapperDiagnostics all constants + DapperOpenTelemetryOptions +
        // AddDapperOpenTelemetry DI extension.
        // ─────────────────────────────────────────────────────────────────────
        ConsoleHelper.PrintStep("G. OpenTelemetry: DapperDiagnostics constants, Meter instruments, and DI registration");

        // ActivitySource & Meter metadata
        ConsoleHelper.PrintInfo("DapperDiagnostics.SourceName", DapperDiagnostics.SourceName);
        ConsoleHelper.PrintInfo("DapperDiagnostics.Version", DapperDiagnostics.Version);
        ConsoleHelper.PrintInfo("DapperDiagnostics.ActivitySource.Name", DapperDiagnostics.ActivitySource.Name);

        // All Meter instruments
        ConsoleHelper.PrintInfo("CommandDurationHistogram.Name", DapperDiagnostics.CommandDurationHistogram.Name);
        ConsoleHelper.PrintInfo("CommandExecutionsCounter.Name", DapperDiagnostics.CommandExecutionsCounter.Name);
        ConsoleHelper.PrintInfo("BulkRowsCounter.Name", DapperDiagnostics.BulkRowsCounter.Name);
        ConsoleHelper.PrintInfo("ResilienceRetriesCounter.Name", DapperDiagnostics.ResilienceRetriesCounter.Name);

        // Semantic convention tag constants
        ConsoleHelper.PrintInfo("TagDbSystem", DapperDiagnostics.TagDbSystem);
        ConsoleHelper.PrintInfo("TagDbName", DapperDiagnostics.TagDbName);
        ConsoleHelper.PrintInfo("TagDbStatement", DapperDiagnostics.TagDbStatement);
        ConsoleHelper.PrintInfo("TagDbOperation", DapperDiagnostics.TagDbOperation);
        ConsoleHelper.PrintInfo("TagDbRowsAffected", DapperDiagnostics.TagDbRowsAffected);
        ConsoleHelper.PrintInfo("TagServerAddress", DapperDiagnostics.TagServerAddress);
        ConsoleHelper.PrintInfo("TagErrorType", DapperDiagnostics.TagErrorType);

        // DapperOpenTelemetryOptions properties
        var otelOptions = new DapperOpenTelemetryOptions();
        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.CaptureSqlStatements (default)", otelOptions.CaptureSqlStatements);
        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.EnableMetrics (default)", otelOptions.EnableMetrics);
        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.MaxStatementLength (default)", otelOptions.MaxStatementLength);

        // AddDapperOpenTelemetry DI extension
        var services = new ServiceCollection();
        services.AddDapperOpenTelemetry(opt =>
        {
            opt.CaptureSqlStatements = true;
            opt.EnableMetrics = true;
            opt.MaxStatementLength = 2048;
        });
        var sp = services.BuildServiceProvider();
        var registeredOtelOptions = sp.GetRequiredService<DapperOpenTelemetryOptions>();
        ConsoleHelper.PrintInfo("AddDapperOpenTelemetry — MaxStatementLength registered", registeredOtelOptions.MaxStatementLength);
        ConsoleHelper.PrintSuccess("OpenTelemetry API surface: all 7 DapperDiagnostics instruments/constants, DapperOpenTelemetryOptions, and AddDapperOpenTelemetry verified.");

        ConsoleHelper.PrintSuccess("Level 11 — Comprehensive API Coverage completed successfully.");
    }
}

#pragma warning restore IL2026, IL3050
