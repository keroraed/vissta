namespace VISSTA.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency = "EGP")
{
    public static Money Zero(string currency = "EGP") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot combine money values with different currencies.");
        }
    }
}
