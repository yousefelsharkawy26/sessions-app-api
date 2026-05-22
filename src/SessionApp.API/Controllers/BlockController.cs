using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Blocks.Commands.BlockUser;
using SessionApp.Application.Features.Blocks.Commands.UnblockUser;
using SessionApp.Application.Features.Blocks.Queries.GetBlockedUsers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SessionApp.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class BlockController : ApiControllerBase
{
    [HttpPost("{username}")]
    public async Task<ActionResult<BaseResponse<bool>>> BlockUser(string username)
    {
        var result = await Mediator.Send(new BlockUserCommand
        {
            BlockerUserId = CurrentUserId!,
            BlockedUsername = username
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{username}")]
    public async Task<ActionResult<BaseResponse<bool>>> UnblockUser(string username)
    {
        var result = await Mediator.Send(new UnblockUserCommand
        {
            BlockerUserId = CurrentUserId!,
            BlockedUsername = username
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<UserProfileDto>>>> GetBlockedUsers()
    {
        var result = await Mediator.Send(new GetBlockedUsersQuery
        {
            UserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
