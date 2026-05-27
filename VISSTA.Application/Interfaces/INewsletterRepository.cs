using VISSTA.Domain.Entities;

namespace VISSTA.Application.Interfaces;

public interface INewsletterRepository
{
    Task<NewsletterSubscriber?> GetByEmailAsync(string email);
    Task<NewsletterSubscriber?> GetByTokenAsync(string token);
    Task AddAsync(NewsletterSubscriber subscriber);
    Task SaveChangesAsync();
}
