// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.Sqlite.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level03_RealWorldUseCases;

/// <summary>
/// Level 3 — Real-World Use Cases: Robust CRUD, Traditional Offset Pagination, and Keyset/Cursor Pagination.
/// </summary>
public static class PaginationAndCrudDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(3, "Real-World Use Cases", "CRUD operations, Offset pagination with metadata, and Keyset/Cursor pagination");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        ConsoleHelper.PrintStep("1. Traditional Offset-Based Pagination with Metadata");
        var paginationParams = PaginationParameters.Create(page: 1, pageSize: 2);

        var pagedList = await connection.QueryPagedAsync<Product>(
            sql: "SELECT id, sku, name, price, stock_quantity AS StockQuantity, is_active AS IsActive, release_date AS ReleaseDate FROM products WHERE is_active = 1",
            countSql: "SELECT COUNT(*) FROM products WHERE is_active = 1",
            pagination: paginationParams).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Current Page", pagedList.Page);
        ConsoleHelper.PrintInfo("Page Size", pagedList.PageSize);
        ConsoleHelper.PrintInfo("Total Items", pagedList.TotalCount);
        ConsoleHelper.PrintInfo("Total Pages", pagedList.TotalPages);
        ConsoleHelper.PrintInfo("Has Next Page?", pagedList.HasNextPage);
        ConsoleHelper.PrintInfo("Has Previous Page?", pagedList.HasPreviousPage);

        foreach (var item in pagedList)
        {
            ConsoleHelper.PrintInfo($"Item [ID {item.Id}]", $"{item.Name} — {item.Price.ToString("C", CultureInfo.InvariantCulture)}");
        }

        ConsoleHelper.PrintStep("2. Multi-Query Single Round-Trip Pagination (QueryPagedMultipleAsync)");
        const string multiPagedSql = """
            SELECT id, sku, name, price, stock_quantity AS StockQuantity, is_active AS IsActive, release_date AS ReleaseDate 
            FROM products 
            WHERE is_active = 1 
            LIMIT 2 OFFSET 0;

            SELECT COUNT(*) FROM products WHERE is_active = 1;
            """;

        var multiPagedList = await connection.QueryPagedMultipleAsync<Product>(
            sql: multiPagedSql,
            pagination: paginationParams).ConfigureAwait(false);

        ConsoleHelper.PrintSuccess($"Multi-query pagination executed in 1 network round-trip. Total: {multiPagedList.TotalCount} rows.");

        ConsoleHelper.PrintStep("3. Keyset / Cursor-Based Pagination");
        var cursorParams = new CursorPaginationParameters
        {
            First = 3
        };

        var cursorPagedList = await connection.QueryCursorPagedAsync<Product>(
            sql: "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products",
            cursorColumn: "id",
            parameters: cursorParams,
            cursorSelector: p => p.Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

        ConsoleHelper.PrintInfo("Retrieved Items", cursorPagedList.Count);
        ConsoleHelper.PrintInfo("Start Cursor", cursorPagedList.StartCursor);
        ConsoleHelper.PrintInfo("End Cursor", cursorPagedList.EndCursor);
        ConsoleHelper.PrintInfo("Has Next Page?", cursorPagedList.HasNextPage);
        ConsoleHelper.PrintInfo("Has Previous Page?", cursorPagedList.HasPreviousPage);

        // Forward navigation using 'After'
        if (cursorPagedList.HasNextPage && cursorPagedList.EndCursor != null)
        {
            ConsoleHelper.PrintStep("4. Forward Navigation Using 'After' Cursor");
            var nextCursorParams = new CursorPaginationParameters
            {
                First = 3,
                After = cursorPagedList.EndCursor
            };

            var nextPageList = await connection.QueryCursorPagedAsync<Product>(
                sql: "SELECT id, sku, name, price, stock_quantity AS StockQuantity FROM products",
                cursorColumn: "id",
                parameters: nextCursorParams,
                cursorSelector: p => p.Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

            ConsoleHelper.PrintInfo("Page 2 - Item Count", nextPageList.Count);
            foreach (var item in nextPageList)
            {
                ConsoleHelper.PrintInfo($"Item [ID {item.Id}]", item.Name);
            }
        }

        ConsoleHelper.PrintSuccess("Level 3 completed successfully.");
    }
}
