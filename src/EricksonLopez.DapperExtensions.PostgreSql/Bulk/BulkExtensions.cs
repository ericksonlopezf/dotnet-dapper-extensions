// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace EricksonLopez.DapperExtensions.PostgreSql.Bulk;

/// <summary>
/// Provides extension methods for bulk INSERT, UPSERT, UPDATE, and DELETE operations using PostgreSQL UNNEST.
/// </summary>
public static class BulkExtensions
{
    /// <summary>
    /// Executes a bulk INSERT statement using PostgreSQL UNNEST array parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL INSERT...SELECT FROM UNNEST(...) statement.</param>
    /// <param name="parameters">The array parameters built with <see cref="BulkParameters{T}"/>.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<int> BulkInsertAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Length == 0)
            return 0;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeout ?? 30;

        if (transaction is not null)
            command.Transaction = transaction;

        command.Parameters.AddRange(parameters);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync().ConfigureAwait(false);

        return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a bulk INSERT ... ON CONFLICT DO UPDATE (upsert) statement using PostgreSQL UNNEST array parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL INSERT...SELECT FROM UNNEST(...) ON CONFLICT statement.</param>
    /// <param name="parameters">The array parameters built with <see cref="BulkParameters{T}"/>.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static Task<int> BulkUpsertAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout);

    /// <summary>
    /// Executes a bulk DELETE statement using PostgreSQL UNNEST array parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL DELETE statement utilizing array criteria or UNNEST.</param>
    /// <param name="parameters">The array parameters built with <see cref="BulkParameters{T}"/>.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static Task<int> BulkDeleteAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout);

    /// <summary>
    /// Executes a bulk UPDATE statement using PostgreSQL UNNEST array parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL UPDATE statement joining against UNNEST.</param>
    /// <param name="parameters">The array parameters built with <see cref="BulkParameters{T}"/>.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static Task<int> BulkUpdateAsync(
        this DbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        DbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout);
}

