namespace BackgroundTimerJob.DistributedLock;

/// <summary>
/// An implementation of <see cref="IDistributedJobLockProvider"/> that uses Azure Blob Storage.
/// </summary>
public class AzureBlobDistributedJobLockProvider(BlobContainerClient containerClient, ILogger<AzureBlobDistributedJobLock>? logger) 
    : IDistributedJobLockProvider
{
    public async Task<IDistributedJobLock?> TryAcquireJobLockAsync(
        string key,
        TimeSpan minimumInterval,
        TimeSpan leaseTime,
        CancellationToken cancellationToken = default) 
    {
        // Determine the blob name based on the key.
        string blobName = $"joblock-{key}";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Ensure the blob exists by attempting to create it.
        try
        {
            await blobClient.UploadAsync(BinaryData.FromString(string.Empty), overwrite: false, cancellationToken: cancellationToken);
            logger?.LogDebug("Created new lock blob: {BlobName}", blobName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            // The blob already exists. This is expected if the lock has been used before.
        }

        // Create a lease client.
        BlobLeaseClient leaseClient = blobClient.GetBlobLeaseClient();

        // Validate and adjust the lease duration (must be between 15 and 60 seconds).
        int leaseDurationSeconds = (int)leaseTime.TotalSeconds;
        if (leaseDurationSeconds < 15 || leaseDurationSeconds > 60)
        {
            leaseDurationSeconds = Math.Clamp(leaseDurationSeconds, 15, 60);
            logger?.LogDebug("Lease time clamped to {LeaseDurationSeconds} seconds.", leaseDurationSeconds);
        }

        // Attempt to acquire the lease.
        Response<BlobLease> leaseResponse;
        try
        {
            leaseResponse = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(leaseDurationSeconds), cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            logger?.LogWarning(ex, "Could not acquire lease on blob {BlobName}. Another instance may be holding the lock.", blobName);
            return null;
        }

        string leaseId = leaseResponse.Value.LeaseId;
        logger?.LogDebug("Acquired lease with id {LeaseId} on blob {BlobName}.", leaseId, blobName);

        // Read the blob's properties and metadata.
        BlobProperties properties;
        try
        {
            properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            logger?.LogError(ex, "Failed to get properties of blob {BlobName}.", blobName);
            await leaseClient.ReleaseAsync(cancellationToken: cancellationToken);
            return null;
        }

        Dictionary<string, string> metadata = new(properties.Metadata);
        DateTime nowUtc = DateTime.UtcNow;

        // Check the "lastrun" metadata to enforce the minimum interval between job runs.
        if (metadata.TryGetValue("lastrun", out string? lastRunString) &&
            DateTime.TryParse(lastRunString, out DateTime lastRunTime))
        {
            if (nowUtc - lastRunTime < minimumInterval)
            {
                logger?.LogInformation("Job was run recently at {LastRunTime}. Minimum interval not elapsed. Skipping.", lastRunTime);
                await leaseClient.ReleaseAsync(cancellationToken: cancellationToken);
                return null;
            }
        }
        else
        {
            logger?.LogDebug("No valid last run timestamp found in metadata; treating as never run.");
        }

        // Update metadata with the current run time.
        try
        {
            metadata["lastrun"] = nowUtc.ToString("o"); // ISO 8601 format.
            BlobRequestConditions conditions = new() { LeaseId = leaseId };
            await blobClient.SetMetadataAsync(metadata, conditions, cancellationToken);
            logger?.LogDebug("Updated last run time metadata to {NowUtc} on blob {BlobName}.", nowUtc, blobName);
        }
        catch (RequestFailedException ex)
        {
            logger?.LogError(ex, "Failed to update metadata for blob {BlobName}.", blobName);
            await leaseClient.ReleaseAsync(cancellationToken: cancellationToken);
            return null;
        }

        // Return the acquired lock, which holds the blob lease.
        return new AzureBlobDistributedJobLock(leaseClient, logger);
    }
}
