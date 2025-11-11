# 🚀 Quick Deployment Guide - Product Update Fix

## ⚡ Fast Track (5 Minutes)

### 1. Run Diagnostic (Optional but Recommended)
```bash
cd /Users/dilarasara/katana
./scripts/diagnose-update-issue.sh
```

### 2. Deploy the Fix
```bash
./scripts/fix-production-update.sh
```

### 3. Test in Browser
1. Open: https://bfmmrp.com/admin/luca-products
2. Edit any product
3. Save and verify success

---

## 🔍 What Was Fixed

| Issue | Before | After |
|-------|--------|-------|
| **DTO Fields** | Sent `Name`, `ProductCode` (wrong) | Sends `productName`, `productCode` ✅ |
| **Missing Fields** | No `unit`, `vatRate` | All fields included ✅ |
| **CategoryId** | Could be 0/null → 400 error | Fallback to 1 ✅ |
| **Logging** | Generic errors | Detailed validation logs ✅ |
| **Serialization** | No explicit attributes | `[JsonPropertyName]` added ✅ |

---

## 🧪 Quick Test Commands

### Test Update Endpoint
```bash
curl -X PUT https://bfmmrp.com/api/Products/luca/1001 \
  -H "Content-Type: application/json" \
  -d '{
    "productCode": "SKU-1001",
    "productName": "Test Product",
    "unit": "Adet",
    "quantity": 100,
    "unitPrice": 15.50,
    "vatRate": 20
  }'
```
**Expected:** HTTP 200 + Product JSON

### Check Logs
```bash
ssh azureuser@bfmmrp.com 'sudo journalctl -u katana-api -n 30 | grep -i "update\|error"'
```

### Verify Service Status
```bash
ssh azureuser@bfmmrp.com 'sudo systemctl status katana-api --no-pager'
```

---

## 🔄 Rollback (If Needed)

```bash
ssh azureuser@bfmmrp.com
sudo systemctl stop katana-api
sudo cp -r /var/backups/katana-api/backup_* /var/www/katana-api/
sudo systemctl start katana-api
```

---

## 📞 Troubleshooting

| Error Code | Likely Cause | Solution |
|------------|-------------|----------|
| **400** | Validation failure | Check logs: `journalctl -u katana-api -n 50 \| grep "Validation"` |
| **404** | Product not in DB | Use existing product ID from GET /api/Products/luca |
| **500** | Database/exception | Check: `journalctl -u katana-api -n 100 \| grep Exception` |

---

## ✅ Success Indicators

- ✅ Deployment script shows "✅ Update endpoint working correctly!"
- ✅ Browser console shows no errors after save
- ✅ Product data updates in table
- ✅ Logs show "Luca product updated successfully: {id}"

---

**Full Documentation:** See `PRODUCTION_UPDATE_FIX.md`
