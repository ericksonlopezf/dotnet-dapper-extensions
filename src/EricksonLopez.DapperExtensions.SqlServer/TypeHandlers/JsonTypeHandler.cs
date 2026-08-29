// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;

namespace EricksonLopez.DapperExtensions.SqlServer.TypeHandlers;

/// <summary>
/// Provides a Dapper type handler for SQL Server JSON columns using System.Text.Json serialization.
/// </summary>
/// <typeparam name="T">The .NET type to serialize and deserialize.</typeparam>
[RequiresUnreferencedCode(
    "JSON type handlers use System.Text.Json reflection-based serialization. " +
    "For NativeAOT and trimmed applications, use source-generated JsonSerializerContext overloads. " +
    "See ADR-006 for the library's AOT policy.")]
public sealed class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    private static readonly System.Text.Json.JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = value is null
            ? DBNull.Value
            : System.Text.Json.JsonSerializer.Serialize(value, _options);
        parameter.DbType = DbType.String;
    }

    /// <inheritdoc/>
    public override T? Parse(object value)
    {
        if (value is DBNull or null)
            return default;

        var json = value.ToString()!;
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
    }
}
