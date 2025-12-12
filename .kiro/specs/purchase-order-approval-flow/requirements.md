# Requirements Document

## Introduction

Bu özellik, satınalma siparişlerinin admin tarafından onaylanması ve onaylandıktan sonra Katana sistemine stok olarak eklenmesi sürecini tanımlar. Şu anda siparişler direkt olarak listeleniyor ancak admin onay mekanizması frontend'de eksik. Backend'de zaten `Pending -> Approved -> Received` durum geçişi mevcut ve `Approved` durumuna geçildiğinde Katana'ya ürün ekleme/güncelleme işlemi yapılıyor.

## Glossary

- **Purchase Order (Satınalma Siparişi)**: Tedarikçiden ürün satın almak için oluşturulan sipariş
- **Admin**: Sistemi yöneten ve siparişleri onaylama yetkisine sahip kullanıcı
- **Katana**: Stok yönetim sistemi
- **Pending**: Onay bekleyen sipariş durumu
- **Approved**: Admin tarafından onaylanmış sipariş durumu
- **Received**: Teslim alınmış sipariş durumu
- **Status Badge**: Sipariş durumunu gösteren görsel etiket
- **Frontend**: Kullanıcı arayüzü (React/TypeScript)
- **Backend**: Sunucu tarafı API (C#/.NET)

## Requirements

### Requirement 1

**User Story:** Admin olarak, satınalma siparişlerini onaylamak istiyorum, böylece sadece onaylanan siparişler Katana sistemine stok olarak eklensin.

#### Acceptance Criteria

1. WHEN admin sipariş listesini görüntülediğinde THEN sistem her siparişin durumunu (Pending, Approved, Received) görsel olarak göstermeli
2. WHEN admin "Pending" durumundaki bir siparişi seçtiğinde THEN sistem sipariş detaylarını ve "Onayla" butonunu göstermeli
3. WHEN admin "Onayla" butonuna tıkladığında THEN sistem siparişin durumunu "Approved" olarak güncellemeli
4. WHEN sipariş "Approved" durumuna geçtiğinde THEN sistem arka planda Katana API'sine ürünleri eklemeli veya mevcut ürünlerin stoklarını artırmalı
5. WHEN admin "Approved" durumundaki bir siparişi görüntülediğinde THEN sistem "Teslim Alındı" butonunu göstermeli

### Requirement 2

**User Story:** Admin olarak, sipariş durumlarını filtreleyebilmek istiyorum, böylece onay bekleyen siparişleri kolayca bulabileyim.

#### Acceptance Criteria

1. WHEN admin sipariş listesinde filtre seçeneklerini görüntülediğinde THEN sistem "Tümü", "Beklemede", "Onaylandı", "Teslim Alındı" filtre seçeneklerini sunmalı
2. WHEN admin "Beklemede" filtresini seçtiğinde THEN sistem sadece "Pending" durumundaki siparişleri göstermeli
3. WHEN admin "Onaylandı" filtresini seçtiğinde THEN sistem sadece "Approved" durumundaki siparişleri göstermeli
4. WHEN admin filtre değiştirdiğinde THEN sistem listeyi otomatik olarak yenilemeli

### Requirement 3

**User Story:** Admin olarak, sipariş onaylama işleminin sonucunu görmek istiyorum, böylece işlemin başarılı olup olmadığını anlayabileyim.

#### Acceptance Criteria

1. WHEN admin bir siparişi onayladığında ve işlem başarılı olduğunda THEN sistem "Sipariş onaylandı ve Katana'ya gönderildi" mesajını göstermeli
2. WHEN admin bir siparişi onayladığında ve işlem başarısız olduğunda THEN sistem hata mesajını detaylı olarak göstermeli
3. WHEN sipariş onaylanırken THEN sistem "Onaylama" butonunu devre dışı bırakmalı ve yükleme göstergesi göstermeli
4. WHEN onaylama işlemi tamamlandığında THEN sistem sipariş detaylarını otomatik olarak yenilemeli

### Requirement 4

**User Story:** Admin olarak, sipariş durumunu "Teslim Alındı" olarak işaretleyebilmek istiyorum, böylece fiziksel teslimat tamamlandığında sistemi güncelleyebileyim.

#### Acceptance Criteria

1. WHEN admin "Approved" durumundaki bir siparişi görüntülediğinde THEN sistem "Teslim Alındı" butonunu göstermeli
2. WHEN admin "Teslim Alındı" butonuna tıkladığında THEN sistem siparişin durumunu "Received" olarak güncellemeli
3. WHEN sipariş "Received" durumuna geçtiğinde THEN sistem stok hareketlerini kaydetmeli
4. WHEN admin "Received" durumundaki bir siparişi görüntülediğinde THEN sistem durum değiştirme butonlarını göstermemeli

### Requirement 5

**User Story:** Admin olarak, sipariş listesinde her siparişin durumunu hızlıca görebilmek istiyorum, böylece hangi siparişlerin onay beklediğini kolayca anlayabileyim.

#### Acceptance Criteria

1. WHEN admin sipariş listesini görüntülediğinde THEN sistem her sipariş için durum badge'i göstermeli
2. WHEN sipariş "Pending" durumunda ise THEN sistem sarı renkte "Beklemede" badge'i göstermeli
3. WHEN sipariş "Approved" durumunda ise THEN sistem mavi renkte "Onaylandı" badge'i göstermeli
4. WHEN sipariş "Received" durumunda ise THEN sistem yeşil renkte "Teslim Alındı" badge'i göstermeli
5. WHEN admin badge üzerine geldiğinde THEN sistem ek bilgi (onaylayan kişi, tarih) göstermeli

### Requirement 6

**User Story:** Sistem olarak, sipariş durum geçişlerini doğru sırayla yapmak istiyorum, böylece veri tutarlılığı sağlanabilsin.

#### Acceptance Criteria

1. WHEN sipariş "Pending" durumunda ise THEN sistem sadece "Approved" durumuna geçişe izin vermeli
2. WHEN sipariş "Approved" durumunda ise THEN sistem sadece "Received" durumuna geçişe izin vermeli
3. WHEN geçersiz bir durum geçişi denendiğinde THEN sistem hata mesajı göstermeli ve işlemi reddetmeli
4. WHEN sipariş "Received" durumuna geçtiğinde THEN sistem durumu değiştirilemez hale getirmeli

### Requirement 7

**User Story:** Admin olarak, sipariş onaylandığında Katana'ya hangi ürünlerin eklendiğini görmek istiyorum, böylece senkronizasyon durumunu takip edebileyim.

#### Acceptance Criteria

1. WHEN sipariş onaylandığında THEN sistem her ürün kalemi için Katana senkronizasyon durumunu kaydetmeli
2. WHEN admin sipariş detaylarını görüntülediğinde THEN sistem her ürün için "Katana'ya Eklendi" veya "Katana'da Güncellendi" bilgisini göstermeli
3. WHEN Katana senkronizasyonu başarısız olduğunda THEN sistem hata mesajını ürün bazında göstermeli
4. WHEN admin sipariş detaylarını görüntülediğinde THEN sistem toplam kaç ürünün başarıyla senkronize edildiğini göstermeli
