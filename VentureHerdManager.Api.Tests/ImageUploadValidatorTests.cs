using Microsoft.AspNetCore.Http;
using VentureHerdManager.Api.Services;
using Xunit;

namespace VentureHerdManager.Api.Tests;

public sealed class ImageUploadValidatorTests
{
    [Fact]
    public async Task ValidPngIsDetectedFromContents()
    {
        byte[] bytes =
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ];
        var file = CreateFile(bytes, "pretend.jpg", "image/png");

        var validated =
            await ImageUploadValidator.ValidateAsync(file);

        Assert.Equal(".png", validated.Extension);
        Assert.Equal("image/png", validated.ContentType);
    }

    [Fact]
    public async Task RenamedNonImageIsRejected()
    {
        var file = CreateFile(
            "this is not an image"u8.ToArray(),
            "not-really.jpg",
            "image/jpeg");

        await Assert.ThrowsAsync<InvalidDataException>(
            () => ImageUploadValidator.ValidateAsync(file));
    }

    [Fact]
    public async Task DeclaredTypeMustMatchImageContents()
    {
        byte[] bytes =
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ];
        var file = CreateFile(bytes, "mismatch.jpg", "image/jpeg");

        await Assert.ThrowsAsync<InvalidDataException>(
            () => ImageUploadValidator.ValidateAsync(file));
    }

    [Theory]
    [InlineData("../animals")]
    [InlineData("heat-events/../../animals")]
    [InlineData("heat events")]
    [InlineData(".")]
    [InlineData("C:\\outside")]
    public void UnsafeFolderIsRejected(string folder)
    {
        Assert.Throws<InvalidDataException>(
            () => ImageUploadValidator.NormalizeFolder(folder));
    }

    [Theory]
    [InlineData(null, "general")]
    [InlineData("", "general")]
    [InlineData("heat-events", "heat-events")]
    [InlineData("animal_photos", "animal_photos")]
    public void SafeFolderIsNormalized(
        string? folder,
        string expected)
    {
        Assert.Equal(
            expected,
            ImageUploadValidator.NormalizeFolder(folder));
    }

    private static FormFile CreateFile(
        byte[] bytes,
        string fileName,
        string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(
            stream,
            0,
            bytes.Length,
            "file",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
