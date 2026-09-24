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
/// propagation, nested savepoints, enlistment hooks, and monadic auto-rollback) is managed by
/// <c>ITransactionManager</c> in the <c>EricksonLopez.Transaction</c> package.
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
    /// Savepoints require the underlying <see cref="System.Data.IDbTransaction"/> to be an instance of
    /// <see cref="System.Data.Common.DbTransaction"/> (i.e., a real ADO.NET provider transaction).
    /// If the underlying transaction is not a <see cref="System.Data.Common.DbTransaction"/> — for example,
    /// when using a custom mock or a non-standard in-memory <see cref="System.Data.IDbTransaction"/> implementation —
    /// a <see cref="NotSupportedException"/> is thrown.
    /// </para>
    /// <para>
    /// To test code that creates savepoints, use a real ADO.NET provider (e.g., SQLite in-memory via
    /// <c>Microsoft.Data.Sqlite</c>) or a test double that derives from <see cref="System.Data.Common.DbTransaction"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty, or whitespace</exception>
    /// <exception cref="NotSupportedException">The underlying transaction does not inherit from <see cref="System.Data.Common.DbTransaction"/> and therefore does not support savepoints</exception>
    /// <exception cref="ObjectDisposedException">The unit of work has already been disposed</exception>
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
}
