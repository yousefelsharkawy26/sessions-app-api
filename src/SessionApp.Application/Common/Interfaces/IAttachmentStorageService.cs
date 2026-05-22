using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Common.Interfaces;

public interface IAttachmentStorageService
{
    Task<string> UploadAttachmentAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);
}
