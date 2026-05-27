using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Application.Features.Auth.Commands.RequestPasswordReset;

// ── Records ──────────────────────────────────────────────────────────────────
public sealed record RequestPasswordResetCommand(string Email) : IRequest<RequestPasswordResetResult>;
public sealed record RequestPasswordResetResult(bool Success, string Message);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class RequestPasswordResetCommandHandler(
    IPasswordResetOtpRepository otpRepository,
    IEmailService emailService,
    IMemoryCache cache) : IRequestHandler<RequestPasswordResetCommand, RequestPasswordResetResult>
{
    private const string SuccessMessage = "If this email is registered, a reset code has been sent.";

    public async Task<RequestPasswordResetResult> Handle(
        RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();
        var cacheKey = $"otp_rl_{email}";

        // 1. Rate-limit check (max 3 requests per email per hour)
        if (cache.TryGetValue(cacheKey, out int count) && count >= 3)
        {
            // Do NOT reveal rate limiting — return same success message
            return new RequestPasswordResetResult(true, SuccessMessage);
        }

        // 2. Generate cryptographically random 6-digit OTP
        var otpValue = RandomNumberGenerator.GetInt32(0, 1_000_000);
        var otp = $"{otpValue:D6}";

        // 3. Hash the OTP (SHA-256 hex, lowercase)
        var otpHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)))
                             .ToLowerInvariant();

        // 4. Invalidate all existing OTPs for this email
        await otpRepository.InvalidateAllForEmailAsync(email);

        // 5. Create and persist the new OTP entity
        var otpEntity = PasswordResetOtp.Create(email, otpHash);
        await otpRepository.AddAsync(otpEntity);
        await otpRepository.SaveChangesAsync();

        // 6. Send email (pass PLAIN otp — only hash is stored)
        try
        {
            await emailService.SendPasswordResetOtpAsync(email, otp);
        }
        catch
        {
            // Swallow email errors — never reveal whether the email exists
        }

        // 7. Increment rate-limit counter (sliding window: 1 hour)
        var newCount = count + 1;
        cache.Set(cacheKey, newCount, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        });

        return new RequestPasswordResetResult(true, SuccessMessage);
    }
}
