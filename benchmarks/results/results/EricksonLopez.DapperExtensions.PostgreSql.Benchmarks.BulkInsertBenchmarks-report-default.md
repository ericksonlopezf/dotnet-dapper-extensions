
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=3  UnrollFactor=1  
WarmupCount=1  

 Method              | RowCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
-------------------- |--------- |-----:|------:|------:|--------:|------------:|
 'Row-by-row INSERT' | 100      |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  BulkInsertBenchmarks.'Row-by-row INSERT': Job-FEWCWF(InvocationCount=1, IterationCount=3, UnrollFactor=1, WarmupCount=1) [RowCount=100]
