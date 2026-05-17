using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class Review : Entity
{
    private Review()
    {
        CustomerId = string.Empty;
        Body = string.Empty;
    }

    public Review(int productId, string customerId, int rating, string body)
    {
        ProductId = productId;
        CustomerId = customerId;
        Rating = rating is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(rating)) : rating;
        Body = body;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public int Rating { get; private set; }
    public string Body { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsApproved { get; private set; }

    public void Approve() => IsApproved = true;
}
