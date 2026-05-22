using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Messages.Commands.SendMessage;
using SessionApp.Application.Features.Messages.Queries.GetChatHistory;

namespace SessionApp.API.Controllers;

[Authorize]
public class MessageController : ApiControllerBase
{
    [HttpPost("send")]
    public async Task<ActionResult<BaseResponse<MessageDto>>> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await Mediator.Send(new SendMessageCommand
        {
            SenderId = CurrentUserId!,
            ReceiverUsername = request.ReceiverUsername,
            Content = request.Content
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("chat/{username}")]
    public async Task<ActionResult<BaseResponse<List<MessageDto>>>> GetChatHistory(string username)
    {
        var result = await Mediator.Send(new GetChatHistoryQuery
        {
            UserId = CurrentUserId!,
            WithUsername = username
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public record SendMessageRequest
{
    public required string ReceiverUsername { get; init; }
    public required string Content { get; init; }
}
