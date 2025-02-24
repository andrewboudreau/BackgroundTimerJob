namespace BackgroundTimerJob.DistributedLock;

/// <summary>
/// An implementation of <see cref="IDistributedJobLock"/> that uses an Azure Blob lease.
/// </summary>
public class AzureBlobDistributedJobLock(BlobLeaseClient leaseClient, ILogger<AzureBlobDistributedJobLock>? logger) 
    : IDistributedJobLock
{
    private bool released = false;

    /// <inheritdoc />
    public async Task ReleaseAsync()
    {
        if (released)
        {
            return;
        }

        try
        {
            await leaseClient.ReleaseAsync();
            logger?.LogDebug("Released blob lease successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to release blob lease.");
        }
        finally
        {
            released = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await ReleaseAsync();
    }
}
