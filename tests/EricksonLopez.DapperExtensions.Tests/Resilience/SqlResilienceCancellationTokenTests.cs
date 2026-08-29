// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Polly;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Resilience;

public sealed class SqlResilienceCancellationTokenTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private readonly ResiliencePipeline _pipeline = SqlResilienceDefaults.Standard(SqliteTransientErrorDetector.Default);

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new SqlResult("SELECT 1", new Dictionary<string, object?>());

        var act = async () => await _connection.ExecuteWithResilienceAsync(
            query,
            _pipeline,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryWithResilienceAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new SqlResult("SELECT 1", new Dictionary<string, object?>());

        var act = async () => await _connection.QueryWithResilienceAsync<int>(
            query,
            _pipeline,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QuerySingleWithResilienceAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new SqlResult("SELECT 1", new Dictionary<string, object?>());

        var act = async () => await _connection.QuerySingleWithResilienceAsync<int>(
            query,
            _pipeline,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteScalarWithResilienceAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new SqlResult("SELECT 1", new Dictionary<string, object?>());

        var act = async () => await _connection.ExecuteScalarWithResilienceAsync<int>(
            query,
            _pipeline,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
