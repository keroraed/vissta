namespace VISSTA.Domain.Entities;

public sealed class PasswordResetOtp
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;       // normalized lowercase
    public string OtpHash { get; private set; } = string.Empty;     // SHA-256 hex of the 6-digit code
    public DateTime ExpiresAt { get; private set; }                  // UTC, +10 min from creation
    public bool IsUsed { get; private set; }
    public int FailedAttempts { get; private set; }                  // max 5
    public DateTime CreatedAt { get; private set; }

    private PasswordResetOtp() { }

    // Factory
    public static PasswordResetOtp Create(string email, string otpHash)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            OtpHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

    // Methods (no public setters — mutate only through methods)
    public void MarkUsed() => IsUsed = true;
    public void IncrementAttempts() => FailedAttempts++;
    public bool IsValid()
        => !IsUsed && FailedAttempts < 5 && DateTime.UtcNow < ExpiresAt;
}
