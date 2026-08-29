// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using Dapper;

namespace EricksonLopez.DapperExtensions.TypeHandlers;

/// <summary>
/// Provides a Dapper type handler for mapping enumeration values to and from string or varchar database columns.
/// </summary>
/// <typeparam name="TEnum">The enumeration type to map.</typeparam>
public sealed class StringEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the default singleton instance of the <see cref="StringEnumTypeHandler{TEnum}"/> class.
    /// </summary>
    public static readonly StringEnumTypeHandler<TEnum> Default = new();

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, TEnum value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    /// <inheritdoc/>
    public override TEnum Parse(object value)
    {
        if (value is null or DBNull)
        {
            return default;
        }

        if (value is TEnum directEnum)
            return directEnum;

        var str = value.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return default;
        }

        if (Enum.TryParse<TEnum>(str, ignoreCase: true, out var result))
        {
            return result;
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }
}
