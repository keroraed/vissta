using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Persistence;

namespace VISSTA.Infrastructure.Repositories;

public sealed class NewsletterRepository(VISSTADbContext db) : INewsletterRepository
{
    public Task<NewsletterSubscriber?> GetByEmailAsync(string email)
    {
        var normalized = email.ToLowerInvariant().Trim();
        return db.NewsletterSubscribers
            .FirstOrDefaultAsync(x => x.Email == normalized);
    }

    public Task<NewsletterSubscriber?> GetByTokenAsync(string token)
    {
        return db.NewsletterSubscribers
            .FirstOrDefaultAsync(x => x.UnsubscribeToken == token && x.IsActive);
    }

    public async Task AddAsync(NewsletterSubscriber subscriber)
        => await db.NewsletterSubscribers.AddAsync(subscriber);

    public Task SaveChangesAsync()
        => db.SaveChangesAsync();
}
