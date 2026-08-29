// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.UnitOfWork;

/// <summary>
/// Represents a named savepoint within a database transaction, enabling partial rollbacks.
/// </summary>
public interface ISavepoint
{
    /// <summary>
    /// Gets the name of the savepoint.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Rolls back the database transaction to this savepoint.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the savepoint, removing it from the active transaction.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
