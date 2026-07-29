using System.Text;
using System.Text.RegularExpressions;

namespace VentureHerdManager.Api.Services;

public sealed record ValidatedImage(
    string Extension,
    string ContentType);

public static partial class ImageUploadValidator
{
    public const long MaximumFileSize = 15 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, HashSet<string>>
        AllowedDeclaredTypes =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] =
                [
                    "image/jpeg",
                    "image/jpg",
                    "image/pjpeg"
                ],
                [".png"] = ["image/png"],
                [".webp"] = ["image/webp"],
                [".heic"] =
                [
                    "image/heic",
                    "image/heif"
                ]
            };

    private static readonly HashSet<string> HeifBrands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "heic",
            "heix",
            "hevc",
            "hevx",
            "heim",
            "heis",
            "mif1",
            "msf1"
        };

    public static async Task<ValidatedImage> ValidateAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new InvalidDataException("The image file is empty.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new InvalidDataException(
                "The image is larger than the 15 MB limit.");
        }

        var header = new byte[16];
        int bytesRead;

        await using (var stream = file.OpenReadStream())
        {
            bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);
        }

        var detected = DetectImage(header.AsSpan(0, bytesRead))
            ?? throw new InvalidDataException(
                "The uploaded file is not a supported image.");

        var declaredType = file.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(declaredType) ||
            !AllowedDeclaredTypes[detected.Extension]
                .Contains(declaredType))
        {
            throw new InvalidDataException(
                "The file contents do not match its image type.");
        }

        return detected;
    }

    public static string NormalizeFolder(string? folder)
    {
        var candidate = string.IsNullOrWhiteSpace(folder)
            ? "general"
            : folder.Trim();

        if (!SafeFolderPattern().IsMatch(candidate))
        {
            throw new InvalidDataException(
                "The upload folder name is invalid.");
        }

        return candidate;
    }

    private static ValidatedImage? DetectImage(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return new ValidatedImage(".jpg", "image/jpeg");
        }

        ReadOnlySpan<byte> pngSignature =
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        ];

        if (header.Length >= pngSignature.Length &&
            header[..pngSignature.Length].SequenceEqual(pngSignature))
        {
            return new ValidatedImage(".png", "image/png");
        }

        if (header.Length >= 12 &&
            header[..4].SequenceEqual("RIFF"u8) &&
            header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return new ValidatedImage(".webp", "image/webp");
        }

        if (header.Length >= 12 &&
            header.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            var brand = Encoding.ASCII.GetString(header.Slice(8, 4));
            if (HeifBrands.Contains(brand))
            {
                return new ValidatedImage(".heic", "image/heic");
            }
        }

        return null;
    }

    [GeneratedRegex(
        "^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SafeFolderPattern();
}
