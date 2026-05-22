using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Messages.Queries.GetChatHistory;

public record GetChatHistoryQuery : IRequest<BaseResponse<List<MessageDto>>>
{
    public required string UserId { get; init; }
    public required string WithUsername { get; init; }
    public string? DeviceId { get; init; }
}

public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, BaseResponse<List<MessageDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public GetChatHistoryQueryHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<List<MessageDto>>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.WithUsername, cancellationToken);
        if (otherUser == null)
        {
            return BaseResponse<List<MessageDto>>.Failure("Target user not found.");
        }

        // Fetch tracking entities to allow modifications/deletions
        var allMessages = await _context.Messages
            .Where(m => (m.SenderId == request.UserId && m.ReceiverId == otherUser.Id) ||
                        (m.SenderId == otherUser.Id && m.ReceiverId == request.UserId && (m.RecipientDeviceId == request.DeviceId || m.RecipientDeviceId == null)))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Dynamic Purge: Remove already expired ephemeral messages from DB
        var expiredMessages = allMessages
            .Where(m => m.BurnAfterSeconds != null && 
                        m.ReadAt != null && 
                        now >= m.ReadAt.Value.AddSeconds(m.BurnAfterSeconds.Value))
            .ToList();

        if (expiredMessages.Any())
        {
            _context.Messages.RemoveRange(expiredMessages);
            await _context.SaveChangesAsync(cancellationToken);
            allMessages = allMessages.Except(expiredMessages).ToList();
        }

        // Mark incoming messages as read
        var unreadIncomingMessages = allMessages
            .Where(m => m.SenderId == otherUser.Id && m.ReceiverId == request.UserId && m.ReadAt == null)
            .ToList();

        if (unreadIncomingMessages.Any())
        {
            foreach (var msg in unreadIncomingMessages)
            {
                msg.ReadAt = now;
                msg.DeliveredAt ??= now;
            }
            await _context.SaveChangesAsync(cancellationToken);

            // Fetch requester username to include in read receipt notification
            var requester = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            var requesterUsername = requester?.UserName ?? string.Empty;

            // Notify the sender in real-time that their messages were read
            var messageIds = unreadIncomingMessages.Select(m => m.Id).ToList();
            await _notificationService.NotifyMessagesReadAsync(
                otherUser.UserName!, 
                requesterUsername, 
                messageIds, 
                cancellationToken);
        }

        var chatMsgIds = allMessages.Select(m => m.Id).ToList();
        var pinnedMessageIds = await _context.PinnedMessages
            .Where(pm => chatMsgIds.Contains(pm.MessageId))
            .Select(pm => pm.MessageId)
            .ToListAsync(cancellationToken);

        var mute = await _context.DirectChatMutes
            .FirstOrDefaultAsync(m => m.MuterId == request.UserId && m.MutedUserId == otherUser.Id, cancellationToken);
        var isCurrentlyMuted = mute != null && (mute.MutedUntil == null || mute.MutedUntil > DateTime.UtcNow);

        var dtos = allMessages
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderUsername = m.Sender!.UserName!,
                ReceiverId = m.ReceiverId,
                ReceiverUsername = m.Receiver != null ? m.Receiver.UserName! : null,
                RecipientDeviceId = m.RecipientDeviceId,
                GroupId = m.GroupId,
                Ciphertext = m.Ciphertext,
                EphemeralKey = m.EphemeralKey,
                SignedPrekeyIdUsed = m.SignedPrekeyIdUsed,
                OneTimePrekeyIdUsed = m.OneTimePrekeyIdUsed,
                SentAt = m.SentAt,
                DeliveredAt = m.DeliveredAt,
                ReadAt = m.ReadAt,
                BurnAfterSeconds = m.BurnAfterSeconds,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt,
                ParentMessageId = m.ParentMessageId,
                IsPinned = pinnedMessageIds.Contains(m.Id),
                IsAlertSilenced = isCurrentlyMuted && m.SenderId == otherUser.Id,
                Reactions = m.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    Username = r.User!.UserName!,
                    ReactionCiphertext = r.ReactionCiphertext
                }).ToList()
            })
            .ToList();

        return BaseResponse<List<MessageDto>>.Success(dtos);
    }
}
