// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace EricksonLopez.DapperExtensions.TypeHandlers;

/// <summary>
/// Provides a Dapper type handler for mapping <see cref="DateOnly"/> values to and from database date and datetime columns.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    /// <summary>
    /// Gets the default singleton instance of the <see cref="DateOnlyTypeHandler"/> class.
    /// </summary>
    public static readonly DateOnlyTypeHandler Default = new();

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    /// <inheritdoc/>
    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            string s when DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var parsed) => parsed,
            DateTimeOffset dto => DateOnly.FromDateTime(dto.DateTime),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value, CultureInfo.InvariantCulture))
        };
    }
}
