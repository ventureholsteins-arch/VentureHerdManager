namespace VentureHerdManager.Api.Services;

public interface IPhotoStorageService
{
    Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default);
}
