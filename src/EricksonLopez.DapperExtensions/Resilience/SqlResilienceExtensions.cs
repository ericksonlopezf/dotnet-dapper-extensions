// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Resilience;
using EricksonLopez.SqlBuilder.Abstractions;
using Polly;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides extension methods for <see cref="IDbConnection"/> that execute SQL queries and commands through resilience pipelines.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Critical rule:</strong> Resilience pipelines must wrap the entire transactional unit — never individual statements inside an open transaction.
/// </para>
/// <para>
/// <strong>Architecture:</strong> Overloads accepting <see cref="IResiliencePipeline"/> are the canonical API aligned with the
/// EricksonLopez.Resilience ecosystem. See ADR-017.
/// </para>
/// </remarks>
public static class SqlResilienceExtensions
{
    // ─── Execute (no result) — EL canonical IResiliencePipeline ──────────────

    /// <summary>
    /// Executes a compiled SQL command through the specified <see cref="IResiliencePipeline"/> and returns the number of affected rows.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the command.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<int> ExecuteWithResilienceAsync(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<int>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.ExecuteAsync(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── Query (collection) — EL canonical ───────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns a sequence of mapped results.
    /// </summary>
    /// <typeparam name="T">The target entity type to map each row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the sequence of mapped <typeparamref name="T"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<IEnumerable<T>> QueryWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<IEnumerable<T>>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.Buffered,
                    cancellationToken: ct);
                return await connection.QueryAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── QuerySingle — EL canonical ──────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns exactly one matching element.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the single matching <typeparamref name="T"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<T> QuerySingleWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<T>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QuerySingleAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── QuerySingleOrDefault — EL canonical ─────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns a single element, or a default value if no element is found.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the single matching <typeparamref name="T"/> instance, or <see langword="default"/> if no elements are found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<T?> QuerySingleOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<T?>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── QueryFirst — EL canonical ────────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns the first matching element.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the first matching <typeparamref name="T"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<T> QueryFirstWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<T>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QueryFirstAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── QueryFirstOrDefault — EL canonical ──────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns the first element, or a default value if no element is found.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the first matching <typeparamref name="T"/> instance, or <see langword="default"/> if no elements are found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<T?> QueryFirstOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<T?>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QueryFirstOrDefaultAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── ExecuteScalar — EL canonical ────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified <see cref="IResiliencePipeline"/> and returns the first column of the first row.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the scalar value of type <typeparamref name="T"/>, or <see langword="default"/> if the result is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static async Task<T?> ExecuteScalarWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        IResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return await pipeline.ExecuteAsync<T?>(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.ExecuteScalarAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ─── Execute (no result) — Polly compat ─────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL command through the specified resilience pipeline and returns the number of affected rows.
    /// </summary>
    /// <param name="connection">The database connection to execute the command on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the command.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<int> ExecuteWithResilienceAsync(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.ExecuteAsync(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── Query (collection) — Polly compat ───────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns a sequence of mapped results.
    /// </summary>
    /// <typeparam name="T">The target entity type to map each row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the sequence of mapped <typeparamref name="T"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<IEnumerable<T>> QueryWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.Buffered,
                    cancellationToken: ct);
                return (IEnumerable<T>)await connection.QueryAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── QuerySingle — Polly compat ───────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns exactly one matching element.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the single matching <typeparamref name="T"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<T> QuerySingleWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QuerySingleAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── QuerySingleOrDefault — Polly compat ──────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns a single element, or a default value if no element is found.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the single matching <typeparamref name="T"/> instance, or <see langword="default"/> if no elements are found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<T?> QuerySingleOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── QueryFirst — Polly compat ────────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns the first matching element.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the first matching <typeparamref name="T"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<T> QueryFirstWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QueryFirstAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── QueryFirstOrDefault — Polly compat ──────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns the first element, or a default value if no element is found.
    /// </summary>
    /// <typeparam name="T">The target entity type to map the row to.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the first matching <typeparamref name="T"/> instance, or <see langword="default"/> if no elements are found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<T?> QueryFirstOrDefaultWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.QueryFirstOrDefaultAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }

    // ─── ExecuteScalar — Polly compat ─────────────────────────────────────────

    /// <summary>
    /// Executes a compiled SQL query through the specified resilience pipeline and returns the first column of the first row.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="connection">The database connection to execute the query on.</param>
    /// <param name="query">The compiled SQL query containing the SQL text and parameters.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the query.</param>
    /// <param name="transaction">The optional transaction to execute within.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the scalar value of type <typeparamref name="T"/>, or <see langword="default"/> if the result is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="pipeline"/> is <see langword="null"/></exception>
    public static Task<T?> ExecuteScalarWithResilienceAsync<T>(
        this IDbConnection connection,
        SqlResult query,
        ResiliencePipeline pipeline,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.ExecuteAsync(
            async ct =>
            {
                var command = new CommandDefinition(
                    query.Sql,
                    query.Parameters,
                    transaction,
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.None,
                    cancellationToken: ct);
                return await connection.ExecuteScalarAsync<T>(command).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
    }
}
