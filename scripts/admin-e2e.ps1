# Automates admin E2E: login -> create pending -> list -> approve
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1

$base = 'http://localhost:5055'

function Read-ResponseBodyFromException($ex) {
    if ($ex.Response -ne $null) {
        try {
            $resp = $ex.Response
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            return $reader.ReadToEnd()
        }
        catch {
            return "(failed to read response body: $($_.Exception.Message))"
        }
    }
    return $null
}

Write-Host "Logging in..."
$loginBody = @{ Username = 'admin'; Password = 'Katana2025!' } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
}
catch {
    Write-Host "Login failed: $($_.Exception.Message)"
    exit 1
}

$token = $loginResp.Token
if (-not $token) { Write-Host 'No token returned'; exit 1 }
Write-Host "Got token (short): $($token.Substring(0,60))..."
$headers = @{ Authorization = "Bearer $token" }

Write-Host "Creating test pending..."
try {
    $createResp = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments/test-create" -Method Post -Headers $headers -ContentType 'application/json' -Body '{}'
    Write-Host "Create response:`n$($createResp | ConvertTo-Json -Depth 4)"
}
catch {
    $body = Read-ResponseBodyFromException($_.Exception)
    Write-Host "Create failed: $($_.Exception.Message)"
    if ($body) { Write-Host "Body:`n$body" }
    exit 1
}

$pendingId = $createResp.pendingId
if (-not $pendingId) { Write-Host 'No pendingId returned from create'; exit 1 }

Write-Host "Listing latest pendings (top 20):"
try {
    $list = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments" -Method Get -Headers $headers
    Write-Host ($list | ConvertTo-Json -Depth 6)
}
catch {
    Write-Host "List failed: $($_.Exception.Message)"
}

Write-Host "Approving pending id $pendingId..."
try {
    $approve = Invoke-RestMethod -Uri "$base/api/adminpanel/pending-adjustments/$pendingId/approve?approvedBy=admin" -Method Post -Headers $headers
    Write-Host "Approve response:`n$($approve | ConvertTo-Json -Depth 4)"
}
catch {
    $body = Read-ResponseBodyFromException($_.Exception)
    Write-Host "Approve failed: $($_.Exception.Message)"
    if ($body) { Write-Host "Body:`n$body" }
    else { Write-Host 'No response body available.' }
    exit 1
}

Write-Host 'Done.'
