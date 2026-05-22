using MediatR;
using Microsoft.AspNetCore.Identity;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Profiles.Commands.UpdateProfile;

public record UpdateProfileCommand : IRequest<BaseResponse<UserProfileDto>>
{
    public required string UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public bool? IsPrivate { get; init; }
    public string? Metadata { get; init; }
    public string? ProfilePictureBase64 { get; init; }
    public string? ProfilePictureFileName { get; init; }
    public DateTime? LastSeenAt { get; init; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, BaseResponse<UserProfileDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageStorageService _imageStorageService;

    public UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager, IImageStorageService imageStorageService)
    {
        _userManager = userManager;
        _imageStorageService = imageStorageService;
    }

    public async Task<BaseResponse<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return BaseResponse<UserProfileDto>.Failure("User not found.");
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.Bio != null)
        {
            user.Bio = request.Bio;
        }

        if (request.IsPrivate.HasValue)
        {
            user.IsPrivate = request.IsPrivate.Value;
        }

        if (request.Metadata != null)
        {
            user.Metadata = request.Metadata;
        }

        if (request.LastSeenAt.HasValue)
        {
            user.LastSeenAt = request.LastSeenAt.Value;
        }

        if (!string.IsNullOrEmpty(request.ProfilePictureBase64) && !string.IsNullOrEmpty(request.ProfilePictureFileName))
        {
            try
            {
                var imageBytes = Convert.FromBase64String(request.ProfilePictureBase64);
                var imageUrl = await _imageStorageService.UploadImageAsync(imageBytes, request.ProfilePictureFileName, cancellationToken);
                user.ProfilePictureUrl = imageUrl;
            }
            catch (Exception ex)
            {
                return BaseResponse<UserProfileDto>.Failure($"Failed to upload profile picture: {ex.Message}");
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BaseResponse<UserProfileDto>.Failure("Failed to update user profile.");
        }

        var dto = new UserProfileDto
        {
            Id = user.Id,
            Username = user.UserName!,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsPrivate = user.IsPrivate,
            Metadata = user.Metadata
        };

        return BaseResponse<UserProfileDto>.Success(dto, "Profile updated successfully.");
    }
}
