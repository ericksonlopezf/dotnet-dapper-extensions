// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.DapperExtensions;

/// <summary>
/// Specifies that a class or struct is a SQL entity suitable for source-generated, zero-reflection Native AOT
/// <see cref="MultiMap.IDataReaderMapper{T}"/> hydration.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SqlEntityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlEntityAttribute"/> class.
    /// </summary>
    public SqlEntityAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the optional table name associated with this entity.
    /// </summary>
    public string? TableName { get; set; }
}
