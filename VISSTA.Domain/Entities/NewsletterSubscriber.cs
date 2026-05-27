namespace VISSTA.Domain.Entities;

public class NewsletterSubscriber
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime SubscribedAt { get; private set; }
    public DateTime? UnsubscribedAt { get; private set; }
    public string? UnsubscribeToken { get; private set; }

    private NewsletterSubscriber() { }

    public static NewsletterSubscriber Create(string email)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            IsActive = true,
            SubscribedAt = DateTime.UtcNow,
            UnsubscribeToken = Guid.NewGuid().ToString("N")
        };

    public void Unsubscribe()
    {
        IsActive = false;
        UnsubscribedAt = DateTime.UtcNow;
    }

    public void Resubscribe()
    {
        IsActive = true;
        UnsubscribedAt = null;
        UnsubscribeToken = Guid.NewGuid().ToString("N");
    }
}
