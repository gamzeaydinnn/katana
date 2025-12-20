using System.Text.RegularExpressions;
using Katana.Core.DTOs;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Implementation of canonical selection service
/// </summary>
public class CanonicalSelector : ICanonicalSelector
{
    private static readonly Regex VersionSuffixPattern = new Regex(@"-V\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly char[] TurkishChars = { 'Ü', 'ü', 'Ş', 'ş', 'İ', 'i', 'Ğ', 'ğ', 'Ç', 'ç', 'Ö', 'ö' };

    /// <summary>
    /// Property 12: Canonical selection by rules
    /// Property 13: Default canonical selection
    /// Selects the canonical stock card based on priority-ordered rules
    /// </summary>
    public StockCardInfo SelectCanonical(DuplicateGroup group, DeduplicationRules rules)
    {
        if (group.StockCards == null || !group.StockCards.Any())
        {
            throw new ArgumentException("Duplicate group must contain at least one stock card", nameof(group));
        }

        // Single card - return it
        if (group.StockCards.Count == 1)
        {
            var card = group.StockCards[0];
            card.IsCanonical = true;
            return card;
        }

        // Apply rules in priority order
        var enabledRules = rules.Rules
            .Where(r => r.Enabled)
            .OrderBy(r => r.Priority)
            .ToList();

        foreach (var rule in enabledRules)
        {
            var selected = ApplyRule(rule, group.StockCards);
            if (selected != null)
            {
                selected.IsCanonical = true;
                return selected;
            }
        }

        // No rule matched - apply default rule
        var defaultSelected = ApplyRule(rules.DefaultRule, group.StockCards);
        if (defaultSelected != null)
        {
            defaultSelected.IsCanonical = true;
            return defaultSelected;
        }

        // Fallback: shortest code (Property 13)
        var fallback = group.StockCards
            .OrderBy(c => c.StockCode.Length)
            .ThenBy(c => c.StockCode)
            .First();
        
        fallback.IsCanonical = true;
        return fallback;
    }

    /// <summary>
    /// Applies a single rule to select a stock card
    /// </summary>
    private StockCardInfo? ApplyRule(CanonicalSelectionRule rule, List<StockCardInfo> cards)
    {
        return rule.Type switch
        {
            RuleType.PreferNoVersionSuffix => ApplyPreferNoVersionSuffix(cards),
            RuleType.PreferLowerVersion => ApplyPreferLowerVersion(cards),
            RuleType.PreferShorterCode => ApplyPreferShorterCode(cards),
            RuleType.PreferNoSpecialCharacters => ApplyPreferNoSpecialCharacters(cards),
            RuleType.PreferCorrectEncoding => ApplyPreferCorrectEncoding(cards),
            _ => null
        };
    }

    /// <summary>
    /// Rule: Prefer stock cards without version suffix
    /// Example: BFM-01 preferred over BFM-01-V2
    /// </summary>
    private StockCardInfo? ApplyPreferNoVersionSuffix(List<StockCardInfo> cards)
    {
        var withoutVersion = cards.Where(c => !VersionSuffixPattern.IsMatch(c.StockCode)).ToList();
        
        if (withoutVersion.Any())
        {
            // If multiple cards without version, pick shortest code
            return withoutVersion
                .OrderBy(c => c.StockCode.Length)
                .ThenBy(c => c.StockCode)
                .First();
        }

        return null;
    }

    /// <summary>
    /// Rule: Prefer lowest version number
    /// Example: BFM-01-V2 preferred over BFM-01-V5
    /// </summary>
    private StockCardInfo? ApplyPreferLowerVersion(List<StockCardInfo> cards)
    {
        var versioned = cards
            .Select(c => new { Card = c, Version = GetVersionNumber(c.StockCode) })
            .Where(x => x.Version > 0)
            .OrderBy(x => x.Version)
            .ThenBy(x => x.Card.StockCode)
            .ToList();

        return versioned.FirstOrDefault()?.Card;
    }

    /// <summary>
    /// Rule: Prefer shortest stock code
    /// Example: BFM-01 preferred over BFM-01-EXTRA
    /// </summary>
    private StockCardInfo? ApplyPreferShorterCode(List<StockCardInfo> cards)
    {
        return cards
            .OrderBy(c => c.StockCode.Length)
            .ThenBy(c => c.StockCode)
            .First();
    }

    /// <summary>
    /// Rule: Prefer codes without special characters
    /// Example: BFM01 preferred over BFM-01
    /// </summary>
    private StockCardInfo? ApplyPreferNoSpecialCharacters(List<StockCardInfo> cards)
    {
        var withoutSpecial = cards
            .Where(c => !c.StockCode.Any(ch => !char.IsLetterOrDigit(ch)))
            .ToList();

        if (withoutSpecial.Any())
        {
            return withoutSpecial
                .OrderBy(c => c.StockCode.Length)
                .ThenBy(c => c.StockCode)
                .First();
        }

        return null;
    }

    /// <summary>
    /// Rule: Prefer correctly encoded Turkish characters
    /// Example: "KROM TALAŞ" preferred over "KROM TALA?"
    /// </summary>
    private StockCardInfo? ApplyPreferCorrectEncoding(List<StockCardInfo> cards)
    {
        // Prefer cards without question marks (encoding issues)
        var withoutQuestionMarks = cards
            .Where(c => !c.StockName.Contains('?'))
            .ToList();

        if (withoutQuestionMarks.Any())
        {
            // Among those, prefer ones with Turkish characters (properly encoded)
            var withTurkishChars = withoutQuestionMarks
                .Where(c => c.StockName.Any(ch => TurkishChars.Contains(ch)))
                .ToList();

            if (withTurkishChars.Any())
            {
                return withTurkishChars
                    .OrderBy(c => c.StockCode.Length)
                    .ThenBy(c => c.StockCode)
                    .First();
            }

            // No Turkish chars, just return first without question marks
            return withoutQuestionMarks
                .OrderBy(c => c.StockCode.Length)
                .ThenBy(c => c.StockCode)
                .First();
        }

        return null;
    }

    /// <summary>
    /// Extracts version number from stock code
    /// </summary>
    private int GetVersionNumber(string stockCode)
    {
        var match = VersionSuffixPattern.Match(stockCode);
        if (!match.Success)
        {
            return 0;
        }

        var versionStr = match.Value.Substring(2); // Skip "-V"
        return int.TryParse(versionStr, out var version) ? version : 0;
    }
}
