using SessionApp.Application.Common.Interfaces;

namespace SessionApp.Infrastructure.Services;

public class LocalImageStorageService : IImageStorageService
{
    public async Task<string> UploadImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            throw new ArgumentException("Image data is empty.");
        }

        // Clean up and generate unique filename
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".jpg"; // fallback
        }
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // Resolve absolute uploads directory path
        var rootDir = Directory.GetCurrentDirectory();
        var uploadsFolder = Path.Combine(rootDir, "wwwroot", "uploads");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        // Return the relative URL served under Static Files
        return $"/uploads/{uniqueFileName}";
    }
}
