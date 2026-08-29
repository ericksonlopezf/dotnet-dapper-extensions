// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace EricksonLopez.DapperExtensions.PostgreSql.Bulk;

/// <summary>
/// Builds typed array parameters for PostgreSQL UNNEST bulk operations.
/// </summary>
/// <remarks>
/// PostgreSQL supports bulk inserts via the <c>UNNEST</c> function:
/// <c>INSERT INTO table (col1, col2) SELECT * FROM UNNEST(@col1, @col2)</c>
/// </remarks>
/// <typeparam name="T">The entity type to bulk-insert.</typeparam>
public sealed class BulkParameters<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly List<(string Name, Array Values, NpgsqlDbType DbType)> _columns = [];

    internal BulkParameters(IEnumerable<T> items)
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Adds a column mapping with a PostgreSQL database type.
    /// </summary>
    /// <typeparam name="TValue">The CLR type of the column elements.</typeparam>
    /// <param name="parameterName">The UNNEST parameter name without the '@' prefix.</param>
    /// <param name="selector">The delegate that extracts the column value from each entity item.</param>
    /// <param name="dbType">The PostgreSQL type of the column.</param>
    /// <returns>The current builder instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="parameterName"/> is empty or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public BulkParameters<T> Add<TValue>(
        string parameterName,
        Func<T, TValue> selector,
        NpgsqlDbType dbType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new TValue[_items.Count];
        for (int i = 0; i < _items.Count; i++)
        {
            values[i] = selector(_items[i]);
        }
        _columns.Add((parameterName, values, dbType));
        return this;
    }

    /// <summary>
    /// Builds the <see cref="NpgsqlParameter"/> array ready to pass to bulk execution methods.
    /// </summary>
    /// <returns>An array of <see cref="NpgsqlParameter"/> instances populated with typed array values.</returns>
    /// <exception cref="InvalidOperationException">No columns have been registered via Add()</exception>
    public NpgsqlParameter[] Build()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("At least one column must be added via Add() before calling Build().");

        return _columns
            .Select(col => new NpgsqlParameter(col.Name, col.DbType | NpgsqlDbType.Array)
            {
                Value = col.Values
            })
            .ToArray();
    }

    /// <summary>
    /// Gets the number of items that will be inserted.
    /// </summary>
    public int Count => _items.Count;
}
