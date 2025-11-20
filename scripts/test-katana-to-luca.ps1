#!/usr/bin/env pwsh
<#
test-katana-to-luca.ps1
Improved end-to-end test to push Katana products to Luca and collect raw Koza responses.

Usage:
  pwsh .\scripts\test-katana-to-luca.ps1

Outputs saved under `scripts/logs`:
 - login.json
 - products.json
 - push-response.json
 - luca-raw-tail.txt (last part of ./logs/luca-raw.log on host)
 - failed-records.json (recent failed sync records anon)
#>

param(
    [string]$BaseUrl = 'http://localhost:5055',
    [int]$Take = 10,
    [int]$WaitSeconds = 6
)

Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$logDir = Join-Path $scriptDir 'logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }

function Write-LogFile($name, $obj) {
    $path = Join-Path $logDir $name
    $json = $obj | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($path, $json, [System.Text.Encoding]::UTF8)
    Write-Host "Wrote: $path"
}

function Login-Admin {
    param($base)
    $creds = @{ username = 'admin'; password = 'Katana2025!' } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $creds -ErrorAction Stop
        Write-Host "Admin login ok"
        return $resp.token
    } catch {
        Write-Warning "Admin login failed: $($_.Exception.Message)"
        return $null
    }
}

function Get-Products {
    param($base, $token)
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    try {
        $products = Invoke-RestMethod -Uri "$base/api/Products" -Method Get -Headers $headers -ErrorAction Stop
        return $products
    } catch {
        Write-Warning "Failed to list products: $($_.Exception.Message)"
        return $null
    }
}

function Trigger-BatchPush {
    param($base, $token, $take)
    $headers = @{ 'Content-Type' = 'application/json' }
    if ($token) { $headers.Authorization = "Bearer $token" }
    try {
        $url = "$base/api/adminpanel/test-push-products-anon?take=$take"
        Write-Host "Calling POST $url"
        $resp = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -ErrorAction Stop
        return $resp
    } catch {
        Write-Warning "Batch push failed: $($_.Exception.Message)"
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
            try {
                $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $body = $sr.ReadToEnd(); $sr.Close()
                Write-Host "Remote response body:`n$body"
                return @{ error = $_.Exception.Message; responseBody = $body }
            } catch {
                return @{ error = $_.Exception.Message }
            }
        }
        return @{ error = $_.Exception.Message }
    }
}

Write-Host "Base URL: $BaseUrl"

$token = Login-Admin -base $BaseUrl
if ($token) { Write-LogFile -name 'login.json' -obj @{ token = $token; timestamp = (Get-Date).ToString('o') } }

$products = Get-Products -base $BaseUrl -token $token
Write-LogFile -name 'products.json' -obj @{ products = $products; timestamp = (Get-Date).ToString('o') }

if (-not $products) {
    Write-Warning "No products retrieved. Create at least one product and re-run. Exiting."
    exit 1
}

Write-Host "Found $($products.Count) products. Sample IDs: $(([array]$products | Select-Object -First 5 | ForEach-Object { $_.id }) -join ', ')"

$resp = Trigger-BatchPush -base $BaseUrl -token $token -take $Take
Write-LogFile -name 'push-response.json' -obj @{ response = $resp; timestamp = (Get-Date).ToString('o') }

Write-Host "Waiting $WaitSeconds seconds for server to write raw Luca logs..."
Start-Sleep -Seconds $WaitSeconds

# Attempt to read host-mounted luca-raw.log (repo root ./logs)
$projectRoot = Resolve-Path (Join-Path $scriptDir '..')
$hostLogPath = Join-Path $projectRoot 'logs\luca-raw.log'
if (Test-Path $hostLogPath) {
    Write-Host "Found host luca raw log: $hostLogPath -- saving tail to scripts/logs/luca-raw-tail.txt"
    $tail = Get-Content -Path $hostLogPath -Tail 400 -ErrorAction SilentlyContinue
    $outPath = Join-Path $logDir 'luca-raw-tail.txt'
    $tail -join "`n" | Out-File -FilePath $outPath -Encoding utf8
    Write-Host "Wrote: $outPath"
} else {
    Write-Warning "Host luca raw log not found at $hostLogPath. If running in Docker, ensure the API container has been restarted after code changes and that './logs' is mounted."
}

# Also fetch recent anonymous failed sync records and sync logs so we can inspect errors recorded in DB
try {
    $failed = Invoke-RestMethod -Uri "$BaseUrl/api/adminpanel/failed-records-anon?take=50" -Method Get -ErrorAction Stop
    Write-LogFile -name 'failed-records.json' -obj @{ failed = $failed; timestamp = (Get-Date).ToString('o') }
} catch {
    Write-Warning "Unable to fetch failed records anon: $($_.Exception.Message)"
}

Write-Host "Done. Check the files under: $logDir and project './logs/luca-raw.log' for raw request/response evidence."

Write-Host "If luca-raw.log contains 'Response:' with an error (500), paste the last 200-400 lines here and I'll analyze the Koza response body to determine why stock cards are rejected."

Write-Host "Example run: pwsh .\scripts\test-katana-to-luca.ps1 -BaseUrl 'http://localhost:5055' -Take 10 -WaitSeconds 6"
