// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Testing.Common;
using EricksonLopez.DapperExtensions.UnitOfWork;
using Xunit;
using UowImpl = EricksonLopez.DapperExtensions.UnitOfWork.UnitOfWork;

namespace EricksonLopez.DapperExtensions.Tests.UnitOfWork;

/// <summary>
/// Fast, isolated unit tests validating guard clauses and contract preconditions for IUnitOfWork and UnitOfWorkExtensions.
/// Does not spin up SQLite tables or databases.
/// </summary>
public sealed class UnitOfWorkGuardsTests
{
    [Fact]
    public void UnitOfWork_Constructor_WhenTransactionNull_ThrowsArgumentNullException()
    {
        var act = () => new UowImpl(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public async Task BeginUnitOfWorkAsync_WhenIDbConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var act = async () => await connection.BeginUnitOfWorkAsync();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task BeginUnitOfWorkAsync_WhenDbConnectionNull_ThrowsArgumentNullException()
    {
        DbConnection connection = null!;
        var act = async () => await connection.BeginUnitOfWorkAsync();
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var act = async () => await connection.WithUnitOfWorkAsync(async (uow, ct) => await Task.Yield());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_WhenActionNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        var act = async () => await connection.WithUnitOfWorkAsync((Func<IUnitOfWork, CancellationToken, Task>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_WhenBothConnectionAndActionNull_ThrowsForConnectionFirst()
    {
        IDbConnection connection = null!;
        var act = async () => await connection.WithUnitOfWorkAsync((Func<IUnitOfWork, CancellationToken, Task>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_Generic_WhenConnectionNull_ThrowsArgumentNullException()
    {
        IDbConnection connection = null!;
        var act = async () => await connection.WithUnitOfWorkAsync(async (uow, ct) => { await Task.Yield(); return 42; });
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_Generic_WhenActionNull_ThrowsArgumentNullException()
    {
        using var connection = new TestAdoConnection();
        var act = async () => await connection.WithUnitOfWorkAsync<int>((Func<IUnitOfWork, CancellationToken, Task<int>>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task WithUnitOfWorkAsync_Generic_WhenBothConnectionAndActionNull_ThrowsForConnectionFirst()
    {
        IDbConnection connection = null!;
        var act = async () => await connection.WithUnitOfWorkAsync<int>((Func<IUnitOfWork, CancellationToken, Task<int>>)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }
}
