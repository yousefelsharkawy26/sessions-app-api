using Microsoft.AspNetCore.SignalR;

namespace SessionApp.Infrastructure.SignalR;

public class UsernameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.Identity?.Name;
    }
}
