using MediatR;
using Microsoft.AspNetCore.Identity;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Identity.Commands.Login;

public record LoginCommand : IRequest<BaseResponse<AuthDto>>
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, BaseResponse<AuthDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<BaseResponse<AuthDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return BaseResponse<AuthDto>.Failure("Invalid username or password.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return BaseResponse<AuthDto>.Failure("Invalid username or password.");
        }

        var token = _tokenGenerator.GenerateToken(user);

        var authDto = new AuthDto
        {
            Token = token,
            Username = user.UserName!,
            DisplayName = user.DisplayName
        };

        return BaseResponse<AuthDto>.Success(authDto, "Logged in successfully.");
    }
}
