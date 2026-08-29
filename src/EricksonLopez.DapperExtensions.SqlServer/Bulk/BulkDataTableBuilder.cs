// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;

namespace EricksonLopez.DapperExtensions.SqlServer.Bulk;

/// <summary>
/// Provides factory methods for creating <see cref="BulkDataTableBuilder{T}"/> instances.
/// </summary>
public static class BulkDataTableBuilder
{
    /// <summary>
    /// Begins building a bulk <see cref="DataTable"/> for the specified collection.
    /// </summary>
    /// <typeparam name="T">The entity type to bulk-insert.</typeparam>
    /// <param name="items">The collection of entities to insert.</param>
    /// <returns>A new <see cref="BulkDataTableBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static BulkDataTableBuilder<T> From<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new BulkDataTableBuilder<T>(items);
    }
}
