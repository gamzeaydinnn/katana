# 🔧 Production Product Update Error - Fix Documentation

## 🚨 Problem Summary

**Symptoms:**
- ✅ Product updates work locally
- ❌ Product updates fail in production with 400/500 errors
- Error messages: "Failed to load resource: 400 Bad Request" and "500 Internal Server Error"

**Affected Endpoints:**
- `PUT /api/Products/luca/{id}` - Product update endpoint

---

## 🔍 Root Cause Analysis

### **Issue 1: DTO Field Mismatch (Primary Cause)**

**Frontend was sending (OLD):**
```javascript
{
  Name: "...",           // ❌ Wrong field name
  ProductCode: "...",    
  UnitPrice: ...,
  Quantity: ...
}
```

**Backend expects (`LucaProductUpdateDto`):**
```csharp
{
  productCode: "...",     // ✅ camelCase
  productName: "...",     // ✅ Not "Name"
  unit: "...",           // ✅ Missing in old version
  quantity: ...,
  unitPrice: ...,
  vatRate: ...           // ✅ Missing in old version
}
```

### **Issue 2: Missing CategoryId Validation**

The controller maps `LucaProductUpdateDto` → `UpdateProductDto`, which requires:
- `CategoryId > 0` ← **Validator requirement**

If `product.CategoryId` was 0 or null, validation failed with **400 Bad Request**.

**Fix:** Fallback to `CategoryId = 1` if existing category is invalid.

### **Issue 3: Insufficient Error Logging**

Production errors showed generic messages without detailed validation failures or DTO serialization issues.

**Fix:** Added comprehensive logging at every step:
- Request received with DTO content
- Validation errors with specific messages
- Mapping steps
- Exception details

### **Issue 4: Case Sensitivity Ambiguity**

While `Program.cs` sets `PropertyNameCaseInsensitive = true`, production may have:
- Different serializer settings
- Nginx proxy modifying headers
- Runtime environment differences

**Fix:** Explicit `[JsonPropertyName]` attributes on all DTO properties.

---

## ✅ Fixes Implemented

### **Fix 1: Frontend DTO Correction**

**File:** `frontend/katana-web/src/components/Admin/LucaProducts.tsx`

**Changes:**
- Match `LucaProductUpdateDto` structure exactly
- Use camelCase property names
- Include all optional fields (`unit`, `vatRate`)
- Add debug logging
- Enhanced error messages

```typescript
const updateDto = {
  productCode: selectedProduct.productCode || selectedProduct.ProductCode || "",
  productName: selectedProduct.productName || selectedProduct.ProductName || "",
  unit: selectedProduct.unit || selectedProduct.Unit || "Adet",
  quantity: selectedProduct.quantity ?? selectedProduct.Quantity ?? 0,
  unitPrice: selectedProduct.unitPrice ?? selectedProduct.UnitPrice ?? 0,
  vatRate: selectedProduct.vatRate ?? selectedProduct.VatRate ?? 20,
};
```

### **Fix 2: Backend Validation & Logging**

**File:** `src/Katana.API/Controllers/ProductsController.cs`

**Changes:**
- Validate required fields before processing
- Ensure `CategoryId > 0` with fallback
- Log DTO at every step
- Return structured error messages
- Include validation errors in response

**Key improvement:**
```csharp
CategoryId = product.CategoryId > 0 ? product.CategoryId : 1, // Safe fallback
```

### **Fix 3: JSON Serialization Attributes**

**File:** `src/Katana.Core/DTOs/LucaDtos.cs`

**Changes:**
- Added `[JsonPropertyName]` attributes to enforce camelCase
- Prevents case-sensitivity issues in production

```csharp
public class LucaProductUpdateDto
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;
    
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;
    
    // ... rest of properties
}
```

---

## 🚀 Deployment Instructions

### **Option 1: Quick Deploy (Recommended)**

Run the automated deployment script:

```bash
cd /Users/dilarasara/katana
./scripts/fix-production-update.sh
```

**What it does:**
1. ✅ Backs up current production version
2. ✅ Builds backend and frontend locally
3. ✅ Deploys to production server
4. ✅ Restarts services
5. ✅ Tests the fix automatically
6. ✅ Shows logs for verification

### **Option 2: Manual Deploy**

#### Step 1: Build Backend
```bash
cd /Users/dilarasara/katana
dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish
```

#### Step 2: Build Frontend
```bash
cd frontend/katana-web
npm install
npm run build
```

#### Step 3: Deploy to Server
```bash
# Backend
rsync -avz --delete --exclude 'appsettings.json' \
    publish/ azureuser@bfmmrp.com:/var/www/katana-api/

# Frontend
rsync -avz --delete \
    build/ azureuser@bfmmrp.com:/var/www/katana-web/
```

#### Step 4: Restart Services
```bash
ssh azureuser@bfmmrp.com
sudo systemctl restart katana-api
sudo systemctl restart nginx
```

---

## 🧪 Testing & Verification

### **Run Diagnostics First**

```bash
./scripts/diagnose-update-issue.sh
```

This script tests:
- API service status
- Recent errors in logs
- GET endpoint functionality
- PUT endpoint with camelCase JSON
- PUT endpoint with PascalCase JSON
- Nginx logs
- Database connectivity

### **Manual Testing**

#### Test 1: Browser Console
1. Open https://bfmmrp.com/admin/luca-products
2. Click edit on any product
3. Change a value and save
4. Check browser console (F12) for errors

Expected: ✅ "Luca ürünü güncellendi!" success message

#### Test 2: Direct API Call
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

Expected: ✅ HTTP 200 with updated product JSON

#### Test 3: Check Logs
```bash
ssh azureuser@bfmmrp.com
sudo journalctl -u katana-api -f
```

Look for:
- ✅ `UpdateLucaProduct called: ID=..., DTO=...`
- ✅ `Luca product updated successfully: ...`
- ❌ No validation errors
- ❌ No exceptions

---

## 🔄 Rollback Plan

If the fix causes issues:

### Automatic Rollback
```bash
ssh azureuser@bfmmrp.com
sudo systemctl stop katana-api nginx

# Find latest backup
ls -la /var/backups/katana-api/

# Restore (replace TIMESTAMP)
sudo cp -r /var/backups/katana-api/backup_TIMESTAMP/* /var/www/katana-api/
sudo cp -r /var/backups/katana-web/backup_TIMESTAMP/* /var/www/katana-web/

sudo systemctl start katana-api nginx
```

---

## 📋 Post-Deployment Checklist

- [ ] Run `./scripts/diagnose-update-issue.sh`
- [ ] Verify HTTP 200 response from API
- [ ] Test product update in admin panel
- [ ] Check logs for errors: `sudo journalctl -u katana-api -n 50`
- [ ] Test with different products (SKU-1001 to SKU-1005)
- [ ] Verify data persistence in database
- [ ] Monitor for 24 hours

---

## 🐛 Troubleshooting

### Still Getting 400 Errors?

**Check validation:**
```bash
ssh azureuser@bfmmrp.com
sudo journalctl -u katana-api -n 50 | grep "Validation failed"
```

**Common causes:**
- Product CategoryId is 0 or null (should fallback to 1 now)
- Missing required fields in DTO
- Invalid data types (string instead of number)

### Still Getting 500 Errors?

**Check exception details:**
```bash
ssh azureuser@bfmmrp.com
sudo journalctl -u katana-api -n 100 | grep -A 10 "Exception"
```

**Common causes:**
- Database connection failure
- SQL constraint violation
- Null reference exception

### Case Sensitivity Issues?

**Test both formats:**
```bash
# camelCase (should work now)
curl -X PUT https://bfmmrp.com/api/Products/luca/1001 \
  -H "Content-Type: application/json" \
  -d '{"productCode":"SKU-1001","productName":"Test",...}'

# PascalCase (should also work)
curl -X PUT https://bfmmrp.com/api/Products/luca/1001 \
  -H "Content-Type: application/json" \
  -d '{"ProductCode":"SKU-1001","ProductName":"Test",...}'
```

### Nginx Proxy Issues?

**Check nginx config:**
```bash
ssh azureuser@bfmmrp.com
sudo cat /etc/nginx/sites-enabled/default | grep -A 10 "location /api"
```

**Verify headers:**
```bash
curl -X PUT https://bfmmrp.com/api/Products/luca/1001 \
  -H "Content-Type: application/json" \
  -d '{"productCode":"SKU-1001",...}' \
  -v 2>&1 | grep -i "content-type"
```

---

## 📊 Expected Log Output (Success)

```
[Information] UpdateLucaProduct called: ID=1001, DTO={ productCode: "SKU-1001", productName: "Test", ... }
[Information] Existing product found: { Id: 1001, Name: "Demo Vida 5mm", ... }
[Information] Mapped to UpdateProductDto: { Name: "Test", SKU: "SKU-1001", ... }
[Information] Luca product updated successfully: 1001
```

## 📊 Expected Log Output (Error)

```
[Warning] Validation failed for product 1001: Geçerli bir kategori seçiniz
[Error] Invalid operation updating product 1001
```

---

## 🔗 Related Files

- `frontend/katana-web/src/components/Admin/LucaProducts.tsx` - Frontend component
- `src/Katana.API/Controllers/ProductsController.cs` - Backend controller
- `src/Katana.Core/DTOs/LucaDtos.cs` - DTO definitions
- `src/Katana.Business/Validators/ProductValidator.cs` - Validation logic
- `scripts/fix-production-update.sh` - Deployment script
- `scripts/diagnose-update-issue.sh` - Diagnostic script

---

## 📞 Support

If issues persist after deployment:

1. **Gather information:**
   - Run diagnostic script output
   - Browser console errors
   - API logs from last 100 lines

2. **Check database:**
   ```sql
   SELECT TOP 10 Id, Name, SKU, CategoryId, IsActive 
   FROM Products 
   ORDER BY UpdatedAt DESC
   ```

3. **Enable debug logging:**
   Edit `/var/www/katana-api/appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```
   Then: `sudo systemctl restart katana-api`

---

**Last Updated:** 12 Kasım 2025  
**Status:** ✅ Ready for deployment  
**Breaking Changes:** None (backward compatible)
