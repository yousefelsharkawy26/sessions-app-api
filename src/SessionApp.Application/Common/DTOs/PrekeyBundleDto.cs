using System.Collections.Generic;

namespace SessionApp.Application.Common.DTOs;

public class PrekeyBundleDto
{
    public required string IdentityKey { get; set; }
    public required string SignedPrekey { get; set; }
    public required string Signature { get; set; }
    public int SignedPrekeyId { get; set; }
    public OneTimePrekeyDto? OneTimePrekey { get; set; }

    public List<DevicePrekeyBundleDto> Devices { get; set; } = new();
}

public class DevicePrekeyBundleDto
{
    public required string DeviceId { get; set; }
    public required string DeviceName { get; set; }
    public required string IdentityKey { get; set; }
    public required string SignedPrekey { get; set; }
    public required string Signature { get; set; }
    public int SignedPrekeyId { get; set; }
    public OneTimePrekeyDto? OneTimePrekey { get; set; }
}
