// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.DapperExtensions.MultiMap;

/// <summary>
/// Provides a fluent builder for configuring and executing multi-mapping queries with Native AOT support.
/// </summary>
/// <typeparam name="TReturn">The root entity type to map.</typeparam>
/// <remarks>
/// Supports up to 7 mapped entities using source-generated <see cref="IDataReaderMapper{T}"/> parsers for Native AOT environments.
/// When reflection fallback is used, mapping is not Native AOT compatible.
/// </remarks>
public sealed class MultiMapBuilder<TReturn> where TReturn : class, new()
{
    private readonly ISqlQuery _query;
    private readonly List<(Type Type, string SplitOn, Func<IDataReader, object>? Parser)> _mappings = new();
    private readonly List<Func<object[], TReturn, TReturn>> _combiners = new();

    private MultiMapBuilder(ISqlQuery query)
    {
        _query = query;
    }

    /// <summary>
    /// Creates a new <see cref="MultiMapBuilder{TReturn}"/> for the specified SQL query.
    /// </summary>
    /// <param name="query">The SQL query whose results will be multi-mapped.</param>
    /// <returns>A new <see cref="MultiMapBuilder{TReturn}"/> instance bound to the query.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/></exception>
    public static MultiMapBuilder<TReturn> Query(ISqlQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new MultiMapBuilder<TReturn>(query);
    }

    /// <summary>
    /// Registers a mapping from the result set to a related entity of type <typeparamref name="T"/>,
    /// using a combiner function to merge the related entity into the root.
    /// </summary>
    /// <typeparam name="T">The type of the related entity to map.</typeparam>
    /// <param name="splitOn">The column name that marks the boundary between the root and the related entity in the result set.</param>
    /// <param name="combiner">A function that receives the root and related entity and returns the updated root.</param>
    /// <param name="parser">
    /// An optional AOT-safe factory that reads an instance of <typeparamref name="T"/> from an <see cref="System.Data.IDataReader"/>.
    /// When <see langword="null"/>, the builder falls back to Dapper's reflection-based mapping.
    /// </param>
    /// <returns>The current builder instance with the mapping registered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="splitOn"/> or <paramref name="combiner"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="splitOn"/> is empty</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2090",
        Justification = "AOT-safe code path: IDataReaderMapper<T> source-generated parsers avoid reflection. " +
                        "Reflection fallback via GetMultiMapReaderFactory is a progressive enhancement for non-AOT scenarios. " +
                        "Documented in ADR-006 as an acceptable architectural trade-off for the reflection fallback path.")]
    public MultiMapBuilder<TReturn> Map<T>(string splitOn, Func<TReturn, T, TReturn> combiner, Func<System.Data.IDataReader, object>? parser = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(splitOn);
        ArgumentNullException.ThrowIfNull(combiner);

        if (parser == null)
        {
            var factoryMethod = typeof(T).GetMethod("GetMultiMapReaderFactory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (factoryMethod != null)
            {
                parser = (Func<System.Data.IDataReader, object>)factoryMethod.Invoke(null, null)!;
            }
        }

        _mappings.Add((typeof(T), splitOn, parser));
        _combiners.Add((parts, root) =>
        {
            if (parts.Length > 0 && parts[0] is T related)
            {
                return combiner(root, related);
            }
            return root;
        });

        return this;
    }

    /// <summary>
    /// Registers a mapping from the result set to a related entity of type <typeparamref name="T"/>,
    /// using an action to apply the related entity to the root.
    /// </summary>
    /// <typeparam name="T">The type of the related entity to map.</typeparam>
    /// <param name="splitOn">The column name that marks the boundary between the root and the related entity in the result set.</param>
    /// <param name="setter">An action that applies the related entity to the root instance.</param>
    /// <param name="parser">
    /// An optional AOT-safe factory that reads an instance of <typeparamref name="T"/> from an <see cref="System.Data.IDataReader"/>.
    /// When <see langword="null"/>, the builder falls back to Dapper's reflection-based mapping.
    /// </param>
    /// <returns>The current builder instance with the mapping registered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setter"/> or <paramref name="splitOn"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="splitOn"/> is empty</exception>
    public MultiMapBuilder<TReturn> Map<T>(string splitOn, Action<TReturn, T> setter, Func<System.Data.IDataReader, object>? parser = null)
    {
        ArgumentNullException.ThrowIfNull(setter);
        return Map<T>(splitOn, (root, related) => { setter(root, related); return root; }, parser);
    }

    /// <summary>
    /// Gets the comma-separated string of split-on column names for all registered mappings.
    /// </summary>
    public string SplitOn => string.Join(",", _mappings.Select(m => m.SplitOn));

    /// <summary>
    /// Gets an array of CLR types representing the root type followed by all registered related entity types.
    /// </summary>
    public Type[] Types
    {
        get
        {
            var types = new Type[_mappings.Count + 1];
            types[0] = typeof(TReturn);
            for (int i = 0; i < _mappings.Count; i++)
            {
                types[i + 1] = _mappings[i].Type;
            }
            return types;
        }
    }

    /// <summary>
    /// Executes the multi-map query and returns an enumerable of hydrated root entities.
    /// </summary>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="compiler">The SQL compiler used to translate the query AST into SQL text.</param>
    /// <param name="transaction">An optional transaction to execute within.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains an enumerable of hydrated <typeparamref name="TReturn"/> instances.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="compiler"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">No entity mappings have been registered, or the root type is missing a generated reader factory</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "ISqlCompiler.Compile() is annotated RequiresUnreferencedCode by the SqlBuilder.Abstractions library. " +
                        "Callers using NativeAOT strict mode should pass pre-compiled SqlResult directly. " +
                        "Documented in ADR-006: trim analyzer enabled; compiler-path suppressed with explicit rationale.")]
    [UnconditionalSuppressMessage("Trimming", "IL2090",
        Justification = "GetMultiMapReaderFactory reflection is the progressive-enhancement AOT path. " +
                        "Fully source-generated IDataReaderMapper<T> path avoids this. ADR-006.")]
    public async Task<IEnumerable<TReturn>> QueryAsync(
        IDbConnection connection,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(compiler);
        cancellationToken.ThrowIfCancellationRequested();

        if (_mappings.Count == 0)
        {
            throw new InvalidOperationException("At least one entity mapping must be registered using Map<T>() before executing the query.");
        }

        var result = compiler.Compile(_query);

        // If any parser is missing, fallback to Dapper
        if (_mappings.Any(m => m.Parser == null))
        {
            var types = Types;
            var combiners = _combiners.ToArray();
            var splitOn = SplitOn;

            TReturn MapCombiner(object[] parts)
            {
                var root = (TReturn)parts[0];
                for (int i = 0; i < combiners.Length; i++)
                {
                    root = combiners[i](parts[(i + 1)..], root);
                }
                return root;
            }

            return await connection.QueryAsync<TReturn>(
                result.Sql,
                types,
                MapCombiner,
                param: result.Parameters,
                transaction: transaction,
                buffered: true,
                splitOn: splitOn,
                commandTimeout: commandTimeout).ConfigureAwait(false);
        }

        // AOT-safe manual parsing
        var rootFactoryMethod = typeof(TReturn).GetMethod("GetMultiMapReaderFactory", BindingFlags.Public | BindingFlags.Static);
        if (rootFactoryMethod == null)
        {
            throw new InvalidOperationException("Root type " + typeof(TReturn).Name + " is missing GetMultiMapReaderFactory(). Ensure it is decorated with [SqlEntity].");
        }
        var rootParser = (Func<IDataReader, object>)rootFactoryMethod.Invoke(null, null)!;

        var parsers = new Func<IDataReader, object>[_mappings.Count + 1];
        parsers[0] = rootParser;
        for (int i = 0; i < _mappings.Count; i++)
        {
            parsers[i + 1] = _mappings[i].Parser!;
        }

        using var reader = await connection.ExecuteReaderAsync(
            result.Sql,
            param: result.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout).ConfigureAwait(false);

        var list = new List<TReturn>();
        var localCombiners = _combiners.ToArray();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = new object[parsers.Length];
            for (int i = 0; i < parsers.Length; i++)
            {
                parts[i] = parsers[i](reader);
            }

            var root = (TReturn)parts[0];
            for (int i = 0; i < localCombiners.Length; i++)
            {
                root = localCombiners[i](parts[(i + 1)..], root);
            }
            list.Add(root);
        }

        return list;
    }

    /// <summary>
    /// Executes the multi-map query and deduplicates root entities by key, grouping related child entities into 1:N collections.
    /// </summary>
    /// <typeparam name="TKey">The type of the root entity primary key.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="compiler">The SQL compiler used to translate the query AST into SQL text.</param>
    /// <param name="keySelector">A function to extract the unique key identifying the root entity instance.</param>
    /// <param name="transaction">An optional transaction to execute within.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains an enumerable of distinct <typeparamref name="TReturn"/> instances with aggregated child relationships.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="compiler"/>, or <paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">No entity mappings have been registered, or the root type is missing a generated reader factory</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "ISqlCompiler.Compile() is annotated RequiresUnreferencedCode by the SqlBuilder.Abstractions library. " +
                        "Callers using NativeAOT strict mode should pass pre-compiled SqlResult directly. " +
                        "Documented in ADR-006: trim analyzer enabled; compiler-path suppressed with explicit rationale.")]
    [UnconditionalSuppressMessage("Trimming", "IL2090",
        Justification = "GetMultiMapReaderFactory reflection is the progressive-enhancement AOT path. " +
                        "Fully source-generated IDataReaderMapper<T> path avoids this. ADR-006.")]
    public async Task<IEnumerable<TReturn>> QueryGroupedAsync<TKey>(
        IDbConnection connection,
        ISqlCompiler compiler,
        Func<TReturn, TKey> keySelector,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(keySelector);
        cancellationToken.ThrowIfCancellationRequested();

        if (_mappings.Count == 0)
        {
            throw new InvalidOperationException("At least one entity mapping must be registered using Map<T>() before executing the query.");
        }

        var result = compiler.Compile(_query);
        var lookup = new Dictionary<TKey, TReturn>();

        // If any parser is missing, fallback to Dapper
        if (_mappings.Any(m => m.Parser == null))
        {
            var types = Types;
            var combiners = _combiners.ToArray();
            var splitOn = SplitOn;

            TReturn MapCombiner(object[] parts)
            {
                var rootCandidate = (TReturn)parts[0];
                var key = keySelector(rootCandidate);
                if (!lookup.TryGetValue(key, out var existingRoot))
                {
                    existingRoot = rootCandidate;
                    lookup[key] = existingRoot;
                }

                for (int i = 0; i < combiners.Length; i++)
                {
                    existingRoot = combiners[i](parts[(i + 1)..], existingRoot);
                }
                return existingRoot;
            }

            await connection.QueryAsync<TReturn>(
                result.Sql,
                types,
                MapCombiner,
                param: result.Parameters,
                transaction: transaction,
                buffered: true,
                splitOn: splitOn,
                commandTimeout: commandTimeout).ConfigureAwait(false);

            return lookup.Values;
        }

        // AOT-safe manual parsing
        var rootFactoryMethod = typeof(TReturn).GetMethod("GetMultiMapReaderFactory", BindingFlags.Public | BindingFlags.Static);
        if (rootFactoryMethod == null)
        {
            throw new InvalidOperationException("Root type " + typeof(TReturn).Name + " is missing GetMultiMapReaderFactory(). Ensure it is decorated with [SqlEntity].");
        }
        var rootParser = (Func<IDataReader, object>)rootFactoryMethod.Invoke(null, null)!;

        var parsers = new Func<IDataReader, object>[_mappings.Count + 1];
        parsers[0] = rootParser;
        for (int i = 0; i < _mappings.Count; i++)
        {
            parsers[i + 1] = _mappings[i].Parser!;
        }

        using var reader = await connection.ExecuteReaderAsync(
            result.Sql,
            param: result.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout).ConfigureAwait(false);

        var localCombiners = _combiners.ToArray();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = new object[parsers.Length];
            for (int i = 0; i < parsers.Length; i++)
            {
                parts[i] = parsers[i](reader);
            }

            var rootCandidate = (TReturn)parts[0];
            var key = keySelector(rootCandidate);
            if (!lookup.TryGetValue(key, out var existingRoot))
            {
                existingRoot = rootCandidate;
                lookup[key] = existingRoot;
            }

            for (int i = 0; i < localCombiners.Length; i++)
            {
                existingRoot = localCombiners[i](parts[(i + 1)..], existingRoot);
            }
        }

        return lookup.Values;
    }

    /// <summary>
    /// Executes the multi-map query and returns the first result, or <see langword="null"/> if no rows are returned.
    /// </summary>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="compiler">The SQL compiler used to translate the query AST into SQL text.</param>
    /// <param name="transaction">An optional transaction to execute within.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the first matching <typeparamref name="TReturn"/> instance,
    /// or <see langword="null"/> if the result set is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="compiler"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">No entity mappings have been registered, or the root type is missing a generated reader factory</exception>
    public async Task<TReturn?> QueryFirstOrDefaultAsync(
        IDbConnection connection,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var results = await QueryAsync(connection, compiler, transaction, commandTimeout, cancellationToken)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Executes the multi-map query with root deduplication and returns the first result, or <see langword="null"/> if no rows are returned.
    /// </summary>
    /// <typeparam name="TKey">The type of the root entity primary key.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="compiler">The SQL compiler used to translate the query AST into SQL text.</param>
    /// <param name="keySelector">A function to extract the unique key identifying the root entity instance.</param>
    /// <param name="transaction">An optional transaction to execute within.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the first hydrated <typeparamref name="TReturn"/> instance with aggregated child relationships, or <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="compiler"/>, or <paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">No entity mappings have been registered, or the root type is missing a generated reader factory</exception>
    public async Task<TReturn?> QueryGroupedFirstOrDefaultAsync<TKey>(
        IDbConnection connection,
        ISqlCompiler compiler,
        Func<TReturn, TKey> keySelector,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default) where TKey : notnull
    {
        var results = await QueryGroupedAsync(connection, compiler, keySelector, transaction, commandTimeout, cancellationToken)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }
}




