using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Profiles.Queries.SearchUser;

public record SearchUserQuery : IRequest<BaseResponse<List<UserProfileDto>>>
{
    public required string SearchTerm { get; init; }
    public string? RequesterId { get; init; }
}

public class SearchUserQueryHandler : IRequestHandler<SearchUserQuery, BaseResponse<List<UserProfileDto>>>
{
    private readonly IApplicationDbContext _context;

    public SearchUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<UserProfileDto>>> Handle(SearchUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return BaseResponse<List<UserProfileDto>>.Success(new List<UserProfileDto>());
        }

        var searchTermLower = request.SearchTerm.ToLower();

        List<string> blockedUserIds = new();
        if (!string.IsNullOrEmpty(request.RequesterId))
        {
            blockedUserIds = await _context.BlockedUsers
                .Where(bu => bu.BlockerId == request.RequesterId || bu.BlockedId == request.RequesterId)
                .Select(bu => bu.BlockerId == request.RequesterId ? bu.BlockedId : bu.BlockerId)
                .ToListAsync(cancellationToken);
        }

        var users = await _context.Users
            .Where(u => !u.IsPrivate && 
                        !blockedUserIds.Contains(u.Id) &&
                        (u.UserName!.ToLower().Contains(searchTermLower) || 
                         u.DisplayName.ToLower().Contains(searchTermLower)))
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.UserName!,
                DisplayName = u.DisplayName,
                Bio = u.Bio,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsPrivate = u.IsPrivate,
                Metadata = u.Metadata
            })
            .ToListAsync(cancellationToken);

        return BaseResponse<List<UserProfileDto>>.Success(users);
    }
}
