using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Calls.Commands;

// 1. InitiateCallCommand
public record InitiateCallCommand : IRequest<BaseResponse<bool>>
{
    public required string CallerId { get; init; }
    public required string ReceiverUsername { get; init; }
    public required string SdpOffer { get; init; }
}

public class InitiateCallCommandHandler : IRequestHandler<InitiateCallCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public InitiateCallCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(InitiateCallCommand request, CancellationToken cancellationToken)
    {
        var caller = await _context.Users.FindAsync(new object[] { request.CallerId }, cancellationToken);
        if (caller == null)
        {
            return BaseResponse<bool>.Failure("Caller user not found.");
        }

        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.ReceiverUsername, cancellationToken);
        if (receiver == null)
        {
            return BaseResponse<bool>.Failure("Receiver user not found.");
        }

        // Blocklist Validation
        var isBlocked = await _context.BlockedUsers
            .AnyAsync(bu => (bu.BlockerId == receiver.Id && bu.BlockedId == caller.Id) || 
                            (bu.BlockerId == caller.Id && bu.BlockedId == receiver.Id), cancellationToken);

        if (isBlocked)
        {
            return BaseResponse<bool>.Failure("You cannot call this user because one of you has blocked the other.");
        }

        // Emit call signal via notification service
        await _notificationService.NotifyCallInitiatedAsync(receiver.UserName!, caller.UserName!, request.SdpOffer, cancellationToken);

        return BaseResponse<bool>.Success(true, "Call initiated successfully.");
    }
}

// 2. AcceptCallCommand
public record AcceptCallCommand : IRequest<BaseResponse<bool>>
{
    public required string CalleeId { get; init; }
    public required string CallerUsername { get; init; }
    public required string SdpAnswer { get; init; }
}

public class AcceptCallCommandHandler : IRequestHandler<AcceptCallCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public AcceptCallCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(AcceptCallCommand request, CancellationToken cancellationToken)
    {
        var callee = await _context.Users.FindAsync(new object[] { request.CalleeId }, cancellationToken);
        if (callee == null)
        {
            return BaseResponse<bool>.Failure("Callee user not found.");
        }

        var caller = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.CallerUsername, cancellationToken);
        if (caller == null)
        {
            return BaseResponse<bool>.Failure("Caller user not found.");
        }

        // Blocklist Validation
        var isBlocked = await _context.BlockedUsers
            .AnyAsync(bu => (bu.BlockerId == caller.Id && bu.BlockedId == callee.Id) || 
                            (bu.BlockerId == callee.Id && bu.BlockedId == caller.Id), cancellationToken);

        if (isBlocked)
        {
            return BaseResponse<bool>.Failure("You cannot accept call from this user.");
        }

        // Emit call accepted SignalR event
        await _notificationService.NotifyCallAcceptedAsync(caller.UserName!, callee.UserName!, request.SdpAnswer, cancellationToken);

        return BaseResponse<bool>.Success(true, "Call accepted successfully.");
    }
}

// 3. SendIceCandidateCommand
public record SendIceCandidateCommand : IRequest<BaseResponse<bool>>
{
    public required string SenderId { get; init; }
    public required string ReceiverUsername { get; init; }
    public required string Candidate { get; init; }
}

public class SendIceCandidateCommandHandler : IRequestHandler<SendIceCandidateCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public SendIceCandidateCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(SendIceCandidateCommand request, CancellationToken cancellationToken)
    {
        var sender = await _context.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
        if (sender == null)
        {
            return BaseResponse<bool>.Failure("Sender user not found.");
        }

        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.ReceiverUsername, cancellationToken);
        if (receiver == null)
        {
            return BaseResponse<bool>.Failure("Receiver user not found.");
        }

        // Send ICE candidate
        await _notificationService.NotifyIceCandidateSentAsync(receiver.UserName!, sender.UserName!, request.Candidate, cancellationToken);

        return BaseResponse<bool>.Success(true, "ICE candidate sent successfully.");
    }
}

// 4. DeclineCallCommand
public record DeclineCallCommand : IRequest<BaseResponse<bool>>
{
    public required string DeclinerId { get; init; }
    public required string CallerUsername { get; init; }
}

public class DeclineCallCommandHandler : IRequestHandler<DeclineCallCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public DeclineCallCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(DeclineCallCommand request, CancellationToken cancellationToken)
    {
        var decliner = await _context.Users.FindAsync(new object[] { request.DeclinerId }, cancellationToken);
        if (decliner == null)
        {
            return BaseResponse<bool>.Failure("Decliner user not found.");
        }

        var caller = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.CallerUsername, cancellationToken);
        if (caller == null)
        {
            return BaseResponse<bool>.Failure("Caller user not found.");
        }

        await _notificationService.NotifyCallDeclinedAsync(caller.UserName!, decliner.UserName!, cancellationToken);

        return BaseResponse<bool>.Success(true, "Call declined successfully.");
    }
}

// 5. HangUpCallCommand
public record HangUpCallCommand : IRequest<BaseResponse<bool>>
{
    public required string SenderId { get; init; }
    public required string ReceiverUsername { get; init; }
}

public class HangUpCallCommandHandler : IRequestHandler<HangUpCallCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public HangUpCallCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(HangUpCallCommand request, CancellationToken cancellationToken)
    {
        var sender = await _context.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
        if (sender == null)
        {
            return BaseResponse<bool>.Failure("Sender user not found.");
        }

        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.ReceiverUsername, cancellationToken);
        if (receiver == null)
        {
            return BaseResponse<bool>.Failure("Receiver user not found.");
        }

        await _notificationService.NotifyCallHungUpAsync(receiver.UserName!, sender.UserName!, cancellationToken);

        return BaseResponse<bool>.Success(true, "Call hung up successfully.");
    }
}
