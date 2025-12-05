# Implementation Plan

- [ ] 1. Create DTOs for ZATEB Integration

  - [ ] 1.1 Create ZatebInvoiceRequest DTO

    - Add required fields: FaturaNo, FaturaTarihi, AliciVkn, AliciUnvan
    - Add financial fields: ToplamTutar, KdvTutar, FaturaTipi
    - Add line items collection: Kalemler
    - _Requirements: 1.1, 5.1_

  - [ ] 1.2 Create ZatebInvoiceLineRequest DTO

    - Add fields: UrunKodu, UrunAdi, Miktar, Birim, BirimFiyat, KdvOrani, ToplamTutar
    - _Requirements: 1.1_

  - [ ] 1.3 Create ZatebInvoiceResponse DTO

    - Add fields: Success, Uuid, Ettn, Message, ErrorCode
    - _Requirements: 1.2, 1.3_

  - [ ] 1.4 Create ZatebStatusResponse DTO

    - Add fields: Uuid, Status, StatusDescription, ProcessedDate, RejectionReason
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ] 1.5 Create ZatebIncomingInvoice DTO
    - Add fields for incoming invoice data
    - _Requirements: 3.1, 3.2_

- [ ] 2. Create Configuration Classes

  - [ ] 2.1 Create ZatebSettings class

    - Add BaseUrl, Username, Password, CompanyVkn
    - Add Timeout, RetryCount settings
    - Add Endpoints configuration
    - _Requirements: 4.1_

  - [ ] 2.2 Register ZatebSettings in DI container
    - Add to appsettings.json template
    - Register IOptions<ZatebSettings>
    - _Requirements: 4.1_

- [ ] 3. Create IZatebService Interface

  - [ ] 3.1 Define outgoing invoice methods

    - SendInvoiceAsync(ZatebInvoiceRequest)
    - GetInvoiceStatusAsync(string uuid)
    - GetBulkStatusAsync(List<string> uuids)
    - _Requirements: 1.1, 2.1_

  - [ ] 3.2 Define incoming invoice methods

    - GetInboxAsync(DateTime? fromDate)
    - AcceptInvoiceAsync(string uuid)
    - RejectInvoiceAsync(string uuid, string reason)
    - _Requirements: 3.1, 3.3, 3.4_

  - [ ] 3.3 Define utility methods
    - CheckRecipientEFaturaStatusAsync(string vkn)
    - AuthenticateAsync()
    - _Requirements: 1.4, 4.1_

- [ ] 4. Implement ZatebService Core

  - [ ] 4.1 Create ZatebService.Core.cs

    - Implement constructor with HttpClient, ILogger, IOptions
    - Implement authentication logic
    - Implement session management
    - _Requirements: 4.1, 4.2, 4.3_

  - [ ] 4.2 Create ZatebService.Invoices.cs

    - Implement SendInvoiceAsync with validation
    - Implement e-fatura vs e-arşiv routing
    - Handle response parsing
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 4.3 Create ZatebService.Status.cs

    - Implement GetInvoiceStatusAsync
    - Implement GetBulkStatusAsync
    - Parse status responses
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ] 4.4 Create ZatebService.Inbox.cs
    - Implement GetInboxAsync
    - Implement AcceptInvoiceAsync
    - Implement RejectInvoiceAsync
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 5. Implement Validation

  - [ ] 5.1 Create ZatebInvoiceValidator
    - Validate required fields (VKN, date, number)
    - Validate totals match line items
    - Validate tax calculations
    - Validate VKN/TCKN format
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 6. Create API Controller

  - [ ] 6.1 Create ZatebController

    - POST /api/zateb/invoice/send
    - GET /api/zateb/invoice/{uuid}/status
    - POST /api/zateb/invoice/status/bulk
    - _Requirements: 1.1, 2.1_

  - [ ] 6.2 Add inbox endpoints

    - GET /api/zateb/inbox
    - POST /api/zateb/invoice/{uuid}/accept
    - POST /api/zateb/invoice/{uuid}/reject
    - _Requirements: 3.1, 3.3, 3.4_

  - [ ] 6.3 Add utility endpoints
    - GET /api/zateb/check-efatura/{vkn}
    - _Requirements: 1.4_

- [ ] 7. Register Services in DI

  - [ ] 7.1 Register ZatebService in Program.cs/Startup.cs
    - Add HttpClient configuration
    - Add IZatebService registration
    - _Requirements: 4.1_

- [ ] 8. Checkpoint - Ensure compilation passes

  - Verify all files compile without errors
  - Ask user for ZATEB API documentation if needed

- [ ] 9. Write Unit Tests

  - [ ] 9.1 Test ZatebInvoiceValidator

    - Test required field validation
    - Test total calculation validation
    - Test VKN format validation
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ] 9.2 Test ZatebService methods
    - Mock HTTP responses
    - Test success scenarios
    - Test error handling
    - _Requirements: 1.1, 1.2, 1.3, 2.1_

- [ ] 10. Final Checkpoint
  - Ensure all tests pass
  - Review with user for any adjustments
