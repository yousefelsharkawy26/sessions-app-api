using Microsoft.AspNetCore.Identity;

namespace SessionApp.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public required string DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsPrivate { get; set; } = false;
    public string? Metadata { get; set; }
    
    // Recovery Phrase Hash for "Session-style" password recovery
    public string? RecoveryPhraseHash { get; set; }

    // Last seen timestamp for automatic account self-destruct / inactivity tracking
    public DateTime? LastSeenAt { get; set; }

    // Recovery lockout properties to protect against mnemonic brute-forcing
    public int FailedRecoveryAttempts { get; set; }
    public DateTime? RecoveryLockoutEnd { get; set; }
}
