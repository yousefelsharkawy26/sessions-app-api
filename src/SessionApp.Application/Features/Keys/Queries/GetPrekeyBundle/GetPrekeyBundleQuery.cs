using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Keys.Queries.GetPrekeyBundle;

public record GetPrekeyBundleQuery : IRequest<BaseResponse<PrekeyBundleDto>>
{
    public required string TargetUsername { get; init; }
    public required string RequesterId { get; init; }
}

public class GetPrekeyBundleQueryHandler : IRequestHandler<GetPrekeyBundleQuery, BaseResponse<PrekeyBundleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPrekeyBundleQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<PrekeyBundleDto>> Handle(GetPrekeyBundleQuery request, CancellationToken cancellationToken)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.TargetUsername, cancellationToken);
        if (targetUser == null)
        {
            return BaseResponse<PrekeyBundleDto>.Failure("User not found.");
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

        var bundle = await _context.PrekeyBundles
            .FirstOrDefaultAsync(pb => pb.UserId == targetUser.Id, cancellationToken);

        if (bundle == null)
        {
            return BaseResponse<PrekeyBundleDto>.Failure("Prekey bundle not found for this user.");
        }

        // Vend one One-Time Prekey (OTP) and delete it from the database
        OneTimePrekeyDto? otpDto = null;
        var otp = await _context.OneTimePrekeys
            .Where(o => o.UserId == targetUser.Id)
            .OrderBy(o => o.KeyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp != null)
        {
            otpDto = new OneTimePrekeyDto
            {
                KeyId = otp.KeyId,
                KeyData = otp.KeyData
            };
            _context.OneTimePrekeys.Remove(otp);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var dto = new PrekeyBundleDto
        {
            IdentityKey = bundle.IdentityKey,
            SignedPrekey = bundle.SignedPrekey,
            Signature = bundle.Signature,
            SignedPrekeyId = bundle.SignedPrekeyId,
            OneTimePrekey = otpDto
        };

        return BaseResponse<PrekeyBundleDto>.Success(dto, "Prekey bundle retrieved successfully.");
    }
}
