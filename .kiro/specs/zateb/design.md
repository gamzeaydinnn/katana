# Design Document

## Overview

ZATEB entegrasyonu, Katana sisteminin Türkiye e-fatura/e-arşiv altyapısıyla iletişim kurmasını sağlar. Bu tasarım, mevcut LucaService mimarisini örnek alarak ZatebService implementasyonunu tanımlar.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Katana API                                │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ SyncController  │  │ InvoiceController│  │ ZatebController │  │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘  │
│           │                    │                    │            │
│           └────────────────────┼────────────────────┘            │
│                                │                                 │
│                    ┌───────────▼───────────┐                    │
│                    │    IZatebService      │                    │
│                    └───────────┬───────────┘                    │
│                                │                                 │
└────────────────────────────────┼────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │      ZatebService       │
                    │  ┌──────────────────┐   │
                    │  │ Authentication   │   │
                    │  │ Invoice Send     │   │
                    │  │ Status Query     │   │
                    │  │ Inbox Fetch      │   │
                    │  │ Accept/Reject    │   │
                    │  └──────────────────┘   │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │      ZATEB API          │
                    │  (External Service)     │
                    └─────────────────────────┘
```

## Data Models

### ZatebInvoiceRequest

```csharp
public class ZatebInvoiceRequest
{
    public string FaturaNo { get; set; }           // Invoice number
    public DateTime FaturaTarihi { get; set; }     // Invoice date
    public string AliciVkn { get; set; }           // Recipient VKN/TCKN
    public string AliciUnvan { get; set; }         // Recipient title
    public string FaturaTipi { get; set; }         // SATIS, IADE, ISTISNA, etc.
    public decimal ToplamTutar { get; set; }       // Total amount
    public decimal KdvTutar { get; set; }          // VAT amount
    public List<ZatebInvoiceLineRequest> Kalemler { get; set; }
}
```

### ZatebInvoiceLineRequest

```csharp
public class ZatebInvoiceLineRequest
{
    public string UrunKodu { get; set; }
    public string UrunAdi { get; set; }
    public decimal Miktar { get; set; }
    public string Birim { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal ToplamTutar { get; set; }
}
```

### ZatebInvoiceResponse

```csharp
public class ZatebInvoiceResponse
{
    public bool Success { get; set; }
    public string Uuid { get; set; }
    public string Ettn { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
}
```

### ZatebStatusResponse

```csharp
public class ZatebStatusResponse
{
    public string Uuid { get; set; }
    public string Status { get; set; }           // Beklemede, Kabul, Red, Hata
    public string StatusDescription { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string RejectionReason { get; set; }
}
```

## Interface Design

### IZatebService

```csharp
public interface IZatebService
{
    // Outgoing invoices
    Task<ZatebInvoiceResponse> SendInvoiceAsync(ZatebInvoiceRequest request);
    Task<ZatebStatusResponse> GetInvoiceStatusAsync(string uuid);
    Task<List<ZatebStatusResponse>> GetBulkStatusAsync(List<string> uuids);

    // Incoming invoices
    Task<List<ZatebIncomingInvoice>> GetInboxAsync(DateTime? fromDate = null);
    Task<ZatebActionResponse> AcceptInvoiceAsync(string uuid);
    Task<ZatebActionResponse> RejectInvoiceAsync(string uuid, string reason);

    // Utilities
    Task<bool> CheckRecipientEFaturaStatusAsync(string vkn);
    Task<ZatebAuthResponse> AuthenticateAsync();
}
```

## API Endpoints

### ZatebController Endpoints

| Method | Endpoint                         | Description                         |
| ------ | -------------------------------- | ----------------------------------- |
| POST   | /api/zateb/invoice/send          | Send outgoing invoice               |
| GET    | /api/zateb/invoice/{uuid}/status | Get invoice status                  |
| POST   | /api/zateb/invoice/status/bulk   | Get bulk status                     |
| GET    | /api/zateb/inbox                 | Get incoming invoices               |
| POST   | /api/zateb/invoice/{uuid}/accept | Accept incoming invoice             |
| POST   | /api/zateb/invoice/{uuid}/reject | Reject incoming invoice             |
| GET    | /api/zateb/check-efatura/{vkn}   | Check if VKN is e-fatura registered |

## Error Handling

### Error Codes

| Code                      | Description               | Action              |
| ------------------------- | ------------------------- | ------------------- |
| ZATEB_AUTH_FAILED         | Authentication failed     | Check credentials   |
| ZATEB_VALIDATION_ERROR    | Request validation failed | Fix request data    |
| ZATEB_SEND_FAILED         | Invoice send failed       | Retry or check data |
| ZATEB_STATUS_NOT_FOUND    | Invoice not found         | Verify UUID         |
| ZATEB_RECIPIENT_NOT_FOUND | Recipient VKN not found   | Route to e-arşiv    |

## Configuration

### appsettings.json

```json
{
  "Zateb": {
    "BaseUrl": "https://api.zateb.com.tr",
    "Username": "",
    "Password": "",
    "CompanyVkn": "",
    "Timeout": 30,
    "RetryCount": 3,
    "Endpoints": {
      "Auth": "/auth/login",
      "SendInvoice": "/efatura/gonder",
      "Status": "/efatura/durum",
      "Inbox": "/efatura/gelen",
      "Accept": "/efatura/kabul",
      "Reject": "/efatura/red",
      "CheckEFatura": "/mükellef/sorgula"
    }
  }
}
```

## Security Considerations

1. Credentials stored securely (not in source code)
2. HTTPS required for all API calls
3. Request/response logging without sensitive data
4. Token refresh before expiry
5. Rate limiting compliance

## Dependencies

- Existing HttpClient infrastructure from LucaService
- JSON serialization (System.Text.Json)
- Logging infrastructure (ILogger)
- Configuration (IOptions pattern)
