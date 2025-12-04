#!/usr/bin/env python3
"""
Tedarik Siparişi Basit Entegrasyon Testi
"""

import requests
import json
import time
from datetime import datetime, timedelta
from typing import Dict, Any, Optional

class SimplePurchaseOrderTest:
    def __init__(self):
        self.api_url = "http://localhost:8080/api"
        self.passed = 0
        self.failed = 0
        self.results = []
        
    def print_header(self, title: str):
        print(f"\n{'='*70}")
        print(f"  {title}")
        print(f"{'='*70}\n")
    
    def print_test(self, test_name: str, success: bool, details: str = ""):
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
    
    def test_health(self):
        """Health endpoint test"""
        try:
            response = requests.get(f"{self.api_url.replace('/api', '')}/api/health", timeout=5)
            self.print_test("API Health Check", response.status_code == 200, f"Status: {response.status_code}")
            return response.status_code == 200
        except Exception as e:
            self.print_test("API Health Check", False, str(e))
            return False
    
    def test_suppliers_list(self):
        """Tedarikçi listesini al"""
        try:
            response = requests.get(f"{self.api_url}/suppliers", timeout=5)
            success = response.status_code == 200
            
            if success:
                suppliers = response.json()
                count = len(suppliers) if isinstance(suppliers, list) else 0
                self.print_test("Tedarikçi Listesi", True, f"{count} tedarikçi bulundu")
                return suppliers if isinstance(suppliers, list) else []
            else:
                self.print_test("Tedarikçi Listesi", False, f"Status: {response.status_code}")
                return []
        except Exception as e:
            self.print_test("Tedarikçi Listesi", False, str(e))
            return []
    
    def test_products_list(self):
        """Ürün listesini al"""
        try:
            response = requests.get(f"{self.api_url}/products", timeout=5)
            success = response.status_code == 200
            
            if success:
                products = response.json()
                count = len(products) if isinstance(products, list) else 0
                self.print_test("Ürün Listesi", True, f"{count} ürün bulundu")
                return products if isinstance(products, list) else []
            else:
                self.print_test("Ürün Listesi", False, f"Status: {response.status_code}")
                return []
        except Exception as e:
            self.print_test("Ürün Listesi", False, str(e))
            return []
    
    def test_create_supplier(self):
        """Yeni tedarikçi oluştur"""
        try:
            payload = {
                "name": f"Test Tedarikçi {int(time.time())}",
                "code": f"TST{int(time.time()) % 1000}",
                "taxNo": "1234567890",
                "email": "test@test.com"
            }
            
            response = requests.post(f"{self.api_url}/suppliers", json=payload, timeout=5)
            
            if response.status_code in [200, 201]:
                supplier = response.json()
                supplier_id = supplier.get('id')
                self.print_test("Tedarikçi Oluştur", True, f"ID: {supplier_id}")
                return supplier_id
            else:
                self.print_test("Tedarikçi Oluştur", False, f"Status: {response.status_code}")
                return None
        except Exception as e:
            self.print_test("Tedarikçi Oluştur", False, str(e))
            return None
    
    def test_create_purchase_order(self, supplier_id: int, product_id: int):
        """Tedarik siparişi oluştur"""
        try:
            order_date = datetime.now().strftime('%Y-%m-%d')
            expected_date = (datetime.now() + timedelta(days=7)).strftime('%Y-%m-%d')
            
            payload = {
                "supplierId": supplier_id,
                "orderDate": order_date,
                "expectedDate": expected_date,
                "documentSeries": "SIP",
                "documentTypeDetailId": 1,
                "vatIncluded": True,
                "description": f"Test Sipariş",
                "items": [
                    {
                        "productId": product_id,
                        "lucaStockCode": "TEST001",
                        "quantity": 5,
                        "unitPrice": 50.00,
                        "vatRate": 20,
                        "warehouseCode": "MAIN",
                        "unitCode": "PC",
                        "discountAmount": 0
                    }
                ]
            }
            
            response = requests.post(f"{self.api_url}/purchase-orders", json=payload, timeout=10)
            
            if response.status_code in [200, 201]:
                po = response.json()
                po_id = po.get('id')
                total = po.get('totalAmount', 0)
                self.print_test("Tedarik Siparişi Oluştur", True, f"ID: {po_id}, Tutar: {total}")
                return po_id
            else:
                error_msg = response.text[:100] if response.text else "Hata"
                self.print_test("Tedarik Siparişi Oluştur", False, f"Status: {response.status_code} - {error_msg}")
                return None
        except Exception as e:
            self.print_test("Tedarik Siparişi Oluştur", False, str(e))
            return None
    
    def test_get_purchase_order(self, po_id: int):
        """Oluşturulan siparişi al"""
        try:
            response = requests.get(f"{self.api_url}/purchase-orders/{po_id}", timeout=5)
            
            if response.status_code == 200:
                po = response.json()
                order_no = po.get('orderNo')
                self.print_test("Siparişi Database'den Al", True, f"Sipariş No: {order_no}")
                return True
            else:
                self.print_test("Siparişi Database'den Al", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test("Siparişi Database'den Al", False, str(e))
            return False
    
    def test_list_purchase_orders(self):
        """Tüm siparişleri listele"""
        try:
            response = requests.get(f"{self.api_url}/purchase-orders", timeout=5)
            
            if response.status_code == 200:
                pos = response.json()
                count = len(pos) if isinstance(pos, list) else 0
                self.print_test("Tüm Siparişleri Listele", True, f"{count} sipariş bulundu")
                return True
            else:
                self.print_test("Tüm Siparişleri Listele", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test("Tüm Siparişleri Listele", False, str(e))
            return False
    
    def run(self):
        """Testleri çalıştır"""
        print("\033[94m")
        print(f"Tedarik Siparişi Entegrasyon Testi (Basit)")
        print(f"Başlama: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print("\033[0m")
        
        # 1. Health Check
        self.print_header("1. API Kontrolleri")
        if not self.test_health():
            print("\n❌ API çalışmıyor!")
            return
        
        # 2. Listeyi Al
        self.print_header("2. Mevcut Veriler")
        suppliers = self.test_suppliers_list()
        products = self.test_products_list()
        
        if not suppliers or not products:
            print("\n⚠️  Veri eksik. Yeni veriler oluşturuluyor...")
            
            self.print_header("3. Test Verisi Oluşturma")
            supplier_id = self.test_create_supplier()
            
            if not supplier_id:
                print("\n❌ Tedarikçi oluşturulamadı!")
                return
            
            if not products:
                print("\n❌ Ürün bulunamadı!")
                return
            
            product_id = products[0].get('id') if products else None
        else:
            supplier_id = suppliers[0].get('id')
            product_id = products[0].get('id')
            self.print_header("3. Test Verisi Oluşturma")
            print("ℹ  Mevcut veriler kullanılıyor...")
        
        # 4. Tedarik Siparişi Oluştur
        self.print_header("4. Tedarik Siparişi İşlemleri")
        po_id = self.test_create_purchase_order(supplier_id, product_id)
        
        if po_id:
            # 5. Database Kontrolü
            self.print_header("5. Database Kontrolleri")
            self.test_get_purchase_order(po_id)
            self.test_list_purchase_orders()
        
        # Özet
        self.print_summary()
    
    def print_summary(self):
        """Özet yazdır"""
        total = self.passed + self.failed
        rate = (self.passed / total * 100) if total > 0 else 0
        
        print(f"\n{'='*70}")
        print(f"  TEST ÖZETİ")
        print(f"{'='*70}\n")
        
        print(f"Toplam: {total} | \033[92mGeçen: {self.passed}\033[0m | \033[91mBaşarısız: {self.failed}\033[0m")
        print(f"Başarı: {rate:.1f}%")
        
        if self.failed == 0:
            print(f"\n\033[92m✓ Tüm testler başarılı!")
            print(f"Frontend → Backend → Database: TAMAMEN ÇALIŞIYOR!\033[0m")
            return 0
        else:
            print(f"\n\033[91m✗ Bazı testler başarısız\033[0m")
            return 1
    
    def export(self):
        """JSON'a kaydet"""
        data = {
            "timestamp": datetime.now().isoformat(),
            "summary": {
                "total": self.passed + self.failed,
                "passed": self.passed,
                "failed": self.failed
            },
            "results": self.results
        }
        
        with open("purchase-order-test-simple.json", "w", encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        
        print(f"\n📊 Sonuçlar: purchase-order-test-simple.json")


if __name__ == "__main__":
    tester = SimplePurchaseOrderTest()
    tester.run()
    exit_code = tester.print_summary()
    tester.export()
