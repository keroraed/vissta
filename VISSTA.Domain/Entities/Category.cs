using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class Category : Entity, IAggregateRoot
{
    private readonly List<Category> _children = [];
    private readonly List<Product> _products = [];

    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Category(string name, string slug, int? parentCategoryId = null)
    {
        Name = name;
        Slug = slug;
        ParentCategoryId = parentCategoryId;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public int? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public void Update(string name, string slug, int? parentCategoryId)
    {
        Name = name;
        Slug = slug;
        ParentCategoryId = parentCategoryId;
    }
}
