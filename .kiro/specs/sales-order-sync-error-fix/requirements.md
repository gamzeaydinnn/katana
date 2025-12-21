# Requirements Document

## Introduction

This specification addresses critical issues in the sales order approval and invoice synchronization system. The system currently experiences two main problems:

1. **Frontend Hydration Error**: Invalid HTML structure in the approval dialog causing React hydration warnings
2. **Backend Sync Failures**: Some sales orders fail to sync to Luca with 400 Bad Request errors, while others succeed

These issues create an inconsistent user experience and prevent reliable order processing. The system must handle all edge cases gracefully and provide clear error messages to users.

## Glossary

- **Sales_Order**: A sales order entity in the Katana system that needs to be synced to Luca
- **Luca**: External accounting/ERP system that receives invoice data
- **Sync_Process**: The operation that transfers sales order data from Katana to Luca
- **Approval_Dialog**: The UI dialog shown when admin clicks "Onayla" (Approve) button
- **Hydration_Error**: React error when server-rendered HTML doesn't match client-side rendering
- **BadRequest_Error**: HTTP 400 error returned when sync validation fails

## Requirements

### Requirement 1: Fix Frontend HTML Structure

**User Story:** As an admin user, I want the approval dialog to render without errors, so that I can approve orders without seeing console warnings or potential UI issues.

#### Acceptance Criteria

1. WHEN the approval dialog is displayed, THE System SHALL render valid HTML without nesting `<div>` elements inside `<p>` elements
2. WHEN the approval dialog contains styled content boxes, THE System SHALL use appropriate HTML elements that allow block-level children
3. WHEN React performs hydration, THE System SHALL not produce any hydration mismatch warnings
4. WHEN the dialog is opened multiple times, THE System SHALL consistently render the same valid HTML structure

### Requirement 2: Validate Sales Order Data Before Sync

**User Story:** As an admin user, I want to receive clear error messages when an order cannot be synced, so that I can understand what data is missing or invalid.

#### Acceptance Criteria

1. WHEN a sales order is submitted for sync, THE System SHALL validate that all required fields are present and valid
2. WHEN a sales order has missing customer information, THE System SHALL return a descriptive error message indicating which customer fields are missing
3. WHEN a sales order has invalid customer codes (e.g., "CUST_" prefix), THE System SHALL return a clear error message explaining the validation rule
4. WHEN a sales order has no order lines, THE System SHALL return an error message instructing the user to re-sync from Katana
5. WHEN validation fails, THE System SHALL return HTTP 400 with a structured error response containing a user-friendly message

### Requirement 3: Handle Edge Cases in Order Data

**User Story:** As a system administrator, I want the sync process to handle all edge cases in order data, so that orders don't fail unexpectedly in production.

#### Acceptance Criteria

1. WHEN a sales order has null or empty string values in optional fields, THE System SHALL handle them gracefully without throwing exceptions
2. WHEN a sales order has whitespace-only values in text fields, THE System SHALL either trim them or treat them as empty
3. WHEN a sales order has missing currency information, THE System SHALL use a default currency or return a clear validation error
4. WHEN a sales order has missing location/depot information, THE System SHALL use a default value or return a clear validation error
5. WHEN a sales order has already been synced successfully, THE System SHALL prevent duplicate sync attempts and return an informative message

### Requirement 4: Improve Error Logging and Debugging

**User Story:** As a developer, I want detailed error logs for sync failures, so that I can quickly diagnose and fix issues in production.

#### Acceptance Criteria

1. WHEN a sync operation fails, THE System SHALL log the complete order data (excluding sensitive information)
2. WHEN a validation error occurs, THE System SHALL log which specific validation rule failed and the actual values that caused the failure
3. WHEN an external API call to Luca fails, THE System SHALL log the request payload, response status, and error message
4. WHEN multiple orders are synced in batch, THE System SHALL log individual success/failure status for each order
5. WHEN errors are logged, THE System SHALL include correlation IDs to trace the full request flow

### Requirement 5: Display User-Friendly Error Messages

**User Story:** As an admin user, I want to see clear, actionable error messages in the UI, so that I know how to fix sync issues without contacting support.

#### Acceptance Criteria

1. WHEN a sync fails due to missing customer data, THE UI SHALL display a message indicating which customer fields need to be filled
2. WHEN a sync fails due to invalid data format, THE UI SHALL display a message explaining the expected format
3. WHEN a sync fails due to duplicate submission, THE UI SHALL display a message indicating the order was already synced
4. WHEN a sync fails due to external API errors, THE UI SHALL display a generic error message and suggest retrying later
5. WHEN multiple errors occur, THE UI SHALL display all error messages in a structured, readable format

### Requirement 6: Prevent Common Data Quality Issues

**User Story:** As a system administrator, I want the system to prevent common data quality issues, so that sync failures are minimized in production.

#### Acceptance Criteria

1. WHEN customer data is imported from Katana, THE System SHALL validate that customer codes do not contain invalid prefixes like "CUST_"
2. WHEN customer data is missing tax numbers, THE System SHALL either require manual entry or prevent order creation
3. WHEN order lines are imported, THE System SHALL validate that all required product information is present
4. WHEN currency conversion rates are needed, THE System SHALL validate that rates are available before allowing sync
5. WHEN depot/location mappings are missing, THE System SHALL provide a clear error during order approval rather than during sync

### Requirement 7: Implement Robust Error Recovery

**User Story:** As an admin user, I want to be able to retry failed syncs after fixing data issues, so that I don't lose orders due to temporary problems.

#### Acceptance Criteria

1. WHEN a sync fails due to validation errors, THE System SHALL allow the user to edit the order data and retry
2. WHEN a sync fails due to external API errors, THE System SHALL allow immediate retry without requiring data changes
3. WHEN an order is in error state, THE System SHALL clearly indicate which fields need correction
4. WHEN an order is retried after fixing errors, THE System SHALL clear previous error messages upon successful sync
5. WHEN multiple orders have errors, THE System SHALL allow bulk retry after fixing common issues

## Notes

- The frontend uses React with Material-UI components
- The backend is ASP.NET Core with Entity Framework
- Luca integration uses HTTP API calls
- Current error rate is approximately 9.4% (29 out of 32 orders showing errors in the screenshot)
- Two specific failing orders identified: SO-75 and SO-59
