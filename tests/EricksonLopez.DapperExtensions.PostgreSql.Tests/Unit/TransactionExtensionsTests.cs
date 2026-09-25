// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.PostgreSql.Transactions;
using EricksonLopez.DapperExtensions.Testing.Common;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

public sealed class TransactionExtensionsTests : TransactionExtensionsTestsBase
{
    protected override Task ExecuteAsync(DbConnection connection, Func<DbTransaction, Task> operation, CancellationToken cancellationToken = default)
        => connection.ExecuteInTransactionAsync(operation, cancellationToken);

    protected override Task<TResult> ExecuteAsync<TResult>(DbConnection connection, Func<DbTransaction, Task<TResult>> operation, CancellationToken cancellationToken = default)
        => connection.ExecuteInTransactionAsync(operation, cancellationToken);
}
