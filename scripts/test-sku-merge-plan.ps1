param(
    [string]$ApiBaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!",
    [int]$OrderId = 0,
    [switch]$Execute,
    [string]$OutFile = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body,
        [hashtable]$Headers = @{}
    )

    $jsonBody = $Body | ConvertTo-Json -Depth 10
    return Invoke-RestMethod -Method Post -Uri $Url -Body $jsonBody -ContentType "application/json" -Headers $Headers
}

function Get-AuthToken {
    param(
        [string]$BaseUrl,
        [string]$User,
        [string]$Pass
    )

    $loginUrl = "$BaseUrl/api/auth/login"
    $loginBody = @{
        username = $User
        password = $Pass
    }

    $response = Invoke-JsonPost -Url $loginUrl -Body $loginBody
    if (-not $response.token) {
        throw "Login failed: token not found in response."
    }
    return $response.token
}

function Get-SalesOrderDetail {
    param(
        [string]$BaseUrl,
        [string]$Token,
        [int]$Id
    )

    $url = "$BaseUrl/api/sales-orders/$Id"
    $headers = @{ Authorization = "Bearer $Token" }
    return Invoke-RestMethod -Method Get -Uri $url -Headers $headers
}

function Sync-SalesOrderToLuca {
    param(
        [string]$BaseUrl,
        [string]$Token,
        [int]$Id
    )

    $url = "$BaseUrl/api/OrderInvoiceSync/sync/$Id"
    $headers = @{ Authorization = "Bearer $Token" }
    return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -ErrorAction Stop
}

if ($OrderId -le 0) {
    throw "OrderId is required. Example: -OrderId 3002"
}

Write-Host "Loading order detail..."
$token = Get-AuthToken -BaseUrl $ApiBaseUrl -User $Username -Pass $Password
$before = Get-SalesOrderDetail -BaseUrl $ApiBaseUrl -Token $token -Id $OrderId

Write-Host "OrderNo: $($before.orderNo)"
Write-Host "KatanaOrderId: $($before.katanaOrderId)"
Write-Host "LineCount: $($before.lines.Count)"
Write-Host "IsSyncedToLuca: $($before.isSyncedToLuca)"
Write-Host "LastSyncAt: $($before.lastSyncAt)"

if (-not $Execute) {
    Write-Host "Execute switch not set. No sync was performed."
    Write-Host "Run with -Execute to trigger Luca invoice sync."
    exit 0
}

Write-Host "Triggering Luca sync (KatanaOrderId grouped)..."
$result = $null
try {
    $result = Sync-SalesOrderToLuca -BaseUrl $ApiBaseUrl -Token $token -Id $OrderId
} catch {
    Write-Host "Sync failed."
    $response = $_.Exception.Response
    if ($response) {
        try {
            $stream = $response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            if ($body) {
                Write-Host "Response body:"
                Write-Host $body
            }
        } catch {
            Write-Host "Failed to read error response body."
        }
    } else {
        Write-Host $_.Exception.Message
    }
    exit 1
}

if ($OutFile -and $OutFile.Trim().Length -gt 0) {
    $result | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutFile -Encoding UTF8
    Write-Host "Result saved to $OutFile"
}

Write-Host "Sync result:"
$result | ConvertTo-Json -Depth 6 | Write-Host

Write-Host "Reloading order detail..."
$after = Get-SalesOrderDetail -BaseUrl $ApiBaseUrl -Token $token -Id $OrderId
Write-Host "IsSyncedToLuca: $($after.isSyncedToLuca)"
Write-Host "LastSyncAt: $($after.lastSyncAt)"
