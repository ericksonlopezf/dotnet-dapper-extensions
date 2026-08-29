# ADR-008: Standard Type Handlers and Dependency Injection Boundary

## Status
Accepted

## Context
Modern .NET applications (using .NET 8, 9, 10) frequently use `DateOnly`, `TimeOnly`, and strongly-typed string Enums in their Domain and Application layers. Dapper does not register handlers for these types out of the box.

Additionally, to keep core libraries lightweight and unencumbered by framework dependencies, DI container integration (`IServiceCollection`) should remain cleanly separated.

## Decision
1. Provide built-in, singleton Dapper type handlers in `EricksonLopez.DapperExtensions.TypeHandlers`:
   - `DateOnlyTypeHandler`
   - `TimeOnlyTypeHandler`
   - `StringEnumTypeHandler<TEnum>`
   - `DapperTypeHandlerRegistrar.RegisterStandardHandlers()`
2. Keep `Microsoft.Extensions.DependencyInjection.Abstractions` out of the core package to prevent dependency bloat, leaving DI registration helper packages as extension artifacts.

## Consequences
- Immediate out-of-the-box support for `DateOnly` and `TimeOnly` with zero external dependencies.
- Zero coupling between core database abstractions and ASP.NET Core service containers.
