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

        var messages = await _context.Messages
            .Where(m => (m.SenderId == request.UserId && m.ReceiverId == otherUser.Id) ||
                        (m.SenderId == otherUser.Id && m.ReceiverId == request.UserId))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderUsername = m.Sender!.UserName!,
                ReceiverId = m.ReceiverId,
                ReceiverUsername = m.Receiver!.UserName!,
                Ciphertext = m.Ciphertext,
                EphemeralKey = m.EphemeralKey,
                SignedPrekeyIdUsed = m.SignedPrekeyIdUsed,
                OneTimePrekeyIdUsed = m.OneTimePrekeyIdUsed,
                SentAt = m.SentAt,
                ReadAt = m.ReadAt
            })
            .ToListAsync(cancellationToken);

        // Mark incoming messages as read
        var unreadIncomingMessages = await _context.Messages
            .Where(m => m.SenderId == otherUser.Id && m.ReceiverId == request.UserId && m.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (unreadIncomingMessages.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var msg in unreadIncomingMessages)
            {
                msg.ReadAt = now;
            }
            await _context.SaveChangesAsync(cancellationToken);

            // Fetch requester username to include in read receipt notification
            var requester = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            var requesterUsername = requester?.UserName ?? string.Empty;

            // Update in the returned list as well
            foreach (var dto in messages.Where(m => m.SenderId == otherUser.Id && m.ReadAt == null))
            {
                dto.ReadAt = now;
            }

            // Notify the sender in real-time that their messages were read
            var messageIds = unreadIncomingMessages.Select(m => m.Id).ToList();
            await _notificationService.NotifyMessagesReadAsync(
                otherUser.UserName!, 
                requesterUsername, 
                messageIds, 
                cancellationToken);
        }

        return BaseResponse<List<MessageDto>>.Success(messages);
    }
}
