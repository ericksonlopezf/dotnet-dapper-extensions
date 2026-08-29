// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level08_Customization;

/// <summary>
/// Level 8 — Customization: Custom implementations of ISqlTransientErrorDetector, TypeHandlers, and IDataReaderMapper.
/// </summary>
public static class CustomDetectorAndHandlerDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(8, "Customization", "Custom implementations of ISqlTransientErrorDetector, TypeHandlers, and AOT Mappers");

        ConsoleHelper.PrintStep("1. Registering Custom TypeHandler (Money)");
        SqlMapper.AddTypeHandler(MoneyTypeHandler.Default);
        ConsoleHelper.PrintSuccess("MoneyTypeHandler registered in SqlMapper.");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        var productPrice = await connection.QuerySingleAsync<Money>(
            "SELECT price FROM products WHERE id = 1;").ConfigureAwait(false);
        ConsoleHelper.PrintInfo("Mapped Value with Custom TypeHandler", productPrice);

        ConsoleHelper.PrintStep("2. Custom Transient Error Detector Evaluation");
        var customDetector = new CustomClusterTransientErrorDetector();
        var clusterException = new InvalidOperationException("Error 8092: cluster topology changing");
        var standardException = new InvalidOperationException("Column not found: user_id");

        ConsoleHelper.PrintInfo("Is 'cluster topology changing' transient?", customDetector.IsTransient(clusterException));
        ConsoleHelper.PrintInfo("Is 'Column not found' transient?", customDetector.IsTransient(standardException));

        var customPipeline = SqlResilienceDefaults.Standard(customDetector);
        ConsoleHelper.PrintSuccess("Polly v8 pipeline constructed with CustomClusterTransientErrorDetector.");

        ConsoleHelper.PrintStep("3. Using Custom AOT IDataReaderMapper");
        var customMapper = new CustomCustomerMapper();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, email, full_name, tier, registered_date FROM customers WHERE id = 1;";
        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        if (reader.Read())
        {
            var customer = customMapper.Map(reader);
            ConsoleHelper.PrintInfo("Customer ID", customer.Id);
            ConsoleHelper.PrintInfo("Customer Email", customer.Email);
            ConsoleHelper.PrintInfo("Customer Tier", customer.Tier);
            ConsoleHelper.PrintSuccess("Custom IDataReaderMapper<Customer> executed successfully.");
        }

        ConsoleHelper.PrintSuccess("Level 8 completed successfully.");
    }
}
