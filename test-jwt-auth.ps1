#!/usr/bin/env pwsh
# JWT Authentication Diagnostic Script

$baseUrl = "http://localhost:5055"
$username = "admin"
$password = "Katana2025!"

Write-Host "=== JWT Authentication Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login and get token
Write-Host "1. Logging in as '$username'..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $username
        password = $password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    $token = $loginResponse.token
    Write-Host "   ✓ Login successful" -ForegroundColor Green
    Write-Host "   Token (first 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "   ✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Decode JWT token (without verification)
Write-Host "2. Decoding JWT token..." -ForegroundColor Yellow
try {
    $parts = $token.Split('.')
    if ($parts.Length -ne 3) {
        throw "Invalid JWT format"
    }

    # Decode header
    $headerJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($parts[0] + "=="))
    $header = $headerJson | ConvertFrom-Json
    Write-Host "   Header:" -ForegroundColor Gray
    Write-Host "     alg: $($header.alg)" -ForegroundColor Gray
    Write-Host "     typ: $($header.typ)" -ForegroundColor Gray
    if ($header.kid) {
        Write-Host "     kid: $($header.kid)" -ForegroundColor Gray
    } else {
        Write-Host "     kid: (not present - expected for HS256)" -ForegroundColor Gray
    }

    # Decode payload
    $payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($parts[1] + "=="))
    $payload = $payloadJson | ConvertFrom-Json
    Write-Host "   Payload:" -ForegroundColor Gray
    Write-Host "     sub: $($payload.sub)" -ForegroundColor Gray
    Write-Host "     role: $($payload.role)" -ForegroundColor Gray
    Write-Host "     iss: $($payload.iss)" -ForegroundColor Gray
    Write-Host "     aud: $($payload.aud)" -ForegroundColor Gray
    
    $exp = [DateTimeOffset]::FromUnixTimeSeconds($payload.exp).LocalDateTime
    $now = Get-Date
    $remaining = ($exp - $now).TotalMinutes
    Write-Host "     exp: $exp (in $([Math]::Round($remaining, 1)) minutes)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "   ✗ Failed to decode token: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Test protected endpoints
Write-Host "3. Testing protected endpoints..." -ForegroundColor Yellow

$endpoints = @(
    @{ Path = "/api/Sync/history"; Method = "GET"; Description = "Sync History" }
    @{ Path = "/api/StockMovementSync/history"; Method = "GET"; Description = "Stock Movement History" }
    @{ Path = "/api/Locations"; Method = "GET"; Description = "Locations" }
)

foreach ($endpoint in $endpoints) {
    Write-Host "   Testing $($endpoint.Description) ($($endpoint.Method) $($endpoint.Path))..." -ForegroundColor Gray
    try {
        $headers = @{
            "Authorization" = "Bearer $token"
            "Accept" = "application/json"
        }

        $response = Invoke-WebRequest -Uri "$baseUrl$($endpoint.Path)" `
            -Method $endpoint.Method `
            -Headers $headers `
            -ErrorAction Stop

        Write-Host "     ✓ Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "     ✗ Status: $statusCode - $($_.Exception.Message)" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "     → Authentication failed - check JWT configuration" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If you see 401 errors above, check:" -ForegroundColor Yellow
Write-Host "  1. JWT Key matches between token generation and validation" -ForegroundColor Gray
Write-Host "  2. Issuer matches: Token iss = Backend ValidIssuer" -ForegroundColor Gray
Write-Host "  3. Audience matches: Token aud = Backend ValidAudience" -ForegroundColor Gray
Write-Host "  4. Token hasn't expired" -ForegroundColor Gray
Write-Host "  5. Backend logs for detailed error messages" -ForegroundColor Gray
