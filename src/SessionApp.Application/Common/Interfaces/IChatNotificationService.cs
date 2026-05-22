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
    Task NotifyGroupMessageDeletedAsync(Guid groupId, Guid messageId, CancellationToken cancellationToken);
    Task NotifyMessageEditedAsync(string targetUsername, MessageDto messageDto, CancellationToken cancellationToken);
    Task JoinGroupRoomAsync(string username, Guid groupId, CancellationToken cancellationToken);
    Task NotifyNewGroupMessageAsync(Guid groupId, MessageDto messageDto, CancellationToken cancellationToken);
    Task NotifyCallInitiatedAsync(string receiverUsername, string callerUsername, string sdpOffer, CancellationToken cancellationToken);
    Task NotifyCallAcceptedAsync(string receiverUsername, string calleeUsername, string sdpAnswer, CancellationToken cancellationToken);
    Task NotifyIceCandidateSentAsync(string receiverUsername, string senderUsername, string candidate, CancellationToken cancellationToken);
    Task NotifyCallDeclinedAsync(string receiverUsername, string declinerUsername, CancellationToken cancellationToken);
    Task NotifyCallHungUpAsync(string receiverUsername, string hangerUpUsername, CancellationToken cancellationToken);
    Task NotifyMessageReactionUpdatedAsync(string targetUsername, Guid messageId, string reactingUsername, string? reactionCiphertext, CancellationToken cancellationToken);
    Task NotifyGroupMessageReactionUpdatedAsync(Guid groupId, Guid messageId, string reactingUsername, string? reactionCiphertext, CancellationToken cancellationToken);
    Task NotifyPrekeyThresholdReachedAsync(string username, string deviceId, int remainingCount, CancellationToken cancellationToken);
}
