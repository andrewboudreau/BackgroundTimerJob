using Azure.Identity;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundTimerJob.DistributedLock;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Blob distributed job lock to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="containerName">The Azure Blob container name.</param>
    public static void AddAzureBlobDistributedJobLockProvider(this IServiceCollection services, string containerName = "backgroundtimerjob")
    {
        services.AddAzureClients(builder =>
            builder
                .UseCredential(new DefaultAzureCredential())
                .AddBlobServiceClient(containerName));

        services.AddDistributedJobLockProvider(containerName);
    }

    /// <summary>
    /// Adds the Azure Blob distributed job lock to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="containerName">The Azure Blob container name.</param>
    public static void AddAzureBlobDistributedJobLockProvider(this IServiceCollection services, string connectionString, string containerName = "backgroundtimerjob")
    {
        services.AddAzureClients(builder => builder.AddBlobServiceClient(connectionString));
        services.AddDistributedJobLockProvider(containerName);
    }

    public static void AddDistributedJobLockProvider(this IServiceCollection services, string containerName)
    {
        services.AddSingleton<IDistributedJobLockProvider>(sp =>
        {
            var logger = sp.GetService<ILogger<AzureBlobDistributedJobLock>>();

            var blobClient = sp.GetRequiredService<BlobServiceClient>();
            var blobContainerClient = blobClient.GetBlobContainerClient(containerName);

            blobContainerClient.CreateIfNotExists(PublicAccessType.None);
            var distributedLockProvider = new AzureBlobDistributedJobLockProvider(blobContainerClient, logger);
            return distributedLockProvider;
        });
    }
}