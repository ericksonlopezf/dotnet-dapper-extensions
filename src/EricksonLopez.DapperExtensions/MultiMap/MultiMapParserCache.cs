// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EricksonLopez.DapperExtensions.MultiMap;

internal static class MultiMapParserCache<TEntity>
{
    public static readonly Func<IDataReader, object>? Parser = InitializeParser();

    [UnconditionalSuppressMessage("Trimming", "IL2090",
        Justification = "AOT-safe code path: IDataReaderMapper<T> source-generated parsers avoid reflection. " +
                        "Reflection fallback via GetMultiMapReaderFactory is a progressive enhancement for non-AOT scenarios. " +
                        "Documented in ADR-006 as an acceptable architectural trade-off for the reflection fallback path.")]
    private static Func<IDataReader, object>? InitializeParser()
    {
        var factoryMethod = typeof(TEntity).GetMethod("GetMultiMapReaderFactory", BindingFlags.Public | BindingFlags.Static);
        return factoryMethod != null
            ? (Func<IDataReader, object>)factoryMethod.Invoke(null, null)!
            : null;
    }
}
