using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using System.Collections.Concurrent;

namespace SessionApp.Infrastructure.SignalR;

[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _context;
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

    public ChatHub(IApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var username = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            OnlineUsers[username] = Context.ConnectionId;
            await Clients.Others.SendAsync("UserStatusChanged", username, true);

            // Fetch groups this user is a member of and add to SignalR groups
            var userGroups = await _context.GroupMembers
                .Where(gm => gm.User!.UserName == username)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            foreach (var groupId in userGroups)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
            }

            // Auto-deliver all undelivered direct messages to this user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user != null)
            {
                var undeliveredMessages = await _context.Messages
                    .Where(m => m.ReceiverId == user.Id && m.DeliveredAt == null)
                    .Include(m => m.Sender)
                    .ToListAsync();

                if (undeliveredMessages.Any())
                {
                    var now = DateTime.UtcNow;
                    foreach (var msg in undeliveredMessages)
                    {
                        msg.DeliveredAt = now;
                    }
                    await _context.SaveChangesAsync(default);

                    // Group by sender username and notify them that their messages were delivered
                    var groupsBySender = undeliveredMessages
                        .GroupBy(m => m.Sender!.UserName!)
                        .Select(g => new { SenderUsername = g.Key, MessageIds = g.Select(m => m.Id).ToList() });

                    foreach (var groupInfo in groupsBySender)
                    {
                        await Clients.User(groupInfo.SenderUsername).SendAsync("MessagesDelivered", new
                        {
                            DelivererUsername = username,
                            MessageIds = groupInfo.MessageIds
                        });
                    }
                }
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            OnlineUsers.TryRemove(username, out _);
            await Clients.Others.SendAsync("UserStatusChanged", username, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public static bool IsUserOnline(string username)
    {
        return OnlineUsers.ContainsKey(username);
    }

    public static string? GetConnectionId(string username)
    {
        return OnlineUsers.TryGetValue(username, out var connectionId) ? connectionId : null;
    }
}
