using System.Globalization;

namespace Katana.Core.ValueObjects;

/// <summary>
/// Immutable money representation that couples value with its ISO currency code.
/// Provides basic arithmetic helpers that prevent accidental currency mixing.
/// </summary>
public readonly record struct Money
{
    private const int CurrencyCodeLength = 3;

    /// <summary>
    /// Monetary amount represented with two decimal places.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// ISO 4217 currency code (e.g. TRY, USD).
    /// </summary>
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency code is required.", nameof(currency));
        }

        currency = currency.Trim().ToUpperInvariant();

        if (currency.Length != CurrencyCodeLength)
        {
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 value.", nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public Money Abs() => new(Math.Abs(Amount), Currency);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:N2} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Money operations require matching currencies.");
        }
    }

    public static implicit operator decimal(Money money) => money.Amount;
}

