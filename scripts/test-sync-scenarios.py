#!/usr/bin/env python3
"""
Katana → Luca Stok Kartı Senkronizasyonu Test Senaryoları
Test 1: İlk Senkronizasyon (Temiz Durum)
Test 2: Duplicate Detection
"""

import requests
import json
import time
from datetime import datetime
from typing import Dict, List, Any

# Konfigürasyon
API_BASE_URL = "http://localhost:8080/api"
ADMIN_API_URL = "http://localhost:8080/api/adminpanel"
TEST_RESULTS_FILE = "test_sync_results.json"

class TestLogger:
    def __init__(self):
        self.logs = []
        self.start_time = datetime.now()
    
    def log(self, level: str, message: str):
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        log_entry = f"[{timestamp}] {level}: {message}"
        self.logs.append(log_entry)
        print(log_entry)
    
    def save(self, filename: str = TEST_RESULTS_FILE):
        with open(filename, 'w') as f:
            json.dump(self.logs, f, indent=2, ensure_ascii=False)
        self.log("INFO", f"Test logları kaydedildi: {filename}")

class KatanaTestSuite:
    def __init__(self):
        self.logger = TestLogger()
        self.test_results = {}
    
    def get_sync_status(self) -> Dict[str, Any]:
        """Senkronizasyon durumunu kontrol et"""
        try:
            response = requests.get(f"{ADMIN_API_URL}/sync-logs-anon", timeout=10)
            if response.status_code == 200:
                return response.json()
            else:
                self.logger.log("ERROR", f"Sync status API hatası: {response.status_code}")
                return {}
        except Exception as e:
            self.logger.log("ERROR", f"Sync status alınamadı: {str(e)}")
            return {}
    
    def get_failed_records(self) -> List[Dict]:
        """Başarısız kayıtları al"""
        try:
            response = requests.get(f"{ADMIN_API_URL}/failed-records-anon", timeout=10)
            if response.status_code == 200:
                data = response.json()
                return data.get('records', []) if isinstance(data, dict) else data
            return []
        except Exception as e:
            self.logger.log("ERROR", f"Başarısız kayıtlar alınamadı: {str(e)}")
            return []
    
    def get_products_count(self) -> int:
        """Ürün sayısını al"""
        try:
            response = requests.get(f"{API_BASE_URL}/Products?pageSize=1", timeout=10)
            if response.status_code == 200:
                data = response.json()
                return data.get('totalCount', 0)
            return 0
        except Exception as e:
            self.logger.log("ERROR", f"Ürün sayısı alınamadı: {str(e)}")
            return 0
    
    def test_1_initial_sync(self):
        """TEST 1: İlk Senkronizasyon (Temiz Durum)"""
        self.logger.log("INFO", "=" * 60)
        self.logger.log("INFO", "TEST 1: İLK SENKRONIZASYON (TEMIZ DURUM)")
        self.logger.log("INFO", "=" * 60)
        
        # Ürün sayısını kontrol et
        product_count = self.get_products_count()
        self.logger.log("INFO", f"Toplam ürün sayısı: {product_count}")
        
        # Sync status'ü başlangıçta al
        initial_status = self.get_sync_status()
        self.logger.log("INFO", f"Başlangıç sync status: {json.dumps(initial_status, ensure_ascii=False)}")
        
        # Manual sync tetikle (API aracılığıyla varsa)
        try:
            self.logger.log("INFO", "Senkronizasyon başlatılıyor... (Manual olarak AdminPanel'den yapınız)")
            self.logger.log("WAIT", "Admin Panel → Stok Kartları Senkronizasyonu → Senkronize Et")
            
            # 60 saniye bekle
            for i in range(60, 0, -10):
                self.logger.log("INFO", f"Bekleniyor... ({i}s)")
                time.sleep(10)
        except Exception as e:
            self.logger.log("ERROR", f"Sync tetiklenemedi: {str(e)}")
        
        # Sonuçları kontrol et
        time.sleep(5)
        failed_records = self.get_failed_records()
        final_status = self.get_sync_status()
        
        self.logger.log("INFO", "TEST 1 SONUÇLARI:")
        self.logger.log("INFO", f"Son status: {json.dumps(final_status, ensure_ascii=False)}")
        self.logger.log("INFO", f"Başarısız kayıt sayısı: {len(failed_records)}")
        
        # Beklenen sonuç
        self.logger.log("INFO", "BEKLENEN SONUÇ:")
        self.logger.log("INFO", "✅ Başarılı: ~50")
        self.logger.log("INFO", "❌ Başarısız: 0")
        self.logger.log("INFO", "⚠️ Duplicate: 0")
        self.logger.log("INFO", "⏭️ Atlanan: 0")
        
        self.test_results['test_1'] = {
            'status': 'COMPLETED',
            'product_count': product_count,
            'final_status': final_status,
            'failed_records_count': len(failed_records)
        }
    
    def test_2_duplicate_detection(self):
        """TEST 2: Duplicate Detection (Aynı Ürünleri Tekrar Gönder)"""
        self.logger.log("INFO", "=" * 60)
        self.logger.log("INFO", "TEST 2: DUPLICATE DETECTION")
        self.logger.log("INFO", "=" * 60)
        
        self.logger.log("INFO", "Aynı senkronizasyonu tekrar çalıştırılıyor...")
        self.logger.log("WAIT", "Admin Panel → Stok Kartları Senkronizasyonu → Senkronize Et (2. kez)")
        
        try:
            # 60 saniye bekle
            for i in range(60, 0, -10):
                self.logger.log("INFO", f"Bekleniyor... ({i}s)")
                time.sleep(10)
        except Exception as e:
            self.logger.log("ERROR", f"Sync tetiklenemedi: {str(e)}")
        
        # Sonuçları kontrol et
        time.sleep(5)
        failed_records = self.get_failed_records()
        final_status = self.get_sync_status()
        
        self.logger.log("INFO", "TEST 2 SONUÇLARI:")
        self.logger.log("INFO", f"Son status: {json.dumps(final_status, ensure_ascii=False)}")
        self.logger.log("INFO", f"Başarısız kayıt sayısı: {len(failed_records)}")
        
        # Beklenen sonuç
        self.logger.log("INFO", "BEKLENEN SONUÇ:")
        self.logger.log("INFO", "✅ Başarılı: 0")
        self.logger.log("INFO", "❌ Başarısız: 0")
        self.logger.log("INFO", "⚠️ Duplicate: ~50 (Tümü duplicate olarak tespit edilmeli)")
        self.logger.log("INFO", "⏭️ Atlanan: 0")
        
        self.test_results['test_2'] = {
            'status': 'COMPLETED',
            'final_status': final_status,
            'failed_records_count': len(failed_records),
            'duplicate_expected': True
        }
    
    def check_backend_logs(self):
        """Backend loglarını kontrol et"""
        self.logger.log("INFO", "=" * 60)
        self.logger.log("INFO", "BACKEND LOGLARINI KONTROL ET")
        self.logger.log("INFO", "=" * 60)
        
        self.logger.log("INFO", "✅ Stok kartı oluşturuldu loglarını arayınız:")
        self.logger.log("INFO", "  docker-compose logs api 2>&1 | grep 'Stok kartı oluşturuldu'")
        self.logger.log("INFO", "")
        self.logger.log("INFO", "⚠️ Duplicate tespit edildi loglarını arayınız:")
        self.logger.log("INFO", "  docker-compose logs api 2>&1 | grep 'Duplicate tespit'")
        self.logger.log("INFO", "")
        self.logger.log("INFO", "📊 Senkronizasyon istatistiklerini görmek için:")
        self.logger.log("INFO", "  curl http://localhost:8080/api/Sync/status")
    
    def run_all_tests(self):
        """Tüm testleri çalıştır"""
        try:
            self.test_1_initial_sync()
            self.logger.log("INFO", "")
            time.sleep(5)
            self.test_2_duplicate_detection()
            self.logger.log("INFO", "")
            self.check_backend_logs()
            
            self.logger.log("INFO", "=" * 60)
            self.logger.log("INFO", "TÜM TESTLER TAMAMLANDI")
            self.logger.log("INFO", "=" * 60)
            self.logger.log("INFO", f"Test Sonuçları: {json.dumps(self.test_results, ensure_ascii=False)}")
            
        except Exception as e:
            self.logger.log("ERROR", f"Test çalıştırma hatası: {str(e)}")
        finally:
            self.logger.save()

def main():
    """Ana fonksiyon"""
    print("🚀 Katana → Luca Stok Kartı Senkronizasyonu Test Senaryoları")
    print("=" * 60)
    
    suite = KatanaTestSuite()
    suite.run_all_tests()
    
    print("\n" + "=" * 60)
    print("✅ Test tamamlandı! Sonuçlar 'test_sync_results.json' dosyasına kaydedildi.")
    print("=" * 60)

if __name__ == "__main__":
    main()
