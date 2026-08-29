// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;

namespace EricksonLopez.DapperExtensions.MultiMap;

/// <summary>
/// Describes how to split and hydrate an entity type in a multi-mapping query result.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MultiMapDescriptor"/> is an <b>extensibility surface</b> for consumers who need to
/// introspect or compose multi-map configurations at runtime — for example, when building
/// dynamic multi-entity pipelines, diagnostic tooling, or custom query runners that sit
/// outside of <see cref="MultiMapBuilder{TReturn}"/>.
/// </para>
/// <para>
/// <see cref="MultiMapBuilder{TReturn}"/> does <b>not</b> use this class internally; it manages
/// mappings as lightweight anonymous tuples for performance. Consumers who need a typed,
/// inspectable mapping descriptor can construct a <see cref="MultiMapDescriptor"/> alongside
/// a <see cref="MultiMapBuilder{TReturn}"/> invocation.
/// </para>
/// </remarks>
public sealed class MultiMapDescriptor
{
    /// <summary>
    /// Gets the CLR type of the entity.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Gets the database table name associated with the entity.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the column names occupied by this entity in the result set.
    /// </summary>
    public string[] ColumnNames { get; }

    /// <summary>
    /// Gets the factory delegate that hydrates an entity instance from an <see cref="IDataReader"/> row.
    /// </summary>
    public Func<IDataReader, object> ReaderFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiMapDescriptor"/> class.
    /// </summary>
    /// <param name="entityType">The CLR type of the entity.</param>
    /// <param name="tableName">The database table name associated with the entity.</param>
    /// <param name="columnNames">The column names occupied by this entity in the result set.</param>
    /// <param name="readerFactory">The factory delegate that hydrates an entity instance from an <see cref="IDataReader"/> row.</param>
    public MultiMapDescriptor(
        Type entityType,
        string tableName,
        string[] columnNames,
        Func<IDataReader, object> readerFactory)
    {
        EntityType = entityType;
        TableName = tableName;
        ColumnNames = columnNames;
        ReaderFactory = readerFactory;
    }
}
