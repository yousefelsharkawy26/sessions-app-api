using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SessionApp.Application.Common.Interfaces;

namespace SessionApp.Infrastructure.Services;

public class LocalAttachmentStorageService : IAttachmentStorageService
{
    public async Task<string> UploadAttachmentAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("Attachment data stream is empty.");
        }

        // Clean up and generate unique filename to prevent path traversal or collision
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".bin"; // default to generic binary payload if no extension provided
        }
        
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // Resolve absolute attachments directory path
        var rootDir = Directory.GetCurrentDirectory();
        var attachmentsFolder = Path.Combine(rootDir, "wwwroot", "attachments");

        if (!Directory.Exists(attachmentsFolder))
        {
            Directory.CreateDirectory(attachmentsFolder);
        }

        var filePath = Path.Combine(attachmentsFolder, uniqueFileName);

        using (var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await fileStream.CopyToAsync(destinationStream, cancellationToken);
        }

        // Return relative URL served under Static Files
        return $"/attachments/{uniqueFileName}";
    }
}
