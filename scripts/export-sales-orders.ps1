param(
    [string]$ApiBaseUrl = "http://localhost:5055",
    [string]$Username = "admin",
    [string]$Password = "Katana2025!",
    [string]$OutFile = "sales-orders.json",
    [int]$PageSize = 50,
    [string]$Status = "",
    [string]$SyncStatus = ""
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

function Get-SalesOrderPage {
    param(
        [string]$BaseUrl,
        [string]$Token,
        [int]$Page,
        [int]$Size,
        [string]$StatusFilter,
        [string]$SyncStatusFilter
    )

    $query = "page=$Page&pageSize=$Size"
    if ($StatusFilter -and $StatusFilter.Trim().Length -gt 0) {
        $query += "&status=$StatusFilter"
    }
    if ($SyncStatusFilter -and $SyncStatusFilter.Trim().Length -gt 0) {
        $query += "&syncStatus=$SyncStatusFilter"
    }

    $url = "$BaseUrl/api/sales-orders?$query"
    $headers = @{ Authorization = "Bearer $Token" }
    return Invoke-RestMethod -Method Get -Uri $url -Headers $headers
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

Write-Host "Fetching sales orders with details..."

$token = Get-AuthToken -BaseUrl $ApiBaseUrl -User $Username -Pass $Password
$allOrders = New-Object System.Collections.Generic.List[object]

$page = 1
while ($true) {
    $pageData = Get-SalesOrderPage -BaseUrl $ApiBaseUrl -Token $token -Page $page -Size $PageSize -StatusFilter $Status -SyncStatusFilter $SyncStatus
    if (-not $pageData -or $pageData.Count -eq 0) {
        break
    }

    foreach ($order in $pageData) {
        $detail = Get-SalesOrderDetail -BaseUrl $ApiBaseUrl -Token $token -Id $order.id
        $allOrders.Add($detail) | Out-Null
    }

    if ($pageData.Count -lt $PageSize) {
        break
    }

    $page++
}

$allOrders | ConvertTo-Json -Depth 12 | Out-File -FilePath $OutFile -Encoding UTF8
Write-Host "Saved $($allOrders.Count) orders to $OutFile"
