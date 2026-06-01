using VISSTA.Domain.Common;
using VISSTA.Domain.Enums;

namespace VISSTA.Domain.Entities;

public sealed class NewsletterCampaign : Entity, IAggregateRoot
{
    private readonly List<NewsletterCampaignProduct> _products = [];

    private NewsletterCampaign()
    {
        Subject = string.Empty;
        Headline = string.Empty;
        Body = string.Empty;
    }

    public NewsletterCampaign(string subject, string headline, string body)
    {
        Subject = subject.Trim();
        Headline = headline.Trim();
        Body = body.Trim();
        Status = NewsletterCampaignStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public int Id { get; private set; }
    public string Subject { get; private set; }
    public string Headline { get; private set; }
    public string Body { get; private set; }
    public NewsletterCampaignStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int SentCount { get; private set; }
    public IReadOnlyCollection<NewsletterCampaignProduct> Products => _products.AsReadOnly();

    public void UpdateContent(string subject, string headline, string body)
    {
        if (Status == NewsletterCampaignStatus.Sent)
        {
            throw new InvalidOperationException("Sent campaigns cannot be edited.");
        }

        Subject = subject.Trim();
        Headline = headline.Trim();
        Body = body.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceProducts(IEnumerable<int> productIds)
    {
        if (Status == NewsletterCampaignStatus.Sent)
        {
            throw new InvalidOperationException("Sent campaigns cannot be edited.");
        }

        _products.Clear();
        var index = 0;
        foreach (var productId in productIds.Where(id => id > 0).Distinct())
        {
            _products.Add(new NewsletterCampaignProduct(productId, index++));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDraft()
    {
        if (Status == NewsletterCampaignStatus.Sent)
        {
            throw new InvalidOperationException("Sent campaigns cannot be returned to draft.");
        }

        Status = NewsletterCampaignStatus.Draft;
        ScheduledAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Schedule(DateTime scheduledAtUtc)
    {
        if (Status == NewsletterCampaignStatus.Sent)
        {
            throw new InvalidOperationException("Sent campaigns cannot be scheduled.");
        }

        Status = NewsletterCampaignStatus.Scheduled;
        ScheduledAt = scheduledAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSent(int sentCount)
    {
        Status = NewsletterCampaignStatus.Sent;
        SentAt = DateTime.UtcNow;
        SentCount = sentCount;
        UpdatedAt = SentAt.Value;
    }
}
