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

namespace SessionApp.Application.Features.Groups.Queries.GetPinnedMessages;

public record GetPinnedMessagesQuery : IRequest<BaseResponse<List<MessageDto>>>
{
    public Guid? GroupId { get; init; }
    public string? WithUsername { get; init; }
    public required string RequestingUserId { get; init; }
}

public class GetPinnedMessagesQueryHandler : IRequestHandler<GetPinnedMessagesQuery, BaseResponse<List<MessageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPinnedMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<MessageDto>>> Handle(GetPinnedMessagesQuery request, CancellationToken cancellationToken)
    {
        List<MessageDto> dtos = new();

        if (request.GroupId.HasValue && request.GroupId.Value != Guid.Empty)
        {
            // Group Pins
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value, cancellationToken);

            if (group == null)
            {
                return BaseResponse<List<MessageDto>>.Failure("Group not found.");
            }

            var isMember = group.Members.Any(m => m.UserId == request.RequestingUserId);
            if (!isMember)
            {
                return BaseResponse<List<MessageDto>>.Failure("You must be a member of the group to retrieve pinned messages.");
            }

            var pinnedMessages = await _context.PinnedMessages
                .Where(pm => pm.GroupId == request.GroupId.Value)
                .Include(pm => pm.Message)
                    .ThenInclude(m => m!.Sender)
                .Include(pm => pm.Message)
                    .ThenInclude(m => m!.Reactions)
                        .ThenInclude(r => r.User)
                .OrderByDescending(pm => pm.PinnedAt)
                .Select(pm => pm.Message)
                .Where(m => m != null)
                .ToListAsync(cancellationToken);

            dtos = pinnedMessages.Select(m => new MessageDto
            {
                Id = m!.Id,
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
                IsPinned = true,
                Reactions = m.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    Username = r.User!.UserName!,
                    ReactionCiphertext = r.ReactionCiphertext
                }).ToList()
            }).ToList();
        }
        else if (!string.IsNullOrEmpty(request.WithUsername))
        {
            // Direct Chat Pins
            var otherUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.WithUsername, cancellationToken);

            if (otherUser == null)
            {
                return BaseResponse<List<MessageDto>>.Failure("Target user not found.");
            }

            var pinnedMessages = await _context.PinnedMessages
                .Include(pm => pm.Message)
                    .ThenInclude(m => m!.Sender)
                .Include(pm => pm.Message)
                    .ThenInclude(m => m!.Receiver)
                .Include(pm => pm.Message)
                    .ThenInclude(m => m!.Reactions)
                        .ThenInclude(r => r.User)
                .Where(pm => pm.Message != null && pm.GroupId == Guid.Empty &&
                            ((pm.Message.SenderId == request.RequestingUserId && pm.Message.ReceiverId == otherUser.Id) ||
                             (pm.Message.SenderId == otherUser.Id && pm.Message.ReceiverId == request.RequestingUserId)))
                .OrderByDescending(pm => pm.PinnedAt)
                .Select(pm => pm.Message)
                .Where(m => m != null)
                .ToListAsync(cancellationToken);

            dtos = pinnedMessages.Select(m => new MessageDto
            {
                Id = m!.Id,
                SenderId = m.SenderId,
                SenderUsername = m.Sender!.UserName!,
                ReceiverId = m.ReceiverId,
                ReceiverUsername = m.Receiver != null ? m.Receiver.UserName! : null,
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
                IsPinned = true,
                Reactions = m.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    MessageId = r.MessageId,
                    UserId = r.UserId,
                    Username = r.User!.UserName!,
                    ReactionCiphertext = r.ReactionCiphertext
                }).ToList()
            }).ToList();
        }
        else
        {
            return BaseResponse<List<MessageDto>>.Failure("Specify either GroupId or WithUsername to query pinned messages.");
        }

        return BaseResponse<List<MessageDto>>.Success(dtos, "Pinned messages retrieved successfully.");
    }
}
