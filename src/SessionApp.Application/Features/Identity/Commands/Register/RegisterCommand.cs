using MediatR;
using Microsoft.AspNetCore.Identity;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Helpers;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Identity.Commands.Register;

public record RegisterCommand : IRequest<BaseResponse<AuthDto>>
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string DisplayName { get; init; }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, BaseResponse<AuthDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<BaseResponse<AuthDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return BaseResponse<AuthDto>.Failure("Username is already taken.");
        }

        var mnemonic = MnemonicHelper.GenerateMnemonic();
        var mnemonicHash = MnemonicHelper.HashMnemonic(mnemonic);

        var user = new ApplicationUser
        {
            UserName = request.Username,
            DisplayName = request.DisplayName,
            RecoveryPhraseHash = mnemonicHash
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BaseResponse<AuthDto>.Failure(errors, "User registration failed.");
        }

        var token = _tokenGenerator.GenerateToken(user);

        var authDto = new AuthDto
        {
            Token = token,
            Username = user.UserName!,
            DisplayName = user.DisplayName,
            RecoveryMnemonic = mnemonic
        };

        return BaseResponse<AuthDto>.Success(authDto, "User registered successfully.");
    }
}
