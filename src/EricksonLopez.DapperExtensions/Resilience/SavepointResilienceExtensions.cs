// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.UnitOfWork;
using EricksonLopez.Resilience;
using Polly;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Provides extension methods for executing transactional operations wrapped with savepoints and resilience pipelines.
/// </summary>
/// <remarks>
/// <para>
/// Retrying individual operations inside an open transaction can corrupt transactional state on relational engines
/// unless intermediate failed operations are rolled back to a savepoint before each retry attempt.
/// </para>
/// <para>
/// <strong>Architecture:</strong> Overloads accepting <see cref="IResiliencePipeline"/> are the canonical API aligned with the
/// EricksonLopez.Resilience ecosystem. See ADR-014 and ADR-017.
/// </para>
/// </remarks>
public static class SavepointResilienceExtensions
{
    // ─── EricksonLopez.Resilience canonical overloads ────────────────────────

    /// <summary>
    /// Executes an asynchronous operation inside a database savepoint using the specified <see cref="IResiliencePipeline"/>.
    /// </summary>
    /// <param name="unitOfWork">The unit of work managing the active database transaction.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the retry strategy.</param>
    /// <param name="operation">The asynchronous operation to execute within the savepoint scope.</param>
    /// <param name="savepointName">The optional custom savepoint name. If <see langword="null"/>, a unique name is generated automatically.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="pipeline"/>, or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task ExecuteInSavepointWithRetryAsync(
        this IUnitOfWork unitOfWork,
        IResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var spName = savepointName ?? $"SP_{Guid.NewGuid():N}";

        await pipeline.ExecuteAsync(async ct =>
        {
            var savepoint = await unitOfWork.CreateSavepointAsync(spName, ct).ConfigureAwait(false);
            try
            {
                await operation(unitOfWork, ct).ConfigureAwait(false);
                await savepoint.ReleaseAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await savepoint.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous operation that returns a value inside a database savepoint using the specified <see cref="IResiliencePipeline"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="unitOfWork">The unit of work managing the active database transaction.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the retry strategy.</param>
    /// <param name="operation">The asynchronous operation returning a value to execute within the savepoint scope.</param>
    /// <param name="savepointName">The optional custom savepoint name. If <see langword="null"/>, a unique name is generated automatically.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the value returned by <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="pipeline"/>, or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task<TResult> ExecuteInSavepointWithRetryAsync<TResult>(
        this IUnitOfWork unitOfWork,
        IResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var spName = savepointName ?? $"SP_{Guid.NewGuid():N}";

        return await pipeline.ExecuteAsync<TResult>(async ct =>
        {
            var savepoint = await unitOfWork.CreateSavepointAsync(spName, ct).ConfigureAwait(false);
            try
            {
                var result = await operation(unitOfWork, ct).ConfigureAwait(false);
                await savepoint.ReleaseAsync(ct).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await savepoint.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    // ─── Polly compatibility overloads ──────────────────────────────────────

    /// <summary>
    /// Executes an asynchronous operation inside a database savepoint using the specified resilience pipeline.
    /// </summary>
    /// <param name="unitOfWork">The unit of work managing the active database transaction.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the retry strategy.</param>
    /// <param name="operation">The asynchronous operation to execute within the savepoint scope.</param>
    /// <param name="savepointName">The optional custom savepoint name. If <see langword="null"/>, a unique name is generated automatically.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="pipeline"/>, or <paramref name="operation"/> is <see langword="null"/></exception>
    public static Task ExecuteInSavepointWithRetryAsync(
        this IUnitOfWork unitOfWork,
        ResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var spName = savepointName ?? $"SP_{Guid.NewGuid():N}";

        return pipeline.ExecuteAsync(async ct =>
        {
            var savepoint = await unitOfWork.CreateSavepointAsync(spName, ct).ConfigureAwait(false);
            try
            {
                await operation(unitOfWork, ct).ConfigureAwait(false);
                await savepoint.ReleaseAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await savepoint.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).AsTask();
    }

    /// <summary>
    /// Executes an asynchronous operation that returns a value inside a database savepoint using the specified resilience pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="unitOfWork">The unit of work managing the active database transaction.</param>
    /// <param name="pipeline">The resilience pipeline used to execute the retry strategy.</param>
    /// <param name="operation">The asynchronous operation returning a value to execute within the savepoint scope.</param>
    /// <param name="savepointName">The optional custom savepoint name. If <see langword="null"/>, a unique name is generated automatically.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the value returned by <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="pipeline"/>, or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task<TResult> ExecuteInSavepointWithRetryAsync<TResult>(
        this IUnitOfWork unitOfWork,
        ResiliencePipeline pipeline,
        Func<IUnitOfWork, CancellationToken, Task<TResult>> operation,
        string? savepointName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(operation);

        var spName = savepointName ?? $"SP_{Guid.NewGuid():N}";

        return await pipeline.ExecuteAsync(async ct =>
        {
            var savepoint = await unitOfWork.CreateSavepointAsync(spName, ct).ConfigureAwait(false);
            try
            {
                var result = await operation(unitOfWork, ct).ConfigureAwait(false);
                await savepoint.ReleaseAsync(ct).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await savepoint.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
