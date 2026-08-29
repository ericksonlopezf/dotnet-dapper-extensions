// Copyright © Erickson Lopez. MIT License.
// NOTE: Benchmarks require a running PostgreSQL instance.
// Set the connection string via environment variable: BENCHMARK_PG_CONN
// Example: "Host=localhost;Database=benchdb;Username=postgres;Password=postgres"
//
// The table must exist:
// CREATE TABLE IF NOT EXISTS bench_products (
//     id uuid PRIMARY KEY, name text NOT NULL, price numeric NOT NULL
// );
using System;
using BenchmarkDotNet.Running;
using EricksonLopez.DapperExtensions.PostgreSql.Benchmarks;

BenchmarkRunner.Run<BulkInsertBenchmarks>(args: args);
