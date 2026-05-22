using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Messages.Commands.SendMessage;
using SessionApp.Application.Features.Messages.Commands.DeleteMessage;
using SessionApp.Application.Features.Messages.Commands.EditMessage;
using SessionApp.Application.Features.Messages.Commands.SendGroupMessage;
using SessionApp.Application.Features.Messages.Commands.DeliverMessages;
using SessionApp.Application.Features.Messages.Queries.GetChatHistory;
using SessionApp.Application.Features.Messages.Queries.GetGroupChatHistory;

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
            Ciphertext = request.Ciphertext,
            EphemeralKey = request.EphemeralKey,
            SignedPrekeyIdUsed = request.SignedPrekeyIdUsed,
            OneTimePrekeyIdUsed = request.OneTimePrekeyIdUsed,
            BurnAfterSeconds = request.BurnAfterSeconds
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

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteMessage(Guid id)
    {
        var result = await Mediator.Send(new DeleteMessageCommand
        {
            MessageId = id,
            UserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse<MessageDto>>> EditMessage(Guid id, [FromBody] EditMessageRequest request)
    {
        var result = await Mediator.Send(new EditMessageCommand
        {
            MessageId = id,
            UserId = CurrentUserId!,
            NewCiphertext = request.NewCiphertext,
            NewEphemeralKey = request.NewEphemeralKey
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("group")]
    public async Task<ActionResult<BaseResponse<MessageDto>>> SendGroupMessage([FromBody] SendGroupMessageRequest request)
    {
        var result = await Mediator.Send(new SendGroupMessageCommand
        {
            SenderId = CurrentUserId!,
            GroupId = request.GroupId,
            Ciphertext = request.Ciphertext,
            EphemeralKey = request.EphemeralKey
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<BaseResponse<List<MessageDto>>>> GetGroupChatHistory(Guid groupId)
    {
        var result = await Mediator.Send(new GetGroupChatHistoryQuery
        {
            GroupId = groupId,
            UserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("deliver")]
    public async Task<ActionResult<BaseResponse<bool>>> DeliverMessages([FromBody] DeliverMessagesRequest request)
    {
        var result = await Mediator.Send(new DeliverMessagesCommand
        {
            UserId = CurrentUserId!,
            MessageIds = request.MessageIds
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public record DeliverMessagesRequest
{
    public required List<Guid> MessageIds { get; init; }
}

public record SendMessageRequest
{
    public required string ReceiverUsername { get; init; }
    public required string Ciphertext { get; init; }
    public required string EphemeralKey { get; init; }
    public int SignedPrekeyIdUsed { get; init; }
    public int? OneTimePrekeyIdUsed { get; init; }
    public int? BurnAfterSeconds { get; init; }
}

public record EditMessageRequest
{
    public required string NewCiphertext { get; init; }
    public required string NewEphemeralKey { get; init; }
}

public record SendGroupMessageRequest
{
    public required Guid GroupId { get; init; }
    public required string Ciphertext { get; init; }
    public string? EphemeralKey { get; init; }
}
