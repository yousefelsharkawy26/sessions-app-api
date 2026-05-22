using SessionApp.Application.Common.DTOs;

namespace SessionApp.Application.Common.Interfaces;

public interface IChatNotificationService
{
    Task NotifyNewMessageAsync(string receiverUsername, MessageDto messageDto, CancellationToken cancellationToken);
}
