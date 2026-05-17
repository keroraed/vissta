using VISSTA.Domain.Common;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Domain.Entities;

public sealed class Customer : Entity, IAggregateRoot
{
    private readonly List<Order> _orders = [];
    private readonly List<Review> _reviews = [];

    private Customer()
    {
        Id = string.Empty;
        FullName = string.Empty;
        PhoneNumber = string.Empty;
    }

    public Customer(string id, string fullName, string phoneNumber, Address? defaultAddress = null)
    {
        Id = id;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        DefaultAddress = defaultAddress;
    }

    public string Id { get; private set; }
    public string FullName { get; private set; }
    public string PhoneNumber { get; private set; }
    public Address? DefaultAddress { get; private set; }
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();
}
