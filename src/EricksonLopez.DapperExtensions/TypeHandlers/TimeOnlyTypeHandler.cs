// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace EricksonLopez.DapperExtensions.TypeHandlers;

/// <summary>
/// Provides a Dapper type handler for mapping <see cref="TimeOnly"/> values to and from database time and timespan columns.
/// </summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    /// <summary>
    /// Gets the default singleton instance of the <see cref="TimeOnlyTypeHandler"/> class.
    /// </summary>
    public static readonly TimeOnlyTypeHandler Default = new();

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    /// <inheritdoc/>
    public override TimeOnly Parse(object value)
    {
        return value switch
        {
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            DateTime dt => TimeOnly.FromDateTime(dt),
            string s when TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => TimeOnly.FromTimeSpan((TimeSpan)value)
        };
    }
}
