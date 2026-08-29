// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.DapperExtensions.Sqlite.Bulk;

/// <summary>
/// Provides factory methods for creating <see cref="BulkBuilder{T}"/> instances.
/// </summary>
public static class BulkBuilder
{
    /// <summary>
    /// Begins building a bulk INSERT operation for the specified collection.
    /// </summary>
    /// <typeparam name="T">The entity type to bulk-insert.</typeparam>
    /// <param name="items">The collection of entities to insert.</param>
    /// <returns>A new <see cref="BulkBuilder{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static BulkBuilder<T> From<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new BulkBuilder<T>(items);
    }
}
