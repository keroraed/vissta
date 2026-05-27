using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VISSTA.Domain.Entities;

namespace VISSTA.Infrastructure.Persistence;

public sealed class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
{
    public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
    {
        builder.ToTable("PasswordResetOtps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.Email);   // non-unique — multiple historical rows allowed

        builder.Property(x => x.OtpHash)
            .IsRequired()
            .HasMaxLength(64);            // SHA-256 hex = 64 chars

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.FailedAttempts)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
