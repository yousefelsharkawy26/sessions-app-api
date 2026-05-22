namespace SessionApp.Application.Common.DTOs;

public class PrekeyBundleDto
{
    public required string IdentityKey { get; set; }
    public required string SignedPrekey { get; set; }
    public required string Signature { get; set; }
    public int SignedPrekeyId { get; set; }
    public OneTimePrekeyDto? OneTimePrekey { get; set; }
}
