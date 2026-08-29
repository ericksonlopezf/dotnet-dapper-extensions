// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Sets up and seeds an in-memory SQLite database connection for demonstration purposes.
/// </summary>
public static class ShowcaseDbContext
{
    public static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Mode=Memory;Cache=Shared");
        await connection.OpenAsync().ConfigureAwait(false);

        const string initSchemaSql = """
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sku TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                price NUMERIC NOT NULL,
                stock_quantity INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                release_date TEXT NOT NULL,
                daily_restock_time TEXT NOT NULL,
                metadata_json TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL UNIQUE,
                full_name TEXT NOT NULL,
                tier TEXT NOT NULL,
                registered_date TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER NOT NULL,
                order_number TEXT NOT NULL UNIQUE,
                status TEXT NOT NULL,
                payment_method TEXT NOT NULL,
                total_amount NUMERIC NOT NULL,
                order_date TEXT NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );

            CREATE TABLE IF NOT EXISTS order_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                product_name TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price NUMERIC NOT NULL,
                FOREIGN KEY (order_id) REFERENCES orders(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            );

            CREATE TABLE IF NOT EXISTS outbox_messages (
                id TEXT PRIMARY KEY,
                message_type TEXT NOT NULL,
                payload TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                processed_at TEXT NULL
            );
            """;

        await connection.ExecuteAsync(initSchemaSql).ConfigureAwait(false);
        return connection;
    }

    public static async Task SeedSampleDataAsync(IDbConnection connection)
    {
        const string seedSql = """
            INSERT OR IGNORE INTO products (id, sku, name, price, stock_quantity, is_active, release_date, daily_restock_time, metadata_json)
            VALUES 
                (1, 'PROD-001', 'Cloud Native Architecture Guide', 49.99, 100, 1, '2026-01-15', '08:00:00', '{"format":"hardcover","weight_g":450}'),
                (2, 'PROD-002', 'High-Throughput Dapper Cookbook', 39.50, 250, 1, '2026-02-10', '09:30:00', '{"format":"ebook","file_size_mb":12}'),
                (3, 'PROD-003', 'Distributed Systems Patterns', 59.95, 75, 1, '2026-03-01', '07:45:00', '{"format":"hardcover","weight_g":600}'),
                (4, 'PROD-004', 'Zero-Allocation C# Masterclass', 65.50, 50, 1, '2026-04-12', '10:00:00', '{"format":"video_course","hours":18}'),
                (5, 'PROD-005', 'PostgreSQL Internals and Indexing', 55.25, 120, 1, '2026-05-20', '08:15:00', '{"format":"paperback","weight_g":380}');

            INSERT OR IGNORE INTO customers (id, email, full_name, tier, registered_date)
            VALUES
                (1, 'erickson@example.com', 'Erickson Lopez', 'Platinum', '2026-01-01'),
                (2, 'alice@example.com', 'Alice Smith', 'Gold', '2026-02-14'),
                (3, 'bob@example.com', 'Bob Johnson', 'Standard', '2026-03-22');

            INSERT OR IGNORE INTO orders (id, customer_id, order_number, status, payment_method, total_amount, order_date)
            VALUES
                (1, 1, 'ORD-2026-001', 'Delivered', 'CreditCard', 89.49, '2026-06-01'),
                (2, 2, 'ORD-2026-002', 'Processing', 'PayPal', 65.50, '2026-06-15');

            INSERT OR IGNORE INTO order_items (id, order_id, product_id, product_name, quantity, unit_price)
            VALUES
                (1, 1, 1, 'Cloud Native Architecture Guide', 1, 49.99),
                (2, 1, 2, 'High-Throughput Dapper Cookbook', 1, 39.50),
                (3, 2, 4, 'Zero-Allocation C# Masterclass', 1, 65.50);
            """;

        await connection.ExecuteAsync(seedSql).ConfigureAwait(false);
    }
}
