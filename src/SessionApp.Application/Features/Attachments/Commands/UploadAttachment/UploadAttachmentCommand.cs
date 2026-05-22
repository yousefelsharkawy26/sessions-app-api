using System.IO;
using MediatR;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Attachments.Commands.UploadAttachment;

public record UploadAttachmentCommand : IRequest<BaseResponse<string>>
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
}

public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, BaseResponse<string>>
{
    private readonly IAttachmentStorageService _attachmentStorageService;

    public UploadAttachmentCommandHandler(IAttachmentStorageService attachmentStorageService)
    {
        _attachmentStorageService = attachmentStorageService;
    }

    public async Task<BaseResponse<string>> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var relativeUrl = await _attachmentStorageService.UploadAttachmentAsync(
                request.FileStream,
                request.FileName,
                cancellationToken);

            return BaseResponse<string>.Success(relativeUrl, "Attachment uploaded successfully.");
        }
        catch (Exception ex)
        {
            return BaseResponse<string>.Failure($"Attachment upload failed: {ex.Message}");
        }
    }
}
