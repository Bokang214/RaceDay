using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>Settings for Azure Blob Storage (section "BlobStorage" in appsettings / environment variables).</summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>Azure Storage connection string. Leave empty to switch image uploads off (they then return 503).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Blob container that holds all RaceDay images.</summary>
    public string ContainerName { get; set; } = "raceday-images";

    /// <summary>
    /// Optional. Public base URL used for the URLs that are saved in the database, for example when the
    /// API reaches Azurite through a Docker network name but browsers must use http://localhost:10000/devstoreaccount1.
    /// Leave empty for a real Azure Storage account.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}

/// <summary>Stores image files in Azure Blob Storage. Only the API talks to Azure - the MVC app never does.</summary>
public interface IBlobStorageService
{
    /// <summary>False when no connection string has been configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads the image and returns the public URL that is saved in the database.</summary>
    Task<string> UploadImageAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions options;
    private readonly SemaphoreSlim containerLock = new(1, 1);
    private BlobContainerClient? container;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        this.options = options.Value;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(this.options.ConnectionString);

    public async Task<string> UploadImageAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default)
    {
        if (!this.IsConfigured)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var containerClient = await this.GetContainerAsync(cancellationToken);

        var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };
        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(this.options.PublicBaseUrl))
        {
            return $"{this.options.PublicBaseUrl.TrimEnd('/')}/{this.options.ContainerName}/{blobName}";
        }

        return blobClient.Uri.ToString();
    }

    // Creates the container the first time it is needed. Blobs are publicly readable so the browser can
    // show the images straight from the stored URL.
    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (this.container is not null)
        {
            return this.container;
        }

        await this.containerLock.WaitAsync(cancellationToken);
        try
        {
            if (this.container is null)
            {
                var client = new BlobContainerClient(this.options.ConnectionString, this.options.ContainerName);
                await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                this.container = client;
            }

            return this.container;
        }
        finally
        {
            this.containerLock.Release();
        }
    }
}
