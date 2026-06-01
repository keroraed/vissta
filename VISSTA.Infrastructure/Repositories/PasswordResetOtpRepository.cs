using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Persistence;

namespace VISSTA.Infrastructure.Repositories;

public sealed class PasswordResetOtpRepository(VISSTADbContext db) : IPasswordResetOtpRepository
{
    public Task<PasswordResetOtp?> GetByIdAsync(Guid id)
    {
        return db.PasswordResetOtps.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<PasswordResetOtp?> GetActiveByEmailAsync(string email)
    {
        var normalized = Normalize(email);
        return db.PasswordResetOtps
            .Where(x => x.Email == normalized
                     && !x.IsUsed
                     && x.FailedAttempts < 5
                     && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task InvalidateAllForEmailAsync(string email)
    {
        var normalized = Normalize(email);
        // Bulk update via ExecuteUpdateAsync for performance
        await db.PasswordResetOtps
            .Where(x => x.Email == normalized && !x.IsUsed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsUsed, true));
    }

    public async Task AddAsync(PasswordResetOtp otp)
        => await db.PasswordResetOtps.AddAsync(otp);

    public Task SaveChangesAsync()
        => db.SaveChangesAsync();

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
