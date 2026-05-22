using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SessionApp.Application.Features.Keys.Queries.GetPrekeyBundle;

public record GetPrekeyBundleQuery : IRequest<BaseResponse<PrekeyBundleDto>>
{
    public required string TargetUsername { get; init; }
    public required string RequesterId { get; init; }
}

public class GetPrekeyBundleQueryHandler : IRequestHandler<GetPrekeyBundleQuery, BaseResponse<PrekeyBundleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public GetPrekeyBundleQueryHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<PrekeyBundleDto>> Handle(GetPrekeyBundleQuery request, CancellationToken cancellationToken)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.TargetUsername, cancellationToken);
        if (targetUser == null)
        {
            return BaseResponse<PrekeyBundleDto>.Failure("User not found.");
        }

        // Blocklist Validation
        var isBlocked = await _context.BlockedUsers
            .AnyAsync(bu => (bu.BlockerId == targetUser.Id && bu.BlockedId == request.RequesterId) || 
                            (bu.BlockerId == request.RequesterId && bu.BlockedId == targetUser.Id), cancellationToken);

        if (isBlocked)
        {
            return BaseResponse<PrekeyBundleDto>.Failure("You cannot view prekeys for this user because one of you has blocked the other.");
        }

        // Privacy Validation
        if (targetUser.IsPrivate && targetUser.Id != request.RequesterId)
        {
            // If the target user is private, only allow fetching prekey bundle if they have previously messaged the requester
            var hasTargetUserInitiated = await _context.Messages
                .AnyAsync(m => m.SenderId == targetUser.Id && m.ReceiverId == request.RequesterId, cancellationToken);

            if (!hasTargetUserInitiated)
            {
                return BaseResponse<PrekeyBundleDto>.Failure("You cannot view prekeys for this private user unless they message you first.");
            }
        }

        var devices = await _context.UserDevices
            .Where(ud => ud.UserId == targetUser.Id)
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
        {
            // If UserDevices table is empty (e.g. legacy data without devices), fall back to checking PrekeyBundles table
            var legacyBundle = await _context.PrekeyBundles
                .FirstOrDefaultAsync(pb => pb.UserId == targetUser.Id, cancellationToken);

            if (legacyBundle == null)
            {
                return BaseResponse<PrekeyBundleDto>.Failure("Prekey bundle not found for this user.");
            }

            // Create temporary device entry for downstream compatibility
            devices.Add(new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = targetUser.Id,
                DeviceId = "primary",
                DeviceName = "Primary Device",
                IdentityKey = legacyBundle.IdentityKey,
                SignedPrekey = legacyBundle.SignedPrekey,
                Signature = legacyBundle.Signature,
                SignedPrekeyId = legacyBundle.SignedPrekeyId
            });
        }

        var deviceDtos = new List<DevicePrekeyBundleDto>();
        bool databaseModified = false;

        foreach (var device in devices)
        {
            // Vend one One-Time Prekey (OTP) for this specific device and delete it from the DB
            OneTimePrekeyDto? deviceOtpDto = null;
            var otp = await _context.OneTimePrekeys
                .Where(o => o.UserId == targetUser.Id && o.DeviceId == device.DeviceId)
                .OrderBy(o => o.KeyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp != null)
            {
                deviceOtpDto = new OneTimePrekeyDto
                {
                    KeyId = otp.KeyId,
                    KeyData = otp.KeyData
                };
                _context.OneTimePrekeys.Remove(otp);
                databaseModified = true;
            }

            deviceDtos.Add(new DevicePrekeyBundleDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                IdentityKey = device.IdentityKey,
                SignedPrekey = device.SignedPrekey,
                Signature = device.Signature,
                SignedPrekeyId = device.SignedPrekeyId,
                OneTimePrekey = deviceOtpDto
            });
        }

        if (databaseModified)
        {
            await _context.SaveChangesAsync(cancellationToken);

            // Proactively check remaining OTP count for warning trigger
            foreach (var device in devices)
            {
                var remainingCount = await _context.OneTimePrekeys
                    .CountAsync(o => o.UserId == targetUser.Id && o.DeviceId == device.DeviceId, cancellationToken);

                if (remainingCount < 5)
                {
                    await _notificationService.NotifyPrekeyThresholdReachedAsync(targetUser.UserName!, device.DeviceId, remainingCount, cancellationToken);
                }
            }
        }

        // Populate backward-compatible primary bundle properties from "primary" device or first device
        var primaryDto = deviceDtos.FirstOrDefault(d => d.DeviceId == "primary") ?? deviceDtos.First();

        var dto = new PrekeyBundleDto
        {
            IdentityKey = primaryDto.IdentityKey,
            SignedPrekey = primaryDto.SignedPrekey,
            Signature = primaryDto.Signature,
            SignedPrekeyId = primaryDto.SignedPrekeyId,
            OneTimePrekey = primaryDto.OneTimePrekey,
            Devices = deviceDtos
        };

        return BaseResponse<PrekeyBundleDto>.Success(dto, "Prekey bundle retrieved successfully.");
    }
}
