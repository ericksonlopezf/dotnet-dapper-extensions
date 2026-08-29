// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.MySql.Transactions;

/// <summary>
/// Provides extension methods for executing operations within a MySQL transaction scope.
/// </summary>
public static class TransactionExtensions
{
    /// <summary>
    /// Executes an asynchronous operation within a newly opened transaction, committing automatically on success and rolling back on failure.
    /// </summary>
    /// <param name="connection">The database connection to execute the transaction on.</param>
    /// <param name="operation">The asynchronous operation to execute within the active transaction.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task ExecuteInTransactionAsync(
        this DbConnection connection,
        Func<DbTransaction, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(operation);

        bool wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                await operation(transaction).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                if (wasClosed)
                    await connection.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes an asynchronous operation returning a result within a newly opened transaction, committing on success and rolling back on failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="connection">The database connection to execute the transaction on.</param>
    /// <param name="operation">The asynchronous operation returning a result to execute within the active transaction.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the value returned by <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
        this DbConnection connection,
        Func<DbTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(operation);

        bool wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TResult result = default!;
            try
            {
                result = await operation(transaction).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                if (wasClosed)
                    await connection.CloseAsync().ConfigureAwait(false);
            }
            return result;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
