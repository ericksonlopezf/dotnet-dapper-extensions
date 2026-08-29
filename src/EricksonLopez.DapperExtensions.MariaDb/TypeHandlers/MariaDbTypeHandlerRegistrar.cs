// Copyright © Erickson Lopez. MIT License.
using Dapper;

namespace EricksonLopez.DapperExtensions.MariaDb.TypeHandlers;

/// <summary>
/// Provides convenience registration methods for MariaDB JSON type handlers.
/// </summary>
public static class MariaDbTypeHandlerRegistrar
{
    /// <summary>
    /// Registers a JSON type handler for the specified type with Dapper.
    /// </summary>
    /// <typeparam name="T">The .NET type to map to JSON columns.</typeparam>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
        "JSON type handlers use System.Text.Json reflection-based serialization. " +
        "For NativeAOT and trimmed applications, use source-generated JsonSerializerContext overloads. " +
        "See ADR-006 for the library's AOT policy.")]
    public static void RegisterJsonHandler<T>()
        => SqlMapper.AddTypeHandler(new JsonTypeHandler<T>());
}
