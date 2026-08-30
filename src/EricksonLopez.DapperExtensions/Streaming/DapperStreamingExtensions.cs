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
/// <b>Memory Profile $O(1)$:</b> Standard Dapper <c>QueryAsync&lt;T&gt;</c> buffers the entire result set into an in-memory list
/// before returning. For large datasets (10K+ rows), this causes high LOH allocations and GC Gen 2 pressure.
/// </para>
/// <para>
/// These extension methods execute unbuffered queries using Dapper's row parsers,
/// streaming rows sequentially off the wire and yielding each entity on-the-fly. The caller must consume the stream promptly.
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
    /// <param name="cancellationToken">Cancellation token for aborting stream consumption.</param>
    /// <returns>An asynchronous stream of <typeparamref name="T"/> entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="sql"/> is <see langword="null"/>.</exception>
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
    /// <param name="cancellationToken">Cancellation token for the enumerator.</param>
    /// <returns>An asynchronous stream of <typeparamref name="T"/> entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
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
