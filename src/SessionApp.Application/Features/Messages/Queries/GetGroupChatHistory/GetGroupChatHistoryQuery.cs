using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Queries.GetGroupChatHistory;

public record GetGroupChatHistoryQuery : IRequest<BaseResponse<List<MessageDto>>>
{
    public Guid GroupId { get; init; }
    public required string UserId { get; init; }
}

public class GetGroupChatHistoryQueryHandler : IRequestHandler<GetGroupChatHistoryQuery, BaseResponse<List<MessageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetGroupChatHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<MessageDto>>> Handle(GetGroupChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<List<MessageDto>>.Failure("Group not found.");
        }

        // Verify user is a member of the group
        var membership = group.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (membership == null)
        {
            return BaseResponse<List<MessageDto>>.Failure("You must be a member of the group to retrieve chat history.");
        }
        var isGroupMuted = membership.MutedUntil.HasValue && membership.MutedUntil.Value > DateTime.UtcNow;

        var messages = await _context.Messages
            .Where(m => m.GroupId == request.GroupId)
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);

        var pinnedMessageIds = await _context.PinnedMessages
            .Where(pm => pm.GroupId == request.GroupId)
            .Select(pm => pm.MessageId)
            .ToListAsync(cancellationToken);

        var dtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderUsername = m.Sender!.UserName!,
            ReceiverId = null,
            ReceiverUsername = null,
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
            IsAlertSilenced = isGroupMuted,
            Reactions = m.Reactions.Select(r => new MessageReactionDto
            {
                Id = r.Id,
                MessageId = r.MessageId,
                UserId = r.UserId,
                Username = r.User!.UserName!,
                ReactionCiphertext = r.ReactionCiphertext
            }).ToList()
        }).ToList();

        return BaseResponse<List<MessageDto>>.Success(dtos, "Group chat history retrieved successfully.");
    }
}
