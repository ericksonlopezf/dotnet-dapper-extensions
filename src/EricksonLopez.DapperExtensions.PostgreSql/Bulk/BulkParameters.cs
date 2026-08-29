// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.DapperExtensions.PostgreSql.Bulk;

/// <summary>
/// Provides factory methods for creating <see cref="BulkParameters{T}"/> instances.
/// </summary>
public static class BulkParameters
{
    /// <summary>
    /// Begins building bulk parameters for the specified collection.
    /// </summary>
    /// <typeparam name="T">The entity type to bulk-insert.</typeparam>
    /// <param name="items">The collection of entities to insert.</param>
    /// <returns>A new <see cref="BulkParameters{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static BulkParameters<T> From<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new BulkParameters<T>(items);
    }
}
