// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.DapperExtensions.MariaDb.Pagination;

/// <summary>
/// Provides extension methods for executing paginated queries with Dapper on MariaDB databases.
/// </summary>
public static class PagedQueryExtensions
{
    /// <summary>
    /// Executes an offset-based paginated query using separate data and total count SQL statements.
    /// </summary>
    /// <typeparam name="T">The entity type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="sql">The SQL statement to retrieve page data without pagination clauses.</param>
    /// <param name="countSql">The SQL statement to calculate the total record count.</param>
    /// <param name="pagination">The pagination parameters including page number and page size.</param>
    /// <param name="param">The optional query parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a counted paged list of <typeparamref name="T"/> items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> or <paramref name="countSql"/> is empty or whitespace</exception>
    public static async Task<ICountedPagedList<T>> QueryPagedAsync<T>(
        this IDbConnection connection,
        string sql,
        string countSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(countSql);

        var offset = (pagination.Page - 1) * pagination.PageSize;
        var pagedSql = $"{sql} LIMIT {pagination.PageSize} OFFSET {offset}";

        var items = await connection.QueryAsync<T>(
            pagedSql, param, transaction, commandTimeout).ConfigureAwait(false);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql, param, transaction, commandTimeout).ConfigureAwait(false);

        return PagedList<T>.WithCount(
            items: items.AsList(),
            parameters: pagination,
            totalCount: totalCount);
    }

    /// <summary>
    /// Executes an offset-based paginated query using a single multi-result SQL statement.
    /// </summary>
    /// <typeparam name="T">The entity type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="sql">The SQL query returning two result sets: the page items followed by the total count.</param>
    /// <param name="pagination">The pagination parameters including page number and page size.</param>
    /// <param name="param">The optional query parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a counted paged list of <typeparamref name="T"/> items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    public static async Task<ICountedPagedList<T>> QueryPagedMultipleAsync<T>(
        this IDbConnection connection,
        string sql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var multi = await connection.QueryMultipleAsync(sql, param, transaction, commandTimeout).ConfigureAwait(false);

        var items = await multi.ReadAsync<T>().ConfigureAwait(false);
        var totalCount = await multi.ReadSingleAsync<int>().ConfigureAwait(false);

        return PagedList<T>.WithCount(items.AsList(), pagination, totalCount);
    }

    /// <summary>
    /// Executes a cursor-based (keyset) paginated query returning a cursor paged list.
    /// </summary>
    /// <typeparam name="T">The entity type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="sql">The base SQL query without keyset filters or ordering.</param>
    /// <param name="cursorColumn">The column name used as the pagination cursor.</param>
    /// <param name="parameters">The cursor pagination parameters containing page boundaries.</param>
    /// <param name="cursorSelector">The delegate that extracts the cursor value from an entity item.</param>
    /// <param name="param">The optional query parameters.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="commandTimeout">The optional command timeout in seconds.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a cursor paged list of <typeparamref name="T"/> items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="cursorSelector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> or <paramref name="cursorColumn"/> is empty or whitespace</exception>
    public static async Task<ICursorPagedList<T>> QueryCursorPagedAsync<T>(
        this IDbConnection connection,
        string sql,
        string cursorColumn,
        CursorPaginationParameters parameters,
        Func<T, string> cursorSelector,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(cursorColumn);
        ArgumentNullException.ThrowIfNull(cursorSelector);

        var dynamicParams = new DynamicParameters();
        if (param is not null)
        {
            dynamicParams.AddDynamicParams(param);
        }

        int pageSize = parameters.GetPageSize(10);
        bool isBackward = parameters.Last.HasValue && !parameters.First.HasValue;
        string comparisonOp = isBackward ? "<" : ">";
        string sortDirection = isBackward ? "DESC" : "ASC";
        string? cursorValue = isBackward ? parameters.Before : parameters.After;

        string filterSql = sql;
        if (!string.IsNullOrEmpty(cursorValue))
        {
            dynamicParams.Add("__cursorValue", cursorValue);
            string whereConnector = filterSql.Contains("WHERE", StringComparison.OrdinalIgnoreCase) ? "AND" : "WHERE";
            filterSql = $"{filterSql} {whereConnector} {cursorColumn} {comparisonOp} @__cursorValue";
        }

        int fetchLimit = pageSize + 1;
        string finalSql = $"{filterSql} ORDER BY {cursorColumn} {sortDirection} LIMIT {fetchLimit}";

        var queryResults = (await connection.QueryAsync<T>(
            finalSql, dynamicParams, transaction, commandTimeout).ConfigureAwait(false)).AsList();

        bool hasMore = queryResults.Count > pageSize;
        if (hasMore)
        {
            queryResults.RemoveAt(queryResults.Count - 1);
        }

        string? startCursor = queryResults.Count > 0 ? cursorSelector(queryResults[0]) : null;
        string? endCursor = queryResults.Count > 0 ? cursorSelector(queryResults[^1]) : null;

        bool hasNextPage = isBackward ? !string.IsNullOrEmpty(parameters.Before) : hasMore;
        bool hasPreviousPage = isBackward ? hasMore : !string.IsNullOrEmpty(parameters.After);

        return new CursorPagedList<T>(
            queryResults,
            startCursor,
            endCursor,
            hasPreviousPage: hasPreviousPage,
            hasNextPage: hasNextPage);
    }
}
