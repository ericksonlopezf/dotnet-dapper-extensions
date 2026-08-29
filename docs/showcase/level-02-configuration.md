# Level 02: Full Configuration & Type Handlers

## 1. Goal
Explore all configuration options, register global and dialect-specific type handlers (including string enums and JSON columns), and understand granular Dependency Injection configuration.

---

## 2. Global Type Handlers & String Enums

### Manual Registration
```csharp
using EricksonLopez.DapperExtensions.TypeHandlers;
using EricksonLopez.DapperExtensions.Sqlite.TypeHandlers;

// 1. Register DateOnly and TimeOnly handlers globally
DapperTypeHandlerRegistrar.RegisterStandardHandlers();

// 2. Register string-backed enum handlers
DapperTypeHandlerRegistrar.RegisterStringEnumHandler<OrderStatus>();
DapperTypeHandlerRegistrar.RegisterStringEnumHandler<PaymentMethod>();
DapperTypeHandlerRegistrar.RegisterStringEnumHandler<CustomerTier>();

// 3. Register JSON type handler for SQLite (or NpgsqlTypeHandlerRegistrar for PostgreSQL)
SqliteTypeHandlerRegistrar.RegisterJsonHandler<ProductMetadata>();
```

### Supported Enums and Models
```csharp
public enum OrderStatus { Draft, Processing, Completed, Cancelled }
public enum PaymentMethod { CreditCard, PayPal, BankTransfer }
public enum CustomerTier { Standard, Gold, Platinum }

public sealed class ProductMetadata
{
    public string? Format { get; set; }
    public int? WeightG { get; set; }
    public int? FileSizeMb { get; set; }
}
```

---

## 3. Dependency Injection Configuration

### Full Configuration via `AddDapperExtensions`
```csharp
using EricksonLopez.DapperExtensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDapperExtensions(options =>
{
    // Auto-registers DateOnly and TimeOnly handlers in Dapper's global registry
    options.RegisterStandardTypeHandlers = true;

    // Registers all provider-specific transient error detectors as singletons
    options.RegisterTransientErrorDetectors = true;
});

var provider = services.BuildServiceProvider();
```

### Granular Registration
When fine-grained control is required, modules can be registered independently:
```csharp
// Register type handlers only
services.AddDapperTypeHandlers();

// Register provider transient error detectors only (SqlServer, PostgreSql, MySql, Sqlite, Oracle)
services.AddDapperTransientErrorDetectors();
```

---

## 4. Querying & Inserting with Type Handlers
```csharp
using Dapper;

// Automatic enum and DateOnly mapping on read
const string orderSql = """
    SELECT id, customer_id AS CustomerId, order_number AS OrderNumber,
           status AS Status, payment_method AS PaymentMethod,
           total_amount AS TotalAmount, order_date AS OrderDate
    FROM orders
    WHERE id = @Id;
    """;

var order = await connection.QuerySingleAsync<Order>(orderSql, new { Id = 1 });

// Inserting with DateOnly, TimeOnly and JSON
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

await connection.ExecuteAsync(insertSql, newProduct);
```

---

## 5. Source Code Reference
- Executable Showcase: [`samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level02_Configuration/ConfigurationDemo.cs`](../../samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level02_Configuration/ConfigurationDemo.cs)
