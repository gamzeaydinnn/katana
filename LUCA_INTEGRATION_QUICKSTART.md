# ✅ Luca/Koza Entegrasyon - Hızlı Başlangıç

## 🎯 Ne Yapıldı?

Postman'daki **tüm Luca Koza endpoint'leri** backend, frontend ve database ile tam uyumlu hale getirildi.

- ✅ **Backend:** 50+ endpoint LucaProxyController'a eklendi
- ✅ **Frontend:** lucaService.ts (800+ satır) oluşturuldu
- ✅ **Database:** Entity'ler zaten Luca-ready (değişiklik gereksiz)
- ✅ **Entegrasyon:** Backend-Frontend-DB tam uyumlu

---

## 📁 Yeni Eklenen Dosyalar

### Frontend
```
frontend/katana-web/src/services/
├── lucaService.ts              ✨ YENİ - Tüm Luca API çağrıları
└── api.ts                      🔄 GÜNCELLENDİ - lucaAPI bölümü eklendi
```

### Dokümantasyon
```
/
├── LUCA_INTEGRATION_COMPLETE_GUIDE.md  ✨ YENİ - Detaylı entegrasyon kılavuzu
└── LUCA_INTEGRATION_QUICKSTART.md      ✨ YENİ - Bu dosya
```

---

## 🚀 Nasıl Kullanılır?

### 1. Backend Zaten Hazır
LucaProxyController'daki tüm endpoint'ler kullanıma hazır:

```
POST /api/luca-proxy/login
POST /api/luca-proxy/customers/create
POST /api/luca-proxy/stock-cards/list
POST /api/luca-proxy/invoices/create
... (50+ endpoint)
```

### 2. Frontend'den Kullanım

#### Yöntem 1: lucaService.ts (Önerilen)
```typescript
import lucaService from '@/services/lucaService';

// Giriş
await lucaService.login();

// Müşteri oluştur
const customer = await lucaService.createCustomer({
  tip: 1,
  cariTipId: 5,
  kartKod: "0087",
  tanim: "Acme Corp",
  paraBirimKod: "TRY",
  // ...
});

// Stok listesi
const stockCards = await lucaService.listStockCards();

// Fatura oluştur
const invoice = await lucaService.createInvoice({
  belgeSeri: "A",
  belgeTarihi: "07/10/2025",
  // ...
});
```

#### Yöntem 2: lucaAPI (Kısa Syntax)
```typescript
import { lucaAPI } from '@/services/api';

// Giriş
await lucaAPI.login();

// Müşteri
const customers = await lucaAPI.customers.list();
await lucaAPI.customers.create({ ... });

// Stok
const stocks = await lucaAPI.stock.list();
await lucaAPI.stock.create({ ... });

// Fatura
const invoices = await lucaAPI.invoices.list();
await lucaAPI.invoices.create({ ... });
```

---

## 📊 Mevcut Endpoint'ler

### Giriş & Yetkilendirme
- `login()` - Luca'ya giriş
- `getBranches()` - Şube listesi
- `selectBranch()` - Şube seçimi

### Cari İşlemler
- `listCustomers()` - Müşteri listesi
- `createCustomer()` - Müşteri oluştur
- `listSuppliers()` - Tedarikçi listesi
- `createSupplier()` - Tedarikçi oluştur
- `listCustomerAddresses()` - Cari adresler
- `getCustomerRisk()` - Cari risk bilgileri

### Stok İşlemler
- `listStockCards()` - Stok kartları
- `createStockCard()` - Stok kartı oluştur
- `listStockCategories()` - Kategoriler
- `listStockCardPurchasePrices()` - Alış fiyatları
- `listStockCardSalesPrices()` - Satış fiyatları
- `listWarehouses()` - Depo listesi
- `createWarehouseTransfer()` - Depo transferi

### Sipariş İşlemler
- `listSalesOrders()` - Satış siparişleri
- `createSalesOrder()` - Satış siparişi oluştur
- `listPurchaseOrders()` - Satınalma siparişleri
- `createPurchaseOrder()` - Satınalma siparişi oluştur

### Fatura İşlemler
- `listInvoices()` - Fatura listesi
- `createInvoice()` - Fatura oluştur
- `getInvoicePdfLink()` - PDF linki
- `closeInvoice()` - Fatura kapat
- `deleteInvoice()` - Fatura sil

### Finans İşlemler
- `createCreditCardEntry()` - Kredi kartı girişi
- `listBanks()` - Banka kartları
- `listCashAccounts()` - Kasa kartları
- `listCustomerTransactions()` - Cari hareketler
- `createCustomerTransaction()` - Cari hareket oluştur

### Genel İşlemler
- `listMeasurementUnits()` - Ölçü birimleri
- `listTaxOffices()` - Vergi daireleri
- `listDocumentTypeDetails()` - Belge türleri
- `listBranchCurrencies()` - Para birimleri
- `listDynamicLovValues()` - Dinamik LOV değerleri

**Toplam:** 50+ fonksiyon kullanıma hazır

---

## 🔄 Veri Akışı

```
┌──────────────┐
│   Frontend   │ → lucaService.createCustomer()
└──────────────┘
       ↓
┌──────────────┐
│   Backend    │ → POST /api/luca-proxy/customers/create
└──────────────┘   → LucaProxyController
       ↓              → ILucaService
┌──────────────┐      → HttpClient
│  Luca API    │ → POST /EkleFinMusteriWS.do
└──────────────┘
       ↓
┌──────────────┐
│   Database   │ → Customer.LucaFinansalNesneId güncellenir
└──────────────┘   → Customer.IsSynced = true
```

---

## 🎨 UI Geliştirme (İsteğe Bağlı)

Eğer kullanıcı arayüzü eklemek isterseniz:

### Örnek: Müşteri Oluşturma Formu
```tsx
import lucaService from '@/services/lucaService';
import { useState } from 'react';

function CreateCustomerForm() {
  const [formData, setFormData] = useState({
    kartKod: '',
    tanim: '',
    vergiNo: '',
    // ...
  });

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    try {
      const result = await lucaService.createCustomer({
        tip: 1,
        cariTipId: 5,
        paraBirimKod: 'TRY',
        ...formData
      });
      
      alert('Müşteri oluşturuldu!');
    } catch (error) {
      alert('Hata: ' + error.message);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input 
        name="kartKod" 
        placeholder="Müşteri Kodu"
        onChange={(e) => setFormData({...formData, kartKod: e.target.value})}
      />
      <input 
        name="tanim" 
        placeholder="Müşteri Adı"
        onChange={(e) => setFormData({...formData, tanim: e.target.value})}
      />
      <button type="submit">Oluştur</button>
    </form>
  );
}
```

---

## 🔐 Güvenlik Notları

1. **Session Yönetimi**
   - Frontend: `X-Luca-Session` header ile session taşır
   - Backend: `LucaCookieJarStore` ile session izole eder
   - Session ID localStorage'da saklanır

2. **Authentication**
   - Backend: JWT token zorunlu
   - Frontend: Token otomatik eklenir (api.ts interceptor)
   - Luca credentials: backend appsettings.json'da güvenli

3. **CORS**
   - Frontend **asla** direkt Luca'ya bağlanmaz
   - Tüm istekler backend proxy üzerinden

---

## 📖 Detaylı Dokümantasyon

- **Tam Kılavuz:** [LUCA_INTEGRATION_COMPLETE_GUIDE.md](./LUCA_INTEGRATION_COMPLETE_GUIDE.md)
  - Mimari detayları
  - Tüm endpoint'lerin listesi
  - Kod örnekleri
  - Veri akışı diyagramları
  - Tip tanımları

- **Postman Koleksiyonu:** [Luca Koza.postman_collection.json](./Luca%20Koza.postman_collection.json)
  - 94 request
  - Tüm kategoriler

---

## ✅ Sistem Durumu

| Katman | Durum | Notlar |
|--------|-------|--------|
| Backend | ✅ Hazır | 50+ endpoint entegre |
| Frontend | ✅ Hazır | lucaService.ts + lucaAPI |
| Database | ✅ Hazır | Entity'ler Luca-ready |
| Entegrasyon | ✅ Tam Uyumlu | Backend-Frontend-DB senkron |
| UI | 🔲 İsteğe Bağlı | Gerektiğinde eklenebilir |

---

## 🎯 Sonraki Adımlar

1. ✅ **Backend entegrasyonu** → Tamamlandı
2. ✅ **Frontend servisleri** → Tamamlandı
3. ✅ **Database uyumu** → Tamamlandı
4. 🔲 **UI geliştirme** → İhtiyaç halinde (müşteri, stok, fatura formları)
5. 🔲 **E2E testler** → UI geliştirildikten sonra

---

## 💡 Hızlı Test

Terminal'de test etmek için:

```bash
# Frontend'i başlat
cd frontend/katana-web
npm start

# Backend'i başlat (ayrı terminal)
cd src/Katana.API
dotnet run

# Browser console'da test
const result = await lucaAPI.login();
console.log('Login result:', result);

const customers = await lucaAPI.customers.list();
console.log('Customers:', customers);
```

---

**Hazırlayan:** GitHub Copilot  
**Tarih:** 8 Aralık 2025  
**Versiyon:** 1.0.0

---

## 🆘 Destek

Sorunlarla karşılaşırsanız:
1. `LUCA_INTEGRATION_COMPLETE_GUIDE.md` dosyasına bakın
2. Backend loglarını kontrol edin (`ILogger<LucaProxyController>`)
3. Browser console'da network tab'ı inceleyin
4. Session ID'nin doğru taşındığını kontrol edin (`X-Luca-Session` header)
