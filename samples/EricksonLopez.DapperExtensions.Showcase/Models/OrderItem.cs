// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.DapperExtensions;

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Order line item entity decorated with [SqlEntity].
/// </summary>
[SqlEntity(TableName = "order_items")]
public partial class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
