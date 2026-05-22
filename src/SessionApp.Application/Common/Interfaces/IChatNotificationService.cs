using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SessionApp.Application.Common.DTOs;

namespace SessionApp.Application.Common.Interfaces;

public interface IChatNotificationService
{
    Task NotifyNewMessageAsync(string receiverUsername, MessageDto messageDto, CancellationToken cancellationToken);
    Task NotifyMessagesDeliveredAsync(string receiverUsername, string delivererUsername, List<Guid> messageIds, CancellationToken cancellationToken);
    Task NotifyMessagesReadAsync(string receiverUsername, string readerUsername, List<Guid> messageIds, CancellationToken cancellationToken);
    Task NotifyMessageDeletedAsync(string targetUsername, Guid messageId, CancellationToken cancellationToken);
    Task NotifyMessageEditedAsync(string targetUsername, MessageDto messageDto, CancellationToken cancellationToken);
    Task JoinGroupRoomAsync(string username, Guid groupId, CancellationToken cancellationToken);
    Task NotifyNewGroupMessageAsync(Guid groupId, MessageDto messageDto, CancellationToken cancellationToken);
}
