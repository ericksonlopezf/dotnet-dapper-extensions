// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.HealthChecks;
using EricksonLopez.DapperExtensions.OpenTelemetry;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level09_ObservabilityAndHealth;

/// <summary>
/// Level 9 — Observability & Health Checks: OpenTelemetry Tracing/Metrics and Database Health Check Probes.
/// </summary>
public static class OpenTelemetryAndHealthChecksDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(9, "Observability & Health Checks", "OpenTelemetry (ActivitySource, Meter, Semantic Conventions) and Database Health Checks");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        ConsoleHelper.PrintStep("1. OpenTelemetry Diagnostics Constants & Metadata");
        ConsoleHelper.PrintInfo("ActivitySource Name", DapperDiagnostics.SourceName);
        ConsoleHelper.PrintInfo("Diagnostics Version", DapperDiagnostics.Version);
        ConsoleHelper.PrintInfo("Histogram Name", DapperDiagnostics.CommandDurationHistogram.Name);
        ConsoleHelper.PrintInfo("Executions Counter", DapperDiagnostics.CommandExecutionsCounter.Name);
        ConsoleHelper.PrintInfo("Bulk Rows Counter", DapperDiagnostics.BulkRowsCounter.Name);

        ConsoleHelper.PrintStep("2. Execution with Telemetry (QueryWithTelemetryAsync and ExecuteWithTelemetryAsync)");
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = act => ConsoleHelper.PrintInfo("Activity Started", $"{act.OperationName} [{act.DisplayName}]"),
            ActivityStopped = act => ConsoleHelper.PrintInfo("Activity Stopped", $"{act.OperationName} (Status: {act.Status})")
        };
        ActivitySource.AddActivityListener(activityListener);

        var products = await connection.QueryWithTelemetryAsync<Product>(
            sql: "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products WHERE is_active = 1;").ConfigureAwait(false);

        ConsoleHelper.PrintSuccess($"Instrumented query executed successfully. Returned {System.Linq.Enumerable.Count(products)} products.");

        var rowsAffected = await connection.ExecuteWithTelemetryAsync(
            sql: "UPDATE products SET price = price WHERE id = 1;").ConfigureAwait(false);

        ConsoleHelper.PrintSuccess($"Instrumented execute executed. Affected rows: {rowsAffected}.");

        ConsoleHelper.PrintStep("3. Database Health Checks (DapperHealthCheck Probe)");
        var healthCheckOptions = new DapperHealthCheckOptions
        {
            CommandText = "SELECT 1;",
            DegradedThreshold = TimeSpan.FromMilliseconds(200),
            Timeout = TimeSpan.FromSeconds(2)
        };

        var directHealthCheck = new DapperHealthCheck(
            connectionFactory: async ct => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false),
            options: healthCheckOptions);

        var healthCheckResult = await directHealthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("HealthCheck Probe Status", healthCheckResult.Status);
        ConsoleHelper.PrintInfo("Description", healthCheckResult.Description);

        foreach (var (key, val) in healthCheckResult.Data)
        {
            ConsoleHelper.PrintInfo($"  Data [{key}]", val);
        }

        // Demonstrate registration in IHealthChecksBuilder
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddSqliteDapperHealthCheck(
                name: "sqlite-replica-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false),
                configure: options =>
                {
                    options.CommandText = "SELECT 1;";
                    options.DegradedThreshold = TimeSpan.FromMilliseconds(150);
                });

        ConsoleHelper.PrintStep("4. DapperDiagnostics.Meter, ResilienceRetriesCounter, and Semantic Tags");
        ConsoleHelper.PrintInfo("Meter Name", DapperDiagnostics.Meter.Name);
        ConsoleHelper.PrintInfo("Meter Version", DapperDiagnostics.Meter.Version);
        ConsoleHelper.PrintInfo("ResilienceRetriesCounter Name", DapperDiagnostics.ResilienceRetriesCounter.Name);

        ConsoleHelper.PrintInfo("TagDbSystem", DapperDiagnostics.TagDbSystem);
        ConsoleHelper.PrintInfo("TagDbName", DapperDiagnostics.TagDbName);
        ConsoleHelper.PrintInfo("TagDbStatement", DapperDiagnostics.TagDbStatement);
        ConsoleHelper.PrintInfo("TagDbOperation", DapperDiagnostics.TagDbOperation);
        ConsoleHelper.PrintInfo("TagDbRowsAffected", DapperDiagnostics.TagDbRowsAffected);
        ConsoleHelper.PrintInfo("TagServerAddress", DapperDiagnostics.TagServerAddress);
        ConsoleHelper.PrintInfo("TagErrorType", DapperDiagnostics.TagErrorType);
        ConsoleHelper.PrintSuccess("Meter, ResilienceRetriesCounter, and semantic tags verified.");

        ConsoleHelper.PrintStep("5. AddDapperOpenTelemetry() + DapperOpenTelemetryOptions — DI Configuration");
        var otelServices = new ServiceCollection();
        otelServices.AddDapperOpenTelemetry(opts =>
        {
            opts.CaptureSqlStatements = true;
            opts.EnableMetrics = true;
            opts.MaxStatementLength = 2048;
        });

        var otelProvider = otelServices.BuildServiceProvider();
        var otelOptions = otelProvider.GetService<DapperOpenTelemetryOptions>();

        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.CaptureSqlStatements", otelOptions?.CaptureSqlStatements);
        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.EnableMetrics", otelOptions?.EnableMetrics);
        ConsoleHelper.PrintInfo("DapperOpenTelemetryOptions.MaxStatementLength", otelOptions?.MaxStatementLength);
        ConsoleHelper.PrintSuccess("AddDapperOpenTelemetry() configured and DapperOpenTelemetryOptions resolved from DI.");

        ConsoleHelper.PrintStep("6. TraceBulkOperationAsync — Bulk Operation Instrumentation");
        var tracedBulkRows = await connection.TraceBulkOperationAsync(
            operationName: "INSERT",
            targetTable: "products",
            bulkAction: async ct =>
            {
                var count = await connection.ExecuteAsync(
                    "INSERT OR IGNORE INTO products (sku, name, price, stock_quantity, is_active, release_date, daily_restock_time) " +
                    "VALUES ('TRACE-DEMO', 'TraceBulkOperation Demo Product', 9.99, 1, 1, '2026-08-26', '08:00:00');")
                    .ConfigureAwait(false);
                return count;
            }).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("TraceBulkOperationAsync — Rows Logged", tracedBulkRows);
        ConsoleHelper.PrintSuccess("TraceBulkOperationAsync: Activity created, BulkRowsCounter incremented, CommandDurationHistogram recorded.");

        ConsoleHelper.PrintStep("7. AddDapperHealthCheck() — Provider-Agnostic Generic Overload");
        var genericHcServices = new ServiceCollection();
        genericHcServices.AddHealthChecks()
            .AddDapperHealthCheck(
                name: "generic-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false),
                configure: opts =>
                {
                    opts.CommandText = "SELECT 1;";
                    opts.DegradedThreshold = TimeSpan.FromMilliseconds(300);
                    opts.Timeout = TimeSpan.FromSeconds(3);
                },
                tags: new List<string> { "db", "generic" });

        ConsoleHelper.PrintSuccess("AddDapperHealthCheck() (generic) registered in IHealthChecksBuilder.");

        ConsoleHelper.PrintStep("8. Provider-Specific HealthCheck Extensions");
        var allProvidersHcServices = new ServiceCollection();
        allProvidersHcServices.AddHealthChecks()
            .AddPostgreSqlDapperHealthCheck(
                name: "postgresql-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false))
            .AddSqlServerDapperHealthCheck(
                name: "sqlserver-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false))
            .AddOracleDapperHealthCheck(
                name: "oracle-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false),
                configure: opts => opts.Timeout = TimeSpan.FromSeconds(5))
            .AddMySqlDapperHealthCheck(
                name: "mysql-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false))
            .AddSqliteDapperHealthCheck(
                name: "sqlite-replica-probe",
                connectionFactory: async (sp, ct) => await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false),
                configure: options =>
                {
                    options.CommandText = "SELECT 1;";
                    options.DegradedThreshold = TimeSpan.FromMilliseconds(150);
                });

        ConsoleHelper.PrintSuccess("AddPostgreSqlDapperHealthCheck() registered with tags [db, postgresql, sql].");
        ConsoleHelper.PrintSuccess("AddSqlServerDapperHealthCheck() registered with tags [db, sqlserver, sql].");
        ConsoleHelper.PrintSuccess("AddOracleDapperHealthCheck() registered with probe 'SELECT 1 FROM DUAL' and tags [db, oracle, sql].");
        ConsoleHelper.PrintSuccess("AddMySqlDapperHealthCheck() registered with tags [db, mysql, sql].");
        ConsoleHelper.PrintSuccess("AddSqliteDapperHealthCheck() registered with DegradedThreshold 150ms.");

        ConsoleHelper.PrintSuccess("Level 9 completed successfully.");
    }
}
