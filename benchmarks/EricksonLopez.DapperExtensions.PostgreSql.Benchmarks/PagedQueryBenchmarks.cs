// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapper;
using EricksonLopez.DapperExtensions.PostgreSql.Pagination;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Npgsql;

namespace EricksonLopez.DapperExtensions.PostgreSql.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public sealed class PagedQueryBenchmarks : IDisposable
{
    private NpgsqlConnection _connection = null!;
    private PaginationParameters _page100;
    private PaginationParameters _page1000;
    private bool _disposed;

    [GlobalSetup]
    public async Task Setup()
    {
        var connStr = Environment.GetEnvironmentVariable("BENCHMARK_PG_CONN")
            ?? "Host=localhost;Database=benchdb;Username=postgres;Password=postgres";

        _connection = new NpgsqlConnection(connStr);
        await _connection.OpenAsync();

        await _connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS bench_paged (
                id    UUID    NOT NULL PRIMARY KEY,
                name  TEXT    NOT NULL
            )
            """);

        await _connection.ExecuteAsync("TRUNCATE TABLE bench_paged");

        var data = Enumerable.Range(1, 10_000)
            .Select(i => new { Id = Guid.NewGuid(), Name = $"Item {i}" })
            .ToList();

        var insertSql = "INSERT INTO bench_paged (id, name) VALUES (@Id, @Name)";

        // Simple sequential insert for benchmark setup
        foreach (var item in data)
        {
            await _connection.ExecuteAsync(insertSql, item);
        }

        _page100 = PaginationParameters.Create(1, 100);
        _page1000 = PaginationParameters.Create(1, 1000);
    }

    [Benchmark(Baseline = true, Description = "QueryPagedAsync - 100 rows")]
    public async Task QueryPagedAsync_100Rows()
    {
        await _connection.QueryPagedAsync<dynamic>(
            "SELECT * FROM bench_paged",
            "SELECT COUNT(*) FROM bench_paged",
            _page100);
    }

    [Benchmark(Description = "QueryPagedAsync - 1000 rows")]
    public async Task QueryPagedAsync_1000Rows()
    {
        await _connection.QueryPagedAsync<dynamic>(
            "SELECT * FROM bench_paged",
            "SELECT COUNT(*) FROM bench_paged",
            _page1000);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _connection.ExecuteAsync("DROP TABLE IF EXISTS bench_paged");
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
