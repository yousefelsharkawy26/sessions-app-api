using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Blocks.Commands.BlockUser;

public record BlockUserCommand : IRequest<BaseResponse<bool>>
{
    public required string BlockerUserId { get; init; }
    public required string BlockedUsername { get; init; }
}

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, BaseResponse<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;

    public BlockUserCommandHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        var blocker = await _userManager.FindByIdAsync(request.BlockerUserId);
        if (blocker == null)
        {
            return BaseResponse<bool>.Failure("Blocker user not found.");
        }

        var blocked = await _userManager.FindByNameAsync(request.BlockedUsername);
        if (blocked == null)
        {
            return BaseResponse<bool>.Failure("User to block not found.");
        }

        if (blocker.Id == blocked.Id)
        {
            return BaseResponse<bool>.Failure("You cannot block yourself.");
        }

        var existingBlock = await _context.BlockedUsers
            .FirstOrDefaultAsync(bu => bu.BlockerId == blocker.Id && bu.BlockedId == blocked.Id, cancellationToken);

        if (existingBlock != null)
        {
            return BaseResponse<bool>.Success(true, "User is already blocked.");
        }

        var block = new BlockedUser
        {
            BlockerId = blocker.Id,
            Blocker = blocker,
            BlockedId = blocked.Id,
            Blocked = blocked
        };

        _context.BlockedUsers.Add(block);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, $"Successfully blocked {request.BlockedUsername}.");
    }
}
