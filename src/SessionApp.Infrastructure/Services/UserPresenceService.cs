using SessionApp.Application.Common.Interfaces;
using SessionApp.Infrastructure.SignalR;

namespace SessionApp.Infrastructure.Services;

public class UserPresenceService : IUserPresenceService
{
    public bool IsUserOnline(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return false;
        }
        return ChatHub.IsUserOnline(username);
    }
}
