// Quick test program to analyze Luca duplicates
// Compile: dotnet script TestDuplicates.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// Simulated stock card data based on user's original report
var stockCards = new List<StockCard>
{
    // Versioning examples
    new StockCard { SkartId = 1, Code = "9310011", Name = "NETSİS KONTROL ET1" },
    new StockCard { SkartId = 2, Code = "9310011-V2", Name = "NETSİS KONTROL ET1" },
    new StockCard { SkartId = 3, Code = "9310011-V3", Name = "NETSİS KONTROL ET1" },
    new StockCard { SkartId = 4, Code = "9310011-V4", Name = "NETSİS KONTROL ET1" },
    
    new StockCard { SkartId = 5, Code = "9310024", Name = "NETSİS KONTROL ET2" },
    new StockCard { SkartId = 6, Code = "9310024-V2", Name = "NETSİS KONTROL ET2" },
    new StockCard { SkartId = 7, Code = "9310024-V3", Name = "NETSİS KONTROL ET2" },
    
    new StockCard { SkartId = 8, Code = "BFM-01", Name = "KROM TALA?" },
    new StockCard { SkartId = 9, Code = "BFM-01-V2", Name = "KROM TALA?" },
    new StockCard { SkartId = 10, Code = "BFM-01-V5", Name = "KROM TALAŞ" },
    new StockCard { SkartId = 11, Code = "BFM-01-V6", Name = "KROM TALAŞ" },
    
    // Concatenation errors
    new StockCard { SkartId = 12, Code = "BFM-01BFM-01", Name = "KROM TALA?KROM TALA?" },
    new StockCard { SkartId = 13, Code = "HIZ01HIZ01", Name = "%1 KDV Lİ MUHTELİF ALIMLAR%1 KDV Lİ MUHTELİF ALIMLAR" },
    new StockCard { SkartId = 14, Code = "93100119310011", Name = "NETSİS KONTROL ET1NETSİS KONTROL ET1" },
    
    // Encoding issues
    new StockCard { SkartId = 15, Code = "HIZ01", Name = "%1 KDV L? MUHTEL?F ALIMLAR" },
    new StockCard { SkartId = 16, Code = "HIZ01-V2", Name = "%1 KDV L? MUHTEL?F ALIMLAR" },
    new StockCard { SkartId = 17, Code = "HIZ01-V3", Name = "%1 KDV L? MUHTEL?F ALIMLAR" },
};

Console.WriteLine("========================================");
Console.WriteLine("LUCA DUPLICATE ANALYSIS");
Console.WriteLine("========================================\n");

// Group by name
var duplicateGroups = stockCards
    .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .ToList();

Console.WriteLine($"Total stock cards: {stockCards.Count}");
Console.WriteLine($"Duplicate groups: {duplicateGroups.Count}");
Console.WriteLine($"Total duplicates: {duplicateGroups.Sum(g => g.Count())}\n");

var versionPattern = new Regex(@"-V\d+$", RegexOptions.IgnoreCase);

foreach (var group in duplicateGroups.OrderByDescending(g => g.Count()))
{
    Console.WriteLine("----------------------------------------");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Product Name: {group.Key}");
    Console.ResetColor();
    Console.WriteLine($"Duplicate Count: {group.Count()}");
    
    // Detect category
    var hasVersion = group.Any(c => versionPattern.IsMatch(c.Code));
    var hasEncoding = group.Any(c => c.Name.Contains('?'));
    var hasConcatenation = group.Any(c => IsConcatenation(c.Code) || IsConcatenation(c.Name));
    
    var categories = new List<string>();
    if (hasVersion) categories.Add("Versioning");
    if (hasConcatenation) categories.Add("Concatenation");
    if (hasEncoding) categories.Add("Encoding");
    
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"Category: {string.Join(", ", categories)}");
    Console.ResetColor();
    
    Console.WriteLine("\nStock Codes:");
    foreach (var card in group.OrderBy(c => GetVersionNumber(c.Code)))
    {
        var issues = new List<string>();
        if (versionPattern.IsMatch(card.Code)) issues.Add("[VERSION]");
        if (card.Name.Contains('?')) issues.Add("[ENCODING]");
        if (IsConcatenation(card.Code)) issues.Add("[CONCAT-CODE]");
        if (IsConcatenation(card.Name)) issues.Add("[CONCAT-NAME]");
        
        var issueStr = issues.Any() ? $" {string.Join(" ", issues)}" : "";
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  - {card.Code} (ID: {card.SkartId}){issueStr}");
        Console.ResetColor();
        
        if (IsConcatenation(card.Code))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"    → Corrected code: {GetCorrected(card.Code)}");
            Console.ResetColor();
        }
        if (IsConcatenation(card.Name))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"    → Corrected name: {GetCorrected(card.Name)}");
            Console.ResetColor();
        }
    }
    Console.WriteLine();
}

bool IsConcatenation(string value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Length < 4) return false;
    
    if (value.Length % 2 == 0)
    {
        var half = value.Length / 2;
        var first = value.Substring(0, half);
        var second = value.Substring(half);
        return first.Equals(second, StringComparison.OrdinalIgnoreCase);
    }
    
    return false;
}

string GetCorrected(string value)
{
    if (value.Length % 2 == 0)
    {
        return value.Substring(0, value.Length / 2);
    }
    return value;
}

int GetVersionNumber(string code)
{
    var match = versionPattern.Match(code);
    if (!match.Success) return 0;
    
    var versionStr = match.Value.Substring(2);
    return int.TryParse(versionStr, out var v) ? v : 0;
}

class StockCard
{
    public long SkartId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
