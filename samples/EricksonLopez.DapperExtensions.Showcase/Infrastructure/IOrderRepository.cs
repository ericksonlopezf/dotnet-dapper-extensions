// Copyright © Erickson Lopez. MIT License.
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Domain Repository interface demonstrating IUnitOfWork coordination.
/// </summary>
public interface IOrderRepository
{
    Task CreateOrderAsync(Order order, IUnitOfWork uow, CancellationToken ct = default);
    Task<Order?> GetOrderByIdAsync(long id, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken ct = default);
}
