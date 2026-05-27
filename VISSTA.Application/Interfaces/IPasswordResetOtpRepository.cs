using VISSTA.Domain.Entities;

namespace VISSTA.Application.Interfaces;

public interface IPasswordResetOtpRepository
{
    /// <summary>Returns the latest active (non-used, non-expired, &lt;5 attempts) OTP for the given email.</summary>
    Task<PasswordResetOtp?> GetActiveByEmailAsync(string email);

    /// <summary>Marks ALL existing OTPs for the email as used (invalidates old ones).</summary>
    Task InvalidateAllForEmailAsync(string email);

    /// <summary>Persists a newly created OTP.</summary>
    Task AddAsync(PasswordResetOtp otp);

    Task SaveChangesAsync();
}
