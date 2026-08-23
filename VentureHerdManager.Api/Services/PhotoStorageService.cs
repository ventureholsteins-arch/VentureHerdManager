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
        var safeFolder =
            ImageUploadValidator.NormalizeFolder(folder);
        var validatedImage =
            await ImageUploadValidator.ValidateAsync(
                file,
                cancellationToken);

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
                    safeFolder,
                    validatedImage,
                    storageTimeout.Token);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (_environment.IsProduction())
                {
                    throw new InvalidOperationException(
                        "Photo storage is unavailable. The herd record was not affected; retry the photo after Blob Storage is restored.",
                        ex);
                }
                Console.WriteLine($"Blob photo upload unavailable; using local development storage. {ex.Message}");
            }
        }

        if (_environment.IsProduction())
        {
            throw new InvalidOperationException(
                "BlobStorage:ConnectionString is not configured. Production photo uploads cannot use temporary local storage.");
        }

        return await UploadToLocalAsync(
            file,
            safeFolder,
            validatedImage,
            cancellationToken);
    }

    private async Task<string> UploadToBlobAsync(
        string connectionString,
        IFormFile file,
        string folder,
        ValidatedImage validatedImage,
        CancellationToken cancellationToken)
    {
        var containerName =
            _configuration["BlobStorage:ContainerName"]
            ?? "event-photos";

        var containerClient = new BlobContainerClient(connectionString, containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var fileName =
            $"{Guid.NewGuid():N}{validatedImage.Extension}";
        var blobPath = $"{folder}/{fileName}";

        var blobClient = containerClient.GetBlobClient(blobPath);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = validatedImage.ContentType
                }
            },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    private async Task<string> UploadToLocalAsync(
        IFormFile file,
        string folder,
        ValidatedImage validatedImage,
        CancellationToken cancellationToken)
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadsBase = Path.GetFullPath(
            Path.Combine(webRootPath, "uploads"));
        var uploadsRoot = Path.GetFullPath(
            Path.Combine(uploadsBase, folder));
        var expectedPrefix =
            uploadsBase.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!uploadsRoot.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The upload path is invalid.");
        }

        Directory.CreateDirectory(uploadsRoot);

        var fileName =
            $"{Guid.NewGuid():N}{validatedImage.Extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return $"/uploads/{folder}/{fileName}".Replace("\\", "/");
        }

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/uploads/{folder}/{fileName}".Replace("\\", "/");
    }
}
