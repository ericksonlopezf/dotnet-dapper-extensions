// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.DependencyInjection;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level01_QuickStart;

/// <summary>
/// Level 1 — Quick Start: Minimal setup, dependency injection configuration, and first functional query.
/// </summary>
public static class QuickStartDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(1, "Quick Start", "Minimal DI setup, TypeHandlers registration, and first functional query");

        ConsoleHelper.PrintStep("1. Dependency Injection Configuration (ServiceCollection)");
        var services = new ServiceCollection();

        // Standard ecosystem registration
        services.AddDapperExtensions(options =>
        {
            options.RegisterStandardTypeHandlers = true;
            options.RegisterTransientErrorDetectors = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        ConsoleHelper.PrintSuccess("DI container configured with AddDapperExtensions.");

        ConsoleHelper.PrintStep("2. Database Connection Creation & Initialization");
        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);
        ConsoleHelper.PrintSuccess("Database connection opened and in-memory schema initialized.");

        ConsoleHelper.PrintStep("3. Executing First Functional Query");
        const string sql = "SELECT id, sku, name, price, stock_quantity AS StockQuantity, release_date AS ReleaseDate FROM products WHERE id = @Id;";
        var product = await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = 1 }).ConfigureAwait(false);

        if (product != null)
        {
            ConsoleHelper.PrintInfo("ID", product.Id);
            ConsoleHelper.PrintInfo("SKU", product.Sku);
            ConsoleHelper.PrintInfo("Name", product.Name);
            ConsoleHelper.PrintInfo("Price", product.Price.ToString("C", CultureInfo.InvariantCulture));
            ConsoleHelper.PrintInfo("Release Date (DateOnly)", product.ReleaseDate);
            ConsoleHelper.PrintSuccess("Entity mapping and DateOnly handling executed successfully.");
        }

        ConsoleHelper.PrintSuccess("Level 1 completed successfully.");
    }
}
