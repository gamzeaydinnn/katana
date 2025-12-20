using System.Text.RegularExpressions;
using Katana.Core.DTOs;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Implementation of duplicate detection service
/// </summary>
public class DuplicateDetector : IDuplicateDetector
{
    // Regex pattern for detecting version suffixes like -V2, -V3, -V4
    private static readonly Regex VersionSuffixPattern = new Regex(@"-V\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    // Turkish characters that might be corrupted to '?'
    private static readonly char[] TurkishChars = { 'Ü', 'ü', 'Ş', 'ş', 'İ', 'i', 'Ğ', 'ğ', 'Ç', 'ç', 'Ö', 'ö' };

    /// <summary>
    /// Detects duplicate stock cards by grouping cards with identical names
    /// Property 1: Duplicate identification by name
    /// </summary>
    public List<DuplicateGroup> DetectDuplicates(List<LucaStockDto> stockCards)
    {
        var duplicateGroups = new List<DuplicateGroup>();
        
        // Group stock cards by name (case-insensitive)
        var groupedByName = stockCards
            .GroupBy(card => card.ProductName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1) // Only groups with duplicates
            .ToList();

        foreach (var group in groupedByName)
        {
            var duplicateGroup = new DuplicateGroup
            {
                GroupId = Guid.NewGuid().ToString(),
                StockName = group.Key,
                StockCards = group.Select(card => new StockCardInfo
                {
                    SkartId = card.SkartId,
                    StockCode = card.ProductCode,
                    StockName = card.ProductName
                }).ToList()
            };

            // Categorize the duplicate group
            duplicateGroup.Category = CategorizeDuplicate(duplicateGroup);
            
            // Sort versioned cards by version number (Property 5)
            SortVersionedCards(duplicateGroup);
            
            // Add issue descriptions to each card
            AddIssueDescriptions(duplicateGroup);

            duplicateGroups.Add(duplicateGroup);
        }

        return duplicateGroups;
    }

    /// <summary>
    /// Categorizes a duplicate group based on the type of duplication issue
    /// Property 2: Duplicate categorization completeness
    /// </summary>
    public DuplicateCategory CategorizeDuplicate(DuplicateGroup group)
    {
        var categories = new HashSet<DuplicateCategory>();

        // Check for versioning duplicates
        if (HasVersioningDuplicates(group))
        {
            categories.Add(DuplicateCategory.Versioning);
        }

        // Check for concatenation errors
        if (HasConcatenationErrors(group))
        {
            categories.Add(DuplicateCategory.ConcatenationError);
        }

        // Check for character encoding issues
        if (HasEncodingIssues(group))
        {
            categories.Add(DuplicateCategory.CharacterEncoding);
        }

        // Return Mixed if multiple categories, otherwise return the single category
        if (categories.Count > 1)
        {
            return DuplicateCategory.Mixed;
        }
        
        return categories.FirstOrDefault();
    }

    /// <summary>
    /// Property 4: Version suffix detection
    /// Detects if stock codes have version suffixes (-V2, -V3, etc.)
    /// </summary>
    private bool HasVersioningDuplicates(DuplicateGroup group)
    {
        return group.StockCards.Any(card => VersionSuffixPattern.IsMatch(card.StockCode));
    }

    /// <summary>
    /// Property 7: Concatenation error detection
    /// Detects if stock code or name is duplicated (first half = second half)
    /// </summary>
    private bool HasConcatenationErrors(DuplicateGroup group)
    {
        return group.StockCards.Any(card => 
            IsConcatenationError(card.StockCode) || IsConcatenationError(card.StockName));
    }

    /// <summary>
    /// Property 9: Character encoding issue detection
    /// Detects if stock names contain question marks (potential Turkish character corruption)
    /// </summary>
    private bool HasEncodingIssues(DuplicateGroup group)
    {
        return group.StockCards.Any(card => card.StockName.Contains('?'));
    }

    /// <summary>
    /// Checks if a string is a concatenation error (first half = second half)
    /// Handles separators like -, _, and space
    /// </summary>
    private bool IsConcatenationError(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 4)
        {
            return false;
        }

        // Try different separators
        var separators = new[] { "", "-", "_", " " };
        
        foreach (var separator in separators)
        {
            var halfLength = value.Length / 2;
            
            // For odd-length strings with separator, check if middle char is separator
            if (value.Length % 2 == 1 && separator.Length > 0)
            {
                var midIndex = value.Length / 2;
                if (value[midIndex].ToString() == separator)
                {
                    var firstHalf = value.Substring(0, midIndex);
                    var secondHalf = value.Substring(midIndex + 1);
                    
                    if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            
            // For even-length strings (no separator in middle)
            if (value.Length % 2 == 0 && separator.Length == 0)
            {
                var firstHalf = value.Substring(0, halfLength);
                var secondHalf = value.Substring(halfLength);
                
                if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Property 5: Versioned card grouping and sorting
    /// Sorts stock cards by version number in ascending order
    /// </summary>
    private void SortVersionedCards(DuplicateGroup group)
    {
        // Only sort if this is a versioning duplicate group
        if (group.Category != DuplicateCategory.Versioning && group.Category != DuplicateCategory.Mixed)
        {
            return;
        }

        group.StockCards = group.StockCards
            .OrderBy(card => GetVersionNumber(card.StockCode))
            .ThenBy(card => card.StockCode)
            .ToList();
    }

    /// <summary>
    /// Extracts version number from stock code (e.g., "BFM-01-V3" returns 3)
    /// Returns 0 if no version suffix found (base version)
    /// </summary>
    private int GetVersionNumber(string stockCode)
    {
        var match = VersionSuffixPattern.Match(stockCode);
        if (!match.Success)
        {
            return 0; // Base version (no suffix)
        }

        // Extract number from "-V3" -> "3"
        var versionStr = match.Value.Substring(2); // Skip "-V"
        return int.TryParse(versionStr, out var version) ? version : 0;
    }

    /// <summary>
    /// Adds issue descriptions to each stock card in the group
    /// Property 8: Concatenation error reporting
    /// </summary>
    private void AddIssueDescriptions(DuplicateGroup group)
    {
        foreach (var card in group.StockCards)
        {
            var issues = new List<string>();

            // Check for version suffix
            if (VersionSuffixPattern.IsMatch(card.StockCode))
            {
                var match = VersionSuffixPattern.Match(card.StockCode);
                issues.Add($"Version suffix detected: {match.Value}");
            }

            // Check for concatenation error in code
            if (IsConcatenationError(card.StockCode))
            {
                var corrected = GetCorrectedValue(card.StockCode);
                issues.Add($"Concatenation error in code. Corrected: {corrected}");
            }

            // Check for concatenation error in name
            if (IsConcatenationError(card.StockName))
            {
                var corrected = GetCorrectedValue(card.StockName);
                issues.Add($"Concatenation error in name. Corrected: {corrected}");
            }

            // Check for encoding issues
            if (card.StockName.Contains('?'))
            {
                issues.Add("Character encoding issue detected (? marks)");
            }

            if (issues.Any())
            {
                card.IssueDescription = string.Join("; ", issues);
            }
        }
    }

    /// <summary>
    /// Gets the corrected value for a concatenation error (returns first half)
    /// Property 8: Concatenation error reporting
    /// </summary>
    private string GetCorrectedValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var separators = new[] { "", "-", "_", " " };
        
        foreach (var separator in separators)
        {
            // For odd-length strings with separator
            if (value.Length % 2 == 1 && separator.Length > 0)
            {
                var midIndex = value.Length / 2;
                if (value[midIndex].ToString() == separator)
                {
                    var firstHalf = value.Substring(0, midIndex);
                    var secondHalf = value.Substring(midIndex + 1);
                    
                    if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase))
                    {
                        return firstHalf;
                    }
                }
            }
            
            // For even-length strings
            if (value.Length % 2 == 0 && separator.Length == 0)
            {
                var halfLength = value.Length / 2;
                var firstHalf = value.Substring(0, halfLength);
                var secondHalf = value.Substring(halfLength);
                
                if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase))
                {
                    return firstHalf;
                }
            }
        }

        return value;
    }
}
