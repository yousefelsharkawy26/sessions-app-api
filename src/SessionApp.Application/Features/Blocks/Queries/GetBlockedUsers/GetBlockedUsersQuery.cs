using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Blocks.Queries.GetBlockedUsers;

public record GetBlockedUsersQuery : IRequest<BaseResponse<List<UserProfileDto>>>
{
    public required string UserId { get; init; }
}

public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, BaseResponse<List<UserProfileDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBlockedUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<UserProfileDto>>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
    {
        var blockedUsers = await _context.BlockedUsers
            .Where(bu => bu.BlockerId == request.UserId)
            .Select(bu => bu.Blocked)
            .Select(user => new UserProfileDto
            {
                Id = user.Id,
                Username = user.UserName!,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsPrivate = user.IsPrivate,
                Metadata = user.Metadata
            })
            .ToListAsync(cancellationToken);

        return BaseResponse<List<UserProfileDto>>.Success(blockedUsers, "Retrieved blocked users successfully.");
    }
}
