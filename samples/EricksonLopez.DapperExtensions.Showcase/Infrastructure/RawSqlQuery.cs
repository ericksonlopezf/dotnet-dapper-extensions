// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Minimal ISqlQuery implementation for Showcase demos.
/// In production, use EricksonLopez.SqlBuilder to build type-safe SQL via the fluent API
/// with full compiler optimization, parameter management, and AOT safety.
/// </summary>
#pragma warning disable IL2026, IL3050 // Required by ISqlQuery.Build signature
internal sealed class RawSqlQuery : ISqlQuery
{
    private readonly string _sql;
    private readonly IReadOnlyDictionary<string, object?> _parameters;

    public RawSqlQuery(string sql, IReadOnlyDictionary<string, object?> parameters)
    {
        _sql = sql;
        _parameters = parameters;
    }

    /// <inheritdoc/>
    public string? Tag => null;

    /// <inheritdoc/>
    [RequiresDynamicCode("Showcase raw SQL query path does not use dynamic code.")]
    [RequiresUnreferencedCode("Showcase raw SQL query path does not access unreferenced members.")]
    public SqlResult Build(ISqlCompiler compiler) => new SqlResult(_sql, _parameters);
}
#pragma warning restore IL2026, IL3050
