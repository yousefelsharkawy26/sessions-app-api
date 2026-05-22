using MediatR;
using Microsoft.AspNetCore.Identity;
using SessionApp.Application.Common.Helpers;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Identity.Commands.Recover;

public record RecoverPasswordCommand : IRequest<BaseResponse>
{
    public required string Username { get; init; }
    public required string Mnemonic { get; init; }
    public required string NewPassword { get; init; }
}

public class RecoverPasswordCommandHandler : IRequestHandler<RecoverPasswordCommand, BaseResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RecoverPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<BaseResponse> Handle(RecoverPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null || string.IsNullOrEmpty(user.RecoveryPhraseHash))
        {
            return BaseResponse.Failure("User recovery is not available or invalid username.");
        }

        var isMnemonicValid = MnemonicHelper.VerifyMnemonic(request.Mnemonic, user.RecoveryPhraseHash);
        if (!isMnemonicValid)
        {
            return BaseResponse.Failure("Invalid recovery mnemonic.");
        }

        // Generate a password reset token
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var errors = resetResult.Errors.Select(e => e.Description).ToList();
            return BaseResponse.Failure(errors, "Failed to reset password.");
        }

        return BaseResponse.Success("Password reset successfully. You can now login with your new password.");
    }
}
