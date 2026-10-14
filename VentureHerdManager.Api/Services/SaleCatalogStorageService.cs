using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace VentureHerdManager.Api.Services;

public sealed class SaleCatalogStorageService(IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private BlobClient Client()
    {
        var connection = configuration["BlobStorage:ConnectionString"] ?? throw new InvalidOperationException("Blob storage is not configured.");
        var container = new BlobContainerClient(connection, configuration["BlobStorage:ContainerName"] ?? "herd-photos");
        container.CreateIfNotExists(PublicAccessType.Blob);
        return container.GetBlobClient("sale/catalog.json");
    }

    public async Task<SaleCatalogDocument> ReadAsync(CancellationToken ct)
    {
        try
        {
            var response = await Client().DownloadContentAsync(ct);
            return response.Value.Content.ToObjectFromJson<SaleCatalogDocument>(JsonOptions) ?? new SaleCatalogDocument();
        }
        catch (RequestFailedException exception) when (exception.Status == 404) { return new SaleCatalogDocument(); }
    }

    public async Task WriteAsync(SaleCatalogDocument catalog, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(catalog, JsonOptions);
        await Client().UploadAsync(BinaryData.FromString(json), new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json", CacheControl = "no-store" } }, ct);
    }
}

public sealed class SaleCatalogDocument
{
    public bool IsLive { get; set; }
    public string SaleName { get; set; } = "Venture Holsteins Tag Sale";
    public string ComingSoonMessage { get; set; } = "The next Venture Holsteins tag sale is being prepared.";
    public string? Dates { get; set; }
    public string? Location { get; set; }
    public string? Notice { get; set; }
    public string? Terms { get; set; }
    public string? ContactUrl { get; set; }
    public JsonElement[] Lots { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
