namespace RaceDay.Api.Services;

/// <summary>Result of checking an uploaded image file.</summary>
public record ImageValidationResult(bool IsValid, string? Error, string ContentType, string Extension);

/// <summary>
/// Checks an uploaded file really is a JPEG, PNG, GIF or WebP image under 5 MB. The file signature (first bytes)
/// is checked, not just the file name, so renamed documents are rejected.
/// </summary>
public static class ImageFileValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static async Task<ImageValidationResult> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return Invalid("Please choose an image file to upload.");
        }

        if (file.Length > MaxBytes)
        {
            return Invalid("The image is too large. The maximum size is 5 MB.");
        }

        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        }

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new ImageValidationResult(true, null, "image/jpeg", ".jpg");
        }

        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return new ImageValidationResult(true, null, "image/png", ".png");
        }

        if (read >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
        {
            return new ImageValidationResult(true, null, "image/gif", ".gif");
        }

        if (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return new ImageValidationResult(true, null, "image/webp", ".webp");
        }

        return Invalid("Only JPG, PNG, GIF or WebP images are allowed.");
    }

    private static ImageValidationResult Invalid(string message) => new(false, message, string.Empty, string.Empty);
}
