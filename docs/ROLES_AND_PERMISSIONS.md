# Rol Bazlı Yetkilendirme (RBAC) - Katana Entegrasyon Sistemi

## Mevcut Roller

Sistemde 3 ana rol bulunmaktadır:

### 1. **Admin** (En Yüksek Yetki)
Sistemin tüm alanlarına tam erişim yetkisi vardır.

**Erişebildiği Alanlar:**
- ✅ Kullanıcı Yönetimi (CRUD - Oluşturma, Okuma, Güncelleme, Silme)
  - Yeni kullanıcı ekleme
  - Kullanıcıları düzenleme (rol, şifre, email, aktif/pasif)
  - Kullanıcıları silme
  - Rol atama/değiştirme
- ✅ Admin Paneli (Tüm Sekmeler)
  - İstatistikler
  - Siparişler
  - Katana/Luca Ürün Listeleri
  - Stok Yönetimi
  - Hatalı Kayıtlar
  - Veri Düzeltme
  - Kullanıcılar
  - Loglar
  - Ayarlar
- ✅ Senkronizasyon İşlemleri
  - Manuel senkronizasyon başlatma
  - Senkronizasyon geçmişi
- ✅ Müşteri/Tedarikçi/Kategori Yönetimi (CRUD)
- ✅ Veri Düzeltme ve Onaylama
- ✅ Pending Stock Adjustments (Onaylama/Reddetme)
- ✅ Ürün Güncelleme (Katana & Luca)
- ✅ Raporlar

**API Endpoint Örnekleri:**
```
POST   /api/Users                    ✅ Admin only
PUT    /api/Users/{id}               ✅ Admin only
DELETE /api/Users/{id}               ✅ Admin only
POST   /api/Sync/start               ✅ Admin only
POST   /api/adminpanel/*             ✅ Admin only (bazıları +StockManager)
PUT    /api/Products/{id}            ✅ Admin + StockManager
DELETE /api/Customers/{id}           ✅ Admin only
```

---

### 2. **Manager** (Orta Seviye Yetki)
Kullanıcı yönetiminde görüntüleme yetkisi var, ancak değişiklik yapamaz. Diğer alanlarda sınırlı erişim.

**Erişebildiği Alanlar:**
- ✅ Kullanıcı Listesini Görüntüleme (sadece okuma)
  - Kullanıcıları görebilir
  - Ekleyemez, düzenleyemez, silemez
- ⚠️ Admin Paneli (sınırlı)
  - Kullanıcılar sekmesini görüntüleme (yazma yok)
  - Bazı raporları görüntüleme
- ⚠️ Stok Yönetimi (StockManager ile aynı yetki varsa)
  - Ürün listelerini görüntüleme
  - Stok güncelleme (eğer StockManager rolü de verilmişse)

**API Endpoint Örnekleri:**
```
GET    /api/Users                    ✅ Manager + Admin
POST   /api/Users                    ❌ Admin only
PUT    /api/Users/{id}               ❌ Admin only
DELETE /api/Users/{id}               ❌ Admin only
GET    /api/Products                 ✅ (AllowAnonymous - geçici)
PUT    /api/Products/{id}            ❌ Admin + StockManager only
```

**Not:** Manager rolü şu anda esas olarak Kullanıcı Yönetimi'nde read-only erişim için tasarlanmış. Diğer alanlarda genelde Staff ile benzer yetkilere sahip.

---

### 3. **Staff** (Temel Kullanıcı / En Düşük Yetki)
Sadece genel görüntüleme ve kendi işlemleriyle sınırlı yetkiler.

**Erişebildiği Alanlar:**
- ✅ Dashboard (Genel İstatistikler - geçici AllowAnonymous)
- ✅ Canlı Stok Görüntüleme
- ✅ Raporları Görüntüleme (sadece okuma)
- ✅ Kendi Profil Bilgilerini Görüntüleme
- ❌ Kullanıcı Yönetimi (hiçbir erişim yok)
- ❌ Admin Paneli (erişim yok)
- ❌ Senkronizasyon İşlemleri (başlatamaz)
- ❌ Ürün/Müşteri/Kategori Ekleme/Güncelleme/Silme

**API Endpoint Örnekleri:**
```
GET    /api/Users                    ❌ Manager + Admin only
GET    /api/Dashboard                ✅ (AllowAnonymous - geçici)
GET    /api/Products                 ✅ (AllowAnonymous - geçici)
PUT    /api/Products/{id}            ❌ Admin + StockManager only
POST   /api/Sync/start               ❌ Admin only
```

---

## Özel Rol: **StockManager**

Bazı endpoint'lerde `Admin,StockManager` birlikte geçiyor. Bu, stok yönetimi odaklı işlemler için özel bir rol:

**Yetkileri:**
- ✅ Ürün güncelleme (Katana/Luca)
- ✅ Stok ayarlama/onaylama
- ✅ Pending stock adjustments görüntüleme ve onaylama
- ✅ Hatalı kayıtları düzeltme

**API Endpoint Örnekleri:**
```
POST   /api/adminpanel/pending-adjustments/{id}/approve  ✅ Admin + StockManager
PUT    /api/Products/{id}                                ✅ Admin + StockManager
GET    /api/Reports/stock                                ✅ Admin + StockManager
```

**Not:** Kullanıcı oluştururken "StockManager" rolü doğrudan mevcut değil; genelde Admin veya Manager + ek yetki olarak düşünülebilir. Sisteminizde bu rolü kullanmak isterseniz, kullanıcı oluştururken "Admin", "Manager" veya "Staff" yerine "StockManager" yazabilirsiniz.

---

## Frontend (UI) Davranışı

### Kullanıcılar Sekmesi (Admin Panel)

- **Admin:** 
  - Tüm alanları görebilir ve düzenleyebilir
  - "Kullanıcı Ekle" butonu aktif
  - Satırlarda "Düzenle" ve "Sil" butonları aktif
  
- **Manager:** 
  - Kullanıcı listesini görebilir
  - "Kullanıcı Ekle" formu gizli (isAdmin=false)
  - Satırlarda "Düzenle" ve "Sil" butonları pasif/disabled
  - Üstte bilgilendirme mesajı: "Bu bölümde yalnızca Admin kullanıcılar değişiklik yapabilir."

- **Staff:** 
  - Admin Paneli'ne erişemez (token olsa bile backend 403 döner)
  - Eğer URL'den girmeye çalışırsa, yetki hatası alır

### Diğer Bölümler

- **Canlı Stok, Dashboard, Raporlar:** Tüm roller görüntüleyebilir (geçici olarak AllowAnonymous)
- **Senkronizasyon:** Sadece Admin tetikleyebilir
- **Admin Paneli Sekmeleri:** Çoğu sadece Admin, bazıları Admin+StockManager

---

## Öneriler

### 1. Rol İsimlendirmesi ve Kullanım
Şu anki rol yapısı:
```
Admin > Manager > Staff
```

Eğer stok odaklı işlemler için özel yetki istiyorsanız:
```
Admin > StockManager > Manager > Staff
```

### 2. Frontend'de Rol Kontrolü
`UsersManagement.tsx` içinde JWT'den rolleri çıkarıp UI'da gösteriyoruz:
```typescript
const rolesFromToken = getJwtRoles(decodeJwtPayload(token));
const isAdmin = rolesFromToken.includes("admin");
```

### 3. Backend'de Rol Kontrolü
Controllers'ta `[Authorize(Roles = "Admin,Manager")]` ile kontrol ediliyor.

### 4. Rol Değiştirme
Admin, başka bir kullanıcının rolünü değiştirebilir:
- PUT /api/Users/{id}/role → body: `"Admin"` | `"Manager"` | `"Staff"`
- Veya PUT /api/Users/{id} ile tam güncelleme

---

## Hızlı Rol Karşılaştırma Tablosu

| Yetki                                    | Admin | Manager | Staff | StockManager |
|------------------------------------------|-------|---------|-------|--------------|
| Kullanıcı listesini görüntüleme          | ✅    | ✅      | ❌    | ❌           |
| Kullanıcı ekleme                         | ✅    | ❌      | ❌    | ❌           |
| Kullanıcı düzenleme/silme                | ✅    | ❌      | ❌    | ❌           |
| Rol atama/değiştirme                     | ✅    | ❌      | ❌    | ❌           |
| Admin Paneli erişim                      | ✅    | ⚠️     | ❌    | ✅           |
| Senkronizasyon başlatma                  | ✅    | ❌      | ❌    | ❌           |
| Ürün güncelleme                          | ✅    | ❌      | ❌    | ✅           |
| Pending stock adjustments onaylama      | ✅    | ❌      | ❌    | ✅           |
| Müşteri/Tedarikçi/Kategori yönetimi      | ✅    | ❌      | ❌    | ❌           |
| Dashboard/Raporları görüntüleme          | ✅    | ✅      | ✅    | ✅           |
| Canlı stok görüntüleme                   | ✅    | ✅      | ✅    | ✅           |

---

## Örnek Kullanım Senaryoları

### Senaryo 1: Yeni Çalışan Ekleme
1. Admin olarak giriş yapın
2. Admin Paneli > Kullanıcılar sekmesine gidin
3. Formu doldurun:
   - Kullanıcı Adı: `ali.veli`
   - E-posta: `ali.veli@firma.com`
   - Şifre: `GuvenliSifre123!`
   - Rol: `Staff`
4. "Kullanıcı Ekle" butonuna tıklayın
5. Ali Veli artık sadece görüntüleme yetkisiyle giriş yapabilir

### Senaryo 2: Stok Yöneticisi Oluşturma
1. Admin olarak giriş yapın
2. Yeni kullanıcı oluştururken Rol: `StockManager` seçin
3. Bu kullanıcı artık ürün güncelleme ve stok onaylama işlemlerini yapabilir

### Senaryo 3: Yönetici Rolü Güncelleme
1. Admin olarak giriş yapın
2. Kullanıcılar listesinde "Düzenle" butonuna tıklayın
3. Rol: `Manager` olarak değiştirin
4. Kaydet
5. Kullanıcı artık kullanıcı listesini görebilir ama değişiklik yapamaz

---

## Güvenlik Notları

1. **Şifre Gereksinimleri:**
   - En az 6 karakter (backend validation)
   - Önerilen: büyük/küçük harf, rakam, özel karakter karışımı

2. **Token Süresi:**
   - JWT token varsayılan 5 dakika (300 saniye) geçerli
   - Süre dolunca tekrar giriş gerekli

3. **Rol Değişiklikleri:**
   - Sadece Admin rol atayabilir/değiştirebilir
   - Kendi rolünü değiştiremesin diye ek kontrol eklenebilir (şu an yok)

4. **Audit Logging:**
   - Tüm kullanıcı ekleme/silme/güncelleme işlemleri AuditLogs tablosuna kaydedilir
   - Kimin ne zaman hangi kullanıcıyı değiştirdiği takip edilebilir

---

## Sorun Giderme

### "403 Forbidden" Hatası
- Kullanıcınızın rolü, erişmeye çalıştığı endpoint için yeterli değil
- Backend loglarını kontrol edin: `[Authorize(Roles = "...")]`
- Gerekirse admin'den rol yükseltme isteyin

### "401 Unauthorized" Hatası
- Token süresi dolmuş veya geçersiz
- Çıkış yapıp tekrar giriş yapın
- localStorage'daki token'ı kontrol edin

### UI'da Buton Görünmüyor
- Rolünüz Admin değilse, bazı butonlar gizli/disabled olur
- Bu tasarım gereği; backend'de de aynı kontrol var

---

## Gelecek İyileştirmeler

1. **Granular Permissions:** Rol bazlı yerine, permission-based yetkilendirme (örn. `can_edit_products`, `can_approve_stock`)
2. **Rol Hiyerarşisi:** Manager, Staff yetkilerini de kapsasın (role inheritance)
3. **Kullanıcı Grupları:** Departman bazlı yetki yönetimi
4. **Aktivite Logları:** Kullanıcıların yaptığı her aksiyonun detaylı kaydı
5. **2FA (Two-Factor Authentication):** Ekstra güvenlik katmanı

---

Bu dokümantasyon, mevcut sistemdeki rol yapısını açıklar. Değişiklik yapmak isterseniz, backend'deki `[Authorize(Roles = "...")]` attribute'larını ve frontend'deki `isAdmin` kontrollerini güncelleyin.
