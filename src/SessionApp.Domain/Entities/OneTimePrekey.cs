using System;

namespace SessionApp.Domain.Entities;

public class OneTimePrekey
{
    public Guid Id { get; set; }
    
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public required string DeviceId { get; set; } // Associated device ID (e.g. "primary")
    
    public int KeyId { get; set; }
    public required string KeyData { get; set; }          // Base64 public key
}
