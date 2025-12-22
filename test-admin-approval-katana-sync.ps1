# ========================================
# ADMIN ONAY VE KATANA SENKRONIZASYON TEST SCRIPTI
# ========================================
# Bu script admin onayindan sonra siparislerin Katana'ya
# dogru sekilde gidip gitmedigini kontrol eder.
#
# Test Adimlari:
# 1. Login ve token al
# 2. Mevcut satis siparislerini listele
# 3. Bekleyen (onaylanmamis) siparis bul
# 4. Admin onayi ver
# 5. Katana'da siparis olusturuldu mu kontrol et
# 6. Hata varsa detayli analiz yap

param(
    [string]$BaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!",
    [int]$OrderId = 0,
    [switch]$SkipApproval,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$apiBase = "$BaseUrl/api"
$token = ""

function Write-Step {
    param([string]$Step, [string]$Message)
    Write-Host "[$Step] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[HATA] $Message" -ForegroundColor Red
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[UYARI] $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "      $Message" -ForegroundColor Gray
}

function Get-ApiError {
    param($ErrorObject)
    
    $errorMsg = ""
    if ($ErrorObject.ErrorDetails.Message) {
        try {
            $errorJson = $ErrorObject.ErrorDetails.Message | ConvertFrom-Json
            if ($errorJson.message) { $errorMsg = $errorJson.message }
            elseif ($errorJson.error) { $errorMsg = $errorJson.error }
            else { $errorMsg = $ErrorObject.ErrorDetails.Message }
        } catch {
            $errorMsg = $ErrorObject.ErrorDetails.Message
        }
    } else {
        $errorMsg = $ErrorObject.Exception.Message
    }
    return $errorMsg
}

Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host "ADMIN ONAY - KATANA SYNC TEST" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White
Write-Host "Tarih: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "API: $apiBase"
Write-Host ""

# ========================================
# ADIM 1: LOGIN
# ========================================
Write-Step "1/7" "Login yapiliyor..."

try {
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Success "Login basarili"
        if ($Verbose) {
            Write-Info "Token: $($token.Substring(0, 30))..."
        }
    } else {
        Write-Fail "Token alinamadi"
        exit 1
    }
} catch {
    Write-Fail "Login hatasi: $(Get-ApiError $_)"
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# ========================================
# ADIM 2: SATIS SIPARISI ISTATISTIKLERI
# ========================================
Write-Host ""
Write-Step "2/7" "Siparis istatistikleri aliniyor..."

try {
    $stats = Invoke-RestMethod -Uri "$apiBase/sales-orders/stats" -Method Get -Headers $headers
    
    Write-Success "Istatistikler alindi"
    Write-Info "Toplam Siparis: $($stats.totalOrders)"
    Write-Info "Senkronize: $($stats.syncedOrders)"
    Write-Info "Hatali: $($stats.errorOrders)"
    Write-Info "Bekleyen: $($stats.pendingOrders)"
    Write-Info "Toplam Deger: $($stats.totalValue) TL"
} catch {
    Write-Warn "Istatistikler alinamadi: $(Get-ApiError $_)"
}

# ========================================
# ADIM 3: ONAYLANMAMIS SIPARIS BUL
# ========================================
Write-Host ""
Write-Step "3/7" "Onaylanmamis siparisler araniyor..."

$targetOrder = $null

try {
    # Onaylanmamis siparisleri listele
    $ordersResponse = Invoke-RestMethod -Uri "$apiBase/sales-orders?pageSize=100" -Method Get -Headers $headers
    
    $orderList = @()
    if ($ordersResponse -is [System.Collections.IEnumerable]) {
        $orderList = @($ordersResponse)
    } elseif ($ordersResponse.items) {
        $orderList = @($ordersResponse.items)
    } elseif ($ordersResponse.data) {
        $orderList = @($ordersResponse.data)
    }
    
    Write-Info "Toplam $($orderList.Count) siparis bulundu"
    
    # Belirli bir OrderId verilmisse onu kullan
    if ($OrderId -gt 0) {
        $targetOrder = $orderList | Where-Object { $_.id -eq $OrderId } | Select-Object -First 1
        if (-not $targetOrder) {
            Write-Warn "Belirtilen siparis bulunamadi (ID: $OrderId), detay sorgusu yapiliyor..."
            try {
                $targetOrder = Invoke-RestMethod -Uri "$apiBase/sales-orders/$OrderId" -Method Get -Headers $headers
            } catch {
                Write-Fail "Siparis $OrderId bulunamadi"
                exit 1
            }
        }
    } else {
        # Onaylanmamis siparis bul (Status != APPROVED ve Status != SHIPPED)
        $pendingOrders = $orderList | Where-Object { 
            $_.status -ne "APPROVED" -and 
            $_.status -ne "SHIPPED" -and 
            $_.status -ne "APPROVED_WITH_ERRORS"
        }
        
        if ($pendingOrders.Count -gt 0) {
            $targetOrder = $pendingOrders | Select-Object -First 1
            Write-Success "$($pendingOrders.Count) onaylanmamis siparis bulundu"
        } else {
            # Hata durumundaki siparisleri kontrol et
            $errorOrders = $orderList | Where-Object { $_.status -eq "APPROVED_WITH_ERRORS" }
            if ($errorOrders.Count -gt 0) {
                $targetOrder = $errorOrders | Select-Object -First 1
                Write-Warn "Onaylanmamis siparis yok, hatali siparis kullaniliyor"
            } else {
                Write-Warn "Onaylanmamis veya hatali siparis bulunamadi"
                Write-Info "Mevcut siparisler:"
                $orderList | Select-Object -First 5 | ForEach-Object {
                    Write-Info "  - ID: $($_.id), No: $($_.orderNo), Durum: $($_.status)"
                }
            }
        }
    }
    
    if ($targetOrder) {
        Write-Success "Hedef siparis secildi"
        Write-Info "ID: $($targetOrder.id)"
        Write-Info "Siparis No: $($targetOrder.orderNo)"
        Write-Info "Musteri: $($targetOrder.customerName)"
        Write-Info "Durum: $($targetOrder.status)"
        Write-Info "Toplam: $($targetOrder.total) $($targetOrder.currency)"
        Write-Info "Luca Sync: $($targetOrder.isSyncedToLuca)"
        if ($targetOrder.lastSyncError) {
            Write-Info "Son Hata: $($targetOrder.lastSyncError)"
        }
    }
} catch {
    Write-Fail "Siparis listesi alinamadi: $(Get-ApiError $_)"
    exit 1
}

if (-not $targetOrder) {
    Write-Warn "Test edilecek siparis bulunamadi"
    Write-Host ""
    Write-Host "COZUM ONERILERI:" -ForegroundColor Yellow
    Write-Host "  1. Katana'dan siparis cek: POST $apiBase/sync/katana-sales-orders"
    Write-Host "  2. Belirli siparis test et: .\test-admin-approval-katana-sync.ps1 -OrderId 123"
    Write-Host ""
    exit 0
}

# ========================================
# ADIM 4: SIPARIS DETAYLARINI AL
# ========================================
Write-Host ""
Write-Step "4/7" "Siparis detaylari aliniyor..."

$orderDetail = $null
try {
    $orderDetail = Invoke-RestMethod -Uri "$apiBase/sales-orders/$($targetOrder.id)" -Method Get -Headers $headers
    
    Write-Success "Siparis detaylari alindi"
    Write-Info "Siparis No: $($orderDetail.orderNo)"
    Write-Info "Musteri ID: $($orderDetail.customerId)"
    Write-Info "Musteri: $($orderDetail.customerName)"
    Write-Info "Tarih: $($orderDetail.orderCreatedDate)"
    Write-Info "Durum: $($orderDetail.status)"
    Write-Info "Katana Order ID: $($orderDetail.katanaOrderId)"
    Write-Info "Luca Order ID: $($orderDetail.lucaOrderId)"
    
    $lineCount = 0
    if ($orderDetail.lines) {
        $lineCount = $orderDetail.lines.Count
    }
    Write-Info "Satir Sayisi: $lineCount"
    
    if ($lineCount -eq 0) {
        Write-Warn "DIKKAT: Siparis satirlari bos! Katana'dan tekrar senkronize edin."
    } else {
        Write-Info "Siparis Satirlari:"
        $orderDetail.lines | Select-Object -First 5 | ForEach-Object {
            Write-Info "  - SKU: $($_.sku), Urun: $($_.productName), Miktar: $($_.quantity), Fiyat: $($_.pricePerUnit)"
        }
        if ($lineCount -gt 5) {
            Write-Info "  ... ve $($lineCount - 5) satir daha"
        }
    }
} catch {
    Write-Fail "Siparis detaylari alinamadi: $(Get-ApiError $_)"
}

# ========================================
# ADIM 5: ADMIN ONAYI
# ========================================
Write-Host ""
Write-Step "5/7" "Admin onayi veriliyor..."

if ($SkipApproval) {
    Write-Warn "Onay atlandi (-SkipApproval parametresi)"
} elseif ($orderDetail.status -eq "APPROVED") {
    Write-Warn "Siparis zaten onaylanmis"
} elseif ($orderDetail.status -eq "SHIPPED") {
    Write-Warn "Siparis zaten gonderilmis"
} else {
    try {
        $approveResponse = Invoke-RestMethod -Uri "$apiBase/sales-orders/$($targetOrder.id)/approve" -Method Post -Headers $headers
        
        if ($approveResponse.success) {
            Write-Success "Siparis onaylandi"
            Write-Info "Mesaj: $($approveResponse.message)"
            Write-Info "Yeni Durum: $($approveResponse.orderStatus)"
            if ($approveResponse.katanaOrderId) {
                Write-Info "Katana Order ID: $($approveResponse.katanaOrderId)"
            }
        } else {
            Write-Fail "Onay basarisiz"
            Write-Info "Mesaj: $($approveResponse.message)"
            if ($approveResponse.error) {
                Write-Info "Hata: $($approveResponse.error)"
            }
        }
    } catch {
        $errorMsg = Get-ApiError $_
        Write-Fail "Onay hatasi: $errorMsg"
        
        # Detayli hata analizi
        Write-Host ""
        Write-Host "HATA ANALIZI:" -ForegroundColor Yellow
        
        if ($errorMsg -match "satirlar") {
            Write-Info "SORUN: Siparis satirlari eksik"
            Write-Info "COZUM: Katana'dan siparisleri tekrar cekin"
            Write-Info "       POST $apiBase/sync/katana-sales-orders"
        }
        elseif ($errorMsg -match "musteri|customer") {
            Write-Info "SORUN: Musteri bilgisi eksik veya hatali"
            Write-Info "COZUM: Musterinin Katana'da kayitli oldugundan emin olun"
        }
        elseif ($errorMsg -match "Katana") {
            Write-Info "SORUN: Katana API baglantisi basarisiz"
            Write-Info "COZUM: Katana API ayarlarini kontrol edin"
            Write-Info "       appsettings.json > KatanaApi > BaseUrl, ApiKey"
        }
        elseif ($errorMsg -match "zaten onaylanmis") {
            Write-Info "SORUN: Siparis daha once onaylanmis"
        }
        else {
            Write-Info "Bilinmeyen hata - loglari kontrol edin"
        }
    }
}

# ========================================
# ADIM 6: ONAY SONRASI KONTROL
# ========================================
Write-Host ""
Write-Step "6/7" "Onay sonrasi durum kontrol ediliyor..."

Start-Sleep -Seconds 2  # Katana sync icin kisa bekleme

try {
    $updatedOrder = Invoke-RestMethod -Uri "$apiBase/sales-orders/$($targetOrder.id)" -Method Get -Headers $headers
    
    Write-Success "Guncel durum alindi"
    Write-Info "Siparis No: $($updatedOrder.orderNo)"
    Write-Info "Durum: $($updatedOrder.status)"
    Write-Info "Katana Order ID: $($updatedOrder.katanaOrderId)"
    Write-Info "Luca Order ID: $($updatedOrder.lucaOrderId)"
    Write-Info "Luca Sync: $($updatedOrder.isSyncedToLuca)"
    Write-Info "Son Sync: $($updatedOrder.lastSyncAt)"
    
    if ($updatedOrder.lastSyncError) {
        Write-Warn "Son Hata: $($updatedOrder.lastSyncError)"
    }
    
    # Durum degerlendirmesi
    Write-Host ""
    Write-Host "DEGERLENDIRME:" -ForegroundColor White
    
    if ($updatedOrder.status -eq "APPROVED" -and $updatedOrder.katanaOrderId) {
        Write-Success "Siparis basariyla onaylandi ve Katana'ya gonderildi"
    }
    elseif ($updatedOrder.status -eq "APPROVED" -and -not $updatedOrder.katanaOrderId) {
        Write-Warn "Siparis onaylandi ama Katana'ya gonderilemedi"
        Write-Info "Katana API baglantisini kontrol edin"
    }
    elseif ($updatedOrder.status -eq "APPROVED_WITH_ERRORS") {
        Write-Warn "Siparis onaylandi ama hatalar olustu"
        Write-Info "Hata: $($updatedOrder.lastSyncError)"
    }
    else {
        Write-Info "Siparis durumu: $($updatedOrder.status)"
    }
    
} catch {
    Write-Fail "Guncel durum alinamadi: $(Get-ApiError $_)"
}

# ========================================
# ADIM 7: KATANA KONTROLU
# ========================================
Write-Host ""
Write-Step "7/7" "Katana'da siparis kontrol ediliyor..."

try {
    # Katana baglanti testi
    $katanaTest = Invoke-RestMethod -Uri "$apiBase/katana/test-connection" -Method Get -Headers $headers -ErrorAction SilentlyContinue
    
    if ($katanaTest.success -or $katanaTest.isConnected) {
        Write-Success "Katana baglantisi aktif"
    } else {
        Write-Warn "Katana baglantisi kontrol edilemedi"
    }
} catch {
    Write-Warn "Katana baglanti testi yapilamadi: $(Get-ApiError $_)"
}

# Katana'dan siparisleri cek (eger endpoint varsa)
try {
    $katanaOrders = Invoke-RestMethod -Uri "$apiBase/katana/sales-orders?limit=10" -Method Get -Headers $headers -ErrorAction SilentlyContinue
    
    if ($katanaOrders) {
        $orderList = @()
        if ($katanaOrders -is [System.Collections.IEnumerable]) {
            $orderList = @($katanaOrders)
        } elseif ($katanaOrders.data) {
            $orderList = @($katanaOrders.data)
        }
        
        Write-Info "Katana'da $($orderList.Count) siparis bulundu"
        
        # Bizim siparisimizi ara
        if ($updatedOrder.katanaOrderId) {
            $foundInKatana = $orderList | Where-Object { $_.id -eq $updatedOrder.katanaOrderId }
            if ($foundInKatana) {
                Write-Success "Siparis Katana'da bulundu"
                Write-Info "Katana Order No: $($foundInKatana.orderNo)"
                Write-Info "Katana Status: $($foundInKatana.status)"
            }
        }
    }
} catch {
    Write-Info "Katana siparis listesi alinamadi (endpoint mevcut olmayabilir)"
}

# ========================================
# OZET
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host "TEST OZETI" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White
Write-Host ""
Write-Host "Siparis: $($targetOrder.orderNo) (ID: $($targetOrder.id))"
Write-Host "Baslangic Durumu: $($targetOrder.status)"
if ($updatedOrder) {
    Write-Host "Son Durum: $($updatedOrder.status)"
    $katanaOrderIdDisplay = if ($updatedOrder.katanaOrderId) { $updatedOrder.katanaOrderId } else { 'Yok' }
    Write-Host "Katana Order ID: $katanaOrderIdDisplay"
}
Write-Host ""

# Sorun tespiti
if ($updatedOrder) {
    if ($updatedOrder.status -eq "APPROVED" -and $updatedOrder.katanaOrderId) {
        Write-Host "SONUC: BASARILI - Siparis Katana'ya gonderildi" -ForegroundColor Green
    }
    elseif ($updatedOrder.status -eq "APPROVED_WITH_ERRORS") {
        Write-Host "SONUC: KISMI BASARI - Hatalar var" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "OLASI SORUNLAR:" -ForegroundColor Yellow
        Write-Host "  1. Katana API baglantisi"
        Write-Host "  2. Urun SKU'lari Katana'da bulunamadi"
        Write-Host "  3. Musteri Katana'da olusturulamadi"
        Write-Host "  4. Variant ID'ler eslestirilemedi"
        Write-Host ""
        Write-Host "COZUM ADIMLARI:" -ForegroundColor Cyan
        Write-Host "  1. Backend loglarini kontrol edin: docker logs katana-api"
        Write-Host "  2. Katana API ayarlarini kontrol edin: appsettings.json"
        Write-Host "  3. Urunlerin Katana'da var oldugundan emin olun"
        Write-Host "  4. Hatalari temizleyin: POST $apiBase/sales-orders/clear-errors"
    }
    elseif (-not $updatedOrder.katanaOrderId) {
        Write-Host "SONUC: BASARISIZ - Katana'ya gonderilemedi" -ForegroundColor Red
        Write-Host ""
        Write-Host "KONTROL EDILECEKLER:" -ForegroundColor Yellow
        Write-Host "  1. Katana API Key gecerli mi?"
        Write-Host "  2. Katana API URL dogru mu?"
        Write-Host "  3. Network baglantisi var mi?"
        Write-Host "  4. Rate limit asildi mi?"
    }
}

Write-Host ""
Write-Host "Test tamamlandi: $(Get-Date -Format 'HH:mm:ss')"
Write-Host ""
