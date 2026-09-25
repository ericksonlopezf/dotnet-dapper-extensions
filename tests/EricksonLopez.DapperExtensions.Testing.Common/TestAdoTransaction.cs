// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

/// <summary>
/// Reusable test transaction double with invocation tracking and configurable delegates.
/// </summary>
public sealed class TestAdoTransaction : DbTransaction
{
    private readonly DbConnection _connection;
    private readonly IsolationLevel _isolationLevel;

    public TestAdoTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public override IsolationLevel IsolationLevel => _isolationLevel;
    protected override DbConnection DbConnection => _connection;

    public int CommitCount { get; private set; }
    public int CommitAsyncCount { get; private set; }
    public int RollbackCount { get; private set; }
    public int RollbackAsyncCount { get; private set; }

    public bool WasCommitCalled => CommitCount > 0;
    public bool WasCommitAsyncCalled => CommitAsyncCount > 0;
    public bool WasRollbackCalled => RollbackCount > 0;
    public bool WasRollbackAsyncCalled => RollbackAsyncCount > 0;

    public Action? OnCommit { get; set; }
    public Action? OnRollback { get; set; }
    public Func<CancellationToken, Task>? OnCommitAsync { get; set; }
    public Func<CancellationToken, Task>? OnRollbackAsync { get; set; }

    public override void Commit()
    {
        CommitCount++;
        OnCommit?.Invoke();
    }

    public override void Rollback()
    {
        RollbackCount++;
        OnRollback?.Invoke();
    }

    public override async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitAsyncCount++;
        if (OnCommitAsync != null)
        {
            await OnCommitAsync(cancellationToken);
        }
        else
        {
            Commit();
        }
    }

    public override async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackAsyncCount++;
        if (OnRollbackAsync != null)
        {
            await OnRollbackAsync(cancellationToken);
        }
        else
        {
            Rollback();
        }
    }
}
