// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.UnitOfWork;

/// <summary>
/// Default implementation of <see cref="IUnitOfWork"/>.
/// Creates a database transaction and provides async-first transaction management
/// with automatic rollback on disposal if <see cref="CommitAsync"/> was not called.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbTransaction _transaction;
    private bool _committed;
    private bool _disposed;

    internal UnitOfWork(IDbTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <inheritdoc/>
    public IDbTransaction Transaction => _transaction;

    /// <inheritdoc/>
    public IsolationLevel IsolationLevel => _transaction.IsolationLevel;

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is DbTransaction dbTx)
        {
            await dbTx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _transaction.Commit();
        }

        _committed = true;
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is DbTransaction dbTx)
        {
            await dbTx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _transaction.Rollback();
        }
    }

    /// <inheritdoc/>
    public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Savepoint name must not be empty.", nameof(name));

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is DbTransaction dbTx)
        {
            await dbTx.SaveAsync(name, cancellationToken).ConfigureAwait(false);
            return new Savepoint(dbTx, name);
        }

        // Fallback: savepoints not supported — return a no-op savepoint
        return new NoOpSavepoint(name);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_committed)
        {
            try
            {
                if (_transaction is DbTransaction dbTx)
                {
                    await dbTx.RollbackAsync().ConfigureAwait(false);
                }
                else
                {
                    _transaction.Rollback();
                }
            }
            catch (Exception)
            {
                // Swallow rollback errors during disposal — the connection may already be closed.
                // The original exception (if any) is already propagating.
            }
        }

        if (_transaction is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _transaction.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Savepoint implementations
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class Savepoint : ISavepoint
    {
        private readonly DbTransaction _transaction;

        public Savepoint(DbTransaction transaction, string name)
        {
            _transaction = transaction;
            Name = name;
        }

        public string Name { get; }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => _transaction.RollbackAsync(Name, cancellationToken);

        public Task ReleaseAsync(CancellationToken cancellationToken = default)
            => _transaction.ReleaseAsync(Name, cancellationToken);
    }

    private sealed class NoOpSavepoint : ISavepoint
    {
        public NoOpSavepoint(string name) => Name = name;
        public string Name { get; }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}



