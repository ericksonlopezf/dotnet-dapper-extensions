// Copyright © Erickson Lopez. MIT License.
using System.Data;

namespace EricksonLopez.DapperExtensions.MultiMap;

/// <summary>
/// Defines a Native AOT-compatible, reflection-free mapper that reads a single entity
/// of type <typeparamref name="T"/> from an <see cref="IDataReader"/>.
/// </summary>
/// <typeparam name="T">The entity type to map.</typeparam>
/// <remarks>
/// Implementations are typically generated at compile time for types annotated with <see cref="SqlEntityAttribute"/>,
/// enabling zero-reflection data reader hydration suitable for Native AOT environments.
/// </remarks>
public interface IDataReaderMapper<T>
{
    /// <summary>
    /// Maps the current row of the <paramref name="reader"/> to an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="reader">The data reader positioned at the row to map.</param>
    /// <returns>The mapped instance of <typeparamref name="T"/>.</returns>
    T Map(IDataReader reader);
}
