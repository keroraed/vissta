using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class ProductImage : Entity
{
    private ProductImage()
    {
        Url = string.Empty;
    }

    public ProductImage(int productId, string url, bool isPrimary, int displayOrder)
    {
        ProductId = productId;
        Url = url;
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string Url { get; private set; }
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }
}
