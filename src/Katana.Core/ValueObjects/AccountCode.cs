using System.Text.RegularExpressions;

namespace Katana.Core.ValueObjects;

/// <summary>
/// Represents an immutable accounting code used while mapping products to ledger accounts.
/// Enforces a simple format (alphanumeric with separators) to keep data consistent.
/// </summary>
public readonly record struct AccountCode
{
    private const int MaxLength = 50;
    private static readonly Regex AllowedPattern = new(@"^[A-Za-z0-9\.\-_/]+$", RegexOptions.Compiled);

    public string Value { get; }

    public AccountCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Account code is required.", nameof(value));
        }

        value = value.Trim();

        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Account code cannot exceed {MaxLength} characters.", nameof(value));
        }

        if (!AllowedPattern.IsMatch(value))
        {
            throw new ArgumentException("Account code may only contain letters, digits, '.', '-', '_' or '/'.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(AccountCode code) => code.Value;

    public static bool TryCreate(string? value, out AccountCode accountCode)
    {
        try
        {
            accountCode = new AccountCode(value ?? string.Empty);
            return true;
        }
        catch
        {
            accountCode = default;
            return false;
        }
    }
}

