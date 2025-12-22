# Requirements Document

## Introduction

Müşteri kritik bir sorun bildirdi: Sistem Katana'dan aldığı veriyi Luca'ya yazmak yerine Katana'ya geri yazıyor ve siparişler karışıyor. Bu, veri akış yönünün yanlış olduğunu gösteriyor. Sistem mimarisi şu şekilde olmalı:

**DOĞRU AKIM: Katana → Integration System → Luca**

Ancak bazı sync metodları ters yönde çalışıyor:
- `SyncProductsFromLucaAsync`: Luca → Katana (YANLIŞ YÖN)
- `SyncStockFromLucaAsync`: Luca → Katana (YANLIŞ YÖN)
- `SyncInvoicesFromLucaAsync`: Luca → Katana (YANLIŞ YÖN)
- `SyncCustomersFromLucaAsync`: Luca → Katana (YANLIŞ YÖN)

Bu metodlar Luca'dan veri çekip Katana'ya (local DB'ye) yazıyor, bu da veri kirliliğine ve sipariş karışıklığına neden oluyor.

## Glossary

- **Katana**: Kaynak ERP sistemi (source of truth)
- **Luca**: Hedef muhasebe sistemi
- **Integration_System**: Katana ve Luca arasındaki entegrasyon katmanı
- **SyncService**: Veri senkronizasyonunu yöneten servis
- **Data_Flow**: Verinin hangi yönde aktığı (Katana → Luca olmalı)
- **Reverse_Sync**: Luca'dan Katana'ya veri akışı (istenmeyen durum)

## Requirements

### Requirement 1: Veri Akış Yönü Kontrolü

**User Story:** Sistem yöneticisi olarak, verinin sadece Katana'dan Luca'ya aktığından emin olmak istiyorum, böylece Katana'daki veriler bozulmaz.

#### Acceptance Criteria

1. WHEN sistem başlatıldığında, THE System SHALL sadece Katana → Luca yönünde sync metodlarını aktif etmek
2. WHEN bir sync işlemi başlatıldığında, THE System SHALL veri akış yönünü doğrulamak
3. IF ters yönde (Luca → Katana) bir sync tespit edilirse, THEN THE System SHALL bu işlemi engellemek ve uyarı loglamak
4. THE System SHALL Luca'dan Katana'ya veri yazma işlemlerini devre dışı bırakmak

### Requirement 2: Ters Yönlü Sync Metodlarının Kaldırılması

**User Story:** Geliştirici olarak, ters yönlü sync metodlarının sistemden tamamen kaldırılmasını istiyorum, böylece yanlışlıkla kullanılmaları önlenir.

#### Acceptance Criteria

1. THE System SHALL `SyncProductsFromLucaAsync` metodunu kaldırmak veya devre dışı bırakmak
2. THE System SHALL `SyncStockFromLucaAsync` metodunu kaldırmak veya devre dışı bırakmak
3. THE System SHALL `SyncInvoicesFromLucaAsync` metodunu kaldırmak veya devre dışı bırakmak
4. THE System SHALL `SyncCustomersFromLucaAsync` metodunu kaldırmak veya devre dışı bırakmak
5. THE System SHALL `SyncAllFromLucaAsync` metodunu kaldırmak veya devre dışı bırakmak
6. WHEN bu metodlar çağrılmaya çalışıldığında, THE System SHALL açıklayıcı bir hata mesajı döndürmek

### Requirement 3: API Endpoint Kontrolü

**User Story:** Sistem yöneticisi olarak, API endpoint'lerinin yanlış yönde sync tetiklemediğinden emin olmak istiyorum.

#### Acceptance Criteria

1. THE System SHALL tüm API endpoint'lerini taramak ve Luca → Katana yönünde sync tetikleyenleri tespit etmek
2. WHEN bir endpoint Luca'dan veri çekip Katana'ya yazmaya çalışırsa, THE System SHALL bu isteği reddetmek
3. THE System SHALL sadece Katana → Luca yönünde sync endpoint'lerini aktif tutmak
4. THE System SHALL her sync işleminin yönünü log'lamak

### Requirement 4: Veri Bütünlüğü Koruması

**User Story:** Müşteri olarak, Katana'daki siparişlerimin ve ürünlerimin bozulmadan kalmasını istiyorum.

#### Acceptance Criteria

1. THE System SHALL Katana veritabanına (Products, Orders, Customers tablolarına) sadece Katana API'den gelen verilerle yazma yapabilmek
2. WHEN Luca'dan veri geldiğinde, THE System SHALL bu veriyi Katana'ya yazmak yerine sadece mapping tablosunda saklamak
3. THE System SHALL her veritabanı yazma işleminin kaynağını (Katana API vs Luca API) log'lamak
4. IF Luca'dan gelen veri Katana'ya yazılmaya çalışılırsa, THEN THE System SHALL bu işlemi engellemek ve kritik seviye log oluşturmak

### Requirement 5: Mevcut Veri Kirliliğinin Temizlenmesi

**User Story:** Sistem yöneticisi olarak, ters sync nedeniyle oluşan veri kirliliğini tespit edip temizlemek istiyorum.

#### Acceptance Criteria

1. THE System SHALL Luca'dan yazılmış kayıtları tespit edebilmek için bir audit mekanizması sağlamak
2. WHEN audit çalıştırıldığında, THE System SHALL Luca kaynaklı kayıtları raporlamak
3. THE System SHALL kirli verileri temizlemek için bir rollback mekanizması sağlamak
4. THE System SHALL temizleme işleminden önce yedekleme yapmak

### Requirement 6: Monitoring ve Alerting

**User Story:** Sistem yöneticisi olarak, yanlış yönde sync denemelerini anında tespit etmek istiyorum.

#### Acceptance Criteria

1. WHEN ters yönde bir sync denemesi yapıldığında, THE System SHALL kritik seviye alert oluşturmak
2. THE System SHALL her sync işleminin yönünü, kaynağını ve hedefini log'lamak
3. THE System SHALL günlük sync yönü raporları oluşturmak
4. THE System SHALL yanlış yönde sync denemelerini sayısal olarak takip etmek

### Requirement 7: Kod Dokümantasyonu

**User Story:** Geliştirici olarak, hangi metodların hangi yönde sync yaptığını açıkça görmek istiyorum.

#### Acceptance Criteria

1. THE System SHALL her sync metodunun dokümantasyonunda veri akış yönünü belirtmek
2. THE System SHALL Katana → Luca yönündeki metodları "ToLuca" suffix'i ile isimlendirmek
3. THE System SHALL ters yönlü metodları deprecated olarak işaretlemek
4. THE System SHALL veri akış diyagramını kod dokümantasyonuna eklemek
