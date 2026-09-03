// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.MariaDb.Pagination;
using EricksonLopez.DapperExtensions.Testing.Common;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.DapperExtensions.MariaDb.Tests.Unit;

public sealed class PagedQueryExtensionsTests : PagedQueryExtensionsTestsBase
{
    protected override string ExpectedOffsetClause(int offset, int pageSize)
        => $"LIMIT {pageSize} OFFSET {offset}";

    protected override Task<ICountedPagedList<Customer>> ExecuteQueryPagedAsync(
        IDbConnection connection,
        string sql,
        string countSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.QueryPagedAsync<Customer>(sql, countSql, pagination, param, transaction, commandTimeout);

    protected override Task<ICountedPagedList<Customer>> ExecuteQueryPagedMultipleAsync(
        IDbConnection connection,
        string combinedSql,
        PaginationParameters pagination,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.QueryPagedMultipleAsync<Customer>(combinedSql, pagination, param, transaction, commandTimeout);

    protected override Task<ICursorPagedList<Customer>> ExecuteQueryCursorPagedAsync(
        IDbConnection connection,
        string sql,
        string cursorColumn,
        CursorPaginationParameters parameters,
        Func<Customer, string> cursorSelector,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.QueryCursorPagedAsync(sql, cursorColumn, parameters, cursorSelector, param, transaction, commandTimeout);
}
