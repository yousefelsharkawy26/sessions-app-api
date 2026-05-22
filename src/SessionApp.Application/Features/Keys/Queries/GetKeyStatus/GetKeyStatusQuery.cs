using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Keys.Queries.GetKeyStatus;

public record GetKeyStatusQuery : IRequest<BaseResponse<KeyStatusDto>>
{
    public required string UserId { get; init; }
}

public class GetKeyStatusQueryHandler : IRequestHandler<GetKeyStatusQuery, BaseResponse<KeyStatusDto>>
{
    private readonly IApplicationDbContext _context;

    public GetKeyStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<KeyStatusDto>> Handle(GetKeyStatusQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _context.PrekeyBundles
            .FirstOrDefaultAsync(b => b.UserId == request.UserId, cancellationToken);

        if (bundle == null)
        {
            return BaseResponse<KeyStatusDto>.Success(new KeyStatusDto
            {
                IsUploaded = false,
                RemainingOneTimePrekeysCount = 0,
                SignedPrekeyId = null,
                LastUpdatedAt = null
            }, "No prekey bundle uploaded yet.");
        }

        var otpCount = await _context.OneTimePrekeys
            .CountAsync(o => o.UserId == request.UserId, cancellationToken);

        return BaseResponse<KeyStatusDto>.Success(new KeyStatusDto
        {
            IsUploaded = true,
            RemainingOneTimePrekeysCount = otpCount,
            SignedPrekeyId = bundle.SignedPrekeyId,
            LastUpdatedAt = bundle.UpdatedAt
        }, "Key status retrieved successfully.");
    }
}

public record KeyStatusDto
{
    public bool IsUploaded { get; init; }
    public int RemainingOneTimePrekeysCount { get; init; }
    public int? SignedPrekeyId { get; init; }
    public DateTime? LastUpdatedAt { get; init; }
}
