param(
    [string]$BaseUrl = "http://85.111.1.49:57005/Yetki/",
    [string]$MemberNumber = "1422649",
    [string]$UserName = "Admin",
    [string]$Password = "WebServis",
    [int]$ForcedBranchId = 854
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "`n=== $Message ===" -ForegroundColor $Color
}

function Save-Response {
    param([string]$Name, $Data)
    $logDir = Join-Path $PSScriptRoot 'logs'
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
    $path = Join-Path $logDir "$Name.json"
    $Data | ConvertTo-Json -Depth 10 | Set-Content $path -Encoding UTF8
    Write-Host "Saved: $path" -ForegroundColor Gray
}

try {
    Write-Step "1. LOGIN"
    $loginBody = @{
        uyeNo = $MemberNumber
        kullaniciAdi = $UserName
        sifre = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}Giris.do" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json; charset=utf-8" `
        -SessionVariable session `
        -UseBasicParsing

    Write-Host "Login status: $($loginResponse.StatusCode)" -ForegroundColor Green

    $cookie = $session.Cookies.GetCookies($BaseUrl) | Where-Object { $_.Name -eq "JSESSIONID" }
    Write-Host "Cookie: $($cookie.Value)" -ForegroundColor Yellow

    Write-Step "2. GET BRANCHES"
    $branchesResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $branches = $branchesResponse.Content | ConvertFrom-Json
    Save-Response "branches-detailed-fixed" $branches

    if ($branches.data) { $count = $branches.data.Count } else { $count = 0 }
    Write-Host "Total branches: $count" -ForegroundColor Cyan

    $targetBranch = $null
    if ($branches.data) {
        $targetBranch = $branches.data | Where-Object { 
            $_.id -eq $ForcedBranchId -or 
            $_.orgSirketSubeId -eq $ForcedBranchId -or
            $_.sirketSubeId -eq $ForcedBranchId
        } | Select-Object -First 1
    } else {
        if ($branches.list) {
            $targetBranch = $branches.list | Where-Object { $_.id -eq $ForcedBranchId -or $_.orgSirketSubeId -eq $ForcedBranchId } | Select-Object -First 1
        }
    }

    if (-not $targetBranch) {
        Write-Host "Branch $ForcedBranchId not found" -ForegroundColor Red
        exit 1
    }

    $branchIdToUse = if ($targetBranch.orgSirketSubeId) { $targetBranch.orgSirketSubeId } elseif ($targetBranch.sirketSubeId) { $targetBranch.sirketSubeId } else { $targetBranch.id }

    Write-Step "3. CHANGE BRANCH (Multiple Attempts)"
    $attempts = @(
        @{ orgSirketSubeId = $branchIdToUse },
        @{ sirketSubeId = $branchIdToUse },
        @{ id = $branchIdToUse },
        @{ orgSirketSubeId = [string]$branchIdToUse },
        @{ orgSirketSubeId = [int]$branchIdToUse }
    )

    $successfulAttempt = $null

    for ($i=0; $i -lt $attempts.Count; $i++) {
        $attempt = $attempts[$i]
        Write-Host "Attempt $($i + 1): $($attempt | ConvertTo-Json -Compress)" -ForegroundColor Cyan
        try {
            $changeBody = $attempt | ConvertTo-Json
            $changeResponse = Invoke-WebRequest `
                -Uri "${BaseUrl}GuncelleYtkSirketSubeDegistir.do" `
                -Method Post `
                -Body $changeBody `
                -ContentType "application/json; charset=utf-8" `
                -WebSession $session `
                -UseBasicParsing

            $raw = $changeResponse.Content
            $json = $null
            try { $json = $raw | ConvertFrom-Json -ErrorAction Stop } catch { }
            if ($json) { $toSave = $json } else { $toSave = @{ raw = $raw } }
            Save-Response "change-branch-attempt-fixed-$($i + 1)" $toSave

            if ($json) {
                if ($json.success -or $json.code -eq 0 -or $json."@message") {
                    Write-Host "SUCCESS with attempt $($i + 1)" -ForegroundColor Green
                    $successfulAttempt = $i + 1
                    break
                } else {
                    Write-Host "Response: $($json | ConvertTo-Json -Compress)" -ForegroundColor Yellow
                }
            } else {
                Write-Host "Non-JSON response saved" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        Start-Sleep -Milliseconds 500
    }

    Write-Step "4. VERIFY BRANCH SELECTION"
    $verifyResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $verifyData = $verifyResponse.Content | ConvertFrom-Json
    Save-Response "verify-branch-fixed" $verifyData

    $currentBranch = $null
    if ($verifyData.data) {
        $currentBranch = $verifyData.data | Where-Object { $_.secili -eq $true -or $_.selected -eq $true } | Select-Object -First 1
    } elseif ($verifyData.list) {
        $currentBranch = $verifyData.list | Where-Object { $_.selected -eq $true -or $_.secili -eq $true } | Select-Object -First 1
    }

    if ($currentBranch) {
        Write-Host "CURRENT BRANCH: ID=$($currentBranch.id), org=$($currentBranch.orgSirketSubeId), Name=$($currentBranch.sirketSubeAdi)" -ForegroundColor Green
        if ($currentBranch.orgSirketSubeId -eq $branchIdToUse -or $currentBranch.id -eq $branchIdToUse) {
            Write-Host "BRANCH SELECTION SUCCESSFUL (attempt #$successfulAttempt)" -ForegroundColor Green
        } else {
            Write-Host "WARNING: Different branch selected" -ForegroundColor Yellow
        }
    } else {
        Write-Host "NO BRANCH SELECTED" -ForegroundColor Red
    }

    Write-Host "All responses saved to scripts/logs/" -ForegroundColor Cyan

} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
}
