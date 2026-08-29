// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace EricksonLopez.DapperExtensions.MySql.Bulk;

/// <summary>
/// Provides extension methods for bulk INSERT, UPSERT, UPDATE, and DELETE operations on MySQL connections.
/// </summary>
public static class BulkExtensions
{
    /// <summary>
    /// Executes a bulk INSERT statement using pre-built SQL and parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The multi-row INSERT SQL statement.</param>
    /// <param name="parameters">The parameterized values for the bulk insert.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static async Task<int> BulkInsertAsync(
        this IDbConnection connection,
        string? sql,
        DynamicParameters? parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(sql) || parameters is null)
            return 0;

        var command = new CommandDefinition(
            sql,
            parameters,
            transaction,
            commandTimeout,
            CommandType.Text,
            CommandFlags.None,
            cancellationToken);

        return await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a bulk INSERT ... ON DUPLICATE KEY UPDATE statement using pre-built SQL and parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The multi-row UPSERT SQL statement.</param>
    /// <param name="parameters">The parameterized values for the bulk operation.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static Task<int> BulkUpsertAsync(
        this IDbConnection connection,
        string? sql,
        DynamicParameters? parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a bulk DELETE statement using pre-built SQL and parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The bulk DELETE SQL statement.</param>
    /// <param name="parameters">The parameterized values for the bulk operation.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static Task<int> BulkDeleteAsync(
        this IDbConnection connection,
        string? sql,
        DynamicParameters? parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a bulk UPDATE statement using pre-built SQL and parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The bulk UPDATE SQL statement.</param>
    /// <param name="parameters">The parameterized values for the bulk operation.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    public static Task<int> BulkUpdateAsync(
        this IDbConnection connection,
        string? sql,
        DynamicParameters? parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout, cancellationToken);
}
