# Docker Desktop'ı başlat ve kontrol et
$maxRetries = 5
$retryCount = 0
$dockerDesktopPath = "C:\Program Files\Docker\Docker\Docker.exe"
$dockerDesktopPath2 = "C:\Program Files\Docker\Docker.exe"

# Docker Desktop bulunabilir konumları
$possiblePaths = @(
    $dockerDesktopPath,
    $dockerDesktopPath2,
    "$env:ProgramFiles\Docker\Docker\resources\dockerd.exe",
    "$env:LocalAppData\Docker\Docker.exe"
)

Write-Host "[*] Docker Desktop başlatılıyor..." -ForegroundColor Cyan

# Docker Desktop'ın PID'ini bul veya başlat
$dockerProcess = Get-Process "Docker Desktop" -ErrorAction SilentlyContinue
if (-not $dockerProcess) {
    # Tüm olası konumları dene
    $started = $false
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            try {
                Start-Process $path -WindowStyle Minimized -ErrorAction SilentlyContinue
                $started = $true
                Write-Host "[+] Docker Desktop başlatıldı: $path" -ForegroundColor Green
                break
            } catch {
                Write-Host "[-] $path konumundan başlatılamadı" -ForegroundColor Yellow
            }
        }
    }
    
    if (-not $started) {
        Write-Host "[-] Docker Desktop başlatılamadı. Lütfen sistem tray'dan manuel başlatın." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[+] Docker Desktop zaten çalışıyor" -ForegroundColor Green
}

# Docker daemon'u kontrol et ve bekle
Write-Host "[*] Docker daemon'u kontrol ediliyor..." -ForegroundColor Cyan
while ($retryCount -lt $maxRetries) {
    Start-Sleep -Seconds 3
    try {
        $result = & docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[+] Docker daemon aktif ve yanıt veriyor!" -ForegroundColor Green
            break
        }
    } catch {
        Write-Host "[-] Bağlanmaya çalışılıyor... ($($retryCount + 1)/$maxRetries)" -ForegroundColor Yellow
        $retryCount++
    }
}

if ($retryCount -eq $maxRetries) {
    Write-Host "[-] Docker daemon'a bağlanılamadı. Lütfen Docker Desktop'ı manuel başlatın." -ForegroundColor Red
    exit 1
}

# Docker konteynerlerini başlat
Write-Host "[*] Docker konteynerler başlatılıyor..." -ForegroundColor Cyan
cd "c:\Users\GAMZE\Desktop\katana"
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "[+] Docker konteynerler başarıyla başlatıldı!" -ForegroundColor Green
    Write-Host "`n[*] Konteyner durumları:" -ForegroundColor Cyan
    docker-compose ps
} else {
    Write-Host "[-] Docker konteynerler başlatılamadı!" -ForegroundColor Red
    exit 1
}
