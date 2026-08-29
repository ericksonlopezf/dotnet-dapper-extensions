// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Domain Repository implementation demonstrating transactional Dapper operations with IUnitOfWork.
/// </summary>
public sealed class DapperOrderRepository : IOrderRepository
{
    public async Task CreateOrderAsync(Order order, IUnitOfWork uow, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(uow);

        const string sql = """
            INSERT INTO orders (customer_id, order_number, status, payment_method, total_amount, order_date)
            VALUES (@CustomerId, @OrderNumber, @Status, @PaymentMethod, @TotalAmount, @OrderDate);
            """;

        await uow.Transaction.Connection!.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                order.CustomerId,
                order.OrderNumber,
                Status = order.Status.ToString(),
                PaymentMethod = order.PaymentMethod.ToString(),
                order.TotalAmount,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
            transaction: uow.Transaction,
            cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<Order?> GetOrderByIdAsync(long id, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = "SELECT id, customer_id AS CustomerId, order_number AS OrderNumber, status AS Status, payment_method AS PaymentMethod, total_amount AS TotalAmount, order_date AS OrderDate FROM orders WHERE id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<Order>(new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
