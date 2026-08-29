// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers;

namespace EricksonLopez.DapperExtensions.PostgreSql.Benchmarks;

public class TestPayload
{
    public string Name { get; set; } = "Test";
    public int Value { get; set; } = 42;
    public bool IsActive { get; set; } = true;
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class JsonbBenchmarks
{
    private JsonbTypeHandler<TestPayload> _handler = null!;
    private TestPayload _payload = null!;
    private string _json = string.Empty;

    [GlobalSetup]
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Benchmark projects are not trimmed. JsonbTypeHandler<T> requires unreferenced code only in trimmed/NativeAOT contexts.")]
    public void Setup()
    {
        _handler = new JsonbTypeHandler<TestPayload>();
        _payload = new TestPayload();

        // Serialize once to get the string for deserialization benchmarks
        var parameter = new FakeDataParameter();
        _handler.SetValue(parameter, _payload);
        _json = parameter.Value?.ToString() ?? "{}";
    }

    [Benchmark]
    public void JsonbTypeHandler_Serialize_Performance()
    {
        var parameter = new FakeDataParameter();
        _handler.SetValue(parameter, _payload);
    }

    [Benchmark]
    public void JsonbTypeHandler_Deserialize_Performance()
    {
        var result = _handler.Parse(_json);
    }

#pragma warning disable CS8767, CS8766, CS8769
    private sealed class FakeDataParameter : IDbDataParameter
    {
        public string ParameterName { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable { get; }
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }
#pragma warning restore CS8767, CS8766, CS8769
}


