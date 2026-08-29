// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.DapperExtensions;

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Customer entity decorated with [SqlEntity] for zero-reflection Native AOT mapping.
/// </summary>
[SqlEntity(TableName = "customers")]
public partial class Customer
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public CustomerTier Tier { get; set; } = CustomerTier.Standard;
    public DateOnly RegisteredDate { get; set; }
}
