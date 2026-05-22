using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Conversations.Commands.MuteConversation;
using SessionApp.Application.Features.Conversations.Commands.UnmuteConversation;
using System;
using System.Threading.Tasks;

namespace SessionApp.API.Controllers;

[Authorize]
public class ConversationController : ApiControllerBase
{
    [HttpPost("mute")]
    public async Task<ActionResult<BaseResponse<bool>>> MuteConversation([FromBody] MuteRequest request)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return Unauthorized();
        }

        var command = new MuteConversationCommand
        {
            GroupId = request.GroupId,
            MutedUsername = request.MutedUsername,
            DurationMinutes = request.DurationMinutes,
            RequestingUserId = CurrentUserId
        };

        var result = await Mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("unmute")]
    public async Task<ActionResult<BaseResponse<bool>>> UnmuteConversation([FromBody] UnmuteRequest request)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return Unauthorized();
        }

        var command = new UnmuteConversationCommand
        {
            GroupId = request.GroupId,
            MutedUsername = request.MutedUsername,
            RequestingUserId = CurrentUserId
        };

        var result = await Mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public class MuteRequest
{
    public Guid? GroupId { get; set; }
    public string? MutedUsername { get; set; }
    public int? DurationMinutes { get; set; }
}

public class UnmuteRequest
{
    public Guid? GroupId { get; set; }
    public string? MutedUsername { get; set; }
}
