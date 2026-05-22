using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Profiles.Queries.GetUserProfile;

public record GetUserProfileQuery : IRequest<BaseResponse<UserProfileDto>>
{
    public required string TargetUsername { get; init; }
    public required string RequesterId { get; init; }
}

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, BaseResponse<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.TargetUsername, cancellationToken);
        if (targetUser == null)
        {
            return BaseResponse<UserProfileDto>.Failure("User not found.");
        }

        var isOwner = targetUser.Id == request.RequesterId;

        // Privacy filter
        if (targetUser.IsPrivate && !isOwner)
        {
            // Requester is not the owner and the account is private
            // Return limited data (e.g. Bio, Picture, Metadata omitted/redacted)
            var limitedDto = new UserProfileDto
            {
                Id = targetUser.Id,
                Username = targetUser.UserName!,
                DisplayName = targetUser.DisplayName,
                Bio = "[Private Profile]",
                ProfilePictureUrl = null,
                IsPrivate = true,
                Metadata = null
            };
            return BaseResponse<UserProfileDto>.Success(limitedDto, "This profile is private. Displaying limited information.");
        }

        var dto = new UserProfileDto
        {
            Id = targetUser.Id,
            Username = targetUser.UserName!,
            DisplayName = targetUser.DisplayName,
            Bio = targetUser.Bio,
            ProfilePictureUrl = targetUser.ProfilePictureUrl,
            IsPrivate = targetUser.IsPrivate,
            Metadata = targetUser.Metadata
        };

        return BaseResponse<UserProfileDto>.Success(dto);
    }
}
