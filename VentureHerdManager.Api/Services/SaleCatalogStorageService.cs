using System.Text.Json;

namespace VentureHerdManager.Api.Services;

public sealed class SaleCatalogStorageService(IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private string CatalogPath
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
                    return Path.Combine(root, "sale-catalog.json");
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
            throw new InvalidOperationException("Sale catalog storage is unavailable.");
        }
    }

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
