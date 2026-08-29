// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Model for demonstrating JSON column serialization / deserialization.
/// </summary>
public sealed class ProductMetadata
{
    public string? Format { get; set; }
    public int? WeightG { get; set; }
    public int? FileSizeMb { get; set; }
    public int? Hours { get; set; }
}
