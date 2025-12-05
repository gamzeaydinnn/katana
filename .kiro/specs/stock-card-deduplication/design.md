# Design Document: Stock Card Deduplication

## Overview

This feature provides a comprehensive solution for detecting, analyzing, and cleaning up duplicate stock cards in the Luca/Koza system. The system addresses three main categories of duplicates:

1. **Versioning Duplicates**: Stock cards with version suffixes (-V2, -V3, -V4)
2. **Concatenation Errors**: Malformed stock cards where code or name is duplicated (e.g., BFM-01BFM-01)
3. **Character Encoding Issues**: Turkish character corruption (e.g., TALA? vs TALAŞ)

The solution follows a safe, preview-first approach where administrators can review all changes before execution, with configurable rules for determining which stock cards to keep.

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   Admin UI      │
│  (Frontend)     │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│   DeduplicationController           │
│   - Analyze                         │
│   - Preview                         │
│   - Execute                         │
│   - Export                          │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│   DeduplicationService              │
│   - DetectDuplicates                │
│   - CategorizeD
```

uplicates │
│ - GeneratePreview │
│ - ExecuteDeduplication │
│ - ExportResults │
└────────┬────────────────────────────┘
│
├──────────────┬──────────────┐
▼ ▼ ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Duplicate │ │ Canonical │ │ Export │
│ Detector │ │ Selector │ │ Service │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘
│ │ │
└────────────────┴────────────────┘
│
▼
┌──────────────────┐
│ LucaService │
│ (Stock Cards) │
└────────┬─────────┘
│
▼
┌──────────────────┐
│ Luca/Koza API │
└──────────────────┘

````

### Component Responsibilities

- **DeduplicationController**: REST API endpoints for duplicate management
- **DeduplicationService**: Core business logic orchestration
- **DuplicateDetector**: Analyzes stock cards and identifies duplicates
- **CanonicalSelector**: Determines which stock card to keep based on rules
- **ExportService**: Generates reports in JSON/CSV formats
- **LucaService**: Communicates with Luca/Koza API

## Components and Interfaces

### 1. Data Transfer Objects (DTOs)

```csharp
// Analysis result
public class DuplicateAnalysisResult
{
    public List<DuplicateGroup> DuplicateGroups { get; set; }
    public DuplicateStatistics Statistics { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class DuplicateGroup
{
    public string GroupId { get; set; }
    public string StockName { get; set; }
    public DuplicateCategory Category { get; set; }
    public List<StockCardInfo> StockCards { get; set; }
}

public class StockCardInfo
{
    public long SkartId { get; set; }
    public string StockCode { get; set; }
    public string StockName { get; set; }
    public bool IsCanonical { get; set; }
    public string? IssueDescription { get; set; }
}

public enum DuplicateCategory
{
    Versioning,
    ConcatenationError,
    CharacterEncoding,
    Mixed
}

public class DuplicateStatistics
{
    public int TotalStockCards { get; set; }
    public int DuplicateGroups { get; set; }
    public int TotalDuplicates { get; set; }
    public int VersioningDuplicates { get; set; }
    public int ConcatenationErrors { get; set; }
    public int EncodingIssues { get; set; }
}

// Preview result
public class DeduplicationPreview
{
    public List<DeduplicationAction> Actions { get; set; }
    public PreviewStatistics Statistics { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DeduplicationAction
{
    public string GroupId { get; set; }
    public StockCardInfo CanonicalCard { get; set; }
    public List<StockCardInfo> CardsToRemove { get; set; }
    public string Reason { get; set; }
    public ActionType Type { get; set; }
}

public enum ActionType
{
    Remove,
    UpdateAndRemove,
    Skip
}

public class PreviewStatistics
{
    public int TotalActions { get; set; }
    public int CardsToKeep { get; set; }
    public int CardsToRemove { get; set; }
    public int CardsToUpdate { get; set; }
}

// Execution result
public class DeduplicationExecutionResult
{
    public int SuccessfulRemovals { get; set; }
    public int FailedRemovals { get; set; }
    public int SkippedActions { get; set; }
    public List<ExecutionError> Errors { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class ExecutionError
{
    public string GroupId { get; set; }
    public string StockCode { get; set; }
    public string ErrorMessage { get; set; }
}

// Configuration
public class DeduplicationRules
{
    public List<CanonicalSelectionRule> Rules { get; set; }
    public CanonicalSelectionRule DefaultRule { get; set; }
}

public class CanonicalSelectionRule
{
    public string Name { get; set; }
    public int Priority { get; set; }
    public RuleType Type { get; set; }
    public bool Enabled { get; set; }
}

public enum RuleType
{
    PreferNoVersionSuffix,
    PreferLowerVersion,
    PreferShorterCode,
    PreferNoSpecialCharacters,
    PreferCorrectEncoding
}
````

### 2. Service Interfaces

```csharp
public interface IDeduplicationService
{
    Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(CancellationToken ct = default);
    Task<DeduplicationPreview> GeneratePreviewAsync(DuplicateAnalysisResult analysis, CancellationToken ct = default);
    Task<DeduplicationExecutionResult> ExecuteDeduplicationAsync(DeduplicationPreview preview, CancellationToken ct = default);
    Task<string> ExportResultsAsync(DuplicateAnalysisResult analysis, ExportFormat format, CancellationToken ct = default);
    Task<DeduplicationRules> GetRulesAsync();
    Task UpdateRulesAsync(DeduplicationRules rules);
}

public interface IDuplicateDetector
{
    List<DuplicateGroup> DetectDuplicates(List<LucaStockDto> stockCards);
    DuplicateCategory CategorizeDuplicate(DuplicateGroup group);
}

public interface ICanonicalSelector
{
    StockCardInfo SelectCanonical(DuplicateGroup group, DeduplicationRules rules);
}

public interface IExportService
{
    Task<string> ExportToJsonAsync(DuplicateAnalysisResult analysis);
    Task<string> ExportToCsvAsync(DuplicateAnalysisResult analysis);
}

public enum ExportFormat
{
    Json,
    Csv
}
```

### 3. Controller Endpoints

```csharp
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class DeduplicationController : ControllerBase
{
    // POST /api/admin/deduplication/analyze
    [HttpPost("analyze")]
    public async Task<ActionResult<DuplicateAnalysisResult>> AnalyzeDuplicates(CancellationToken ct);

    // POST /api/admin/deduplication/preview
    [HttpPost("preview")]
    public async Task<ActionResult<DeduplicationPreview>> GeneratePreview(
        [FromBody] DuplicateAnalysisResult analysis, CancellationToken ct);

    // POST /api/admin/deduplication/execute
    [HttpPost("execute")]
    public async Task<ActionResult<DeduplicationExecutionResult>> ExecuteDeduplication(
        [FromBody] DeduplicationPreview preview, CancellationToken ct);

    // GET /api/admin/deduplication/export?format=json|csv
    [HttpGet("export")]
    public async Task<IActionResult> ExportResults(
        [FromQuery] ExportFormat format, CancellationToken ct);

    // GET /api/admin/deduplication/rules
    [HttpGet("rules")]
    public async Task<ActionResult<DeduplicationRules>> GetRules();

    // PUT /api/admin/deduplication/rules
    [HttpPut("rules")]
    public async Task<IActionResult> UpdateRules([FromBody] DeduplicationRules rules);
}
```

## Data Models

### Duplicate Detection Models

The system uses the existing `LucaStockDto` from the Luca service as the source data model. All analysis is performed in-memory without modifying the database schema.

### Configuration Storage

Deduplication rules are stored in application configuration (appsettings.json) with the following default structure:

```json
{
  "Deduplication": {
    "Rules": [
      {
        "Name": "PreferNoVersionSuffix",
        "Priority": 1,
        "Type": "PreferNoVersionSuffix",
        "Enabled": true
      },
      {
        "Name": "PreferCorrectEncoding",
        "Priority": 2,
        "Type": "PreferCorrectEncoding",
        "Enabled": true
      },
      {
        "Name": "PreferShorterCode",
        "Priority": 3,
        "Type": "PreferShorterCode",
        "Enabled": true
      }
    ],
    "DefaultRule": {
      "Name": "ShortestCode",
      "Type": "PreferShorterCode"
    }
  }
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property Reflection

Before defining the final properties, we must eliminate redundancy:

**Redundancy Analysis:**

- Properties 1.1 and 1.2 overlap: 1.1 tests the overall analysis flow while 1.2 tests duplicate identification. Since 1.2 is more specific and valuable, 1.1 can be absorbed into integration testing.
- Properties 2.2 and 2.3 can be combined: grouping and sorting are part of the same operation.
- Properties 3.1 and 3.2 are similar patterns: can be combined into one property for concatenation detection.
- Properties 4.4 and 4.5 overlap with 4.2: grouping already implies all variants are together.
- Properties 5.3, 5.4, and 5.5 are all about preview completeness: can be combined.
- Properties 8.3, 8.4, and 8.5 are all about export structure: can be combined per format.

**Final Property Set (after redundancy elimination):**

Property 1: Duplicate identification by name
_For any_ collection of stock cards, all stock cards with identical names (ignoring stock code) should be grouped together as duplicates
**Validates: Requirements 1.2**

Property 2: Duplicate categorization completeness
_For any_ duplicate group, the system should assign exactly one category (Versioning, ConcatenationError, CharacterEncoding, or Mixed)
**Validates: Requirements 1.3**

Property 3: Analysis report structure
_For any_ analysis result, the report should contain all duplicate groups with their stock codes, names, and categories
**Validates: Requirements 1.4**

Property 4: Version suffix detection
_For any_ stock code, if it contains the pattern "-V" followed by digits, it should be detected as a versioned stock card
**Validates: Requirements 2.1**

Property 5: Versioned card grouping and sorting
_For any_ set of versioned stock cards with the same base code and name, they should be grouped together and sorted by version number in ascending order
**Validates: Requirements 2.2, 2.3**

Property 6: Version count accuracy
_For any_ analysis result, the reported count of versioned groups and total versioned cards should match the actual detected versions
**Validates: Requirements 2.4**

Property 7: Concatenation error detection
_For any_ stock code or name, if the first half equals the second half (allowing for minor separators), it should be detected as a concatenation error
**Validates: Requirements 3.1, 3.2**

Property 8: Concatenation error reporting
_For any_ detected concatenation error, the report should include both the malformed value and the corrected value (first half only)
**Validates: Requirements 3.4**

Property 9: Character encoding issue detection
_For any_ stock name containing question marks, it should be flagged as a potential character encoding issue
**Validates: Requirements 4.1**

Property 10: Encoding similarity grouping
_For any_ pair of stock names that differ only in Turkish character encoding (e.g., "TALA?" vs "TALAŞ"), they should be grouped together
**Validates: Requirements 4.2**

Property 11: Preview completeness
_For any_ deduplication preview, each action should include the canonical card, cards to remove, the reason, and corrected values (if applicable)
**Validates: Requirements 5.3, 5.4, 5.5**

Property 12: Canonical selection by rules
_For any_ duplicate group and rule set, the canonical card should be selected according to the highest priority enabled rule that applies
**Validates: Requirements 5.2, 7.2, 7.3**

Property 13: Default canonical selection
_For any_ duplicate group where no rules determine a canonical card, the stock card with the shortest stock code should be selected
**Validates: Requirements 7.4**

Property 14: Execution follows preview
_For any_ approved preview plan, executing deduplication should process each group according to the plan (keep canonical, remove duplicates)
**Validates: Requirements 6.2**

Property 15: Canonical existence check
_For any_ deletion operation, the system should verify the canonical card exists before removing any duplicates
**Validates: Requirements 6.3**

Property 16: Execution summary completeness
_For any_ deduplication execution, the summary report should include counts of successful removals, failures, and skipped items
**Validates: Requirements 6.5**

Property 17: Export structure preservation (JSON)
_For any_ analysis result, exporting to JSON and parsing back should preserve the complete hierarchical structure of duplicate groups
**Validates: Requirements 8.4**

Property 18: Export column completeness (CSV)
_For any_ CSV export, all rows should contain columns for stock code, stock name, duplicate category, group identifier, and suggested action
**Validates: Requirements 8.3**

## Error Handling

### Error Categories

1. **API Communication Errors**

   - Luca service unavailable
   - Authentication failures
   - Timeout errors
   - Response: Return error to client with retry suggestion

2. **Data Quality Errors**

   - Malformed stock card data
   - Missing required fields
   - Response: Log warning, skip problematic cards, continue analysis

3. **Execution Errors**

   - Canonical card not found during deletion
   - API call fails during removal
   - Response: Halt processing, return detailed error report

4. **Configuration Errors**
   - Invalid rule configuration
   - Missing required settings
   - Response: Use default rules, log warning

### Error Response Format

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Retry Strategy

- **Analysis**: No retry (read-only operation)
- **Execution**: No automatic retry (requires admin confirmation)
- **Export**: Retry once on transient failures

## Testing Strategy

### Unit Testing

Unit tests will cover:

- Duplicate detection logic for each category (versioning, concatenation, encoding)
- Canonical selection with various rule combinations
- Export format generation (JSON/CSV structure)
- Error handling for edge cases

Example unit tests:

- `DetectVersionedDuplicates_WithV2V3Suffixes_GroupsCorrectly`
- `DetectConcatenationError_WithDoubledCode_ReturnsError`
- `SelectCanonical_WithNoVersionRule_SelectsBaseVersion`
- `ExportToCsv_WithDuplicates_ContainsAllColumns`

### Property-Based Testing

Property-based tests will verify universal properties using a PBT library (e.g., FsCheck for C#). Each test will run a minimum of 100 iterations with randomly generated data.

**Test Data Generators:**

- `StockCardGenerator`: Generates random stock cards with configurable patterns
- `DuplicateGroupGenerator`: Generates groups with known duplicate patterns
- `RuleSetGenerator`: Generates random rule configurations

**Property Tests:**

- Each correctness property (1-18) will have a corresponding property-based test
- Tests will be tagged with comments referencing the design document property
- Format: `// Feature: stock-card-deduplication, Property N: [property text]`

Example property test structure:

```csharp
[Property]
// Feature: stock-card-deduplication, Property 1: Duplicate identification by name
public Property DuplicateIdentification_GroupsByName()
{
    return Prop.ForAll(
        StockCardGenerator.Generate(),
        stockCards =>
        {
            var result = _detector.DetectDuplicates(stockCards);
            // Verify all cards with same name are in same group
            return VerifyGroupingByName(result, stockCards);
        });
}
```

### Integration Testing

Integration tests will verify:

- End-to-end flow from analysis to execution
- Interaction with Luca service
- Export file generation and format validation
- Rule configuration persistence

### Test Coverage Goals

- Unit test coverage: >80% for core logic
- Property-based tests: All 18 correctness properties
- Integration tests: All API endpoints
- Edge cases: Empty data, single card, all duplicates

## Implementation Notes

### Performance Considerations

- **In-Memory Analysis**: All duplicate detection runs in-memory for speed
- **Batch Processing**: Execution processes groups in batches of 10
- **Caching**: Analysis results cached for 5 minutes to support preview/execute flow
- **Pagination**: Export supports streaming for large result sets

### Security Considerations

- **Authorization**: All endpoints require Admin role
- **Confirmation**: Execution requires explicit preview approval
- **Audit Logging**: All deduplication actions logged with admin user ID
- **Dry-Run Mode**: Preview never modifies data

### Scalability

- **Expected Load**: <1000 stock cards typical, <10000 maximum
- **Analysis Time**: <5 seconds for 1000 cards
- **Execution Time**: ~1 second per duplicate removal (API call)
- **Concurrent Operations**: Single admin operation at a time (mutex lock)

### Future Enhancements

- Automatic deduplication scheduling
- Machine learning for encoding correction
- Bulk update support for canonical cards
- Undo/rollback capability
- Duplicate prevention on stock card creation
