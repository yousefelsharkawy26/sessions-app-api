using System;

namespace SessionApp.Domain.Entities;

public class PrekeyBundle
{
    public Guid Id { get; set; }
    
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public required string IdentityKey { get; set; }      // Base64 public key
    public required string SignedPrekey { get; set; }      // Base64 public key
    public required string Signature { get; set; }         // Base64 signature
    public int SignedPrekeyId { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
