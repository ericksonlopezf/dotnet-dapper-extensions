// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapper;
using EricksonLopez.DapperExtensions.PostgreSql.Transactions;
using Npgsql;

namespace EricksonLopez.DapperExtensions.PostgreSql.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public sealed class TransactionBenchmarks : IDisposable
{
    private NpgsqlConnection _connection = null!;
    private bool _disposed;

    [GlobalSetup]
    public async Task Setup()
    {
        var connStr = Environment.GetEnvironmentVariable("BENCHMARK_PG_CONN")
            ?? "Host=localhost;Database=benchdb;Username=postgres;Password=postgres";

        _connection = new NpgsqlConnection(connStr);
        await _connection.OpenAsync();

        await _connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS bench_tx (
                id    UUID    NOT NULL PRIMARY KEY,
                val   INT     NOT NULL
            )
            """);
    }

    [IterationSetup]
    public async Task CleanTable()
    {
        await _connection.ExecuteAsync("TRUNCATE TABLE bench_tx");
    }

    [Benchmark(Baseline = true)]
    public async Task ExecuteInTransactionAsync_Performance()
    {
        await _connection.ExecuteInTransactionAsync(async tx =>
        {
            await _connection.ExecuteAsync(
                "INSERT INTO bench_tx (id, val) VALUES (@Id, @Val)",
                new { Id = Guid.NewGuid(), Val = 1 },
                transaction: tx);
        });
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _connection.ExecuteAsync("DROP TABLE IF EXISTS bench_tx");
        await _connection.DisposeAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
