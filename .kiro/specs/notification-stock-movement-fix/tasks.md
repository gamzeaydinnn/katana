# Implementation Plan

## 1. Backend - Event Types ve Interface Genişletme

- [ ] 1.1 Yeni event sınıflarını oluştur

  - `ProductCreatedEvent`, `ProductUpdatedEvent` sınıflarını Katana.Core/Events klasörüne ekle
  - `StockTransferCreatedEvent`, `StockAdjustmentCreatedEvent` sınıflarını ekle
  - `StockMovementSyncedEvent`, `StockMovementFailedEvent` sınıflarını ekle
  - _Requirements: 1.1, 3.1, 3.2, 3.3, 3.4_

- [ ] 1.2 INotificationPublisher interface'ini genişlet
  - Yeni publish metodlarını interface'e ekle
  - IPendingNotificationPublisher'ı INotificationPublisher olarak yeniden adlandır
  - _Requirements: 1.1, 3.1, 3.2, 3.3, 3.4_

## 2. Backend - SignalRNotificationPublisher Genişletme

- [ ] 2.1 ProductCreated event publisher metodunu implement et

  - `PublishProductCreatedAsync` metodunu ekle
  - SignalR hub üzerinden "ProductCreated" eventi gönder
  - Notification tablosuna kaydet
  - _Requirements: 1.1, 1.4_

- [ ] 2.2 Property test: Product creation triggers notification

  - **Property 1: Product Creation Triggers Notification**
  - **Validates: Requirements 1.1, 1.4**

- [ ] 2.3 StockMovement event publisher metodlarını implement et

  - `PublishStockTransferCreatedAsync` metodunu ekle
  - `PublishStockAdjustmentCreatedAsync` metodunu ekle
  - `PublishStockMovementSyncedAsync` metodunu ekle
  - `PublishStockMovementFailedAsync` metodunu ekle
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 2.4 Property test: Stock movement event publishing
  - **Property 5: Stock Movement Event Publishing**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## 3. Backend - Controller Entegrasyonu

- [ ] 3.1 ProductsController'a bildirim entegrasyonu ekle

  - Ürün oluşturma endpoint'inde `PublishProductCreatedAsync` çağır
  - Mevcut `ProductCreated` SignalR çağrısını yeni publisher'a taşı
  - _Requirements: 1.1, 1.4_

- [ ] 3.2 SyncService'e bildirim entegrasyonu ekle

  - Katana sync sonrası yeni ürünler için bildirim gönder
  - _Requirements: 1.1_

- [ ] 3.3 StockMovementSyncService'e bildirim entegrasyonu ekle
  - Transfer/Adjustment sync sonrası bildirim gönder
  - Başarılı/başarısız durumlar için farklı eventler
  - _Requirements: 3.3, 3.4_

## 4. Checkpoint - Backend testlerini çalıştır

- Ensure all tests pass, ask the user if questions arise.

## 5. Frontend - SignalR Service Genişletme

- [ ] 5.1 Yeni event handler'ları signalr.ts'e ekle

  - `onProductCreated`, `offProductCreated` fonksiyonlarını ekle
  - `onStockTransferCreated`, `offStockTransferCreated` fonksiyonlarını ekle
  - `onStockAdjustmentCreated`, `offStockAdjustmentCreated` fonksiyonlarını ekle
  - `onStockMovementSynced`, `offStockMovementSynced` fonksiyonlarını ekle
  - `onStockMovementFailed`, `offStockMovementFailed` fonksiyonlarını ekle
  - _Requirements: 1.2, 3.1, 3.2, 3.3, 3.4_

- [ ] 5.2 TypeScript interface'lerini tanımla
  - `ProductNotification`, `StockMovementNotification`, `SyncNotification` tiplerini ekle
  - _Requirements: 1.2_

## 6. Frontend - Header Bildirim Entegrasyonu

- [ ] 6.1 Header.tsx'e yeni event listener'ları ekle

  - `onProductCreated` handler'ını useEffect'e ekle
  - `onStockTransferCreated` handler'ını ekle
  - `onStockAdjustmentCreated` handler'ını ekle
  - Cleanup fonksiyonlarında off metodlarını çağır
  - _Requirements: 1.2_

- [ ] 6.2 Notification state yönetimini güncelle

  - Yeni bildirim tiplerini NotificationItem interface'ine ekle
  - Badge count hesaplamasını güncelle
  - _Requirements: 1.2, 1.3_

- [ ] 6.3 Property test: Notification badge updates on event
  - **Property 2: Notification Badge Updates on Event**
  - **Validates: Requirements 1.2**

## 7. Frontend - StockMovementSyncPage İyileştirme

- [ ] 7.1 Real-time güncelleme ekle

  - SignalR event'lerini dinle ve listeyi otomatik güncelle
  - Yeni hareket geldiğinde listeye ekle
  - Sync durumu değiştiğinde güncelle
  - _Requirements: 2.1, 2.2_

- [ ] 7.2 Boş liste durumunu iyileştir

  - Veri yokken kullanıcı dostu mesaj göster
  - Yenileme butonu ekle
  - _Requirements: 2.3_

- [ ] 7.3 Property test: Stock movement list completeness

  - **Property 3: Stock Movement List Completeness**
  - **Validates: Requirements 2.1**

- [ ] 7.4 Property test: Filter correctness
  - **Property 4: Filter Correctness**
  - **Validates: Requirements 2.4**

## 8. Backend - Retry ve Fallback Mekanizması

- [ ] 8.1 Mevcut retry mekanizmasını doğrula

  - Polly policy'nin tüm yeni metodlarda kullanıldığını kontrol et
  - FailedNotifications tablosuna kayıt işlemini doğrula
  - _Requirements: 4.2, 4.3_

- [ ] 8.2 Property test: Retry and fallback mechanism
  - **Property 6: Retry and Fallback Mechanism**
  - **Validates: Requirements 4.2, 4.3**

## 9. Frontend - Kaçırılan Bildirimleri Yükleme

- [ ] 9.1 Sayfa yüklendiğinde bildirimleri API'den çek

  - `/api/notifications/recent` endpoint'ini çağır
  - Son 24 saatteki bildirimleri yükle
  - _Requirements: 4.4_

- [ ] 9.2 Backend'e notifications endpoint'i ekle
  - `GET /api/notifications/recent` endpoint'i oluştur
  - Filtreleme ve sayfalama desteği ekle
  - _Requirements: 4.4_

## 10. Final Checkpoint - Tüm testleri çalıştır

- Ensure all tests pass, ask the user if questions arise.
