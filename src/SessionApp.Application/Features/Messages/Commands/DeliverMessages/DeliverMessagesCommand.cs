using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Commands.DeliverMessages;

public record DeliverMessagesCommand : IRequest<BaseResponse<bool>>
{
    public required string UserId { get; init; }
    public required List<Guid> MessageIds { get; init; }
}

public class DeliverMessagesCommandHandler : IRequestHandler<DeliverMessagesCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public DeliverMessagesCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(DeliverMessagesCommand request, CancellationToken cancellationToken)
    {
        var receiver = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (receiver == null)
        {
            return BaseResponse<bool>.Failure("Receiver user not found.");
        }

        var messagesToDeliver = await _context.Messages
            .Where(m => request.MessageIds.Contains(m.Id) && m.ReceiverId == receiver.Id && m.DeliveredAt == null)
            .Include(m => m.Sender)
            .ToListAsync(cancellationToken);

        if (!messagesToDeliver.Any())
        {
            return BaseResponse<bool>.Success(true, "No messages needed delivery updates.");
        }

        var now = DateTime.UtcNow;
        foreach (var msg in messagesToDeliver)
        {
            msg.DeliveredAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Group messages by sender and notify them
        var groups = messagesToDeliver
            .GroupBy(m => m.Sender!.UserName!)
            .Select(g => new { SenderUsername = g.Key, MessageIds = g.Select(m => m.Id).ToList() });

        foreach (var group in groups)
        {
            await _notificationService.NotifyMessagesDeliveredAsync(
                group.SenderUsername,
                receiver.UserName!,
                group.MessageIds,
                cancellationToken);
        }

        return BaseResponse<bool>.Success(true, "Messages marked as delivered successfully.");
    }
}
