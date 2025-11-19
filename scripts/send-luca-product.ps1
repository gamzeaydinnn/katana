param(
    [string]$BaseUrl = 'http://localhost:5055',
    [string]$Username = 'admin',
    [string]$Password = 'Katana2025!',
    [int]$ProductId = 1001,
    [string]$ProductCode = 'TEST-SKU-001',
    [string]$ProductName = 'Test Ürünü',
    [string]$Unit = 'ADET',
    [int]$Quantity = 10,
    [decimal]$UnitPrice = 125.50,
    [int]$VatRate = 18
)

function ExitWithError($msg, $code = 1) {
    Write-Host "ERROR: $msg" -ForegroundColor Red
    exit $code
}

# 1) Login
Write-Host "Logging in to $BaseUrl/api/auth/login as $Username..."
$loginBody = @{ Username = $Username; Password = $Password } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body $loginBody -ErrorAction Stop
} catch {
    ExitWithError "Login request failed: $($_.Exception.Message)"
}

# 2) Extract token (try common locations)
$token = $null
if ($null -ne $loginResp.token) { $token = $loginResp.token }
elseif ($null -ne $loginResp.data -and $null -ne $loginResp.data.token) { $token = $loginResp.data.token }
elseif ($null -ne $loginResp.accessToken) { $token = $loginResp.accessToken }

if (-not $token) {
    Write-Host "Login response:"; $loginResp | Format-List -Force
    ExitWithError "Could not locate auth token in login response."
}

Write-Host "Got token (length: $($token.Length)). Decoding payload to inspect roles..."
try {
    $parts = $token.Split('.')
    $payload = $parts[1]
    # Base64 padding fix
    switch ($payload.Length % 4) {
        2 { $payload += '==' }
        3 { $payload += '=' }
    }
    $jsonPayload = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload)) | ConvertFrom-Json
    Write-Host "Token payload:"; $jsonPayload | Format-List -Force
} catch {
    Write-Host "Warning: failed to decode JWT payload: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3) Prepare PUT body
$putBody = @{
    productCode = $ProductCode
    productName = $ProductName
    unit = $Unit
    quantity = $Quantity
    unitPrice = $UnitPrice
    vatRate = $VatRate
} | ConvertTo-Json

Write-Host "Sending PUT to $BaseUrl/api/Products/luca/$ProductId ..."
$headers = @{ Authorization = "Bearer $token" }
try {
    $putResp = Invoke-RestMethod -Uri "$BaseUrl/api/Products/luca/$ProductId" -Method Put -Body $putBody -ContentType 'application/json' -Headers $headers -ErrorAction Stop
    Write-Host "PUT response:"; $putResp | Format-List -Force
} catch {
    # If unauthorized or server error, show message and exit non-zero
    if ($_.Exception.Response -ne $null) {
        try { $status = $_.Exception.Response.StatusCode.value__ } catch {}
        if ($status) { Write-Host "Request failed with HTTP status $status" -ForegroundColor Red }
        try { $_.Exception.Response.GetResponseStream() | ForEach-Object { [System.IO.StreamReader]::new($_).ReadToEnd() } | Write-Host }
        catch {}
    }
    ExitWithError "PUT request failed: $($_.Exception.Message)"
}

# 4) Verify by querying Luca-style products
Write-Host "Querying $BaseUrl/api/Products/luca to verify creation..."
try {
    $list = Invoke-RestMethod -Uri "$BaseUrl/api/Products/luca" -Method Get -ErrorAction Stop
} catch {
    ExitWithError "GET /api/Products/luca failed: $($_.Exception.Message)"
}

if ($null -ne $list.data) {
    $found = $list.data | Where-Object { ($_.productCode -eq $ProductCode) -or ($_.productName -like "*$ProductName*") }
    if ($found) {
        Write-Host "Product found in Luca-style list:" -ForegroundColor Green
        $found | Format-List -Force
        exit 0
    } else {
        Write-Host "Product not found in response data. Dumping response summary:" -ForegroundColor Yellow
        $list | Format-List -Force
        ExitWithError "Verification failed: product not present in /api/Products/luca"
    }
} else {
    Write-Host "Unexpected response shape from GET /api/Products/luca:" -ForegroundColor Yellow
    $list | Format-List -Force
    ExitWithError "Verification failed: /api/Products/luca returned no data"
}
