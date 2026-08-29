// Copyright © Erickson Lopez. MIT License.
using Dapper;

namespace EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers;

/// <summary>
/// Provides convenience registration methods for PostgreSQL JSONB type handlers.
/// </summary>
public static class NpgsqlTypeHandlerRegistrar
{
    /// <summary>
    /// Registers a JSONB type handler for the specified type with Dapper.
    /// </summary>
    /// <typeparam name="T">The .NET type to map to JSONB columns.</typeparam>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
        "JSON type handlers use System.Text.Json reflection-based serialization. " +
        "For NativeAOT and trimmed applications, use source-generated JsonSerializerContext overloads. " +
        "See ADR-006 for the library's AOT policy.")]
    public static void RegisterJsonbHandler<T>()
        => SqlMapper.AddTypeHandler(new JsonbTypeHandler<T>());
}
