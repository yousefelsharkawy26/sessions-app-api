using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Infrastructure.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Infrastructure.Services;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;

    public ChatNotificationService(IHubContext<ChatHub> hubContext, IServiceProvider serviceProvider)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
    }

    public async Task NotifyNewMessageAsync(string receiverUsername, MessageDto messageDto, CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var receiver = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == receiverUsername, cancellationToken);
            if (receiver != null)
            {
                var mute = await dbContext.DirectChatMutes
                    .FirstOrDefaultAsync(m => m.MuterId == receiver.Id && m.MutedUserId == messageDto.SenderId, cancellationToken);

                if (mute != null && (mute.MutedUntil == null || mute.MutedUntil > DateTime.UtcNow))
                {
                    messageDto.IsAlertSilenced = true;
                }
            }
        }

        // Push the message directly to the recipient using their username as the SignalR user identifier
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("ReceiveMessage", messageDto, cancellationToken);
    }

    public async Task NotifyMessagesDeliveredAsync(string receiverUsername, string delivererUsername, List<Guid> messageIds, CancellationToken cancellationToken)
    {
        // Push the delivery receipt directly to the original sender (receiver of this notification)
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("MessagesDelivered", new
            {
                DelivererUsername = delivererUsername,
                MessageIds = messageIds
            }, cancellationToken);
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

    public async Task NotifyGroupMessageDeletedAsync(Guid groupId, Guid messageId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"Group_{groupId}")
            .SendAsync("GroupMessageDeleted", new 
            { 
                GroupId = groupId,
                MessageId = messageId 
            }, cancellationToken);
    }

    public async Task NotifyMessageEditedAsync(string targetUsername, MessageDto messageDto, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(targetUsername)
            .SendAsync("MessageEdited", messageDto, cancellationToken);
    }

    public async Task JoinGroupRoomAsync(string username, Guid groupId, CancellationToken cancellationToken)
    {
        var connectionId = ChatHub.GetConnectionId(username);
        if (!string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"Group_{groupId}", cancellationToken);
        }
    }

    public async Task NotifyNewGroupMessageAsync(Guid groupId, MessageDto messageDto, CancellationToken cancellationToken)
    {
        List<string> mutedUsernames = new();
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            mutedUsernames = await dbContext.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.MutedUntil != null && gm.MutedUntil > DateTime.UtcNow)
                .Select(gm => gm.User!.UserName!)
                .ToListAsync(cancellationToken);
        }

        await _hubContext.Clients.Group($"Group_{groupId}")
            .SendAsync("ReceiveGroupMessage", new 
            {
                Message = messageDto,
                MutedUsernames = mutedUsernames
            }, cancellationToken);
    }

    public async Task NotifyCallInitiatedAsync(string receiverUsername, string callerUsername, string sdpOffer, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("CallInitiated", new { CallerUsername = callerUsername, SdpOffer = sdpOffer }, cancellationToken);
    }

    public async Task NotifyCallAcceptedAsync(string receiverUsername, string calleeUsername, string sdpAnswer, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("CallAccepted", new { CalleeUsername = calleeUsername, SdpAnswer = sdpAnswer }, cancellationToken);
    }

    public async Task NotifyIceCandidateSentAsync(string receiverUsername, string senderUsername, string candidate, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("IceCandidateReceived", new { SenderUsername = senderUsername, Candidate = candidate }, cancellationToken);
    }

    public async Task NotifyCallDeclinedAsync(string receiverUsername, string declinerUsername, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("CallDeclined", new { DeclinerUsername = declinerUsername }, cancellationToken);
    }

    public async Task NotifyCallHungUpAsync(string receiverUsername, string hangerUpUsername, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(receiverUsername)
            .SendAsync("CallHungUp", new { HangerUpUsername = hangerUpUsername }, cancellationToken);
    }

    public async Task NotifyMessageReactionUpdatedAsync(string targetUsername, Guid messageId, string reactingUsername, string? reactionCiphertext, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(targetUsername)
            .SendAsync("MessageReactionUpdated", new
            {
                MessageId = messageId,
                ReactingUsername = reactingUsername,
                ReactionCiphertext = reactionCiphertext
            }, cancellationToken);
    }

    public async Task NotifyGroupMessageReactionUpdatedAsync(Guid groupId, Guid messageId, string reactingUsername, string? reactionCiphertext, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"Group_{groupId}")
            .SendAsync("GroupMessageReactionUpdated", new
            {
                MessageId = messageId,
                ReactingUsername = reactingUsername,
                ReactionCiphertext = reactionCiphertext
            }, cancellationToken);
    }

    public async Task NotifyPrekeyThresholdReachedAsync(string username, string deviceId, int remainingCount, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.User(username)
            .SendAsync("PrekeysRunningLow", new
            {
                DeviceId = deviceId,
                RemainingCount = remainingCount,
                Warning = "One-Time Prekeys pool is running low. Please upload more prekeys to avoid E2EE channel setup failures."
            }, cancellationToken);
    }
}
