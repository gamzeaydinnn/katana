# Luca API Session Management - Quick Start Guide

## 🚀 Quick Start

### 1. Build the Solution
```bash
dotnet build Katana.Integration.sln
```

**Expected Output**:
```
Oluşturma başarılı oldu.
0 Hata
```

### 2. Run the Application
```bash
dotnet run --project src/Katana.API/Katana.API.csproj
```

### 3. Test Stock Card Sync
```bash
# PowerShell
pwsh scripts/test-luca-session-recovery.ps1

# Or use the API endpoint
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'
```

---

## 🔍 Monitoring & Debugging

### Check Logs
```bash
# View raw HTTP traffic
tail -f logs/luca-raw.log

# View HTTP headers/bodies
ls -la logs/*-http-*.txt | tail -10

# View branch info
cat logs/luca-branches.json
```

### Key Log Messages

**Session Established**:
```
🔐 Login SUCCESS: JSESSIONID acquired. Cookie preview: JSESSIONID=ABC123..., Expires: 14:30:45
```

**HTML Response Detected**:
```
❌ ListStockCardsAsync HTML response aldı (session timeout/login gerekli). Attempt: 1
🚨 Login sayfasına redirect ediliyor! Cookie problemi var.
```

**Session Refreshed**:
```
🔄 ForceSessionRefreshAsync: Tüm session state temizleniyor...
✅ ForceSessionRefreshAsync tamamlandı. Authenticated: True, Cookie: True
```

**Stock Card Created**:
```
✅ Stock card created with skartId=79409. Message: 00013225 - Test Ürünü stok kartı başarılı bir şekilde kaydedilmiştir.
```

**Duplicate Detected**:
```
⚠️ Stok kartı 'PRD-001' Luca'da zaten mevcut (duplicate). Güncelleme yapılmayacak.
```

---

## 🧪 Testing Scenarios

### Scenario 1: Normal Stock Card Creation
```bash
# Send a new stock card
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'

# Expected: ✅ Stock card created with skartId=XXXXX
```

### Scenario 2: Session Timeout Recovery
```bash
# 1. Wait for session to expire (or manually expire it)
# 2. Send stock card sync
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'

# Expected: 
# ❌ ListStockCardsAsync HTML response aldı
# 🔄 ForceSessionRefreshAsync: Tüm session state temizleniyor...
# ✅ Stock card created (after retry)
```

### Scenario 3: Duplicate Prevention
```bash
# 1. Create a stock card
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'

# 2. Try to create the same card again
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'

# Expected: ⚠️ Stok kartı zaten mevcut ve değişiklik yok, atlanıyor
```

### Scenario 4: Batch Processing
```bash
# Send 150 stock cards (3 batches of 50)
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'

# Expected:
# Processing batch 1/3 (50 cards)
# Batch 1 tamamlandı. Sonraki batch için 1 saniye bekleniyor...
# Processing batch 2/3 (50 cards)
# Batch 2 tamamlandı. Sonraki batch için 1 saniye bekleniyor...
# Processing batch 3/3 (50 cards)
```

---

## 🔧 Configuration

### appsettings.json
```json
{
  "LucaApiSettings": {
    "BaseUrl": "http://luca-server:8080",
    "Username": "your-username",
    "Password": "your-password",
    "MemberNumber": "your-member-number",
    "UseTokenAuth": false,
    "UseHeadlessAuth": false,
    "ManualSessionCookie": "JSESSIONID=your-session-id",
    "DefaultBranchId": 1,
    "ForcedBranchId": null,
    "Encoding": "cp1254"
  }
}
```

### Key Settings
- **UseTokenAuth**: Use token-based auth instead of cookies
- **UseHeadlessAuth**: Use headless browser for session acquisition
- **ManualSessionCookie**: Manually set session cookie (for testing)
- **DefaultBranchId**: Default branch to use
- **ForcedBranchId**: Force specific branch (overrides default)

---

## 📊 Response Format

### Stock Card Creation Request
```json
{
  "kartAdi": "Test Ürünü",
  "kartKodu": "00013225",
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "olcumBirimiId": 1,
  "baslangicTarihi": "06/04/2022",
  "kartTuru": 1,
  "kategoriAgacKod": null,
  "barkod": "8888888",
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

### Stock Card Creation Response
```json
{
  "skartId": 79409,
  "error": false,
  "message": "00013225 - Test Ürünü stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

### Sync Result Response
```json
{
  "syncType": "PRODUCT_STOCK_CARD",
  "processedRecords": 150,
  "successfulRecords": 145,
  "failedRecords": 2,
  "skippedRecords": 3,
  "isSuccess": true,
  "message": "Sync completed: 145 successful, 2 failed, 3 skipped",
  "errors": [
    "PRD-002: Luca API error message",
    "PRD-003: Another error"
  ]
}
```

---

## 🐛 Troubleshooting

### Issue: "HTML response" errors
**Cause**: Session expired or cookie invalid  
**Solution**:
1. Check `logs/luca-raw.log` for HTML preview
2. Verify `ManualSessionCookie` in config
3. Check Luca server is running
4. Restart application to force re-login

### Issue: "Stok kartı bulunamadı" (Stock card not found)
**Cause**: ListStockCardsAsync returned empty list  
**Solution**:
1. Check session is valid: `🔐 Login SUCCESS` in logs
2. Verify branch is selected: `✅ Branch selected` in logs
3. Check Luca has stock cards: `📋 ListStockCardsAsync başlıyor`

### Issue: "Duplicate" errors
**Cause**: Stock card already exists in Luca  
**Solution**:
1. This is expected behavior - duplicates are skipped
2. Check `⚠️ Stok kartı zaten mevcut` in logs
3. If you need to update, manually edit in Luca (API doesn't support updates)

### Issue: Timeout errors
**Cause**: Luca server slow or network issue  
**Solution**:
1. Increase timeout in `ListStockCardsAsync` (currently 20 seconds)
2. Check network connectivity to Luca server
3. Check Luca server logs for errors

---

## 📈 Performance Tuning

### Batch Size
```csharp
// In SendStockCardsAsync (line 3160)
const int batchSize = 50; // Increase for faster processing, decrease for stability
```

### Rate Limiting
```csharp
// In SendStockCardsAsync (line 3161)
const int rateLimitDelayMs = 500; // Increase if Luca API throttles
```

### Timeout
```csharp
// In ListStockCardsAsync (line 2630)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // Increase if slow network
```

---

## 🔐 Security Notes

1. **Session Cookie**: Never commit real session cookies to version control
2. **Credentials**: Use environment variables or secure config for credentials
3. **HTTPS**: Always use HTTPS in production
4. **Logging**: Be careful with sensitive data in logs (passwords, tokens)

---

## 📞 Support

### Check Status
```bash
# Check if API is running
curl http://localhost:5000/health

# Check logs
tail -f logs/luca-raw.log

# Run tests
dotnet test tests/Katana.Tests/Services/LucaServiceSessionRecoveryTests.cs
```

### Common Commands
```bash
# Build
dotnet build Katana.Integration.sln

# Run
dotnet run --project src/Katana.API/Katana.API.csproj

# Test
dotnet test tests/Katana.Tests/

# Clean logs
rm -rf logs/*
```

---

## 📚 Related Documentation

- `LUCA_SESSION_MANAGEMENT_IMPLEMENTATION.md` - Detailed implementation guide
- `src/Katana.Infrastructure/APIClients/LucaService.cs` - Source code
- `tests/Katana.Tests/Services/LucaServiceSessionRecoveryTests.cs` - Unit tests
- `scripts/test-luca-session-recovery.ps1` - Integration tests

