using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Keys.Commands.UploadKeys;
using SessionApp.Application.Features.Keys.Queries.GetPrekeyBundle;

using SessionApp.Application.Features.Keys.Queries.GetKeyStatus;

namespace SessionApp.API.Controllers;

[Authorize]
public class KeysController : ApiControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<BaseResponse<bool>>> UploadKeys([FromBody] UploadKeysRequest request)
    {
        var result = await Mediator.Send(new UploadKeysCommand
        {
            UserId = CurrentUserId!,
            IdentityKey = request.IdentityKey,
            SignedPrekey = request.SignedPrekey,
            Signature = request.Signature,
            SignedPrekeyId = request.SignedPrekeyId,
            OneTimePrekeys = request.OneTimePrekeys
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("bundle/{username}")]
    public async Task<ActionResult<BaseResponse<PrekeyBundleDto>>> GetPrekeyBundle(string username)
    {
        var result = await Mediator.Send(new GetPrekeyBundleQuery
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

    [HttpGet("status")]
    public async Task<ActionResult<BaseResponse<KeyStatusDto>>> GetKeyStatus()
    {
        var result = await Mediator.Send(new GetKeyStatusQuery
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

public record UploadKeysRequest
{
    public required string IdentityKey { get; init; }
    public required string SignedPrekey { get; init; }
    public required string Signature { get; init; }
    public int SignedPrekeyId { get; init; }
    public required List<OneTimePrekeyDto> OneTimePrekeys { get; init; }
}
