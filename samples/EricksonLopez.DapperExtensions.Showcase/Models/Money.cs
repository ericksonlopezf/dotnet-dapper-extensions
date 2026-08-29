// Copyright © Erickson Lopez. MIT License.
using System.Globalization;

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Value object to demonstrate custom TypeHandler implementation.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";
}
