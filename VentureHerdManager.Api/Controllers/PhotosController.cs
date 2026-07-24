using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif"
    ];

    private readonly IPhotoStorageService _photoStorageService;

    public PhotosController(IPhotoStorageService photoStorageService)
    {
        _photoStorageService = photoStorageService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<PhotoUploadResponse>> Upload(
        IFormFile file,
        [FromForm] string? folder,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest("Unsupported file type.");
        }

        var targetFolder = string.IsNullOrWhiteSpace(folder)
            ? "general"
            : folder;

        var url = await _photoStorageService.UploadImageAsync(
            file,
            targetFolder,
            cancellationToken);

        return Ok(new PhotoUploadResponse
        {
            Url = url
        });
    }
}

public class PhotoUploadResponse
{
    public string Url { get; set; } = string.Empty;
}
