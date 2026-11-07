using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Stand-in for Azure Blob Storage in the unit tests: nothing is uploaded, a fake URL is returned
/// and the call is recorded so tests can check it happened.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    private readonly object sync = new();
    private readonly List<string> uploads = new();

    /// <summary>Set to false to simulate "Azure Blob Storage is not configured".</summary>
    public bool Configured { get; set; } = true;

    public bool IsConfigured => this.Configured;

    public IReadOnlyList<string> Uploads
    {
        get
        {
            lock (this.sync)
            {
                return this.uploads.ToList();
            }
        }
    }

    public Task<string> UploadImageAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default)
    {
        var url = $"https://fake-blob.test/raceday-images/{folder}/{Guid.NewGuid():N}{extension}";

        lock (this.sync)
        {
            this.uploads.Add(url);
        }

        return Task.FromResult(url);
    }
}
