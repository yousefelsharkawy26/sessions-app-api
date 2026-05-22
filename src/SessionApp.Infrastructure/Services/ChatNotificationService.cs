using Microsoft.AspNetCore.SignalR;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Infrastructure.SignalR;

namespace SessionApp.Infrastructure.Services;

public class ChatNotificationService(IHubContext<ChatHub> _hubContext) : IChatNotificationService
{
    public async Task NotifyNewMessageAsync(string receiverUsername, MessageDto messageDto, CancellationToken cancellationToken)
    {
        // Push the message directly to the recipient using their username as the SignalR user identifier
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("ReceiveMessage", messageDto, cancellationToken);
    }

    public async Task NotifyMessagesReadAsync(string receiverUsername, string readerUsername, List<Guid> messageIds, CancellationToken cancellationToken)
    {
        // Push the read receipt directly to the original sender (receiver of this notification)
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("MessagesRead", new 
            { 
                ReaderUsername = readerUsername, 
                MessageIds = messageIds 
            }, cancellationToken);
    }

    public async Task NotifyMessageDeletedAsync(string targetUsername, Guid messageId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(targetUsername)
            .SendAsync("MessageDeleted", new 
            { 
                MessageId = messageId 
            }, cancellationToken);
    }
}
