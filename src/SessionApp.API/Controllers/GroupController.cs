using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Groups.Commands.AddGroupMember;
using SessionApp.Application.Features.Groups.Commands.CreateGroup;
using SessionApp.Application.Features.Groups.Commands.RemoveGroupMember;
using SessionApp.Application.Features.Groups.Queries.GetGroupPrekeys;
using SessionApp.Application.Features.Groups.Commands.UpdateGroupMemberRole;
using SessionApp.Application.Features.Groups.Commands.UpdateGroupMetadata;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SessionApp.API.Controllers;

[Authorize]
public class GroupController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<GroupDto>>> Create([FromBody] CreateGroupRequest request)
    {
        var result = await Mediator.Send(new CreateGroupCommand
        {
            Name = request.Name,
            CreatorUserId = CurrentUserId!,
            MemberUsernames = request.MemberUsernames
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id}/member")]
    public async Task<ActionResult<BaseResponse<bool>>> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        var result = await Mediator.Send(new AddGroupMemberCommand
        {
            GroupId = id,
            Username = request.Username,
            RequestingUserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id}/member")]
    public async Task<ActionResult<BaseResponse<bool>>> RemoveMember(Guid id, [FromBody] RemoveGroupMemberRequest request)
    {
        var result = await Mediator.Send(new RemoveGroupMemberCommand
        {
            GroupId = id,
            Username = request.Username,
            RequestingUserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{id}/prekeys")]
    public async Task<ActionResult<BaseResponse<List<GroupMemberPrekeyDto>>>> GetPrekeys(Guid id)
    {
        var result = await Mediator.Send(new GetGroupPrekeysQuery
        {
            GroupId = id,
            RequestingUserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id}/role")]
    public async Task<ActionResult<BaseResponse<bool>>> UpdateMemberRole(Guid id, [FromBody] UpdateGroupMemberRoleRequest request)
    {
        var result = await Mediator.Send(new UpdateGroupMemberRoleCommand
        {
            GroupId = id,
            Username = request.Username,
            NewRole = request.NewRole,
            RequestingUserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id}/metadata")]
    public async Task<ActionResult<BaseResponse<bool>>> UpdateMetadata(Guid id, [FromBody] UpdateGroupMetadataRequest request)
    {
        var result = await Mediator.Send(new UpdateGroupMetadataCommand
        {
            GroupId = id,
            NewName = request.NewName,
            RequestingUserId = CurrentUserId!
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public record UpdateGroupMemberRoleRequest
{
    public required string Username { get; init; }
    public required string NewRole { get; init; }
}

public record UpdateGroupMetadataRequest
{
    public required string NewName { get; init; }
}

public record CreateGroupRequest
{
    public required string Name { get; init; }
    public List<string> MemberUsernames { get; init; } = new();
}

public record AddGroupMemberRequest
{
    public required string Username { get; init; }
}

public record RemoveGroupMemberRequest
{
    public required string Username { get; init; }
}
