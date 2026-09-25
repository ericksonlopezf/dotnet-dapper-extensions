// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace EricksonLopez.DapperExtensions.MultiMap;

/// <summary>
/// Provides a low-level, Native AOT-compatible reader for multi-entity query results
/// that parses each row using explicit per-type reader delegates without Dapper reflection.
/// </summary>
/// <remarks>
/// <para>
/// Intended as a standalone utility for callers who need direct,
/// low-level access to the AOT multi-entity reading loop - for example, in custom query
/// runners, benchmarks, or integration tests that validate AOT mapping independently.
/// </para>
/// <para>
/// MultiMapBuilder implements its own inline AOT reader loop for performance (avoiding
/// delegate indirection per row). Both implementations are intentionally maintained in
/// parallel: this reader covers the testable, documented utility surface; the builder
/// covers the optimized production path.
/// </para>
/// </remarks>
internal static class AotMultiMapReader
{
    /// <summary>
    /// Executes a low-level, Native AOT-compatible multi-mapping query using explicit per-type reader delegates.
    /// </summary>
    /// <typeparam name="TReturn">The root entity type to map.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="sql">The SQL query text.</param>
    /// <param name="param">The optional query parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="commandType">The optional command type.</param>
    /// <param name="mappings">The list of entity mapping specifications and reader delegates.</param>
    /// <param name="combiners">The array of combiner functions to merge related entities into the root.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an enumerable of hydrated root entities.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAotAsync<TReturn>(
        IDbConnection connection,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IReadOnlyList<(Type Type, string SplitOn, Func<IDataReader, object> Parser)> mappings,
        Func<object[], TReturn, TReturn>[] combiners)
    {
        var results = new List<TReturn>();

        using var reader = await connection.ExecuteReaderAsync(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType).ConfigureAwait(false);

        while (reader.Read())
        {
            var parts = new object[mappings.Count + 1];
            parts[0] = mappings[0].Parser(reader);

            for (int i = 0; i < combiners.Length; i++)
            {
                parts[i + 1] = mappings[i + 1].Parser(reader);
            }

            var root = (TReturn)parts[0];
            for (int i = 0; i < combiners.Length; i++)
            {
                root = combiners[i](parts[(i + 1)..], root);
            }

            results.Add(root);
        }

        return results;
    }
}
