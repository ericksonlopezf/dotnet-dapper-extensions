// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace EricksonLopez.DapperExtensions.Testing.Common;

/// <summary>
/// Reusable test entity and data factory for verifying bulk insert and batch operations across database dialects.
/// Centralizes product schemas, data tables, and batch generators to eliminate duplication.
/// </summary>
public sealed record BulkTestProduct(Guid Id, string Name, decimal Price, bool IsActive)
{
    /// <summary>
    /// Returns a fixed, deterministic set of 3 test products for unit testing.
    /// </summary>
    public static List<BulkTestProduct> CreateDefaultProducts() =>
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", 9.99m, true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Gadget", 49.99m, true),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Doohickey", 4.99m, false)
    ];

    /// <summary>
    /// Generates a specified count of dynamic test products.
    /// </summary>
    public static List<BulkTestProduct> GenerateProducts(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new BulkTestProduct(Guid.NewGuid(), $"Product {i}", i * 9.99m, i % 2 == 0))
            .ToList();

    /// <summary>
    /// Creates an ADO.NET <see cref="DataTable"/> populated with the given test products.
    /// </summary>
    public static DataTable CreateProductDataTable(IEnumerable<BulkTestProduct> products, string tableName = "products")
    {
        var table = new DataTable(tableName);
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("price", typeof(decimal));
        table.Columns.Add("is_active", typeof(bool));

        foreach (var p in products)
        {
            table.Rows.Add(p.Id, p.Name, p.Price, p.IsActive);
        }

        return table;
    }

    /// <summary>
    /// Centralized DDL definitions for the standard test 'products' table across database dialects in integration tests.
    /// </summary>
    public static class Ddl
    {
        /// <summary>SQL Server DDL for products table.</summary>
        public const string SqlServerProductsTable = """
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='products' AND xtype='U')
            CREATE TABLE products (
                id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                name        NVARCHAR(100)    NOT NULL,
                price       DECIMAL(18,2)    NOT NULL,
                is_active   BIT              NOT NULL DEFAULT 1,
                created_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
            );
            """;

        /// <summary>PostgreSQL DDL for products table.</summary>
        public const string PostgreSqlProductsTable = """
            CREATE TABLE IF NOT EXISTS products (
                id          UUID             NOT NULL PRIMARY KEY,
                name        VARCHAR(100)     NOT NULL,
                price       NUMERIC(18,2)    NOT NULL,
                is_active   BOOLEAN          NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMPTZ      NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;

        /// <summary>MySQL DDL for products table.</summary>
        public const string MySqlProductsTable = """
            CREATE TABLE IF NOT EXISTS products (
                id          CHAR(36)         NOT NULL PRIMARY KEY,
                name        VARCHAR(100)     NOT NULL,
                price       DECIMAL(18,2)    NOT NULL,
                is_active   TINYINT(1)       NOT NULL DEFAULT 1,
                created_at  DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;

        /// <summary>MariaDB DDL for products table.</summary>
        public const string MariaDbProductsTable = """
            CREATE TABLE IF NOT EXISTS products (
                id          CHAR(36)         NOT NULL PRIMARY KEY,
                name        VARCHAR(100)     NOT NULL,
                price       DECIMAL(18,2)    NOT NULL,
                is_active   TINYINT(1)       NOT NULL DEFAULT 1,
                created_at  DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;

        /// <summary>Oracle DDL for products table.</summary>
        public const string OracleProductsTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE products (
                    id          VARCHAR2(36)  NOT NULL PRIMARY KEY,
                    name        VARCHAR2(100) NOT NULL,
                    price       NUMBER(10,2)  NOT NULL,
                    is_active   NUMBER(1)     DEFAULT 1 NOT NULL,
                    created_at  TIMESTAMP     DEFAULT CURRENT_TIMESTAMP NOT NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN RAISE; END IF;
            END;
            """;

        /// <summary>SQLite DDL for products table.</summary>
        public const string SqliteProductsTable = """
            CREATE TABLE IF NOT EXISTS products (
                id      INTEGER NOT NULL PRIMARY KEY,
                name    TEXT    NOT NULL,
                price   REAL    NOT NULL,
                active  INTEGER NOT NULL DEFAULT 1
            );
            """;
    }
}
