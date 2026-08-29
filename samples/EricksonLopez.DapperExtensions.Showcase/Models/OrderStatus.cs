// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.DapperExtensions.Showcase.Models;

/// <summary>
/// Domain enumeration for order state lifecycle.
/// </summary>
public enum OrderStatus
{
    Draft = 0,
    PendingPayment = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
