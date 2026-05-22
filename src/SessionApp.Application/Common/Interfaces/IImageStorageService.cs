using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task<string> UploadImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken);
}
