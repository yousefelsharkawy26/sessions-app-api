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
        var isMember = group.Members.Any(m => m.UserId == request.UserId);
        if (!isMember)
        {
            return BaseResponse<List<MessageDto>>.Failure("You must be a member of the group to retrieve chat history.");
        }

        var messages = await _context.Messages
            .Where(m => m.GroupId == request.GroupId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
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
            EditedAt = m.EditedAt
        }).ToList();

        return BaseResponse<List<MessageDto>>.Success(dtos, "Group chat history retrieved successfully.");
    }
}
