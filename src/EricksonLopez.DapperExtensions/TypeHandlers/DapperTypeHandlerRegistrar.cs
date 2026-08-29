// Copyright © Erickson Lopez. MIT License.
using System;
using Dapper;

namespace EricksonLopez.DapperExtensions.TypeHandlers;

/// <summary>
/// Provides registration methods for standard Dapper type handlers.
/// </summary>
public static class DapperTypeHandlerRegistrar
{
    /// <summary>
    /// Registers standard modern .NET type handlers (<see cref="DateOnly"/> and <see cref="TimeOnly"/>) with Dapper.
    /// </summary>
    /// <remarks>
    /// Call once during application initialization before executing queries.
    /// </remarks>
    public static void RegisterStandardHandlers()
    {
        SqlMapper.AddTypeHandler(DateOnlyTypeHandler.Default);
        SqlMapper.AddTypeHandler(TimeOnlyTypeHandler.Default);
    }

    /// <summary>
    /// Registers a string-based enum handler for the specified enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type to map as strings.</typeparam>
    public static void RegisterStringEnumHandler<TEnum>() where TEnum : struct, Enum
    {
        SqlMapper.AddTypeHandler(StringEnumTypeHandler<TEnum>.Default);
    }
}
