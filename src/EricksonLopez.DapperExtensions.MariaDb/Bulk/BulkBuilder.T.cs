// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;

namespace EricksonLopez.DapperExtensions.MariaDb.Bulk;

/// <summary>
/// Builds parameterized multi-row INSERT SQL statements and Dapper parameters for bulk operations on MariaDB.
/// </summary>
/// <remarks>
/// MariaDB uses multi-row VALUES syntax:
/// <c>INSERT INTO `table` (`col1`, `col2`) VALUES (@p0_0, @p0_1), (@p1_0, @p1_1), ...</c>
/// </remarks>
/// <typeparam name="T">The entity type to bulk-insert.</typeparam>
public sealed class BulkBuilder<T>
{
    private readonly IReadOnlyList<T> _items;
    private string? _tableName;
    private readonly List<(string ColumnName, Func<T, object?> Selector)> _columns = [];

    internal BulkBuilder(IEnumerable<T> items)
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Sets the destination table name for the bulk INSERT operation.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>The current builder instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> is empty or whitespace</exception>
    public BulkBuilder<T> Table(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _tableName = tableName;
        return this;
    }

    /// <summary>
    /// Adds a column mapping to the bulk INSERT operation.
    /// </summary>
    /// <param name="columnName">The column name in the target table.</param>
    /// <param name="selector">The delegate that extracts the column value from each entity item.</param>
    /// <returns>The current builder instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="columnName"/> is empty or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public BulkBuilder<T> Column(string columnName, Func<T, object?> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentNullException.ThrowIfNull(selector);
        _columns.Add((columnName, selector));
        return this;
    }

    /// <summary>
    /// Builds the SQL INSERT statement and a <see cref="DynamicParameters"/> object containing all row values as named parameters.
    /// </summary>
    /// <returns>
    /// A tuple containing the generated SQL text and parameter collection, or <c>(null, null)</c> if there are no items.
    /// </returns>
    /// <exception cref="InvalidOperationException">The destination table name or column mappings have not been configured</exception>
    public (string? Sql, DynamicParameters? Parameters) Build()
    {
        if (_items.Count == 0)
            return (null, null);

        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Table name must be set via Table() before calling Build().");

        if (_columns.Count == 0)
            throw new InvalidOperationException("At least one column must be added via Column() before calling Build().");

        var columnList = string.Join(", ", _columns.Select(c => $"`{c.ColumnName}`"));
        var parameters = new DynamicParameters();
        var rowPlaceholders = new List<string>(_items.Count);

        for (int i = 0; i < _items.Count; i++)
        {
            var colPlaceholders = new string[_columns.Count];
            for (int j = 0; j < _columns.Count; j++)
            {
                var paramName = $"p{i}_{j}";
                parameters.Add(paramName, _columns[j].Selector(_items[i]));
                colPlaceholders[j] = $"@{paramName}";
            }
            rowPlaceholders.Add($"({string.Join(", ", colPlaceholders)})");
        }

        var sql = new StringBuilder()
            .Append("INSERT INTO `")
            .Append(_tableName)
            .Append("` (")
            .Append(columnList)
            .Append(") VALUES ")
            .Append(string.Join(", ", rowPlaceholders))
            .ToString();

        return (sql, parameters);
    }

    /// <summary>
    /// Gets the number of items to be inserted.
    /// </summary>
    public int Count => _items.Count;
}
