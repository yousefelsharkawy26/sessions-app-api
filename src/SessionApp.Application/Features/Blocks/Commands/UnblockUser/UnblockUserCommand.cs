using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Blocks.Commands.UnblockUser;

public record UnblockUserCommand : IRequest<BaseResponse<bool>>
{
    public required string BlockerUserId { get; init; }
    public required string BlockedUsername { get; init; }
}

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, BaseResponse<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;

    public UnblockUserCommandHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var blocker = await _userManager.FindByIdAsync(request.BlockerUserId);
        if (blocker == null)
        {
            return BaseResponse<bool>.Failure("Blocker user not found.");
        }

        var blocked = await _userManager.FindByNameAsync(request.BlockedUsername);
        if (blocked == null)
        {
            return BaseResponse<bool>.Failure("User to unblock not found.");
        }

        var block = await _context.BlockedUsers
            .FirstOrDefaultAsync(bu => bu.BlockerId == blocker.Id && bu.BlockedId == blocked.Id, cancellationToken);

        if (block == null)
        {
            return BaseResponse<bool>.Failure("User is not blocked.");
        }

        _context.BlockedUsers.Remove(block);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, $"Successfully unblocked {request.BlockedUsername}.");
    }
}
