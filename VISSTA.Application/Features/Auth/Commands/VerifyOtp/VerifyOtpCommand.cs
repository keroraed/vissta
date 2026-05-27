using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using VISSTA.Application.Interfaces;

namespace VISSTA.Application.Features.Auth.Commands.VerifyOtp;

// ── Records ──────────────────────────────────────────────────────────────────
public sealed record VerifyOtpCommand(string Email, string Otp) : IRequest<VerifyOtpResult>;
public sealed record VerifyOtpResult(bool Success, string? ErrorMessage, Guid? OtpId);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Otp).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class VerifyOtpCommandHandler(
    IPasswordResetOtpRepository otpRepository) : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
{
    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();

        // 1. Hash the submitted OTP the same way as at creation
        var submittedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Otp)))
                                   .ToLowerInvariant();

        // 2. Fetch the active OTP record
        var otpEntity = await otpRepository.GetActiveByEmailAsync(email);
        if (otpEntity is null)
        {
            return new VerifyOtpResult(false, "Invalid or expired code.", null);
        }

        // 3. Check validity (expired, used, too many attempts)
        if (!otpEntity.IsValid())
        {
            return new VerifyOtpResult(false, "This code has expired.", null);
        }

        // 4. Compare hashes (Ordinal string comparison)
        if (!string.Equals(otpEntity.OtpHash, submittedHash, StringComparison.Ordinal))
        {
            otpEntity.IncrementAttempts();
            await otpRepository.SaveChangesAsync();

            if (otpEntity.FailedAttempts >= 5)
            {
                return new VerifyOtpResult(false, "Too many incorrect attempts. Please request a new code.", null);
            }

            var remaining = 5 - otpEntity.FailedAttempts;
            return new VerifyOtpResult(false, $"Incorrect code. {remaining} attempt(s) remaining.", null);
        }

        // 5. Success — do NOT mark as used yet (used only after password is actually changed)
        return new VerifyOtpResult(true, null, otpEntity.Id);
    }
}
