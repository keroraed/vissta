using MediatR;
using Microsoft.AspNetCore.Identity;
using VISSTA.Application.Features.Auth.Commands.ResetPasswordOtp;
using VISSTA.Application.Interfaces;
using VISSTA.Infrastructure.Identity;

namespace VISSTA.Infrastructure.Handlers;

/// <summary>
/// Handler lives in Infrastructure (not Application) so it can access ApplicationUser
/// from VISSTA.Infrastructure.Identity without creating a circular project dependency.
/// Infrastructure already references Application for the command/result types.
/// </summary>
public sealed class ResetPasswordOtpCommandHandler(
    IPasswordResetOtpRepository otpRepository,
    UserManager<ApplicationUser> userManager) : IRequestHandler<ResetPasswordOtpCommand, ResetPasswordOtpResult>
{
    public async Task<ResetPasswordOtpResult> Handle(
        ResetPasswordOtpCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // 1. Fetch OTP by exact Id, then verify the email still matches.
        var otpEntity = await otpRepository.GetByIdAsync(request.OtpId);
        if (otpEntity is null || !string.Equals(otpEntity.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return new ResetPasswordOtpResult(false, "Session expired. Please start again.");
        }

        // 2. Validate OTP is still valid
        if (!otpEntity.IsValid())
        {
            return new ResetPasswordOtpResult(false, "Session expired. Please start again.");
        }

        // 3. Find user by normalized email
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new ResetPasswordOtpResult(false, "Session expired. Please start again.");
        }

        // 4. Reset through Identity so password validators and security stamp updates run.
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(" ", resetResult.Errors.Select(e => e.Description));
            return new ResetPasswordOtpResult(false, errors);
        }

        // 5. Mark OTP as used and save
        otpEntity.MarkUsed();
        await otpRepository.SaveChangesAsync();

        return new ResetPasswordOtpResult(true, null);
    }
}
