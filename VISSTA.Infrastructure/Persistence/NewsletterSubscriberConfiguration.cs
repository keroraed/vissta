using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VISSTA.Domain.Entities;

namespace VISSTA.Infrastructure.Persistence;

public sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("NewsletterSubscribers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.SubscribedAt)
            .IsRequired();

        builder.Property(x => x.UnsubscribedAt);

        builder.Property(x => x.UnsubscribeToken)
            .HasMaxLength(32);

        builder.HasIndex(x => x.UnsubscribeToken);
    }
}
