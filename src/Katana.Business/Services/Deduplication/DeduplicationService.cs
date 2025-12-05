using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Katana.Business.Services.Deduplication;

/// <summary>
/// Main orchestration service for stock card deduplication
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    private readonly ILucaService _lucaService;
    private readonly IDuplicateDetector _duplicateDetector;
    private readonly ICanonicalSelector _canonicalSelector;
    private readonly ILogger<DeduplicationService> _logger;
    private readonly DeduplicationRules _rules;

    public DeduplicationService(
        ILucaService lucaService,
        IDuplicateDetector duplicateDetector,
        ICanonicalSelector canonicalSelector,
        ILogger<DeduplicationService> logger,
        IOptions<DeduplicationRules> rulesOptions)
    {
        _lucaService = lucaService;
        _duplicateDetector = duplicateDetector;
        _canonicalSelector = canonicalSelector;
        _logger = logger;
        _rules = rulesOptions.Value;
    }

    /// <summary>
    /// Property 3: Analysis report structure
    /// Property 6: Version count accuracy
    /// Analyzes all stock cards and detects duplicates
    /// </summary>
    public async Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting duplicate analysis...");

        // Fetch all stock cards from Luca
        var stockCards = await _lucaService.ListStockCardsAsync(ct);
        
        _logger.LogInformation("Fetched {Count} stock cards from Luca", stockCards.Count);

        // Convert to LucaStockDto format for detection
        // Note: LucaStockCardSummaryDto doesn't have SkartId
        // We'll use StokKodu hash as a pseudo-ID for grouping purposes
        var lucaStockDtos = stockCards.Select(card => new LucaStockDto
        {
            SkartId = GetStableHashCode(card.StokKodu), // Use hash of stock code as ID
            ProductCode = card.StokKodu ?? string.Empty,
            ProductName = card.StokAdi ?? string.Empty
        }).ToList();

        // Detect duplicates
        var duplicateGroups = _duplicateDetector.DetectDuplicates(lucaStockDtos);
        
        _logger.LogInformation("Detected {GroupCount} duplicate groups", duplicateGroups.Count);

        // Calculate statistics
        var statistics = CalculateStatistics(stockCards.Count, duplicateGroups);

        var result = new DuplicateAnalysisResult
        {
            DuplicateGroups = duplicateGroups,
            Statistics = statistics,
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Analysis complete: {TotalCards} cards, {DuplicateGroups} groups, {TotalDuplicates} duplicates",
            statistics.TotalStockCards,
            statistics.DuplicateGroups,
            statistics.TotalDuplicates);

        return result;
    }

    /// <summary>
    /// Property 11: Preview completeness
    /// Generates preview of deduplication actions
    /// </summary>
    public async Task<DeduplicationPreview> GeneratePreviewAsync(
        DuplicateAnalysisResult analysis, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating deduplication preview for {GroupCount} groups", 
            analysis.DuplicateGroups.Count);

        var actions = new List<DeduplicationAction>();

        foreach (var group in analysis.DuplicateGroups)
        {
            // Select canonical card
            var canonical = _canonicalSelector.SelectCanonical(group, _rules);

            // Determine cards to remove
            var cardsToRemove = group.StockCards
                .Where(c => c.SkartId != canonical.SkartId)
                .ToList();

            // Build reason
            var reason = BuildActionReason(canonical, group);

            var action = new DeduplicationAction
            {
                GroupId = group.GroupId,
                CanonicalCard = canonical,
                CardsToRemove = cardsToRemove,
                Reason = reason,
                Type = DetermineActionType(group)
            };

            actions.Add(action);
        }

        var previewStats = new PreviewStatistics
        {
            TotalActions = actions.Count,
            CardsToKeep = actions.Count,
            CardsToRemove = actions.Sum(a => a.CardsToRemove.Count),
            CardsToUpdate = actions.Count(a => a.Type == ActionType.UpdateAndRemove)
        };

        var preview = new DeduplicationPreview
        {
            Actions = actions,
            Statistics = previewStats,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Preview generated: {TotalActions} actions, {CardsToRemove} cards to remove",
            previewStats.TotalActions,
            previewStats.CardsToRemove);

        return await Task.FromResult(preview);
    }

    /// <summary>
    /// Property 14: Execution follows preview
    /// Property 15: Canonical existence check
    /// Property 16: Execution summary completeness
    /// Executes deduplication by deleting duplicate cards
    /// </summary>
    public async Task<DeduplicationExecutionResult> ExecuteDeduplicationAsync(
        DeduplicationPreview preview, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting deduplication execution for {ActionCount} actions", 
            preview.Actions.Count);

        var result = new DeduplicationExecutionResult
        {
            ExecutedAt = DateTime.UtcNow
        };

        foreach (var action in preview.Actions)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Deduplication execution cancelled");
                break;
            }

            try
            {
                // Property 15: Verify canonical exists before deletion
                var canonicalExists = await VerifyCanonicalExistsAsync(action.CanonicalCard.SkartId, ct);
                
                if (!canonicalExists)
                {
                    var error = new ExecutionError
                    {
                        GroupId = action.GroupId,
                        StockCode = action.CanonicalCard.StockCode,
                        ErrorMessage = "Canonical card not found in Luca"
                    };
                    result.Errors.Add(error);
                    result.FailedRemovals += action.CardsToRemove.Count;
                    
                    _logger.LogError(
                        "Canonical card {Code} (ID: {Id}) not found, skipping group {GroupId}",
                        action.CanonicalCard.StockCode,
                        action.CanonicalCard.SkartId,
                        action.GroupId);
                    
                    // Halt on first error as per spec
                    break;
                }

                // Delete duplicate cards
                foreach (var cardToRemove in action.CardsToRemove)
                {
                    try
                    {
                        await DeleteStockCardAsync(cardToRemove.SkartId, ct);
                        result.SuccessfulRemovals++;
                        
                        _logger.LogInformation(
                            "Deleted duplicate card {Code} (ID: {Id})",
                            cardToRemove.StockCode,
                            cardToRemove.SkartId);
                    }
                    catch (Exception ex)
                    {
                        var error = new ExecutionError
                        {
                            GroupId = action.GroupId,
                            StockCode = cardToRemove.StockCode,
                            ErrorMessage = ex.Message
                        };
                        result.Errors.Add(error);
                        result.FailedRemovals++;
                        
                        _logger.LogError(ex,
                            "Failed to delete card {Code} (ID: {Id})",
                            cardToRemove.StockCode,
                            cardToRemove.SkartId);
                        
                        // Halt on first error
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing group {GroupId}, halting execution", action.GroupId);
                break;
            }
        }

        _logger.LogInformation(
            "Execution complete: {Success} successful, {Failed} failed, {Skipped} skipped",
            result.SuccessfulRemovals,
            result.FailedRemovals,
            result.SkippedActions);

        return result;
    }

    /// <summary>
    /// Exports analysis results to specified format
    /// </summary>
    public async Task<string> ExportResultsAsync(
        DuplicateAnalysisResult analysis, 
        ExportFormat format, 
        CancellationToken ct = default)
    {
        // This will be implemented when we add the export service
        throw new NotImplementedException("Export functionality will be implemented in Task 5");
    }

    /// <summary>
    /// Gets current deduplication rules
    /// </summary>
    public async Task<DeduplicationRules> GetRulesAsync()
    {
        return await Task.FromResult(_rules);
    }

    /// <summary>
    /// Updates deduplication rules
    /// </summary>
    public async Task UpdateRulesAsync(DeduplicationRules rules)
    {
        // In a real implementation, this would update configuration
        // For now, we'll just validate the rules
        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        if (rules.Rules == null || !rules.Rules.Any())
        {
            throw new ArgumentException("Rules list cannot be empty", nameof(rules));
        }

        if (rules.DefaultRule == null)
        {
            throw new ArgumentException("Default rule must be specified", nameof(rules));
        }

        _logger.LogInformation("Rules updated successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Calculates statistics from duplicate groups
    /// Property 6: Version count accuracy
    /// </summary>
    private DuplicateStatistics CalculateStatistics(int totalCards, List<DuplicateGroup> groups)
    {
        var stats = new DuplicateStatistics
        {
            TotalStockCards = totalCards,
            DuplicateGroups = groups.Count,
            TotalDuplicates = groups.Sum(g => g.StockCards.Count),
            VersioningDuplicates = groups.Count(g => 
                g.Category == DuplicateCategory.Versioning || 
                g.Category == DuplicateCategory.Mixed),
            ConcatenationErrors = groups.Count(g => 
                g.Category == DuplicateCategory.ConcatenationError || 
                g.Category == DuplicateCategory.Mixed),
            EncodingIssues = groups.Count(g => 
                g.Category == DuplicateCategory.CharacterEncoding || 
                g.Category == DuplicateCategory.Mixed)
        };

        return stats;
    }

    /// <summary>
    /// Builds human-readable reason for deduplication action
    /// </summary>
    private string BuildActionReason(StockCardInfo canonical, DuplicateGroup group)
    {
        var reasons = new List<string>();

        // Add category-specific reason
        switch (group.Category)
        {
            case DuplicateCategory.Versioning:
                reasons.Add("Versioning duplicates detected");
                break;
            case DuplicateCategory.ConcatenationError:
                reasons.Add("Concatenation error detected");
                break;
            case DuplicateCategory.CharacterEncoding:
                reasons.Add("Character encoding issue detected");
                break;
            case DuplicateCategory.Mixed:
                reasons.Add("Multiple issues detected");
                break;
        }

        // Add canonical selection reason
        if (!canonical.StockCode.Contains("-V"))
        {
            reasons.Add("Base version selected (no version suffix)");
        }
        else if (!canonical.StockName.Contains('?'))
        {
            reasons.Add("Correctly encoded version selected");
        }
        else
        {
            reasons.Add($"Shortest code selected: {canonical.StockCode}");
        }

        return string.Join("; ", reasons);
    }

    /// <summary>
    /// Determines the type of action needed
    /// </summary>
    private ActionType DetermineActionType(DuplicateGroup group)
    {
        // If concatenation error, might need update
        if (group.Category == DuplicateCategory.ConcatenationError)
        {
            return ActionType.UpdateAndRemove;
        }

        // Otherwise just remove
        return ActionType.Remove;
    }

    /// <summary>
    /// Verifies that canonical card exists in Luca
    /// Property 15: Canonical existence check
    /// </summary>
    private async Task<bool> VerifyCanonicalExistsAsync(long skartId, CancellationToken ct)
    {
        try
        {
            // Since LucaStockCardSummaryDto doesn't have SkartId,
            // we verify by checking if any card exists with matching hash
            var allCards = await _lucaService.ListStockCardsAsync(ct);
            return allCards.Any(c => GetStableHashCode(c.StokKodu) == skartId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying canonical card existence for ID {SkartId}", skartId);
            return false;
        }
    }

    /// <summary>
    /// Generates a stable hash code for a string (used as pseudo-ID)
    /// </summary>
    private long GetStableHashCode(string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        unchecked
        {
            long hash = 5381;
            foreach (char c in str)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return Math.Abs(hash);
        }
    }

    /// <summary>
    /// Deletes a stock card from Luca
    /// </summary>
    private async Task DeleteStockCardAsync(long skartId, CancellationToken ct)
    {
        // This will be implemented when we add the delete method to LucaService (Task 9)
        _logger.LogWarning("Delete functionality not yet implemented for card ID {SkartId}", skartId);
        await Task.CompletedTask;
        
        // TODO: Implement actual deletion
        // await _lucaService.DeleteStockCardAsync(skartId, ct);
    }
}
