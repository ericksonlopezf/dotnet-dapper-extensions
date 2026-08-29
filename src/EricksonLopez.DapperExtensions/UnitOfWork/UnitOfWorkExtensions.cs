// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.UnitOfWork;

/// <summary>
/// Provides extension methods on <see cref="IDbConnection"/> for starting transactions encapsulated as an <see cref="IUnitOfWork"/>.
/// </summary>
public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Begins a new transaction wrapped in an <see cref="IUnitOfWork"/> with the specified isolation level.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="isolationLevel">The isolation level for the transaction. Defaults to <see cref="IsolationLevel.ReadCommitted"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an active <see cref="IUnitOfWork"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static async Task<IUnitOfWork> BeginUnitOfWorkAsync(
        this IDbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State != ConnectionState.Open)
        {
            if (connection is DbConnection dbConn)
            {
                await dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                connection.Open();
            }
        }

        IDbTransaction transaction;
        if (connection is DbConnection asyncDbConn)
        {
            transaction = await asyncDbConn.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            transaction = connection.BeginTransaction(isolationLevel);
        }

        return new UnitOfWork(transaction);
    }

    /// <summary>
    /// Begins a new transaction wrapped in an <see cref="IUnitOfWork"/> using fully asynchronous methods on <see cref="DbConnection"/>.
    /// </summary>
    /// <param name="connection">The asynchronous database connection.</param>
    /// <param name="isolationLevel">The isolation level for the transaction. Defaults to <see cref="IsolationLevel.ReadCommitted"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an active <see cref="IUnitOfWork"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static async Task<IUnitOfWork> BeginUnitOfWorkAsync(
        this DbConnection connection,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        return new UnitOfWork(transaction);
    }

    /// <summary>
    /// Executes a transactional operation using a Unit of Work scope, committing automatically on success and rolling back on failure.
    /// </summary>
    /// <param name="connection">The database connection to execute the operation on.</param>
    /// <param name="action">The asynchronous operation to execute within the transaction scope.</param>
    /// <param name="isolationLevel">The isolation level for the transaction. Defaults to <see cref="IsolationLevel.ReadCommitted"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="action"/> is <see langword="null"/></exception>
    public static async Task WithUnitOfWorkAsync(
        this IDbConnection connection,
        Func<IUnitOfWork, CancellationToken, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(action);

        await using var uow = await connection.BeginUnitOfWorkAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        await action(uow, cancellationToken).ConfigureAwait(false);
        await uow.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a transactional operation returning a result using a Unit of Work scope, committing automatically on success and rolling back on failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="connection">The database connection to execute the operation on.</param>
    /// <param name="action">The asynchronous operation to execute within the transaction scope.</param>
    /// <param name="isolationLevel">The isolation level for the transaction. Defaults to <see cref="IsolationLevel.ReadCommitted"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the value returned by <paramref name="action"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="action"/> is <see langword="null"/></exception>
    public static async Task<TResult> WithUnitOfWorkAsync<TResult>(
        this IDbConnection connection,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(action);

        await using var uow = await connection.BeginUnitOfWorkAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        var result = await action(uow, cancellationToken).ConfigureAwait(false);
        await uow.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}



