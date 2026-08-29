// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace EricksonLopez.DapperExtensions.SqlServer.Bulk;

/// <summary>
/// Provides extension methods for bulk INSERT, UPDATE, and DELETE operations on SQL Server connections.
/// </summary>
public static class BulkExtensions
{
    /// <summary>
    /// Executes a high-throughput bulk INSERT into a SQL Server table using <see cref="SqlBulkCopy"/>.
    /// </summary>
    /// <param name="connection">The SQL Server database connection.</param>
    /// <param name="destinationTableName">The destination table name in the database.</param>
    /// <param name="dataTable">The pre-populated <see cref="DataTable"/> containing row values to insert.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="batchSize">The number of rows per batch, or 0 for a single batch.</param>
    /// <param name="bulkCopyTimeout">The timeout in seconds for the bulk copy operation.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows copied to the destination table.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="dataTable"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="destinationTableName"/> is empty or whitespace, or <paramref name="connection"/> is not a <see cref="SqlConnection"/></exception>
    public static async Task<int> BulkInsertAsync(
        this DbConnection connection,
        string destinationTableName,
        DataTable dataTable,
        DbTransaction? transaction = null,
        int batchSize = 0,
        int bulkCopyTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);
        ArgumentNullException.ThrowIfNull(dataTable);

        if (dataTable.Rows.Count == 0)
            return 0;

        if (connection is not SqlConnection && BulkCopyExecutor == DefaultBulkCopyExecutor)
            throw new ArgumentException(
                $"Connection must be a {nameof(SqlConnection)}. Got: {connection.GetType().Name}",
                nameof(connection));

        var sqlConnection = connection as SqlConnection;
        SqlTransaction? sqlTransaction = transaction as SqlTransaction;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await BulkCopyExecutor(
            sqlConnection!,
            destinationTableName,
            dataTable,
            sqlTransaction,
            batchSize,
            bulkCopyTimeout,
            cancellationToken).ConfigureAwait(false);
    }

    internal static readonly Func<SqlConnection, string, DataTable, SqlTransaction?, int, int, CancellationToken, Task<int>> DefaultBulkCopyExecutor =
        (sqlConnection, destinationTableName, dataTable, sqlTransaction, batchSize, bulkCopyTimeout, cancellationToken) =>
        {
            var bulkCopy = CreateSqlBulkCopy(sqlConnection, sqlTransaction, destinationTableName, dataTable, batchSize, bulkCopyTimeout);
            return ExecuteSqlBulkCopyAsync(bulkCopy, dataTable, cancellationToken);
        };

    internal static Func<SqlConnection, string, DataTable, SqlTransaction?, int, int, CancellationToken, Task<int>> BulkCopyExecutor =
        DefaultBulkCopyExecutor;

    internal static SqlBulkCopy CreateSqlBulkCopy(
        SqlConnection connection,
        SqlTransaction? transaction,
        string destinationTableName,
        DataTable dataTable,
        int batchSize,
        int bulkCopyTimeout)
    {
        var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = destinationTableName;
        bulkCopy.BatchSize = batchSize;
        bulkCopy.BulkCopyTimeout = bulkCopyTimeout;

        foreach (DataColumn col in dataTable.Columns)
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        return bulkCopy;
    }

    internal static readonly Func<SqlBulkCopy, DataTable, CancellationToken, Task<int>> DefaultBulkCopyWriter =
        async (bulkCopy, dataTable, cancellationToken) =>
        {
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);
            return dataTable.Rows.Count;
        };

    internal static Func<SqlBulkCopy, DataTable, CancellationToken, Task<int>> BulkCopyWriter =
        DefaultBulkCopyWriter;

    internal static async Task<int> ExecuteSqlBulkCopyAsync(
        SqlBulkCopy bulkCopy,
        DataTable dataTable,
        CancellationToken cancellationToken)
    {
        using (bulkCopy)
        {
            return await BulkCopyWriter(bulkCopy, dataTable, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a bulk DELETE operation on SQL Server using parameterized criteria or Table-Valued Parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL DELETE statement.</param>
    /// <param name="param">The optional command parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<int> BulkDeleteAsync(
        this DbConnection connection,
        string sql,
        object? param = null,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new Dapper.CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            CommandType.Text,
            CommandFlags.None,
            cancellationToken);

        return await Dapper.SqlMapper.ExecuteAsync(connection, command).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a bulk UPDATE operation on SQL Server using parameterized criteria or Table-Valued Parameters.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="sql">The SQL UPDATE statement.</param>
    /// <param name="param">The optional command parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<int> BulkUpdateAsync(
        this DbConnection connection,
        string sql,
        object? param = null,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new Dapper.CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            CommandType.Text,
            CommandFlags.None,
            cancellationToken);

        return await Dapper.SqlMapper.ExecuteAsync(connection, command).ConfigureAwait(false);
    }
}
