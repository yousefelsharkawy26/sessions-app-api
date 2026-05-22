using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Commands.DeleteMessage;

public record DeleteMessageCommand : IRequest<BaseResponse<bool>>
{
    public Guid MessageId { get; init; }
    public required string UserId { get; init; }
}

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public DeleteMessageCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
        {
            return BaseResponse<bool>.Failure("Message not found.");
        }

        if (message.GroupId.HasValue)
        {
            // Group message deletion permissions: sender or group Admin/Owner
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == message.GroupId.Value && gm.UserId == request.UserId, cancellationToken);

            if (membership == null)
            {
                return BaseResponse<bool>.Failure("You are not a member of this group.");
            }

            var isSender = message.SenderId == request.UserId;
            var isAdminOrOwner = membership.Role == SessionApp.Domain.Enums.GroupRole.Admin || membership.Role == SessionApp.Domain.Enums.GroupRole.Owner;

            if (!isSender && !isAdminOrOwner)
            {
                return BaseResponse<bool>.Failure("You do not have permission to delete this message.");
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync(cancellationToken);

            await _notificationService.NotifyGroupMessageDeletedAsync(message.GroupId.Value, message.Id, cancellationToken);
        }
        else
        {
            // Direct message deletion permissions: sender or receiver
            if (message.SenderId != request.UserId && message.ReceiverId != request.UserId)
            {
                return BaseResponse<bool>.Failure("You do not have permission to delete this message.");
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync(cancellationToken);

            string otherPartyUsername = request.UserId == message.SenderId 
                ? message.Receiver!.UserName! 
                : message.Sender!.UserName!;

            await _notificationService.NotifyMessageDeletedAsync(otherPartyUsername, message.Id, cancellationToken);
        }

        return BaseResponse<bool>.Success(true, "Message deleted successfully.");
    }
}
