namespace SessionApp.Application.Common.DTOs;

public class AuthDto
{
    public required string Token { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public string? RecoveryMnemonic { get; set; }
}
