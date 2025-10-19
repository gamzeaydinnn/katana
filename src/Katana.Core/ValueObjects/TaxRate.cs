using System.Globalization;

namespace Katana.Core.ValueObjects;

/// <summary>
/// Immutable representation of a tax rate normalised between 0 and 1.
/// Provides helpers to work with percentages while guarding against invalid states.
/// </summary>
public readonly record struct TaxRate
{
    /// <summary>
    /// Normalised tax value (0.18 == %18 KDV).
    /// </summary>
    public decimal Value { get; }

    public TaxRate(decimal value)
    {
        if (value is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Tax rate must be between 0 and 1.");
        }

        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public static TaxRate FromPercentage(decimal percentage)
    {
        return new TaxRate(percentage / 100m);
    }

    public decimal ToPercentage() => Value * 100m;

    public decimal ApplyTo(decimal amount) => decimal.Round(amount * Value, 2, MidpointRounding.AwayFromZero);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{ToPercentage():N2}%");

    public static implicit operator decimal(TaxRate rate) => rate.Value;
}

