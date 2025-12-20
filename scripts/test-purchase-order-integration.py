#!/usr/bin/env python3
"""
Tedarik Siparişi Entegrasyon Testi
Frontend, Backend ve Database arasında tam çalışma akışını test eder
"""

import requests
import json
import time
from datetime import datetime, timedelta
from typing import Dict, Any, Optional, Tuple
import sys

class PurchaseOrderIntegrationTest:
    def __init__(self):
        self.api_url = "http://localhost:8080/api"
        self.passed = 0
        self.failed = 0
        self.auth_token = None
        self.test_supplier_id = None
        self.test_product_id = None
        self.created_purchase_order_id = None
        self.results = []
        self.headers = {"Content-Type": "application/json"}
        
    def login(self) -> bool:
        """API'ye giriş yap"""
        try:
            login_payload = {
                "username": "admin",
                "password": "Admin@123"
            }
            
            response = requests.post(
                f"{self.api_url.replace('/api', '')}/api/auth/login",
                json=login_payload,
                timeout=5
            )
            
            if response.status_code in [200, 201]:
                data = response.json()
                self.auth_token = data.get('token') or data.get('accessToken')
                if self.auth_token:
                    self.headers["Authorization"] = f"Bearer {self.auth_token}"
                    self.print_test_result("API'ye Giriş Yap", True, "Token başarıyla alındı")
                    return True
                else:
                    self.print_test_result("API'ye Giriş Yap", False, "Token bulunamadı")
                    return False
            else:
                self.print_test_result("API'ye Giriş Yap", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("API'ye Giriş Yap", False, str(e))
            return False
        
    def print_header(self, title: str):
        print(f"\n{'='*70}")
        print(f"  {title}")
        print(f"{'='*70}\n")
    
    def print_test_result(self, test_name: str, success: bool, details: str = ""):
        status = "✓ GEÇTI" if success else "✗ BAŞARISIZ"
        color = "\033[92m" if success else "\033[91m"
        reset = "\033[0m"
        
        if success:
            self.passed += 1
        else:
            self.failed += 1
        
        print(f"{color}{status}{reset}: {test_name}")
        if details:
            print(f"         {details}")
        
        self.results.append({
            "test": test_name,
            "status": "PASSED" if success else "FAILED",
            "details": details
        })
    
    def test_api_health(self) -> bool:
        """API'nin çalışıp çalışmadığını kontrol et"""
        try:
            response = requests.get(f"{self.api_url.replace('/api', '')}/api/health", timeout=5)
            success = response.status_code == 200
            self.print_test_result("API Health Check", success, f"Status: {response.status_code}")
            return success
        except Exception as e:
            self.print_test_result("API Health Check", False, str(e))
            return False
    
    def get_test_supplier(self) -> Optional[Dict[str, Any]]:
        """Test için bir tedarikçi al"""
        try:
            response = requests.get(f"{self.api_url}/suppliers", headers=self.headers, timeout=5)
            if response.status_code == 200:
                suppliers = response.json()
                if isinstance(suppliers, list) and len(suppliers) > 0:
                    supplier = suppliers[0]
                    self.test_supplier_id = supplier.get('id')
                    self.print_test_result(
                        "Tedarikçi Listesi Alındı", 
                        True, 
                        f"Tedarikçi ID: {self.test_supplier_id}, Ad: {supplier.get('name')}"
                    )
                    return supplier
                else:
                    self.print_test_result("Tedarikçi Listesi Alındı", False, "Sistem veritabanında hiç tedarikçi yok")
                    return None
            else:
                self.print_test_result("Tedarikçi Listesi Alındı", False, f"Status: {response.status_code}")
                return None
        except Exception as e:
            self.print_test_result("Tedarikçi Listesi Alındı", False, str(e))
            return None
    
    def create_test_supplier(self) -> Optional[Dict[str, Any]]:
        """Test için yeni bir tedarikçi oluştur"""
        try:
            payload = {
                "name": f"Test Tedarikçi {datetime.now().strftime('%H%M%S')}",
                "code": f"TEST{int(time.time()) % 10000}",
                "taxNo": "1234567890",
                "email": "test@supplier.com",
                "phone": "+90212123456",
                "address": "Test Adresi"
            }
            
            response = requests.post(f"{self.api_url}/suppliers", json=payload, headers=self.headers, timeout=5)
            if response.status_code in [200, 201]:
                supplier = response.json()
                self.test_supplier_id = supplier.get('id')
                self.print_test_result(
                    "Yeni Tedarikçi Oluşturuldu", 
                    True, 
                    f"Tedarikçi ID: {self.test_supplier_id}, Ad: {supplier.get('name')}"
                )
                return supplier
            else:
                self.print_test_result("Yeni Tedarikçi Oluşturuldu", False, f"Status: {response.status_code} - {response.text}")
                return None
        except Exception as e:
            self.print_test_result("Yeni Tedarikçi Oluşturuldu", False, str(e))
            return None
    
    def get_test_product(self) -> Optional[Dict[str, Any]]:
        """Test için bir ürün al"""
        try:
            response = requests.get(f"{self.api_url}/products", headers=self.headers, timeout=5)
            if response.status_code == 200:
                products = response.json()
                if isinstance(products, list) and len(products) > 0:
                    product = products[0]
                    self.test_product_id = product.get('id')
                    self.print_test_result(
                        "Ürün Listesi Alındı", 
                        True, 
                        f"Ürün ID: {self.test_product_id}, Ad: {product.get('name')}"
                    )
                    return product
                else:
                    self.print_test_result("Ürün Listesi Alındı", False, "Sistem veritabanında hiç ürün yok")
                    return None
            else:
                self.print_test_result("Ürün Listesi Alındı", False, f"Status: {response.status_code}")
                return None
        except Exception as e:
            self.print_test_result("Ürün Listesi Alındı", False, str(e))
            return None
    
    def create_purchase_order(self) -> Optional[Dict[str, Any]]:
        """Tedarik siparişi oluştur"""
        if not self.test_supplier_id or not self.test_product_id:
            self.print_test_result(
                "Tedarik Siparişi Oluştur", 
                False, 
                "Tedarikçi veya Ürün bulunamadı"
            )
            return None
        
        try:
            order_date = datetime.now().strftime('%Y-%m-%d')
            expected_date = (datetime.now() + timedelta(days=7)).strftime('%Y-%m-%d')
            
            payload = {
                "supplierId": self.test_supplier_id,
                "orderDate": order_date,
                "expectedDate": expected_date,
                "documentSeries": "SIP",
                "documentTypeDetailId": 1,
                "vatIncluded": True,
                "projectCode": "TEST",
                "description": f"Test Sipariş {datetime.now().isoformat()}",
                "items": [
                    {
                        "productId": self.test_product_id,
                        "lucaStockCode": "TEST001",
                        "quantity": 10,
                        "unitPrice": 100.00,
                        "vatRate": 20,
                        "warehouseCode": "MAIN",
                        "unitCode": "PC",
                        "discountAmount": 0
                    }
                ]
            }
            
            response = requests.post(f"{self.api_url}/purchase-orders", json=payload, headers=self.headers, timeout=10)
            if response.status_code in [200, 201]:
                po = response.json()
                self.created_purchase_order_id = po.get('id')
                self.print_test_result(
                    "Tedarik Siparişi Oluşturuldu", 
                    True, 
                    f"Sipariş ID: {self.created_purchase_order_id}, Tutar: {po.get('totalAmount')}"
                )
                return po
            else:
                error_msg = response.text if response.text else "Bilinmeyen Hata"
                self.print_test_result(
                    "Tedarik Siparişi Oluşturuldu", 
                    False, 
                    f"Status: {response.status_code} - {error_msg[:100]}"
                )
                return None
        except Exception as e:
            self.print_test_result("Tedarik Siparişi Oluşturuldu", False, str(e))
            return None
    
    def verify_purchase_order_in_database(self) -> bool:
        """Oluşturulan siparişin database'de olduğunu kontrol et"""
        if not self.created_purchase_order_id:
            self.print_test_result(
                "Database'de Sipariş Kontrolü", 
                False, 
                "Sipariş ID bulunamadı"
            )
            return False
        
        try:
            response = requests.get(
                f"{self.api_url}/purchase-orders/{self.created_purchase_order_id}", 
                timeout=5
            )
            if response.status_code == 200:
                po = response.json()
                self.print_test_result(
                    "Database'de Sipariş Kontrolü", 
                    True, 
                    f"Sipariş Bulundu: {po.get('orderNo')}"
                )
                return True
            else:
                self.print_test_result(
                    "Database'de Sipariş Kontrolü", 
                    False, 
                    f"Status: {response.status_code}"
                )
                return False
        except Exception as e:
            self.print_test_result("Database'de Sipariş Kontrolü", False, str(e))
            return False
    
    def list_purchase_orders(self) -> bool:
        """Tüm tedarik siparişlerini listele"""
        try:
            response = requests.get(f"{self.api_url}/purchase-orders", headers=self.headers, timeout=5)
            if response.status_code == 200:
                pos = response.json()
                count = len(pos) if isinstance(pos, list) else 1
                self.print_test_result(
                    "Siparişleri Listele", 
                    True, 
                    f"Toplam {count} sipariş bulundu"
                )
                return True
            else:
                self.print_test_result("Siparişleri Listele", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("Siparişleri Listele", False, str(e))
            return False
    
    def update_purchase_order(self) -> bool:
        """Oluşturulan siparişi güncelle"""
        if not self.created_purchase_order_id:
            self.print_test_result(
                "Siparişi Güncelle", 
                False, 
                "Sipariş ID bulunamadı"
            )
            return False
        
        try:
            payload = {
                "description": f"Güncellenen Sipariş - {datetime.now().isoformat()}"
            }
            
            response = requests.put(
                f"{self.api_url}/purchase-orders/{self.created_purchase_order_id}", 
                json=payload, 
                timeout=5
            )
            
            if response.status_code in [200, 204]:
                self.print_test_result("Siparişi Güncelle", True, "Sipariş başarıyla güncellendi")
                return True
            else:
                self.print_test_result(
                    "Siparişi Güncelle", 
                    False, 
                    f"Status: {response.status_code}"
                )
                return False
        except Exception as e:
            self.print_test_result("Siparişi Güncelle", False, str(e))
            return False
    
    def test_purchase_order_items(self) -> bool:
        """Siparış satırlarını kontrol et"""
        if not self.created_purchase_order_id:
            self.print_test_result(
                "Siparış Satırlarını Kontrol Et", 
                False, 
                "Sipariş ID bulunamadı"
            )
            return False
        
        try:
            response = requests.get(
                f"{self.api_url}/purchase-orders/{self.created_purchase_order_id}", 
                timeout=5
            )
            if response.status_code == 200:
                po = response.json()
                items = po.get('items', [])
                if len(items) > 0:
                    self.print_test_result(
                        "Siparış Satırlarını Kontrol Et", 
                        True, 
                        f"{len(items)} satır bulundu"
                    )
                    return True
                else:
                    self.print_test_result(
                        "Siparış Satırlarını Kontrol Et", 
                        False, 
                        "Siparış satırları bulunamadı"
                    )
                    return False
            else:
                self.print_test_result(
                    "Siparış Satırlarını Kontrol Et", 
                    False, 
                    f"Status: {response.status_code}"
                )
                return False
        except Exception as e:
            self.print_test_result("Siparış Satırlarını Kontrol Et", False, str(e))
            return False
    
    def run_all_tests(self):
        """Tüm testleri çalıştır"""
        print("\033[94m")  # Mavi renk
        print(f"Tedarik Siparişi Entegrasyon Testi")
        print(f"Başlama Zamanı: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print("\033[0m")  # Renk sıfırla
        
        # 1. API Kontrolleri
        self.print_header("1. API Kontrolleri")
        if not self.test_api_health():
            print("\n❌ API çalışmıyor. Test devam ettirilemiyor.")
            return
        
        # 1.5 Login
        self.print_header("1.5 Kimlik Doğrulama")
        if not self.login():
            print("\n❌ API'ye giriş yapılamadı. Test devam ettirilemiyor.")
            return
        
        # 2. Tedarikçi Kontrolleri
        self.print_header("2. Tedarikçi Kontrolleri")
        supplier = self.get_test_supplier()
        if not supplier:
            print("ℹ  Mevcut tedarikçi yok, yeni bir tane oluşturuluyor...")
            supplier = self.create_test_supplier()
        
        if not supplier:
            print("\n❌ Tedarikçi alınamadı. Test devam ettirilemiyor.")
            return
        
        # 3. Ürün Kontrolleri
        self.print_header("3. Ürün Kontrolleri")
        product = self.get_test_product()
        if not product:
            print("\n❌ Ürün bulunamadı. Test devam ettirilemiyor.")
            return
        
        # 4. Tedarik Siparişi Oluşturma
        self.print_header("4. Tedarik Siparişi Oluşturma")
        po = self.create_purchase_order()
        if not po:
            print("\n❌ Siparış oluşturulamadı. Test başarısız.")
            return
        
        # 5. Database Kontrolleri
        self.print_header("5. Database Kontrolleri")
        self.verify_purchase_order_in_database()
        self.list_purchase_orders()
        self.test_purchase_order_items()
        
        # 6. CRUD İşlemleri
        self.print_header("6. CRUD İşlemleri")
        self.update_purchase_order()
        
        # Özet
        self.print_summary()
    
    def print_summary(self):
        """Test özeti yazdır"""
        total = self.passed + self.failed
        success_rate = (self.passed / total * 100) if total > 0 else 0
        
        print(f"\n{'='*70}")
        print(f"  ENTEGRASYON TEST ÖZETİ")
        print(f"{'='*70}\n")
        
        print(f"Toplam Testler: {total}")
        print(f"\033[92mGeçen Testler: {self.passed}\033[0m")
        print(f"\033[91mBaşarısız Testler: {self.failed}\033[0m")
        print(f"Başarı Oranı: {success_rate:.1f}%")
        
        if self.failed == 0:
            print(f"\n\033[92m✓ Tüm Entegrasyon Testleri Başarılı!")
            print(f"Frontend → Backend → Database Akışı Tamamen Çalışıyor!\033[0m")
            return 0
        else:
            print(f"\n\033[91m✗ Bazı Testler Başarısız")
            print(f"Lütfen hata detaylarını kontrol edin\033[0m")
            return 1
    
    def export_results(self, filename: str = "purchase-order-test-results.json"):
        """Sonuçları JSON dosyasına kaydet"""
        data = {
            "timestamp": datetime.now().isoformat(),
            "test_type": "Purchase Order Integration Test",
            "summary": {
                "total": self.passed + self.failed,
                "passed": self.passed,
                "failed": self.failed,
                "success_rate": (self.passed / (self.passed + self.failed) * 100) if (self.passed + self.failed) > 0 else 0
            },
            "test_data": {
                "supplier_id": self.test_supplier_id,
                "product_id": self.test_product_id,
                "created_purchase_order_id": self.created_purchase_order_id
            },
            "results": self.results
        }
        
        with open(filename, "w", encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        
        print(f"\n📊 Detaylı Sonuçlar: {filename}")


def main():
    tester = PurchaseOrderIntegrationTest()
    tester.run_all_tests()
    exit_code = tester.print_summary()
    tester.export_results()
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
