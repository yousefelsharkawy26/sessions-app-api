using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;

namespace SessionApp.Application.Features.Keys.Commands.UploadKeys;

public record UploadKeysCommand : IRequest<BaseResponse<bool>>
{
    public required string UserId { get; init; }
    public required string IdentityKey { get; init; }
    public required string SignedPrekey { get; init; }
    public required string Signature { get; init; }
    public int SignedPrekeyId { get; init; }
    public required List<OneTimePrekeyDto> OneTimePrekeys { get; init; }
}

public class UploadKeysCommandHandler : IRequestHandler<UploadKeysCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public UploadKeysCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UploadKeysCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null)
        {
            return BaseResponse<bool>.Failure("User not found.");
        }

        // 1. Create or Update PrekeyBundle
        var bundle = await _context.PrekeyBundles
            .FirstOrDefaultAsync(pb => pb.UserId == request.UserId, cancellationToken);

        if (bundle == null)
        {
            bundle = new PrekeyBundle
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                IdentityKey = request.IdentityKey,
                SignedPrekey = request.SignedPrekey,
                Signature = request.Signature,
                SignedPrekeyId = request.SignedPrekeyId,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PrekeyBundles.Add(bundle);
        }
        else
        {
            bundle.IdentityKey = request.IdentityKey;
            bundle.SignedPrekey = request.SignedPrekey;
            bundle.Signature = request.Signature;
            bundle.SignedPrekeyId = request.SignedPrekeyId;
            bundle.UpdatedAt = DateTime.UtcNow;
        }

        // 2. Add new One-Time Prekeys (skip duplicates)
        foreach (var otpDto in request.OneTimePrekeys)
        {
            var exists = await _context.OneTimePrekeys
                .AnyAsync(otp => otp.UserId == request.UserId && otp.KeyId == otpDto.KeyId, cancellationToken);

            if (!exists)
            {
                var otp = new OneTimePrekey
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    KeyId = otpDto.KeyId,
                    KeyData = otpDto.KeyData
                };
                _context.OneTimePrekeys.Add(otp);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, "Prekeys uploaded and updated successfully.");
    }
}
