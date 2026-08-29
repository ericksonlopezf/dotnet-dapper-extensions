# Quick Start Guide — EricksonLopez.DapperExtensions

Get up and running with **EricksonLopez.DapperExtensions** in under 5 minutes.

---

## 1. Install Packages

Install the Core library and your target database provider package:

```bash
# Core abstractions & Unit of Work
dotnet add package EricksonLopez.DapperExtensions

# Dependency Injection support (ASP.NET Core / Generic Host)
dotnet add package EricksonLopez.DapperExtensions.DependencyInjection

# Choose your database provider (e.g. PostgreSQL, SQL Server, MySQL, SQLite, Oracle)
dotnet add package EricksonLopez.DapperExtensions.PostgreSql
```

---

## 2. Register Services in `Program.cs`

Configure Dapper type handlers (`DateOnly`, `TimeOnly`, Enums) and database transient error detectors automatically:

```csharp
using EricksonLopez.DapperExtensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register DapperExtensions infrastructure
builder.Services.AddDapperExtensions(options =>
{
    options.RegisterStandardTypeHandlers = true;
    options.RegisterTransientErrorDetectors = true;
});
```

---

## 3. Execute Async Transactions with Unit of Work

Manage transactional scopes with automatic commit on success and deterministic rollback on failure:

```csharp
using System.Data;
using Dapper;
using EricksonLopez.DapperExtensions.UnitOfWork;

app.MapPost("/orders", async (OrderDto dto, IDbConnection connection, CancellationToken ct) =>
{
    await connection.WithUnitOfWorkAsync(async (uow, token) =>
    {
        // All commands share the active transaction
        await connection.ExecuteAsync(
            "INSERT INTO orders (id, customer, total) VALUES (@Id, @Customer, @Total)",
            new { dto.Id, dto.Customer, dto.Total },
            uow.Transaction);

        await connection.ExecuteAsync(
            "INSERT INTO audit_log (action, order_id) VALUES ('Created', @Id)",
            new { dto.Id },
            uow.Transaction);
    }, cancellationToken: ct);

    return Results.Created($"/orders/{dto.Id}", dto);
});
```

---

## 4. Query with Keyset Pagination

Execute fast keyset paginated queries avoiding $O(N)$ scanning penalty:

```csharp
using System.Data;
using EricksonLopez.DapperExtensions.PostgreSql.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

app.MapGet("/products", async (string? afterCursor, int? pageSize, IDbConnection connection, CancellationToken ct) =>
{
    var parameters = new CursorPaginationParameters
    {
        First = pageSize ?? 20,
        After = afterCursor
    };

    var page = await connection.QueryCursorPagedAsync<ProductDto>(
        sql: "SELECT id, name, price FROM products",
        cursorColumn: "id",
        parameters: parameters,
        cursorSelector: p => p.Id.ToString(),
        cancellationToken: ct);

    return Results.Ok(page);
});
```

---

## 5. Next Steps

- Explore [Getting Started Guide](getting-started.md) for full configuration.
- Check out the [Cookbook](cookbook.md) for battle-tested patterns.
- Read [Best Practices](best-practices.md) for architectural guidelines.
- Explore the [Showcase Documentation](showcase/README.md) for 11 progressive learning levels.
