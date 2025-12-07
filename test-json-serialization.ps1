#!/usr/bin/env pwsh
# Test JSON serialization to see what fields are included

Write-Host "Testing JSON serialization..." -ForegroundColor Cyan

# Get the latest log entry with LUCA JSON REQUEST
$logs = docker logs katana-api-1 --tail 200 2>&1 | Select-String "LUCA JSON REQUEST"

$json = ""
if ($logs) {
    Write-Host "`nLatest JSON being sent to Luca:" -ForegroundColor Yellow
    $logs | Select-Object -Last 1 | ForEach-Object {
        $line = $_.ToString()
        if ($line -match '\{.*\}') {
            $json = $matches[0]
            # Pretty print the JSON
            $json | ConvertFrom-Json | ConvertTo-Json -Depth 10
        }
    }
} else {
    Write-Host "No LUCA JSON REQUEST found in logs" -ForegroundColor Red
    exit 1
}

Write-Host "`nChecking for missing fields..." -ForegroundColor Cyan
$requiredFields = @(
    "gtipKodu",
    "ihracatKategoriNo",
    "detayAciklama",
    "stopajOran",
    "alisIskontoOran1",
    "satisIskontoOran1",
    "perakendeAlisBirimFiyat",
    "perakendeSatisBirimFiyat"
)

foreach ($field in $requiredFields) {
    if ($json -match $field) {
        Write-Host "OK: $field is present" -ForegroundColor Green
    } else {
        Write-Host "MISSING: $field" -ForegroundColor Red
    }
}
