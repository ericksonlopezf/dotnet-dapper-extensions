// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.DapperExtensions;

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Product entity decorated with [SqlEntity] for zero-reflection Native AOT mapping.
/// </summary>
[SqlEntity(TableName = "products")]
public partial class Product
{
    public long Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly ReleaseDate { get; set; }
    public TimeOnly DailyRestockTime { get; set; }
    public string? MetadataJson { get; set; }
}
