using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace VentureHerdManager.Api.Services;

public class PhotoStorageService : IPhotoStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PhotoStorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration["BlobStorage:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            using var storageTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            storageTimeout.CancelAfter(TimeSpan.FromSeconds(20));

            try
            {
                return await UploadToBlobAsync(
                    connectionString,
                    file,
                    folder,
                    storageTimeout.Token);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine(
                    $"Blob photo upload unavailable; using local storage. {ex.Message}");
            }
        }

        return await UploadToLocalAsync(file, folder, cancellationToken);
    }

    private async Task<string> UploadToBlobAsync(
        string connectionString,
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        var containerName =
            _configuration["BlobStorage:ContainerName"]
            ?? "event-photos";

        var containerClient = new BlobContainerClient(connectionString, containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var extension = Path.GetExtension(file.FileName);
        var safeFolder = folder.Trim('/').Trim();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var blobPath = string.IsNullOrWhiteSpace(safeFolder)
            ? fileName
            : $"{safeFolder}/{fileName}";

        var blobClient = containerClient.GetBlobClient(blobPath);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    private async Task<string> UploadToLocalAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var safeFolder = folder.Trim('/').Trim();
        var uploadsRoot = Path.Combine(webRootPath, "uploads", safeFolder);
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return $"/uploads/{safeFolder}/{fileName}".Replace("\\", "/");
        }

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/uploads/{safeFolder}/{fileName}".Replace("\\", "/");
    }
}
