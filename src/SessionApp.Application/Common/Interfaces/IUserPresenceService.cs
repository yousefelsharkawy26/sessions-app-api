namespace SessionApp.Application.Common.Interfaces;

public interface IUserPresenceService
{
    bool IsUserOnline(string username);
}
