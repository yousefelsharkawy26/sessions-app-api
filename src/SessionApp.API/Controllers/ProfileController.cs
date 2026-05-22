using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Profiles.Commands.UpdateProfile;
using SessionApp.Application.Features.Profiles.Queries.GetPresence;
using SessionApp.Application.Features.Profiles.Queries.GetUserProfile;
using SessionApp.Application.Features.Profiles.Queries.SearchUser;

namespace SessionApp.API.Controllers;

[Authorize]
public class ProfileController : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<BaseResponse<UserProfileDto>>> GetMyProfile()
    {
        var result = await Mediator.Send(new GetUserProfileQuery
        {
            TargetUsername = CurrentUsername!,
            RequesterId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<BaseResponse<UserProfileDto>>> GetUserProfile(string username)
    {
        var result = await Mediator.Send(new GetUserProfileQuery
        {
            TargetUsername = username,
            RequesterId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<BaseResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (request.LastSeenAt.HasValue)
        {
            HttpContext.Items["BypassUserActivity"] = true;
        }

        var result = await Mediator.Send(new UpdateProfileCommand
        {
            UserId = CurrentUserId!,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            IsPrivate = request.IsPrivate,
            Metadata = request.Metadata,
            ProfilePictureBase64 = request.ProfilePictureBase64,
            ProfilePictureFileName = request.ProfilePictureFileName,
            LastSeenAt = request.LastSeenAt
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<BaseResponse<List<UserProfileDto>>>> SearchUser([FromQuery] string searchTerm)
    {
        var result = await Mediator.Send(new SearchUserQuery 
        { 
            SearchTerm = searchTerm,
            RequesterId = CurrentUserId!
        });
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("presence")]
    public async Task<ActionResult<BaseResponse<List<UserPresenceDto>>>> GetUsersPresence([FromBody] List<string> usernames)
    {
        var result = await Mediator.Send(new GetPresenceQuery { Usernames = usernames });
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public record UpdateProfileRequest
{
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public bool? IsPrivate { get; init; }
    public string? Metadata { get; init; }
    public string? ProfilePictureBase64 { get; init; }
    public string? ProfilePictureFileName { get; init; }
    public DateTime? LastSeenAt { get; init; }
}
