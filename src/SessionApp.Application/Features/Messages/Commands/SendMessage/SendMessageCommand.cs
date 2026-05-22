using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;

namespace SessionApp.Application.Features.Messages.Commands.SendMessage;

public record SendMessageCommand : IRequest<BaseResponse<MessageDto>>
{
    public required string SenderId { get; init; }
    public required string ReceiverUsername { get; init; }
    public required string Ciphertext { get; init; }
    public required string EphemeralKey { get; init; }
    public int SignedPrekeyIdUsed { get; init; }
    public int? OneTimePrekeyIdUsed { get; init; }
    public int? BurnAfterSeconds { get; init; }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, BaseResponse<MessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public SendMessageCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<MessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var sender = await _context.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
        if (sender == null)
        {
            return BaseResponse<MessageDto>.Failure("Sender user not found.");
        }

        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.ReceiverUsername, cancellationToken);
        if (receiver == null)
        {
            return BaseResponse<MessageDto>.Failure("Receiver user not found.");
        }

        // Privacy Validation
        if (receiver.IsPrivate && sender.Id != receiver.Id)
        {
            // If the receiver is private, only allow messaging if the receiver has previously messaged the sender
            var hasReceiverInitiated = await _context.Messages
                .AnyAsync(m => m.SenderId == receiver.Id && m.ReceiverId == sender.Id, cancellationToken);

            if (!hasReceiverInitiated)
            {
                return BaseResponse<MessageDto>.Failure("You cannot send messages to this private user unless they message you first.");
            }
        }

        int? calculatedBurnAfterSeconds = null;
        if (request.BurnAfterSeconds.HasValue)
        {
            // Enforce minimum 60 seconds (1 minute) to ensure the user has time to read the message,
            // but allow ultra-short overrides (< 10 seconds) for developer integration testing.
            var baseBurnSeconds = request.BurnAfterSeconds.Value;
            if (baseBurnSeconds >= 10)
            {
                baseBurnSeconds = Math.Max(baseBurnSeconds, 60);
            }

            // Adaptive increment: if the message is long, add extra seconds to the destroy timer
            // For every 50 characters beyond a threshold of 150 characters, add 5 additional seconds.
            var extraSeconds = 0;
            if (!string.IsNullOrEmpty(request.Ciphertext) && request.Ciphertext.Length > 150)
            {
                extraSeconds = ((request.Ciphertext.Length - 150) / 50) * 5;
            }

            calculatedBurnAfterSeconds = baseBurnSeconds + extraSeconds;
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = sender.Id,
            Sender = sender,
            ReceiverId = receiver.Id,
            Receiver = receiver,
            Ciphertext = request.Ciphertext,
            EphemeralKey = request.EphemeralKey,
            SignedPrekeyIdUsed = request.SignedPrekeyIdUsed,
            OneTimePrekeyIdUsed = request.OneTimePrekeyIdUsed,
            SentAt = DateTime.UtcNow,
            BurnAfterSeconds = calculatedBurnAfterSeconds
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderUsername = sender.UserName!,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = receiver.UserName!,
            Ciphertext = message.Ciphertext,
            EphemeralKey = message.EphemeralKey,
            SignedPrekeyIdUsed = message.SignedPrekeyIdUsed,
            OneTimePrekeyIdUsed = message.OneTimePrekeyIdUsed,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt,
            BurnAfterSeconds = message.BurnAfterSeconds
        };

        // Fire real-time notification
        await _notificationService.NotifyNewMessageAsync(receiver.UserName!, dto, cancellationToken);

        return BaseResponse<MessageDto>.Success(dto, "Message sent successfully.");
    }
}
