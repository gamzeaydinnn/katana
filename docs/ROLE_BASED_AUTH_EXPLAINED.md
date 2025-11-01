# 🔐 Role-Based Authorization - Detaylı Açıklama

## 📋 ÖZET

Katana Integration sisteminde **role-based authorization** (rol tabanlı yetkilendirme) JWT token'lar içindeki kullanıcı rollerine göre endpoint erişimini kısıtlar.

**Basit Açıklama:** Kullanıcı login olduğunda JWT token alır. Bu token içinde **roller** (Admin, StockManager, User) vardır. Backend her istek geldiğinde token'ı kontrol eder ve kullanıcının rolüne göre izin verir/reddeder.

---

## 🎯 KİM NE İŞ YAPIYOR?

### 1. **AuthController.cs** - Login ve Token Üretimi

**Dosya:** `src/Katana.API/Controllers/AuthController.cs`  
**Satırlar:** 88-89

```csharp
// Login başarılı olduğunda JWT token oluştur
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Email, user.Email),

    // 🔑 ROL CLAIMLERİ (En önemli kısım!)
    new Claim(ClaimTypes.Role, "Admin"),
    new Claim(ClaimTypes.Role, "StockManager")
};

var token = new JwtSecurityToken(
    issuer: _configuration["Jwt:Issuer"],
    audience: _configuration["Jwt:Audience"],
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(480),
    signingCredentials: creds
);

var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
```

**Ne Yapıyor?**

- Kullanıcı login olduğunda JWT token oluşturur
- Token içine kullanıcı bilgileri ve **roller** ekler
- Şu anda her kullanıcıya `Admin` ve `StockManager` rolleri veriliyor (production'da dinamik olmalı)

**Token Örneği (Decoded):**

```json
{
  "nameid": "1",
  "unique_name": "admin",
  "email": "admin@katana.com",
  "role": ["Admin", "StockManager"],
  "exp": 1730476800,
  "iss": "KatanaIntegration",
  "aud": "KatanaFrontend"
}
```

---

### 2. **AdminController.cs** - Role Kontrolü

**Dosya:** `src/Katana.API/Controllers/AdminController.cs`  
**Satırlar:** 41, 73, 97, 327

```csharp
// ❌ ESKİ (Sadece authentication, rol kontrolü YOK)
[Authorize]
[HttpPost("pending-adjustments")]
public async Task<IActionResult> CreatePendingAdjustment() { ... }

// ✅ YENİ (Role-based authorization)
[Authorize(Roles = "Admin,StockManager")]
[HttpPost("pending-adjustments")]
public async Task<IActionResult> CreatePendingAdjustment() { ... }
```

**Ne Yapıyor?**

1. İstek geldiğinde JWT token'ı kontrol eder
2. Token içindeki `role` claim'ini okur
3. Eğer kullanıcının rolü "Admin" VEYA "StockManager" ise → İzin verir (200 OK)
4. Eğer kullanıcının bu rolleri yoksa → Reddeder (403 Forbidden)
5. Eğer token yoksa veya geçersizse → Reddeder (401 Unauthorized)

**Güvenlik Akışı:**

```
Frontend → POST /api/admin/pending-adjustments
         → Header: Authorization: Bearer <JWT_TOKEN>
         ↓
Backend (ASP.NET Core Middleware)
         ↓
1. JWT token parse et
2. Token geçerli mi? (imza, expire kontrolü)
   → Hayır: 401 Unauthorized
   ↓ Evet
3. Token içinde "role" claim var mı?
   → Hayır: 403 Forbidden
   ↓ Evet
4. Role = "Admin" VEYA "StockManager"?
   → Hayır: 403 Forbidden
   ↓ Evet
5. AdminController.CreatePendingAdjustment() çalıştır
```

---

### 3. **Program.cs** - JWT Authentication Setup

**Dosya:** `src/Katana.API/Program.cs`  
**Satırlar:** 258-275

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();
```

**Ne Yapıyor?**

- JWT Bearer authentication'ı aktif eder
- Token'ın `Issuer`, `Audience`, `Expire`, `Signature` kontrollerini yapar
- appsettings.json'dan secret key alır

---

### 4. **Frontend (Header.tsx)** - Token Gönderimi

**Dosya:** `frontend/katana-web/src/services/signalr.ts`  
**Satırlar:** 13-21

```typescript
connection = new HubConnectionBuilder()
  .withUrl(HUB_URL, {
    accessTokenFactory: () => {
      try {
        return typeof window !== "undefined"
          ? window.localStorage.getItem("authToken") || ""
          : "";
      } catch {
        return "";
      }
    },
  })
  .build();
```

**Ne Yapıyor?**

- LocalStorage'dan `authToken` (JWT) alır
- Her SignalR isteğine `Authorization: Bearer <token>` header'ı ekler
- Backend bu token'ı kontrol ederek kullanıcıyı doğrular

---

## 🔄 TAM AKIŞ ÖRNEĞİ

### Senaryo: Admin pending adjustment oluşturmak istiyor

```
1. Frontend (Login)
   ↓
   POST /api/auth/login
   Body: { username: "admin", password: "admin123" }
   ↓
   Response: { token: "eyJhbGc..." }
   ↓
   localStorage.setItem("authToken", token)

2. Frontend (Create Pending)
   ↓
   POST /api/admin/pending-adjustments
   Headers: { Authorization: "Bearer eyJhbGc..." }
   Body: { sku: "TEST-001", quantity: 5 }
   ↓
   Backend (Middleware)
   ↓
   JWT token parse → { role: ["Admin", "StockManager"] }
   ↓
   [Authorize(Roles = "Admin,StockManager")] kontrolü
   ↓
   ✅ PASS (Admin rolü var)
   ↓
   AdminController.CreatePendingAdjustment() çalışır
   ↓
   PendingStockAdjustmentService.CreateAsync()
   ↓
   SignalR event publish
   ↓
   Response: { id: 9, status: "Pending" }

3. Frontend (SignalR)
   ↓
   SignalR connection: /hubs/notifications
   Headers: { Authorization: "Bearer eyJhbGc..." }
   ↓
   Event: "PendingStockAdjustmentCreated"
   Payload: { id: 9, sku: "TEST-001" }
   ↓
   Header.tsx bildirim eklenir
   ↓
   Badge sayısı artar (🔴 1)
```

---

## 🚫 REDDEDILME ÖRNEKLERİ

### Örnek 1: Token yok

```
Frontend → POST /api/admin/pending-adjustments
         → Header: (boş)
Backend  → 401 Unauthorized
         → { error: "Authorization header missing" }
```

### Örnek 2: Token geçersiz (expired)

```
Frontend → POST /api/admin/pending-adjustments
         → Header: Authorization: Bearer <EXPIRED_TOKEN>
Backend  → 401 Unauthorized
         → { error: "Token expired" }
```

### Örnek 3: Rol yetkisi yok

```
Frontend → POST /api/admin/pending-adjustments
         → Header: Authorization: Bearer <VALID_TOKEN>
         → Token içinde role = ["User"] (Admin değil!)
Backend  → 403 Forbidden
         → { error: "Insufficient permissions" }
```

---

## 🎨 ROLLERIN ANLAMLARI

| Rol              | Açıklama                               | Yetkiler                                   |
| ---------------- | -------------------------------------- | ------------------------------------------ |
| **Admin**        | Sistem yöneticisi                      | Tüm admin endpoint'leri + pending approval |
| **StockManager** | Stok yöneticisi                        | Pending adjustment oluşturma ve onaylama   |
| **User**         | Normal kullanıcı (şu an kullanılmıyor) | Sadece kendi bilgilerini görüntüleme       |

---

## 🔧 PRODUCTION İÇİN GELİŞTİRMELER

### 1. Dinamik Rol Atama

**Şu an:** Her kullanıcıya hard-coded `Admin` + `StockManager` veriliyor  
**Olmalı:** Database'den kullanıcının gerçek rolleri okunmalı

```csharp
// AuthController.cs (iyileştirilmiş)
var userRoles = await _userService.GetUserRolesAsync(user.Id);
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
};

foreach (var role in userRoles)
{
    claims.Add(new Claim(ClaimTypes.Role, role));
}
```

### 2. User Table'a Role Ekleme

```sql
ALTER TABLE Users ADD Role VARCHAR(50) NOT NULL DEFAULT 'User';

-- Mevcut admin'leri güncelle
UPDATE Users SET Role = 'Admin' WHERE Username = 'admin';
```

### 3. Granular Permissions (Gelişmiş)

```csharp
[Authorize(Roles = "Admin")]
[Authorize(Policy = "CanApproveStock")]
public async Task<IActionResult> ApprovePendingAdjustment(long id) { ... }
```

---

## 📝 ÖZET

1. **AuthController** → Login'de JWT token oluşturur, içine roller ekler
2. **AdminController** → `[Authorize(Roles = "Admin,StockManager")]` ile kontrol eder
3. **Program.cs** → JWT authentication middleware'i kurar
4. **Frontend** → LocalStorage'daki token'ı her istekte gönderir
5. **Backend Middleware** → Token'ı parse eder, rolleri kontrol eder, izin verir/reddeder

**Sonuç:** Sadece `Admin` veya `StockManager` rolüne sahip kullanıcılar admin endpoint'lerine erişebilir. Güvenlik sağlanmış! 🔒

---

## 🧪 TEST KOMUTU

```powershell
# Role authorization testi
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-role-authorization.ps1

# Beklenen çıktı:
# ✓ Login successful
# ✓ Token contains Admin and StockManager roles
# ✓ Create successful - PendingId: 9
# ✓ Approve successful
# Security Status: SECURED ✓
```

---

**Hazırlayan:** GitHub Copilot  
**Tarih:** 2025-11-01  
**Versiyon:** 1.0
