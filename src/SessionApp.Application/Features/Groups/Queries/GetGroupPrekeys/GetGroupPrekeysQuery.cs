using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Queries.GetGroupPrekeys;

public record GetGroupPrekeysQuery : IRequest<BaseResponse<List<GroupMemberPrekeyDto>>>
{
    public Guid GroupId { get; init; }
    public required string RequestingUserId { get; init; }
}

public class GroupMemberPrekeyDto
{
    public required string Username { get; set; }
    public required PrekeyBundleDto PrekeyBundle { get; set; }
}

public class GetGroupPrekeysQueryHandler : IRequestHandler<GetGroupPrekeysQuery, BaseResponse<List<GroupMemberPrekeyDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetGroupPrekeysQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<GroupMemberPrekeyDto>>> Handle(GetGroupPrekeysQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<List<GroupMemberPrekeyDto>>.Failure("Group not found.");
        }

        // Verify requesting user is a member of the group
        var isRequesterMember = group.Members.Any(m => m.UserId == request.RequestingUserId);
        if (!isRequesterMember)
        {
            return BaseResponse<List<GroupMemberPrekeyDto>>.Failure("You must be a member of the group to fetch member prekeys.");
        }

        var otherMembers = await _context.GroupMembers
            .Where(gm => gm.GroupId == request.GroupId && gm.UserId != request.RequestingUserId)
            .Include(gm => gm.User)
            .ToListAsync(cancellationToken);

        var result = new List<GroupMemberPrekeyDto>();

        foreach (var member in otherMembers)
        {
            var targetUser = member.User;
            if (targetUser == null) continue;

            var bundle = await _context.PrekeyBundles
                .FirstOrDefaultAsync(pb => pb.UserId == targetUser.Id, cancellationToken);

            if (bundle == null) continue; // Skip if no key uploaded yet

            // Vend one One-Time Prekey (OTP) and delete it from database
            OneTimePrekeyDto? otpDto = null;
            var otp = await _context.OneTimePrekeys
                .Where(o => o.UserId == targetUser.Id)
                .OrderBy(o => o.KeyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp != null)
            {
                otpDto = new OneTimePrekeyDto
                {
                    KeyId = otp.KeyId,
                    KeyData = otp.KeyData
                };
                _context.OneTimePrekeys.Remove(otp);
            }

            var prekeyBundleDto = new PrekeyBundleDto
            {
                IdentityKey = bundle.IdentityKey,
                SignedPrekey = bundle.SignedPrekey,
                Signature = bundle.Signature,
                SignedPrekeyId = bundle.SignedPrekeyId,
                OneTimePrekey = otpDto
            };

            result.Add(new GroupMemberPrekeyDto
            {
                Username = targetUser.UserName!,
                PrekeyBundle = prekeyBundleDto
            });
        }

        // Save changes to commit any consumed OTP deletions
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<List<GroupMemberPrekeyDto>>.Success(result, "Group member prekeys retrieved successfully.");
    }
}
