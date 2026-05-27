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
        var email = request.Email.ToLowerInvariant();

        // 1. Fetch OTP by Id AND email (both must match for security)
        var otpEntity = await otpRepository.GetActiveByEmailAsync(email);
        if (otpEntity is null || otpEntity.Id != request.OtpId)
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

        // 4. Generate new password hash and update
        var newHash = userManager.PasswordHasher.HashPassword(user, request.NewPassword);
        user.PasswordHash = newHash;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
            return new ResetPasswordOtpResult(false, errors);
        }

        // 5. Mark OTP as used and save
        otpEntity.MarkUsed();
        await otpRepository.SaveChangesAsync();

        return new ResetPasswordOtpResult(true, null);
    }
}
