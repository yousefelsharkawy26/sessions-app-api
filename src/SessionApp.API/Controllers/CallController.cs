using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Calls.Commands;
using System.Threading.Tasks;

namespace SessionApp.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CallController : ApiControllerBase
{
    [HttpPost("initiate")]
    public async Task<ActionResult<BaseResponse<bool>>> InitiateCall([FromBody] InitiateCallRequest request)
    {
        var result = await Mediator.Send(new InitiateCallCommand
        {
            CallerId = CurrentUserId!,
            ReceiverUsername = request.ReceiverUsername,
            SdpOffer = request.SdpOffer
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("accept")]
    public async Task<ActionResult<BaseResponse<bool>>> AcceptCall([FromBody] AcceptCallRequest request)
    {
        var result = await Mediator.Send(new AcceptCallCommand
        {
            CalleeId = CurrentUserId!,
            CallerUsername = request.CallerUsername,
            SdpAnswer = request.SdpAnswer
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("candidate")]
    public async Task<ActionResult<BaseResponse<bool>>> SendIceCandidate([FromBody] SendIceCandidateRequest request)
    {
        var result = await Mediator.Send(new SendIceCandidateCommand
        {
            SenderId = CurrentUserId!,
            ReceiverUsername = request.ReceiverUsername,
            Candidate = request.Candidate
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("decline")]
    public async Task<ActionResult<BaseResponse<bool>>> DeclineCall([FromBody] DeclineCallRequest request)
    {
        var result = await Mediator.Send(new DeclineCallCommand
        {
            DeclinerId = CurrentUserId!,
            CallerUsername = request.CallerUsername
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("hangup")]
    public async Task<ActionResult<BaseResponse<bool>>> HangUpCall([FromBody] HangUpCallRequest request)
    {
        var result = await Mediator.Send(new HangUpCallCommand
        {
            SenderId = CurrentUserId!,
            ReceiverUsername = request.ReceiverUsername
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public record InitiateCallRequest
{
    public required string ReceiverUsername { get; init; }
    public required string SdpOffer { get; init; }
}

public record AcceptCallRequest
{
    public required string CallerUsername { get; init; }
    public required string SdpAnswer { get; init; }
}

public record SendIceCandidateRequest
{
    public required string ReceiverUsername { get; init; }
    public required string Candidate { get; init; }
}

public record DeclineCallRequest
{
    public required string CallerUsername { get; init; }
}

public record HangUpCallRequest
{
    public required string ReceiverUsername { get; init; }
}
