// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.DapperExtensions;

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Order aggregate root decorated with [SqlEntity].
/// </summary>
[SqlEntity(TableName = "orders")]
public partial class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
    public decimal TotalAmount { get; set; }
    public DateOnly OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
