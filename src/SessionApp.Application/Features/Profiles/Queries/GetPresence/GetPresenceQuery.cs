using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;

namespace SessionApp.Application.Features.Profiles.Queries.GetPresence;

public record GetPresenceQuery : IRequest<BaseResponse<List<UserPresenceDto>>>
{
    public required List<string> Usernames { get; init; }
}

public class GetPresenceQueryHandler : IRequestHandler<GetPresenceQuery, BaseResponse<List<UserPresenceDto>>>
{
    private readonly IUserPresenceService _presenceService;

    public GetPresenceQueryHandler(IUserPresenceService presenceService)
    {
        _presenceService = presenceService;
    }

    public async Task<BaseResponse<List<UserPresenceDto>>> Handle(GetPresenceQuery request, CancellationToken cancellationToken)
    {
        if (request.Usernames == null || !request.Usernames.Any())
        {
            return BaseResponse<List<UserPresenceDto>>.Success(new List<UserPresenceDto>());
        }

        var results = request.Usernames
            .Select(username => new UserPresenceDto
            {
                Username = username,
                IsOnline = _presenceService.IsUserOnline(username)
            })
            .ToList();

        return await Task.FromResult(BaseResponse<List<UserPresenceDto>>.Success(results));
    }
}
