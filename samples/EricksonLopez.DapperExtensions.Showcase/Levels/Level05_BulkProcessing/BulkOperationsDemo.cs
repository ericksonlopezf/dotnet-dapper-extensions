// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.Sqlite.Bulk;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level05_BulkProcessing;

/// <summary>
/// Level 5 — Bulk Processing: High-throughput Bulk Insert, Upsert, and Delete strategies across database dialects.
/// </summary>
public static class BulkOperationsDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(5, "Bulk Processing", "Dialect-specific bulk strategies (PostgreSQL UNNEST, SQL Server SqlBulkCopy, MySQL/Oracle/SQLite Builders)");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        ConsoleHelper.PrintStep("1. Executable SQLite Demonstration (Multi-Row Parameterized Batch)");
        var bulkProducts = new List<Product>();
        for (int i = 100; i < 150; i++)
        {
            bulkProducts.Add(new Product
            {
                Sku = $"BULK-SKU-{i}",
                Name = $"Batch Engineered Book Volume {i}",
                Price = 29.99m + (i % 10),
                StockQuantity = 100,
                IsActive = true,
                ReleaseDate = new DateOnly(2026, 8, 1),
                DailyRestockTime = new TimeOnly(8, 0, 0),
                MetadataJson = "{\"bulk\":true}"
            });
        }

        // Build batch SQL and parameters
        var (sql, parameters) = BulkBuilder.From(bulkProducts)
            .Table("products")
            .Column("sku", p => p.Sku)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("stock_quantity", p => p.StockQuantity)
            .Column("is_active", p => p.IsActive ? 1 : 0)
            .Column("release_date", p => p.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Column("daily_restock_time", p => p.DailyRestockTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
            .Column("metadata_json", p => p.MetadataJson)
            .Build();

        var rowsInserted = await connection.BulkInsertAsync(sql, parameters).ConfigureAwait(false);
        ConsoleHelper.PrintSuccess($"BulkInsertAsync in SQLite inserted {rowsInserted} rows in a single network round-trip.");

        ConsoleHelper.PrintStep("2. Implementation Reference for PostgreSQL (UNNEST)");
        Console.WriteLine(""""
    // PostgreSQL UNNEST: 10-30x faster than row-by-row parameterized inserts
    var pgParams = BulkParameters.From(bulkProducts)
        .Add("Ids",        p => p.Id,            NpgsqlDbType.Bigint)
        .Add("Skus",       p => p.Sku,           NpgsqlDbType.Text)
        .Add("Names",      p => p.Name,          NpgsqlDbType.Text)
        .Add("Prices",     p => p.Price,         NpgsqlDbType.Numeric)
        .Add("StockQtys",  p => p.StockQuantity, NpgsqlDbType.Integer)
        .Build();

    const string pgInsertSql = """
        INSERT INTO products (id, sku, name, price, stock_quantity)
        SELECT * FROM UNNEST(@Ids, @Skus, @Names, @Prices, @StockQtys);
        """;

    await pgConnection.BulkInsertAsync(pgInsertSql, pgParams);
"""");

        ConsoleHelper.PrintStep("3. Implementation Reference for SQL Server (SqlBulkCopy)");
        Console.WriteLine("""
    // SQL Server SqlBulkCopy: High-throughput binary streaming
    var dataTable = BulkDataTableBuilder.From(bulkProducts)
        .Column("sku",            p => p.Sku)
        .Column("name",           p => p.Name)
        .Column("price",          p => p.Price)
        .Column("stock_quantity", p => p.StockQuantity)
        .Build();

    await sqlServerConnection.BulkInsertAsync("dbo.products", dataTable, batchSize: 5000);
""");

        ConsoleHelper.PrintStep("4. Implementation Reference for Oracle (INSERT ALL)");
        Console.WriteLine("""
    // Oracle Database: INSERT ALL ... SELECT 1 FROM DUAL
    var (oracleSql, oracleParams) = EricksonLopez.DapperExtensions.Oracle.Bulk.BulkBuilder.From(bulkProducts)
        .Table("products")
        .Column("sku",   p => p.Sku)
        .Column("name",  p => p.Name)
        .Column("price", p => p.Price)
        .Build();

    await oracleConnection.BulkInsertAsync(oracleSql, oracleParams);
""");

        ConsoleHelper.PrintStep("5. BulkBuilder<T>.Count — Batch Row Count Verification");
        var upsertProducts = new List<Product>();
        for (int i = 200; i < 210; i++)
        {
            upsertProducts.Add(new Product
            {
                Sku = $"UPSERT-SKU-{i}",
                Name = $"Upsert Product {i}",
                Price = 49.99m,
                StockQuantity = 50,
                IsActive = true,
                ReleaseDate = new DateOnly(2026, 8, 1),
                DailyRestockTime = new TimeOnly(9, 0, 0),
                MetadataJson = "{\"upsert\":true}"
            });
        }

        var upsertBuilder = BulkBuilder.From(upsertProducts)
            .Table("products")
            .Column("sku", p => p.Sku)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("stock_quantity", p => p.StockQuantity)
            .Column("is_active", p => p.IsActive ? 1 : 0)
            .Column("release_date", p => p.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Column("daily_restock_time", p => p.DailyRestockTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
            .Column("metadata_json", p => p.MetadataJson);

        ConsoleHelper.PrintInfo("BulkBuilder<Product>.Count (rows to insert)", upsertBuilder.Count);

        ConsoleHelper.PrintStep("6. BulkUpsertAsync (SQLite) — INSERT OR REPLACE INTO");
        var (upsertSql, upsertParams) = upsertBuilder.Build();
        var upsertSqlFinal = upsertSql!.Replace("INSERT INTO", "INSERT OR REPLACE INTO", StringComparison.Ordinal);

        var upsertRows = await connection.BulkUpsertAsync(upsertSqlFinal, upsertParams).ConfigureAwait(false);
        ConsoleHelper.PrintSuccess($"BulkUpsertAsync in SQLite upserted {upsertRows} rows (INSERT OR REPLACE).");

        ConsoleHelper.PrintStep("7. BulkDeleteAsync (SQLite) — Batch DELETE by Primary Key");
        var deleteIds = new long[] { 200, 201, 202 };
        var deleteParams = new DynamicParameters();
        var inPlaceholders = new List<string>();
        for (int i = 0; i < deleteIds.Length; i++)
        {
            var paramName = $"deleteId{i}";
            deleteParams.Add(paramName, deleteIds[i]);
            inPlaceholders.Add($"@{paramName}");
        }
        var deleteSql = $"DELETE FROM products WHERE id IN ({string.Join(", ", inPlaceholders)});";

        var deletedRows = await connection.BulkDeleteAsync(deleteSql, deleteParams).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("BulkDeleteAsync — Deleted rows", deletedRows);
        ConsoleHelper.PrintSuccess("BulkDeleteAsync executed successfully.");

        ConsoleHelper.PrintStep("8. BulkUpdateAsync (SQLite) — Batch UPDATE");
        var updateParams = new DynamicParameters();
        updateParams.Add("newPrice", 39.99m);
        updateParams.Add("targetSku", "UPSERT-SKU-200");
        var updateSql = "UPDATE products SET price = @newPrice WHERE sku = @targetSku;";

        var updatedRows = await connection.BulkUpdateAsync(updateSql, updateParams).ConfigureAwait(false);
        ConsoleHelper.PrintInfo("BulkUpdateAsync — Updated rows", updatedRows);
        ConsoleHelper.PrintSuccess("BulkUpdateAsync executed successfully.");

        ConsoleHelper.PrintSuccess("Level 5 completed successfully.");
    }
}
