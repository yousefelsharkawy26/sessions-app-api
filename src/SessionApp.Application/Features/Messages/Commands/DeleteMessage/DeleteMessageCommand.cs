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

        // Validate that the request sender is either the sender or receiver of the message
        if (message.SenderId != request.UserId && message.ReceiverId != request.UserId)
        {
            return BaseResponse<bool>.Failure("You do not have permission to delete this message.");
        }

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync(cancellationToken);

        // Send a notification to the other party to let them know the message has been deleted
        string otherPartyUsername = request.UserId == message.SenderId 
            ? message.Receiver!.UserName! 
            : message.Sender!.UserName!;

        await _notificationService.NotifyMessageDeletedAsync(otherPartyUsername, message.Id, cancellationToken);

        return BaseResponse<bool>.Success(true, "Message deleted successfully.");
    }
}
