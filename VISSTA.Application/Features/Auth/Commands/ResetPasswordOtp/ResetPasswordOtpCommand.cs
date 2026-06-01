using FluentValidation;
using MediatR;

namespace VISSTA.Application.Features.Auth.Commands.ResetPasswordOtp;

// ── Records ──────────────────────────────────────────────────────────────────
public sealed record ResetPasswordOtpCommand(
    Guid OtpId,
    string Email,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordOtpResult>;

public sealed record ResetPasswordOtpResult(bool Success, string? ErrorMessage);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class ResetPasswordOtpCommandValidator : AbstractValidator<ResetPasswordOtpCommand>
{
    public ResetPasswordOtpCommandValidator()
    {
        RuleFor(x => x.OtpId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$")
                .WithMessage("Password must contain uppercase, lowercase, a number, and a special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword)
                .WithMessage("Passwords do not match.");
    }
}
