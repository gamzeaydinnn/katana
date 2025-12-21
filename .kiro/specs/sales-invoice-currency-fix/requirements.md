# Requirements Document

## Introduction

Satış siparişlerinin Luca'ya fatura olarak senkronize edilmesi sırasında, özellikle EUR ve USD gibi dövizli siparişlerde "Beklenmedik Hata!" mesajı alınmaktadır. Bu sorun, yeni siparişlerde de tekrar edebilir ve müşteri deneyimini olumsuz etkilemektedir. Sistemin dövizli faturaları doğru kur bilgisi ile güvenilir şekilde Luca'ya göndermesi gerekmektedir.

## Glossary

- **System**: Katana-Luca entegrasyon sistemi
- **SalesOrder**: Katana'dan gelen veya sistemde oluşturulan satış siparişi
- **Invoice**: Luca'ya gönderilen fatura belgesi
- **ConversionRate**: Döviz kuru (örn: 1 EUR = 36.5 TRY)
- **Currency**: Para birimi (TRY, EUR, USD, GBP)
- **Luca**: Muhasebe/ERP sistemi
- **MappingHelper**: Sipariş verilerini Luca formatına dönüştüren yardımcı sınıf

## Requirements

### Requirement 1: Döviz Kuru Yönetimi

**User Story:** Sistem yöneticisi olarak, dövizli siparişlerin doğru kur bilgisi ile Luca'ya gönderilmesini istiyorum, böylece muhasebe kayıtları tutarlı olur.

#### Acceptance Criteria

1. WHEN bir sipariş EUR, USD veya GBP para biriminde ise, THE System SHALL ConversionRate alanını kontrol etmeli
2. WHEN ConversionRate null veya sıfır ise, THE System SHALL güncel kur bilgisini harici bir kaynaktan almalı
3. WHEN kur bilgisi alınamazsa, THE System SHALL varsayılan kur değerlerini kullanmalı ve uyarı loglamalı
4. WHEN TRY para birimi kullanılıyorsa, THE System SHALL ConversionRate'i 1.0 olarak ayarlamalı
5. WHEN fatura oluşturulurken, THE System SHALL kullanılan kur bilgisini log'a yazmalı

### Requirement 2: Hata Yönetimi ve Loglama

**User Story:** Geliştirici olarak, fatura senkronizasyon hatalarının detaylı loglarını görmek istiyorum, böylece sorunları hızlıca tespit edip çözebilirim.

#### Acceptance Criteria

1. WHEN fatura senkronizasyonu başlatıldığında, THE System SHALL sipariş detaylarını (OrderNo, Currency, ConversionRate) loglamalı
2. WHEN Luca API'ye request gönderildiğinde, THE System SHALL request payload'ını loglamalı
3. WHEN Luca API'den response alındığında, THE System SHALL response body'sini loglamalı
4. IF Luca API hata döndürürse, THEN THE System SHALL hata mesajını ve stack trace'i detaylı loglamalı
5. WHEN mapping işlemi sırasında exception oluşursa, THE System SHALL exception detaylarını ve sipariş bilgilerini loglamalı

### Requirement 3: Veri Validasyonu

**User Story:** Sistem yöneticisi olarak, eksik veya hatalı verilerle Luca'ya fatura gönderilmemesini istiyorum, böylece veri bütünlüğü korunur.

#### Acceptance Criteria

1. WHEN fatura oluşturulmadan önce, THE System SHALL müşteri bilgilerinin (CariKodu, CariAd) dolu olduğunu kontrol etmeli
2. WHEN fatura oluşturulmadan önce, THE System SHALL sipariş satırlarının (Lines) var olduğunu kontrol etmeli
3. WHEN fatura oluşturulmadan önce, THE System SHALL her satırda SKU ve Quantity bilgisinin dolu olduğunu kontrol etmeli
4. IF validasyon başarısız olursa, THEN THE System SHALL açıklayıcı hata mesajı döndürmeli
5. WHEN validasyon başarısız olursa, THE System SHALL Luca API'ye request göndermemeli

### Requirement 4: Kur Bilgisi Kaynağı

**User Story:** Sistem yöneticisi olarak, güncel döviz kurlarının güvenilir bir kaynaktan alınmasını istiyorum, böylece faturalar doğru tutarlarla oluşturulur.

#### Acceptance Criteria

1. THE System SHALL TCMB (Türkiye Cumhuriyet Merkez Bankası) API'sini kur kaynağı olarak kullanmalı
2. WHEN TCMB API'ye erişilemezse, THE System SHALL alternatif bir kur kaynağına (örn: ExchangeRate-API) geçmeli
3. WHEN hiçbir kur kaynağına erişilemezse, THE System SHALL varsayılan kur değerlerini kullanmalı ve uyarı loglamalı
4. THE System SHALL kur bilgilerini cache'lemeli (örn: 1 saat)
5. WHEN cache süresi dolduğunda, THE System SHALL kur bilgilerini yeniden almalı

### Requirement 5: Kullanıcı Arayüzü İyileştirmeleri

**User Story:** Kullanıcı olarak, sipariş senkronizasyonu sırasında oluşan hataları anlaşılır şekilde görmek istiyorum, böylece ne yapacağımı bilebilirim.

#### Acceptance Criteria

1. WHEN senkronizasyon hatası oluştuğunda, THE System SHALL kullanıcıya anlaşılır bir hata mesajı göstermeli
2. WHEN hata kur bilgisi ile ilgiliyse, THE System SHALL "Döviz kuru bilgisi eksik veya hatalı" mesajı göstermeli
3. WHEN hata müşteri bilgisi ile ilgiliyse, THE System SHALL "Müşteri bilgileri eksik" mesajı göstermeli
4. WHEN hata sipariş satırları ile ilgiliyse, THE System SHALL "Sipariş satırları bulunamadı" mesajı göstermeli
5. THE System SHALL hata mesajlarında teknik detayları (stack trace) göstermemeli

### Requirement 6: Retry Mekanizması

**User Story:** Sistem yöneticisi olarak, geçici ağ hatalarında otomatik retry yapılmasını istiyorum, böylece manuel müdahale gerekmez.

#### Acceptance Criteria

1. WHEN Luca API timeout hatası döndürürse, THE System SHALL 3 kez retry yapmalı
2. WHEN Luca API 5xx hatası döndürürse, THE System SHALL 2 kez retry yapmalı
3. WHEN Luca API 4xx hatası döndürürse, THE System SHALL retry yapmamalı
4. WHEN retry yapılırken, THE System SHALL exponential backoff stratejisi kullanmalı (1s, 2s, 4s)
5. WHEN tüm retry'lar başarısız olursa, THE System SHALL son hatayı kullanıcıya göstermeli

### Requirement 7: Test Edilebilirlik

**User Story:** Geliştirici olarak, kur bilgisi ve fatura oluşturma mantığını kolayca test edebilmek istiyorum, böylece kod kalitesi yüksek olur.

#### Acceptance Criteria

1. THE System SHALL kur bilgisi alma işlemini ayrı bir interface (ICurrencyService) üzerinden yapmalı
2. THE System SHALL fatura mapping işlemini ayrı bir metod olarak ayırmalı
3. THE System SHALL Luca API çağrılarını mock'lanabilir şekilde tasarlamalı
4. THE System SHALL validasyon mantığını ayrı bir metod olarak ayırmalı
5. THE System SHALL her kritik işlem için unit test yazılabilir olmalı
