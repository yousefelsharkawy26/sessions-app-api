namespace SessionApp.Application.Common.DTOs;

public record UserPresenceDto
{
    public required string Username { get; init; }
    public required bool IsOnline { get; init; }
}
