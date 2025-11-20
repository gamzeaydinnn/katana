#!/usr/bin/env pwsh
<#
push-luca-test.ps1
Quick test script to trigger Katana -> Luca forwarding.
It lists local products, optionally logs in as admin, and calls
POST /api/adminpanel/test-push-products-anon to exercise the Luca pipeline.

Usage:
  pwsh .\scripts\push-luca-test.ps1

Notes:
 - Ensure API is running (default http://localhost:5055).
 - This script writes simple logs to `scripts/logs`.
#>

param(
    [string]$BaseUrl = 'http://localhost:5055',
    [switch]$UseAdminLogin = $true
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
    param($base, $token)
    $headers = @{
        'Content-Type' = 'application/json'
    }
    if ($token) { $headers.Authorization = "Bearer $token" }
    try {
        Write-Host "Calling POST $base/api/adminpanel/test-push-products-anon"
        $resp = Invoke-RestMethod -Uri "$base/api/adminpanel/test-push-products-anon" -Method Post -Headers $headers -ErrorAction Stop
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

$token = $null
if ($UseAdminLogin) {
    $token = Login-Admin -base $BaseUrl
    if ($token) { Write-LogFile -name 'login.json' -obj @{ token = $token; timestamp = (Get-Date).ToString('o') } }
}

$products = Get-Products -base $BaseUrl -token $token
Write-LogFile -name 'products.json' -obj @{ products = $products; timestamp = (Get-Date).ToString('o') }

if (-not $products) {
    Write-Warning "No products retrieved. Create at least one product (POST /api/products) and re-run. Exiting."
    exit 1
}

Write-Host "Found $($products.Count) products. Sample IDs: $(([array]$products | Select-Object -First 5 | ForEach-Object { $_.id }) -join ', ')"

$resp = Trigger-BatchPush -base $BaseUrl -token $token
Write-LogFile -name 'push-response.json' -obj @{ response = $resp; timestamp = (Get-Date).ToString('o') }

Write-Host "Done. Logs saved under: $logDir"
Write-Host "Tail API logs while you re-run this if you need request/remote debug.`nExample to tail logs:`nGet-Content -Path 'src/Katana.API/logs/app-*.log' -Wait -Tail 200"
