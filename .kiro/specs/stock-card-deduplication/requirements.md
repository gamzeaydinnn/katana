# Requirements Document

## Introduction

This feature addresses critical data quality issues in the stock card system where duplicate and malformed stock cards exist due to versioning, integration errors, and character encoding problems. The system currently contains hundreds of duplicate stock cards with the same name but different codes (e.g., V2, V3, V4 suffixes), concatenation errors (e.g., BFM-01BFM-01), and Turkish character encoding issues (e.g., TALA? vs TALAŞ). This feature will detect, analyze, and provide tools to clean up these duplicates while preserving data integrity.

## Glossary

- **Stock Card**: A record in the Luca system representing a product with a unique stock code and name
- **Katana System**: The integration middleware that synchronizes data between local database and Luca
- **Luca System**: The external ERP system that stores stock cards
- **Duplicate Stock Card**: A stock card with the same name but different stock code, typically caused by versioning or integration errors
- **Versioned Stock Card**: A stock card with a suffix like -V2, -V3, -V4 appended to the stock code
- **Concatenation Error**: A malformed stock card where the code or name is duplicated (e.g., BFM-01BFM-01)
- **Character Encoding Issue**: A stock card with corrupted Turkish characters (e.g., ? instead of ş, ı, ğ)
- **Canonical Stock Card**: The primary, correct version of a stock card that should be retained
- **Deduplication**: The process of identifying and removing duplicate stock cards

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to detect duplicate stock cards in the Luca system, so that I can identify data quality issues.

#### Acceptance Criteria

1. WHEN the administrator requests duplicate detection THEN the Katana System SHALL retrieve all stock cards from the Luca System and analyze them for duplicates
2. WHEN analyzing stock cards THEN the Katana System SHALL identify duplicates based on identical stock names regardless of stock code differences
3. WHEN duplicates are found THEN the Katana System SHALL categorize them into versioning duplicates, concatenation errors, and character encoding issues
4. WHEN the analysis is complete THEN the Katana System SHALL return a report containing duplicate groups with stock codes, names, and categorization
5. WHEN no duplicates exist THEN the Katana System SHALL return an empty report with a success status

### Requirement 2

**User Story:** As a system administrator, I want to identify versioned stock cards, so that I can understand which products have multiple versions.

#### Acceptance Criteria

1. WHEN analyzing stock cards THEN the Katana System SHALL detect stock codes containing version suffixes matching the pattern -V followed by digits
2. WHEN versioned stock cards are detected THEN the Katana System SHALL group them by base stock code and name
3. WHEN grouping versioned cards THEN the Katana System SHALL sort them by version number in ascending order
4. WHEN the analysis is complete THEN the Katana System SHALL report the count of versioned groups and total versioned cards
5. WHEN a stock card has no version suffix THEN the Katana System SHALL treat it as version 1 or the base version

### Requirement 3

**User Story:** As a system administrator, I want to detect concatenation errors in stock cards, so that I can identify malformed data from integration failures.

#### Acceptance Criteria

1. WHEN analyzing stock cards THEN the Katana System SHALL detect stock codes where the first half equals the second half
2. WHEN analyzing stock cards THEN the Katana System SHALL detect stock names where the first half equals the second half
3. WHEN concatenation errors are detected THEN the Katana System SHALL flag them as critical data quality issues
4. WHEN reporting concatenation errors THEN the Katana System SHALL include both the malformed value and the suggested corrected value
5. WHEN a stock card has both code and name concatenation errors THEN the Katana System SHALL report both issues

### Requirement 4

**User Story:** As a system administrator, I want to detect character encoding issues in stock cards, so that I can identify Turkish character corruption.

#### Acceptance Criteria

1. WHEN analyzing stock cards THEN the Katana System SHALL detect stock names containing question marks that may represent corrupted Turkish characters
2. WHEN character encoding issues are detected THEN the Katana System SHALL group stock cards with similar names differing only in character encoding
3. WHEN grouping encoding issues THEN the Katana System SHALL identify the correctly encoded version if it exists
4. WHEN reporting encoding issues THEN the Katana System SHALL show both corrupted and correct versions side by side
5. WHEN multiple stock cards have the same name with different encodings THEN the Katana System SHALL flag all variants in the same group

### Requirement 5

**User Story:** As a system administrator, I want to preview deduplication actions before execution, so that I can verify the changes are correct.

#### Acceptance Criteria

1. WHEN the administrator requests a deduplication preview THEN the Katana System SHALL generate a plan showing which stock cards will be kept and which will be removed
2. WHEN generating the preview THEN the Katana System SHALL identify the canonical stock card for each duplicate group based on configurable rules
3. WHEN displaying the preview THEN the Katana System SHALL show the canonical card, duplicate cards to be removed, and the reason for each action
4. WHEN the preview includes concatenation errors THEN the Katana System SHALL show the corrected values that will be applied
5. WHEN the preview includes encoding issues THEN the Katana System SHALL show which encoding will be preserved

### Requirement 6

**User Story:** As a system administrator, I want to execute deduplication with safeguards, so that I can clean up duplicates without losing critical data.

#### Acceptance Criteria

1. WHEN the administrator executes deduplication THEN the Katana System SHALL require explicit confirmation before making any changes
2. WHEN deduplication is executed THEN the Katana System SHALL process each duplicate group according to the approved preview plan
3. WHEN removing duplicate stock cards THEN the Katana System SHALL verify that the canonical card exists before deletion
4. WHEN deduplication encounters an error THEN the Katana System SHALL halt processing and report the error without continuing to other groups
5. WHEN deduplication is complete THEN the Katana System SHALL return a summary report showing successful removals, failures, and skipped items

### Requirement 7

**User Story:** As a system administrator, I want to configure deduplication rules, so that I can control which stock cards are considered canonical.

#### Acceptance Criteria

1. WHEN configuring deduplication rules THEN the Katana System SHALL allow the administrator to specify priority order for canonical selection
2. WHEN selecting canonical cards THEN the Katana System SHALL support rules based on version number, stock code length, and presence of special characters
3. WHEN multiple rules apply THEN the Katana System SHALL evaluate them in the configured priority order
4. WHEN no rule determines a canonical card THEN the Katana System SHALL default to the stock card with the shortest stock code
5. WHEN rules are updated THEN the Katana System SHALL apply them to subsequent deduplication operations without requiring system restart

### Requirement 8

**User Story:** As a system administrator, I want to export duplicate analysis results, so that I can review them offline or share with stakeholders.

#### Acceptance Criteria

1. WHEN the administrator requests an export THEN the Katana System SHALL generate a file containing all duplicate groups and their details
2. WHEN exporting results THEN the Katana System SHALL support JSON and CSV formats
3. WHEN exporting to CSV THEN the Katana System SHALL include columns for stock code, stock name, duplicate category, group identifier, and suggested action
4. WHEN exporting to JSON THEN the Katana System SHALL preserve the complete hierarchical structure of duplicate groups
5. WHEN the export is complete THEN the Katana System SHALL return the file path or content to the administrator
