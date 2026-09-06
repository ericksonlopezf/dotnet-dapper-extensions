
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=3  UnrollFactor=1  
WarmupCount=1  

 Method              | RowCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
-------------------- |--------- |-----:|------:|------:|--------:|------------:|
 'Row-by-row INSERT' | 100      |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  BulkInsertBenchmarks.'Row-by-row INSERT': Job-FEWCWF(InvocationCount=1, IterationCount=3, UnrollFactor=1, WarmupCount=1) [RowCount=100]
