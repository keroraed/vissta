namespace VISSTA.Domain.ValueObjects;

public sealed record Address(
    string Street,
    string City,
    string Governorate,
    string PostalCode,
    string Country = "Egypt");
