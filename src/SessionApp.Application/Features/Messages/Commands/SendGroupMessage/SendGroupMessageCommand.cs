using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Commands.SendGroupMessage;

public record SendGroupMessageCommand : IRequest<BaseResponse<MessageDto>>
{
    public required string SenderId { get; init; }
    public Guid GroupId { get; init; }
    public required string Ciphertext { get; init; }
    public string? EphemeralKey { get; init; }
}

public class SendGroupMessageCommandHandler : IRequestHandler<SendGroupMessageCommand, BaseResponse<MessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public SendGroupMessageCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<MessageDto>> Handle(SendGroupMessageCommand request, CancellationToken cancellationToken)
    {
        var sender = await _context.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
        if (sender == null)
        {
            return BaseResponse<MessageDto>.Failure("Sender user not found.");
        }

        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<MessageDto>.Failure("Group not found.");
        }

        // Verify sender is a member of the group
        var isMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == request.GroupId && gm.UserId == request.SenderId, cancellationToken);

        if (!isMember)
        {
            return BaseResponse<MessageDto>.Failure("You are not a member of this group.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = sender.Id,
            Sender = sender,
            GroupId = group.Id,
            Group = group,
            Ciphertext = request.Ciphertext,
            EphemeralKey = request.EphemeralKey,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderUsername = sender.UserName!,
            ReceiverId = null,
            ReceiverUsername = null,
            GroupId = group.Id,
            Ciphertext = message.Ciphertext,
            EphemeralKey = message.EphemeralKey,
            SignedPrekeyIdUsed = null,
            OneTimePrekeyIdUsed = null,
            SentAt = message.SentAt,
            DeliveredAt = message.DeliveredAt,
            ReadAt = message.ReadAt,
            BurnAfterSeconds = message.BurnAfterSeconds,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt
        };

        // Notify group members in real-time
        await _notificationService.NotifyNewGroupMessageAsync(group.Id, dto, cancellationToken);

        return BaseResponse<MessageDto>.Success(dto, "Group message sent successfully.");
    }
}
