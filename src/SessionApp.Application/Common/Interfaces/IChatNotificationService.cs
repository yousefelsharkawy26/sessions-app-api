using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SessionApp.Application.Common.DTOs;

namespace SessionApp.Application.Common.Interfaces;

public interface IChatNotificationService
{
    Task NotifyNewMessageAsync(string receiverUsername, MessageDto messageDto, CancellationToken cancellationToken);
    Task NotifyMessagesReadAsync(string receiverUsername, string readerUsername, List<Guid> messageIds, CancellationToken cancellationToken);
    Task NotifyMessageDeletedAsync(string targetUsername, Guid messageId, CancellationToken cancellationToken);
}
