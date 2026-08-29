// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Showcase.Infrastructure;
using EricksonLopez.DapperExtensions.Showcase.Models;
using EricksonLopez.DapperExtensions.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Showcase.Levels.Level10_EnterpriseArchitecture;

/// <summary>
/// Level 10 — Enterprise Architecture: Transactional Outbox Pattern, Repositories with UoW, and Sagas with Savepoints.
/// </summary>
public static class EnterprisePatternsDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader(10, "Enterprise Architecture", "Transactional Outbox Pattern, Unit of Work Repositories, and Sagas with Savepoints");

        using var connection = await ShowcaseDbContext.CreateOpenConnectionAsync().ConfigureAwait(false);
        await ShowcaseDbContext.SeedSampleDataAsync(connection).ConfigureAwait(false);

        var pipeline = SqlResilienceDefaults.ForSqlite();
        var orderRepo = new DapperOrderRepository();

        ConsoleHelper.PrintStep("1. Transactional Outbox Pattern with Unit of Work and Resilience (ADR-016)");

        var newOrder = new Order
        {
            CustomerId = 1,
            OrderNumber = "ORD-ENT-999",
            Status = OrderStatus.PendingPayment,
            PaymentMethod = PaymentMethod.CreditCard,
            TotalAmount = 249.99m,
            OrderDate = new DateOnly(2026, 8, 26)
        };

        // Wrap entire transactional scope in resilience pipeline
        await pipeline.ExecuteAsync(async ct =>
        {
            await using var uow = await connection.BeginUnitOfWorkAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);

            // 1. Domain Mutation
            await orderRepo.CreateOrderAsync(newOrder, uow, ct).ConfigureAwait(false);
            ConsoleHelper.PrintInfo("Domain", "Order ORD-ENT-999 persisted");

            // 2. Outbox Event in the same atomic transaction
            const string outboxSql = """
                INSERT INTO outbox_messages (id, message_type, payload, status, created_at)
                VALUES (@Id, @MessageType, @Payload, 'Pending', @CreatedAt);
                """;

            var outboxMessage = new
            {
                Id = Guid.NewGuid().ToString(),
                MessageType = "OrderPlacedDomainEvent",
                Payload = "{\"orderNumber\":\"ORD-ENT-999\",\"amount\":249.99}",
                CreatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };

            await connection.ExecuteAsync(new CommandDefinition(
                outboxSql,
                outboxMessage,
                transaction: uow.Transaction,
                cancellationToken: ct)).ConfigureAwait(false);

            ConsoleHelper.PrintInfo("Outbox", "Event OrderPlacedDomainEvent enqueued atomically");

            // Commit atomic unit of work
            await uow.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConsoleHelper.PrintSuccess("Order and Outbox message committed atomically.");

        ConsoleHelper.PrintStep("2. Outbox Dispatcher Simulation");
        const string pendingMessagesSql = "SELECT id, message_type AS MessageType, payload, status FROM outbox_messages WHERE status = 'Pending';";
        var pendingMessages = await connection.QueryAsync(pendingMessagesSql).ConfigureAwait(false);

        foreach (var msg in pendingMessages)
        {
            ConsoleHelper.PrintInfo("Dispatching Message", $"{msg.id} [{msg.MessageType}]");

            await connection.ExecuteAsync(
                "UPDATE outbox_messages SET status = 'Processed', processed_at = @ProcessedAt WHERE id = @Id;",
                new { Id = msg.id, ProcessedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }).ConfigureAwait(false);
        }

        ConsoleHelper.PrintSuccess("Dispatcher processed and marked all Outbox events.");

        ConsoleHelper.PrintSuccess("Level 10 completed successfully.");
    }
}
