using System;

namespace SessionApp.Domain.Entities;

public class UserDevice
{
    public Guid Id { get; set; }
    
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public required string DeviceId { get; set; } // Unique client-generated device ID (e.g. "primary", "desktop-1")
    public required string DeviceName { get; set; }
    
    public required string IdentityKey { get; set; }      // Base64 public key
    public required string SignedPrekey { get; set; }      // Base64 public key
    public required string Signature { get; set; }         // Base64 signature
    public int SignedPrekeyId { get; set; }
    
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
