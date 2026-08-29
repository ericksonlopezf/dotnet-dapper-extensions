// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.DependencyInjection;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.Sqlite.TypeHandlers;
using EricksonLopez.DapperExtensions.TypeHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level02_Configuration;

/// <summary>
/// Level 2 — Full Configuration: DI options, standard DateOnly/TimeOnly TypeHandlers, Enum Handlers, and JSON Handlers.
/// </summary>
public static class ConfigurationDemo
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Showcase demonstration of JSON type handler")]
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(2, "Full Configuration", "Configuration options, standard TypeHandlers, Enum Handlers, and JSON Handlers");

        ConsoleHelper.PrintStep("1. Manual Global Registration of TypeHandlers");
        // Register DateOnly and TimeOnly handlers
        DapperTypeHandlerRegistrar.RegisterStandardHandlers();
        ConsoleHelper.PrintSuccess("DateOnlyTypeHandler and TimeOnlyTypeHandler registered globally.");

        // Register string-backed enum handlers
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<OrderStatus>();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<PaymentMethod>();
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<CustomerTier>();
        ConsoleHelper.PrintSuccess("StringEnumTypeHandler registered for OrderStatus, PaymentMethod, and CustomerTier.");

        // Register JSON TypeHandler for SQLite
        SqliteTypeHandlerRegistrar.RegisterJsonHandler<ProductMetadata>();
        ConsoleHelper.PrintSuccess("JsonTypeHandler<ProductMetadata> registered for SQLite dialect.");

        ConsoleHelper.PrintStep("2. Advanced Configuration via IServiceCollection");
        var services = new ServiceCollection();
        services.AddDapperExtensions(options =>
        {
            options.RegisterStandardTypeHandlers = true;
            options.RegisterTransientErrorDetectors = true;
        });

        var provider = services.BuildServiceProvider();
        var pgDetector = provider.GetService<PostgreSqlTransientErrorDetector>();
        var sqlServerDetector = provider.GetService<SqlServerTransientErrorDetector>();
        var sqliteDetector = provider.GetService<SqliteTransientErrorDetector>();

        ConsoleHelper.PrintInfo("PostgreSQL Detector in DI", pgDetector != null ? "Registered (Singleton)" : "Not registered");
        ConsoleHelper.PrintInfo("SQL Server Detector in DI", sqlServerDetector != null ? "Registered (Singleton)" : "Not registered");
        ConsoleHelper.PrintInfo("SQLite Detector in DI", sqliteDetector != null ? "Registered (Singleton)" : "Not registered");

        ConsoleHelper.PrintStep("3. Runtime Verification of TypeHandlers");
        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        // Test querying enums and dates
        const string orderSql = "SELECT id, customer_id AS CustomerId, order_number AS OrderNumber, status AS Status, payment_method AS PaymentMethod, total_amount AS TotalAmount, order_date AS OrderDate FROM orders WHERE id = 1;";
        var order = await connection.QuerySingleAsync<Order>(orderSql).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Order Number", order.OrderNumber);
        ConsoleHelper.PrintInfo("Order Status (Enum)", order.Status);
        ConsoleHelper.PrintInfo("Payment Method (Enum)", order.PaymentMethod);
        ConsoleHelper.PrintInfo("Order Date (DateOnly)", order.OrderDate);

        // Test inserting with TimeOnly and JSON
        var newProduct = new Product
        {
            Sku = "PROD-CFG-01",
            Name = "Domain-Driven Design with .NET",
            Price = 79.99m,
            StockQuantity = 40,
            IsActive = true,
            ReleaseDate = new DateOnly(2026, 8, 20),
            DailyRestockTime = new TimeOnly(14, 30, 0),
            MetadataJson = "{\"format\":\"hardcover\",\"weight_g\":720}"
        };

        const string insertSql = """
            INSERT INTO products (sku, name, price, stock_quantity, is_active, release_date, daily_restock_time, metadata_json)
            VALUES (@Sku, @Name, @Price, @StockQuantity, @IsActive, @ReleaseDate, @DailyRestockTime, @MetadataJson);
            """;

        await connection.ExecuteAsync(insertSql, newProduct).ConfigureAwait(false);
        ConsoleHelper.PrintSuccess("Insert with DateOnly, TimeOnly, and JSON executed successfully.");

        ConsoleHelper.PrintStep("4. AddDapperTypeHandlers() — Standalone TypeHandler Registration");
        var servicesTypeOnly = new ServiceCollection();
        servicesTypeOnly.AddDapperTypeHandlers();
        ConsoleHelper.PrintSuccess("AddDapperTypeHandlers() executed. DateOnly and TimeOnly handlers registered in Dapper.");
        ConsoleHelper.PrintInfo("ServiceCollection Count (TypeHandlers only)", servicesTypeOnly.Count);

        ConsoleHelper.PrintStep("5. AddDapperTransientErrorDetectors() — Standalone Error Detector Registration");
        var servicesDetectorsOnly = new ServiceCollection();
        servicesDetectorsOnly.AddDapperTransientErrorDetectors();
        var detectorsProvider = servicesDetectorsOnly.BuildServiceProvider();

        var pgDet = detectorsProvider.GetService<PostgreSqlTransientErrorDetector>();
        var sqlDet = detectorsProvider.GetService<SqlServerTransientErrorDetector>();
        var mysDet = detectorsProvider.GetService<MySqlTransientErrorDetector>();
        var sqliteDet = detectorsProvider.GetService<SqliteTransientErrorDetector>();
        var oracleDet = detectorsProvider.GetService<OracleTransientErrorDetector>();

        ConsoleHelper.PrintInfo("PostgreSqlTransientErrorDetector (DI)", pgDet != null ? "✔ Singleton registered" : "✘ Not registered");
        ConsoleHelper.PrintInfo("SqlServerTransientErrorDetector (DI)", sqlDet != null ? "✔ Singleton registered" : "✘ Not registered");
        ConsoleHelper.PrintInfo("MySqlTransientErrorDetector (DI)", mysDet != null ? "✔ Singleton registered" : "✘ Not registered");
        ConsoleHelper.PrintInfo("SqliteTransientErrorDetector (DI)", sqliteDet != null ? "✔ Singleton registered" : "✘ Not registered");
        ConsoleHelper.PrintInfo("OracleTransientErrorDetector (DI)", oracleDet != null ? "✔ Singleton registered" : "✘ Not registered");
        ConsoleHelper.PrintSuccess("AddDapperTransientErrorDetectors() registered all 5 provider detectors as Singleton.");

        ConsoleHelper.PrintSuccess("Level 2 completed successfully.");
    }
}
