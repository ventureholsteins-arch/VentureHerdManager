using Microsoft.AspNetCore.Mvc;
using VentureHerdManager.Api.Services;

namespace VentureHerdManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
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

        var targetFolder = string.IsNullOrWhiteSpace(folder)
            ? "general"
            : folder;

        string url;
        try
        {
            url = await _photoStorageService.UploadImageAsync(
                file,
                targetFolder,
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "Photo storage is temporarily unavailable",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

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
