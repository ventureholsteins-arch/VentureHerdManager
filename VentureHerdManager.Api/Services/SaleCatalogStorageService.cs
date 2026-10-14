using System.Text.Json;

namespace VentureHerdManager.Api.Services;

public sealed class SaleCatalogStorageService(IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private string StorageRoot
    {
        get
        {
            var candidates = environment.IsProduction()
                ? new[]
                {
                    Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/home", "data", "venture-holsteins"),
                    Path.Combine(Path.GetTempPath(), "venture-holsteins")
                }
                : new[] { Path.Combine(environment.ContentRootPath, "App_Data") };
            foreach (var root in candidates)
            {
                try
                {
                    Directory.CreateDirectory(root);
                    return root;
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
            throw new InvalidOperationException("Sale catalog storage is unavailable.");
        }
    }

    private string CatalogPath => Path.Combine(StorageRoot, "sale-catalog.json");

    public async Task<SaleCatalogDocument> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(CatalogPath)) return new SaleCatalogDocument();
        await using var stream = File.OpenRead(CatalogPath);
        return await JsonSerializer.DeserializeAsync<SaleCatalogDocument>(stream, JsonOptions, ct) ?? new SaleCatalogDocument();
    }

    public async Task WriteAsync(SaleCatalogDocument catalog, CancellationToken ct)
    {
        var temporaryPath = CatalogPath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, catalog, JsonOptions, ct);
        File.Move(temporaryPath, CatalogPath, true);
    }

    public async Task<string> WritePhotoAsync(IFormFile file, CancellationToken ct)
    {
        var validated = await ImageUploadValidator.ValidateAsync(file, ct);
        var photosRoot = Path.Combine(StorageRoot, "photos");
        Directory.CreateDirectory(photosRoot);
        var fileName = $"{Guid.NewGuid():N}{validated.Extension}";
        var path = Path.Combine(photosRoot, fileName);
        await using var source = file.OpenReadStream();
        await using var destination = File.Create(path);
        await source.CopyToAsync(destination, ct);
        return fileName;
    }

    public (string Path, string ContentType)? FindPhoto(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal)) return null;
        var path = Path.Combine(StorageRoot, "photos", safeName);
        if (!File.Exists(path)) return null;
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            _ => "application/octet-stream"
        };
        return (path, contentType);
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
    public JsonElement? HomePage { get; set; }
    public JsonElement? GeneticsPage { get; set; }
    public JsonElement[] GeneticsItems { get; set; } = [];
    public JsonElement[] Lots { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
