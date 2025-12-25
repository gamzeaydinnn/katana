# Docker Restart Force + Port 5055 Kill Script
param([switch]$All, [switch]$Start, [switch]$Stop, [switch]$KillPort)

if (-not $Start -and -not $Stop -and -not $KillPort -and -not $All) { $All = $true }

# Kill Port 5055
if ($KillPort -or $All) {
    Write-Host 'Port 5055 oldurulup...' -ForegroundColor Cyan
    $conns = netstat -ano | findstr ':5055'
    if ($conns) {
        $conns | ForEach-Object {
            if ($_ -match '(\d+)\s*$') {
                $pid = $matches[1]
                taskkill /PID $pid /F 2> | Out-Null
                Write-Host "PID $pid olduruldu" -ForegroundColor Green
            }
        }
    } else {
        Write-Host 'Port 5055 temiz' -ForegroundColor Green
    }
}

# Stop Docker
if ($Stop -or $All) {
    Write-Host 'Docker durduruluyor...' -ForegroundColor Yellow
    Get-Process -Name 'Docker Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Stop-Service docker -Force -ErrorAction SilentlyContinue
    Write-Host 'Docker durduruldu' -ForegroundColor Green
    Start-Sleep 2
}

# Start Docker
if ($Start -or $All) {
    Write-Host 'Docker baslatiliyor...' -ForegroundColor Cyan
    Start-Service docker -ErrorAction SilentlyContinue
    Start-Sleep 3
    
    if (Test-Path 'C:\Program Files\Docker\Docker\Docker.exe') {
        Start-Process 'C:\Program Files\Docker\Docker\Docker.exe' -WindowStyle Hidden
        Write-Host 'Docker Desktop baslatildi' -ForegroundColor Green
        Write-Host 'Bekleniyor...' -ForegroundColor Yellow
        Start-Sleep 10
    }
    
    if (Test-Path 'C:\Users\GAMZE\Desktop\katana\docker-compose.yml') {
        Write-Host 'Containerlar baslatiliyor...' -ForegroundColor Cyan
        Set-Location 'C:\Users\GAMZE\Desktop\katana'
        docker-compose up -d 2>
        Write-Host 'Containerlar baslatildi' -ForegroundColor Green
    }
}

Write-Host 'TAMAMLANDI!' -ForegroundColor Green
