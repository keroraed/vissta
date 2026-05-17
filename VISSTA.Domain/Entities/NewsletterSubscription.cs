using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class NewsletterSubscription : Entity
{
    private NewsletterSubscription()
    {
        Email = string.Empty;
    }

    public NewsletterSubscription(string email)
    {
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
