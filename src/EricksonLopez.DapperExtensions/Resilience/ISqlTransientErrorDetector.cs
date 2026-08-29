// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;

namespace EricksonLopez.DapperExtensions.Resilience;

/// <summary>
/// Defines a mechanism for determining whether a database exception represents a transient failure that can be safely retried.
/// </summary>
/// <remarks>
/// <para>
/// A transient error is caused by a temporary condition (such as connection timeouts, deadlocks, or failover events)
/// rather than a permanent defect (such as constraint violations or syntax errors).
/// </para>
/// <para>
/// <strong>Architectural Role &amp; Convergence:</strong> In the unified EricksonLopez ecosystem,
/// <c>ISqlTransientErrorDetector</c> provides database-specific classification of errors, feeding into the centralized
/// <c>EricksonLopez.Resilience</c> engine (<c>IErrorClassifier</c>). <c>DapperExtensions</c> classifies the database
/// operation context, while <c>EricksonLopez.Resilience</c> determines and orchestrates the retry/circuit-breaker policy.
/// </para>
/// <para>
/// <strong>Critical rule:</strong> Only wrap the entire transactional unit with a retry policy.
/// Never retry individual SQL statements inside an existing transaction.
/// </para>
/// </remarks>
public interface ISqlTransientErrorDetector
{
    /// <summary>
    /// Determines whether the specified exception represents a transient database error.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the error is transient and the operation can be retried;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool IsTransient(Exception exception);
}



