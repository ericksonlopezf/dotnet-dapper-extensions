// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace EricksonLopez.DapperExtensions.Streaming;

/// <summary>
/// Provides unbuffered, memory-efficient streaming extensions for Dapper queries returning <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Memory Profile O(1) for the streaming operation:</b> Standard Dapper <c>QueryAsync&lt;T&gt;</c> buffers the entire result set into an in-memory list
/// before returning. For large datasets (10K+ rows), this causes high LOH allocations and GC Gen 2 pressure.
/// These extension methods stream rows one-by-one off the wire, keeping the streaming operation itself at O(1) memory.
/// Note: if the caller accumulates results in a <c>List&lt;T&gt;</c> or similar collection, overall memory usage becomes O(N).
/// </para>
/// <para>
/// These extension methods execute unbuffered queries using Dapper's row parsers,
/// streaming rows sequentially off the wire and yielding each entity on-the-fly. The caller must consume the stream promptly.
/// </para>
/// <para>
/// <b>Exception propagation:</b> Database provider exceptions (e.g., <c>NpgsqlException</c>, <c>SqlException</c>)
/// thrown during reader execution or row reading propagate directly to the caller without being caught or wrapped.
/// </para>
/// <para>
/// <b>Native AOT limitation (ADR-006, ADR-019):</b> <c>StreamAsync&lt;T&gt;</c> uses Dapper's <c>GetRowParser&lt;T&gt;()</c>
/// internally, which is reflection-based. For fully AOT-safe streaming, use <c>MultiMapBuilder&lt;T&gt;</c>
/// with <c>[SqlEntity]</c> source-generated parsers instead.
/// </para>
/// </remarks>
public static class DapperStreamingExtensions
{
    /// <summary>
    /// Streams rows one-by-one from the database connection without buffering the full result set into memory.
    /// </summary>
    /// <typeparam name="T">The entity or projected type.</typeparam>
    /// <param name="connection">The database connection. Must be open or capable of being opened.</param>
    /// <param name="sql">The SQL query text.</param>
    /// <param name="param">Optional query parameters.</param>
    /// <param name="transaction">Optional active database transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <param name="commandType">Optional command type (Text, StoredProcedure, TableDirect).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous stream of <typeparamref name="T"/> entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="sql"/> is <see langword="null"/></exception>
    public static IAsyncEnumerable<T> StreamAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(sql);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            commandType: commandType,
            flags: CommandFlags.None,
            cancellationToken: cancellationToken);

        return connection.StreamAsync<T>(command, cancellationToken);
    }

    /// <summary>
    /// Streams rows one-by-one from the database connection using a <see cref="CommandDefinition"/> with unbuffered execution.
    /// </summary>
    /// <typeparam name="T">The entity or projected type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="command">The command definition specifying query parameters, timeout, and cancellation token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous stream of <typeparamref name="T"/> entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static IAsyncEnumerable<T> StreamAsync<T>(
        this IDbConnection connection,
        CommandDefinition command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return CoreStreamAsync<T>(connection, command, cancellationToken);
    }

    private static async IAsyncEnumerable<T> CoreStreamAsync<T>(
        IDbConnection connection,
        CommandDefinition command,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ensure connection is open
        if (connection.State != ConnectionState.Open)
        {
            if (connection is DbConnection dbConn)
            {
                await dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                connection.Open();
            }
        }

        var effectiveToken = cancellationToken == default ? command.CancellationToken : cancellationToken;

        var unbufferedCommand = new CommandDefinition(
            commandText: command.CommandText,
            parameters: command.Parameters,
            transaction: command.Transaction,
            commandTimeout: command.CommandTimeout,
            commandType: command.CommandType,
            flags: CommandFlags.None,
            cancellationToken: effectiveToken);

        var reader = await connection.ExecuteReaderAsync(unbufferedCommand).ConfigureAwait(false);
        try
        {
            if (reader is DbDataReader dbReader)
            {
                var rowParser = reader.GetRowParser<T>();
                while (await dbReader.ReadAsync(effectiveToken).ConfigureAwait(false))
                {
                    yield return rowParser(reader);
                }
            }
            else
            {
                var rowParser = reader.GetRowParser<T>();
                while (reader.Read())
                {
                    effectiveToken.ThrowIfCancellationRequested();
                    yield return rowParser(reader);
                }
            }
        }
        finally
        {
            if (reader is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                reader.Dispose();
            }
        }
    }
}
