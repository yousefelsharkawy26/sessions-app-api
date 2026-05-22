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

        // Check for lockout
        if (user.RecoveryLockoutEnd.HasValue && user.RecoveryLockoutEnd.Value > System.DateTime.UtcNow)
        {
            var remainingTime = user.RecoveryLockoutEnd.Value - System.DateTime.UtcNow;
            return BaseResponse.Failure($"Account recovery is locked out due to too many failed attempts. Try again in {System.Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
        }

        var isMnemonicValid = MnemonicHelper.VerifyMnemonic(request.Mnemonic, user.RecoveryPhraseHash);
        if (!isMnemonicValid)
        {
            user.FailedRecoveryAttempts++;
            if (user.FailedRecoveryAttempts >= 5)
            {
                // Progressive backoff lockout duration: 5 attempts = 5 min, 6 attempts = 15 min, 7+ attempts = 60 min
                int lockoutMinutes = user.FailedRecoveryAttempts switch
                {
                    5 => 5,
                    6 => 15,
                    _ => 60
                };
                user.RecoveryLockoutEnd = System.DateTime.UtcNow.AddMinutes(lockoutMinutes);
            }

            await _userManager.UpdateAsync(user);

            if (user.FailedRecoveryAttempts >= 5)
            {
                return BaseResponse.Failure($"Invalid recovery mnemonic. Account recovery is now locked out for {user.FailedRecoveryAttempts switch { 5 => 5, 6 => 15, _ => 60 }} minutes.");
            }

            return BaseResponse.Failure($"Invalid recovery mnemonic. {5 - user.FailedRecoveryAttempts} attempts remaining before lockout.");
        }

        // Reset lockout stats on successful verification
        user.FailedRecoveryAttempts = 0;
        user.RecoveryLockoutEnd = null;
        await _userManager.UpdateAsync(user);

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
