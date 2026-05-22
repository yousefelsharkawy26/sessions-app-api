using System;

namespace SessionApp.Domain.Entities;

public class OneTimePrekey
{
    public Guid Id { get; set; }
    
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public int KeyId { get; set; }
    public required string KeyData { get; set; }          // Base64 public key
}
