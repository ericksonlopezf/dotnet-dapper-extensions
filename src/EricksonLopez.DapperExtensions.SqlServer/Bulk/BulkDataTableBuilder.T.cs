// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace EricksonLopez.DapperExtensions.SqlServer.Bulk;

/// <summary>
/// Builds a <see cref="DataTable"/> populated from an entity collection for high-performance <see cref="SqlBulkCopy"/> operations.
/// </summary>
/// <typeparam name="T">The entity type to bulk-insert.</typeparam>
public sealed class BulkDataTableBuilder<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly List<(string ColumnName, Func<T, object?> Selector, Type ColumnType)> _columns = [];

    internal BulkDataTableBuilder(IEnumerable<T> items)
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Adds a column mapping with an extracted strongly-typed value to the table schema.
    /// </summary>
    /// <typeparam name="TValue">The CLR type of the column value.</typeparam>
    /// <param name="columnName">The column name in the target SQL Server table.</param>
    /// <param name="selector">The delegate that extracts the column value from each entity item.</param>
    /// <returns>The current builder instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="columnName"/> is empty or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public BulkDataTableBuilder<T> Column<TValue>(string columnName, Func<T, TValue> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentNullException.ThrowIfNull(selector);

        var nullable = Nullable.GetUnderlyingType(typeof(TValue));
        var columnType = nullable ?? typeof(TValue);

        _columns.Add((columnName, item =>
        {
            var val = selector(item);
            return val is null ? DBNull.Value : (object)val;
        }, columnType));

        return this;
    }

    /// <summary>
    /// Builds a <see cref="DataTable"/> pre-populated with rows from the entity collection.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> populated with column schemas and item row values.</returns>
    /// <exception cref="InvalidOperationException">No columns have been registered via Column()</exception>
    public DataTable Build()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("At least one column must be added via Column() before calling Build().");

        var table = new DataTable();

        foreach (var (colName, _, colType) in _columns)
            table.Columns.Add(colName, colType);

        foreach (var item in _items)
        {
            var row = table.NewRow();
            for (int j = 0; j < _columns.Count; j++)
                row[j] = _columns[j].Selector(item);
            table.Rows.Add(row);
        }

        return table;
    }

    /// <summary>
    /// Gets the number of items to be inserted.
    /// </summary>
    public int Count => _items.Count;
}
