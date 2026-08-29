// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using Dapper;
using EricksonLopez.DapperExtensions.Showcase.Models;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Custom Dapper TypeHandler for Money value objects.
/// </summary>
public sealed class MoneyTypeHandler : SqlMapper.TypeHandler<Money>
{
    public static readonly MoneyTypeHandler Default = new();

    public override void SetValue(IDbDataParameter parameter, Money value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.Decimal;
        parameter.Value = value.Amount;
    }

    public override Money Parse(object value)
    {
        var amount = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return new Money(amount, "USD");
    }
}
