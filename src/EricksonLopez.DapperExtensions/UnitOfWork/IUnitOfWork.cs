// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.UnitOfWork;

/// <summary>
/// Represents an active database Unit of Work that manages a transaction boundary across multiple operations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Architectural Role &amp; Clean Architecture Boundary:</strong>
/// <c>IUnitOfWork</c> represents the application/domain boundary contract expressing that a set of operations belongs
/// to a single logical unit of work. Infrastructure-level transactional coordination (such as ambient <c>AsyncLocal</c>
/// propagation, nested savepoints, enlistment hooks, and monadic auto-rollback) is managed by <c>ITransactionManager</c>
/// in <c>EricksonLopez.Transaction</c>.
/// </para>
/// <para>
/// If <see cref="CommitAsync"/> is not called before disposal, the transaction is automatically rolled back.
/// </para>
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets the underlying database transaction.
    /// </summary>
    IDbTransaction Transaction { get; }

    /// <summary>
    /// Gets the isolation level of the active transaction.
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Commits all operations executed within this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">The unit of work has already been disposed</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all operations executed within this unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">The unit of work has already been disposed</exception>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a named savepoint within the active transaction.
    /// </summary>
    /// <param name="name">The name of the savepoint.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="ISavepoint"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// If the underlying <see cref="System.Data.IDbTransaction"/> is not a <see cref="System.Data.Common.DbTransaction"/>
    /// (for example, when using mock or in-memory database drivers), this method returns a no-op savepoint.
    /// On a no-op savepoint, <see cref="ISavepoint.RollbackAsync"/> and <see cref="ISavepoint.ReleaseAsync"/>
    /// complete without any database-level effect.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The unit of work has already been disposed</exception>
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
}
