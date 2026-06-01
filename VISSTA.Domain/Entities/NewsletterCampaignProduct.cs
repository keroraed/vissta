namespace VISSTA.Domain.Entities;

public sealed class NewsletterCampaignProduct
{
    private NewsletterCampaignProduct()
    {
    }

    public NewsletterCampaignProduct(int productId, int displayOrder)
    {
        ProductId = productId;
        DisplayOrder = displayOrder;
    }

    public int NewsletterCampaignId { get; private set; }
    public NewsletterCampaign? NewsletterCampaign { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int DisplayOrder { get; private set; }
}
