using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Commands.EditMessage;

public record EditMessageCommand : IRequest<BaseResponse<MessageDto>>
{
    public Guid MessageId { get; init; }
    public required string UserId { get; init; }
    public required string NewCiphertext { get; init; }
    public required string NewEphemeralKey { get; init; }
}

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, BaseResponse<MessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public EditMessageCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<MessageDto>> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
        {
            return BaseResponse<MessageDto>.Failure("Message not found.");
        }

        // Only the sender of the message can edit it
        if (message.SenderId != request.UserId)
        {
            return BaseResponse<MessageDto>.Failure("You do not have permission to edit this message.");
        }

        // If the message is ephemeral and has already been read, it has started burning
        if (message.BurnAfterSeconds.HasValue && message.ReadAt.HasValue)
        {
            return BaseResponse<MessageDto>.Failure("Cannot edit a self-destructing message after it has started burning.");
        }

        // Apply new encrypted contents
        message.Ciphertext = request.NewCiphertext;
        message.EphemeralKey = request.NewEphemeralKey;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderUsername = message.Sender!.UserName!,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = message.Receiver!.UserName!,
            Ciphertext = message.Ciphertext,
            EphemeralKey = message.EphemeralKey,
            SignedPrekeyIdUsed = message.SignedPrekeyIdUsed,
            OneTimePrekeyIdUsed = message.OneTimePrekeyIdUsed,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt,
            BurnAfterSeconds = message.BurnAfterSeconds,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt
        };

        // Notify the recipient in real-time
        await _notificationService.NotifyMessageEditedAsync(message.Receiver!.UserName!, dto, cancellationToken);

        return BaseResponse<MessageDto>.Success(dto, "Message edited successfully.");
    }
}
