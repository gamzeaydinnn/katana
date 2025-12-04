#!/usr/bin/env python3
"""
Katana Uygulaması Test Script
API ve Database bağlantısını kontrol eder
"""

import requests
import subprocess
import json
import sys
from datetime import datetime
from typing import Tuple, Dict, Any

class KatanaHealthCheck:
    def __init__(self):
        self.api_url = "http://localhost:8080"
        self.passed = 0
        self.failed = 0
        self.results = []
        
    def print_header(self, title: str):
        print(f"\n{'='*60}")
        print(f"  {title}")
        print(f"{'='*60}\n")
    
    def print_test_result(self, test_name: str, success: bool, message: str = ""):
        status = "✓ GEÇTI" if success else "✗ BAŞARISIZ"
        color = "\033[92m" if success else "\033[91m"
        reset = "\033[0m"
        
        if success:
            self.passed += 1
        else:
            self.failed += 1
        
        print(f"{color}{status}{reset}: {test_name}")
        if message:
            print(f"         {message}")
        
        self.results.append({
            "test": test_name,
            "status": "PASSED" if success else "FAILED",
            "message": message
        })
    
    def test_api_health(self) -> bool:
        """API health endpoint'ini kontrol et"""
        try:
            response = requests.get(f"{self.api_url}/api/health", timeout=5)
            if response.status_code == 200:
                self.print_test_result("API Health Endpoint", True, f"Status: {response.status_code}")
                return True
            else:
                self.print_test_result("API Health Endpoint", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("API Health Endpoint", False, str(e))
            return False
    
    def test_api_connectivity(self) -> bool:
        """API'ye temel bağlantı test et"""
        try:
            response = requests.get(f"{self.api_url}/api", timeout=5)
            self.print_test_result("API Bağlantısı", True)
            return True
        except requests.exceptions.ConnectionError:
            self.print_test_result("API Bağlantısı", False, "API erişilemez")
            return False
        except Exception as e:
            self.print_test_result("API Bağlantısı", False, str(e))
            return False
    
    def test_suppliers_endpoint(self) -> bool:
        """Suppliers endpoint'ini test et"""
        try:
            headers = {"Authorization": "Bearer test"}
            response = requests.get(f"{self.api_url}/api/suppliers", headers=headers, timeout=5)
            if response.status_code in [200, 401]:  # 401 auth hatası olabilir ama API çalışıyor
                self.print_test_result("Suppliers Endpoint", True, f"Status: {response.status_code}")
                return True
            else:
                self.print_test_result("Suppliers Endpoint", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("Suppliers Endpoint", False, str(e))
            return False
    
    def test_products_endpoint(self) -> bool:
        """Products endpoint'ini test et"""
        try:
            headers = {"Authorization": "Bearer test"}
            response = requests.get(f"{self.api_url}/api/products", headers=headers, timeout=5)
            if response.status_code in [200, 401]:
                self.print_test_result("Products Endpoint", True, f"Status: {response.status_code}")
                return True
            else:
                self.print_test_result("Products Endpoint", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("Products Endpoint", False, str(e))
            return False
    
    def test_purchase_orders_endpoint(self) -> bool:
        """Purchase Orders endpoint'ini test et"""
        try:
            headers = {"Authorization": "Bearer test"}
            response = requests.get(f"{self.api_url}/api/purchase-orders", headers=headers, timeout=5)
            if response.status_code in [200, 401]:
                self.print_test_result("Purchase Orders Endpoint", True, f"Status: {response.status_code}")
                return True
            else:
                self.print_test_result("Purchase Orders Endpoint", False, f"Status: {response.status_code}")
                return False
        except Exception as e:
            self.print_test_result("Purchase Orders Endpoint", False, str(e))
            return False
    
    def check_docker_containers(self) -> bool:
        """Docker container'ları kontrol et"""
        try:
            result = subprocess.run(["docker", "ps", "--format", "{{.Names}}"], 
                                  capture_output=True, text=True, timeout=5)
            containers = result.stdout.strip().split('\n')
            
            api_running = any("katana-api" in c for c in containers)
            db_running = any("katana-db" in c for c in containers)
            
            self.print_test_result("API Container", api_running)
            self.print_test_result("Database Container", db_running)
            
            return api_running and db_running
        except Exception as e:
            self.print_test_result("Docker Container Check", False, str(e))
            return False
    
    def check_ports(self) -> bool:
        """Port kontrolü yap"""
        try:
            # macOS için lsof komutu
            result_8080 = subprocess.run(["lsof", "-i", ":8080"], 
                                        capture_output=True, timeout=5)
            result_1433 = subprocess.run(["lsof", "-i", ":1433"], 
                                        capture_output=True, timeout=5)
            
            port_8080_open = result_8080.returncode == 0
            port_1433_open = result_1433.returncode == 0
            
            self.print_test_result("API Port (8080)", port_8080_open)
            self.print_test_result("Database Port (1433)", port_1433_open)
            
            return port_8080_open and port_1433_open
        except Exception as e:
            self.print_test_result("Port Check", False, str(e))
            return False
    
    def run_all_tests(self):
        """Tüm testleri çalıştır"""
        print("\033[94m")  # Mavi renk
        print(f"Katana Uygulaması Sağlık Kontrolü")
        print(f"Başlama Zamanı: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print("\033[0m")  # Renk sıfırla
        
        self.print_header("1. Backend API Kontrolleri")
        self.test_api_connectivity()
        self.test_api_health()
        self.test_suppliers_endpoint()
        self.test_products_endpoint()
        self.test_purchase_orders_endpoint()
        
        self.print_header("2. Docker Container'ları")
        self.check_docker_containers()
        
        self.print_header("3. Port Kontrolü")
        self.check_ports()
        
        self.print_summary()
    
    def print_summary(self):
        """Test özeti yazdır"""
        total = self.passed + self.failed
        success_rate = (self.passed / total * 100) if total > 0 else 0
        
        print(f"\n{'='*60}")
        print(f"  TEST ÖZETİ")
        print(f"{'='*60}\n")
        
        print(f"Toplam Testler: {total}")
        print(f"\033[92mGeçen Testler: {self.passed}\033[0m")
        print(f"\033[91mBaşarısız Testler: {self.failed}\033[0m")
        print(f"Başarı Oranı: {success_rate:.1f}%")
        
        if self.failed == 0:
            print(f"\n\033[92m✓ Tüm Testler Başarılı - Sistem Çalışıyor!\033[0m")
            return 0
        else:
            print(f"\n\033[91m✗ Bazı Testler Başarısız - Kontrol Edin\033[0m")
            return 1
    
    def export_results(self, filename: str = "test-results.json"):
        """Sonuçları JSON dosyasına kaydet"""
        data = {
            "timestamp": datetime.now().isoformat(),
            "summary": {
                "total": self.passed + self.failed,
                "passed": self.passed,
                "failed": self.failed,
                "success_rate": (self.passed / (self.passed + self.failed) * 100) if (self.passed + self.failed) > 0 else 0
            },
            "results": self.results
        }
        
        with open(filename, "w") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        
        print(f"Sonuçlar kaydedildi: {filename}")


def main():
    checker = KatanaHealthCheck()
    checker.run_all_tests()
    exit_code = checker.print_summary()
    checker.export_results("katana-test-results.json")
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
