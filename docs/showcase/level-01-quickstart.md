# Level 01: Quick Start

## 1. Goal
Configure the dependency injection container with standard type handlers and transient error detectors, establish a database connection, and execute a parameterized query mapping modern .NET types (`DateOnly`, `TimeOnly`).

---

## 2. Step-by-Step Implementation

### Step 1: Add Package Dependencies
Install the required packages via the .NET CLI:
```bash
dotnet add package EricksonLopez.DapperExtensions
dotnet add package EricksonLopez.DapperExtensions.DependencyInjection
dotnet add package EricksonLopez.DapperExtensions.Sqlite # Or PostgreSQL, SqlServer, etc.
```

### Step 2: Register Services in `IServiceCollection`
```csharp
using EricksonLopez.DapperExtensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Registers DateOnly/TimeOnly type handlers and provider transient error detectors
services.AddDapperExtensions(options =>
{
    options.RegisterStandardTypeHandlers = true;
    options.RegisterTransientErrorDetectors = true;
});

var serviceProvider = services.BuildServiceProvider();
```

### Step 3: Execute a Functional Parameterized Query
```csharp
using System;
using System.Data;
using Dapper;

using var connection = await OpenDatabaseConnectionAsync();

const string sql = """
    SELECT id, sku, name, price, stock_quantity AS StockQuantity, release_date AS ReleaseDate
    FROM products
    WHERE id = @Id;
    """;

var product = await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = 1 });

if (product != null)
{
    Console.WriteLine($"Product: {product.Name} (Release Date: {product.ReleaseDate})");
}
```

---

## 3. Entity Definition
```csharp
public sealed class Product
{
    public long Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public TimeOnly DailyRestockTime { get; set; }
}
```

---

## 4. Key Takeaways
- `DateOnly` and `TimeOnly` types map to database `DATE` and `TIME` columns automatically without custom conversion logic.
- `AddDapperExtensions` provides a single entry point for framework configuration.

---

## 5. Source Code Reference
- Executable Showcase: [`samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level01_QuickStart/QuickStartDemo.cs`](../../samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level01_QuickStart/QuickStartDemo.cs)
