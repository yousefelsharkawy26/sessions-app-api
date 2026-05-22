namespace SessionApp.Application.Common.DTOs;

public class UserProfileDto
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsPrivate { get; set; }
    public string? Metadata { get; set; }
}
