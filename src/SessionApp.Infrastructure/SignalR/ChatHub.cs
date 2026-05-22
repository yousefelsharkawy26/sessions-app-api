using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SessionApp.Infrastructure.SignalR;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var username = Context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            OnlineUsers[username] = Context.ConnectionId;
            await Clients.Others.SendAsync("UserStatusChanged", username, true);
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
}
