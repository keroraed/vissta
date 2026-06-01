namespace VISSTA.Domain.Entities;

public sealed class AdminNotification
{
    private AdminNotification()
    {
    }

    public AdminNotification(string type, string title, string body, string linkUrl)
    {
        Type = type.Trim();
        Title = title.Trim();
        Body = body.Trim();
        LinkUrl = linkUrl.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string LinkUrl { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    public void MarkRead()
    {
        IsRead = true;
    }
}
