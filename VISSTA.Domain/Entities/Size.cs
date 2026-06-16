using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class Size : Entity, IAggregateRoot
{
    private Size()
    {
        Name = string.Empty;
    }

    public Size(string name, int displayOrder)
    {
        Name = name.Trim();
        DisplayOrder = displayOrder;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }

    public void Update(string name, int displayOrder)
    {
        Name = name.Trim();
        DisplayOrder = displayOrder;
    }
}
