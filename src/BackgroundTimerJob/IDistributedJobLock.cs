namespace AzureBlobDistributedJobLockProvider;

/// <summary>
/// Represents an acquired distributed job lock.
/// </summary>
public interface IDistributedJobLock : IAsyncDisposable
{
    /// <summary>
    /// Releases the acquired lock.
    /// </summary>
    Task ReleaseAsync();
}

/// <summary>
/// Provides a mechanism to acquire a distributed job lock, ensuring a minimum interval between job runs.
/// </summary>
public interface IDistributedJobLockProvider
{
    /// <summary>
    /// Tries to acquire a distributed job lock if the job is due to run.
    /// </summary>
    /// <param name="key">A unique key that identifies the job lock.</param>
    /// <param name="minimumInterval">The minimum interval that must elapse between consecutive job runs.</param>
    /// <param name="leaseTime">
    /// The duration for which the lock is held while the job is running.
    /// Note: Azure Blob leases must be between 15 and 60 seconds (unless an infinite lease is used).
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The acquired <see cref="IDistributedJobLock"/> if successful; otherwise, <c>null</c>.</returns>
    Task<IDistributedJobLock?> TryAcquireJobLockAsync(
        string key,
        TimeSpan minimumInterval,
        TimeSpan leaseTime,
        CancellationToken cancellationToken = default);
}
