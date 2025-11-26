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
    $path = "scripts/logs/$Name.json"
    $Data | ConvertTo-Json -Depth 10 | Set-Content $path -Encoding UTF8
    Write-Host "Saved: $path" -ForegroundColor Gray
}

try {
    # 1. LOGIN
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

    Write-Host " Login: $($loginResponse.StatusCode)" -ForegroundColor Green
    
    $cookie = $session.Cookies.GetCookies($BaseUrl) | Where-Object { $_.Name -eq "JSESSIONID" }
    Write-Host " Cookie: $($cookie.Value)" -ForegroundColor Yellow

    # 2. GET BRANCHES
    Write-Step "2. GET BRANCHES"
    $branchesResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $branches = $branchesResponse.Content | ConvertFrom-Json
    Save-Response "branches-detailed" $branches

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
        # If response is different shape, try list
        if ($branches.list) {
            $targetBranch = $branches.list | Where-Object { $_.id -eq $ForcedBranchId -or $_.orgSirketSubeId -eq $ForcedBranchId } | Select-Object -First 1
        }
    }

    if ($targetBranch) {
        Write-Host " Found branch:" -ForegroundColor Green
        Write-Host "   ID: $($targetBranch.id)"
        Write-Host "   orgSirketSubeId: $($targetBranch.orgSirketSubeId)"
        Write-Host "   sirketSubeId: $($targetBranch.sirketSubeId)"
        Write-Host "   Name: $($targetBranch.sirketSubeAdi)"
        
        $branchIdToUse = if ($targetBranch.orgSirketSubeId) { 
            $targetBranch.orgSirketSubeId 
        } elseif ($targetBranch.sirketSubeId) { 
            $targetBranch.sirketSubeId 
        } else { 
            $targetBranch.id 
        }
    } else {
        Write-Host " Branch $ForcedBranchId NOT FOUND!" -ForegroundColor Red
        Write-Host "Available branches:" -ForegroundColor Yellow
        if ($branches.data) {
            $branches.data | ForEach-Object {
                Write-Host "  - ID: $($_.id), orgSirketSubeId: $($_.orgSirketSubeId), Name: $($_.sirketSubeAdi)"
            }
        } elseif ($branches.list) {
            $branches.list | ForEach-Object { Write-Host "  - ID: $($_.id), orgSirketSubeId: $($_.orgSirketSubeId), Name: $($_.ack)" }
        }
        exit 1
    }

    # 3. CHANGE BRANCH - Test Multiple Formats
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
        Write-Host "`nAttempt $($i + 1): $($attempt | ConvertTo-Json -Compress)" -ForegroundColor Cyan

        try {
            $changeBody = $attempt | ConvertTo-Json
            
            $changeResponse = Invoke-WebRequest `
                -Uri "${BaseUrl}GuncelleYtkSirketSubeDegistir.do" `
                -Method Post `
                -Body $changeBody `
                -ContentType "application/json; charset=utf-8" `
                -WebSession $session `
                -UseBasicParsing

            $changeResult = $changeResponse.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
            Save-Response "change-branch-attempt-$($i + 1)" $changeResult

            if ($changeResult) {
                if ($changeResult.success -or $changeResult.code -eq 0 -or $changeResult."@message") {
                    Write-Host " SUCCESS with attempt $($i + 1)" -ForegroundColor Green
                    $successfulAttempt = $i + 1
                    break
                } else {
                    Write-Host " Response: $($changeResult | ConvertTo-Json -Compress)" -ForegroundColor Yellow
                }
            } else {
                Write-Host " Non-JSON change-branch response; saved to logs" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        }

        Start-Sleep -Milliseconds 500
    }

    # 4. VERIFY
    Write-Step "4. VERIFY BRANCH SELECTION"

    $verifyResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $verifyData = $verifyResponse.Content | ConvertFrom-Json
    Save-Response "verify-branch" $verifyData

    $currentBranch = $null
    if ($verifyData.data) {
        $currentBranch = $verifyData.data | Where-Object { $_.secili -eq $true -or $_.selected -eq $true } | Select-Object -First 1
    } elseif ($verifyData.list) {
        $currentBranch = $verifyData.list | Where-Object { $_.selected -eq $true -or $_.secili -eq $true } | Select-Object -First 1
    }

    if ($currentBranch) {
        Write-Host " CURRENT BRANCH:" -ForegroundColor Green
        Write-Host "   ID: $($currentBranch.id)"
        Write-Host "   orgSirketSubeId: $($currentBranch.orgSirketSubeId)"
        Write-Host "   Name: $($currentBranch.sirketSubeAdi)"
        Write-Host "   Selected: $($currentBranch.secili)"
        
        if ($currentBranch.orgSirketSubeId -eq $branchIdToUse -or $currentBranch.id -eq $branchIdToUse) {
            Write-Host "`n🎉 BRANCH SELECTION SUCCESSFUL!" -ForegroundColor Green
            Write-Host "Successful attempt was: #$successfulAttempt"
        } else {
            Write-Host "`n WARNING: Different branch selected!" -ForegroundColor Yellow
        }
    } else {
        Write-Host " NO BRANCH SELECTED!" -ForegroundColor Red
    }

    Write-Host "`n All responses saved to scripts/logs/" -ForegroundColor Cyan

} catch {
    Write-Host "`n ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
}
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
    $path = "scripts/logs/$Name.json"
    $Data | ConvertTo-Json -Depth 10 | Set-Content $path -Encoding UTF8
    Write-Host "Saved: $path" -ForegroundColor Gray
}

try {
    # 1. LOGIN
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

    Write-Host "Login: $($loginResponse.StatusCode)" -ForegroundColor Green
    
    $cookie = $session.Cookies.GetCookies($BaseUrl) | Where-Object { $_.Name -eq "JSESSIONID" }
    Write-Host " Cookie: $($cookie.Value)" -ForegroundColor Yellow

    # 2. GET BRANCHES
    Write-Step "2. GET BRANCHES"
    $branchesResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $branches = $branchesResponse.Content | ConvertFrom-Json
    Save-Response "branches-detailed" $branches

    if ($branches.data) { $count = $branches.data.Count } else { $count = 0 }
    Write-Host " Total branches: $count" -ForegroundColor Cyan
    
    $targetBranch = $null
    if ($branches.data) {
        $targetBranch = $branches.data | Where-Object { 
            $_.id -eq $ForcedBranchId -or 
            $_.orgSirketSubeId -eq $ForcedBranchId -or
            $_.sirketSubeId -eq $ForcedBranchId
        } | Select-Object -First 1
    } else {
        # If response is different shape, try list
        if ($branches.list) {
            $targetBranch = $branches.list | Where-Object { $_.id -eq $ForcedBranchId -or $_.orgSirketSubeId -eq $ForcedBranchId } | Select-Object -First 1
        }
    }

    if ($targetBranch) {
        Write-Host "✅ Found branch:" -ForegroundColor Green
        Write-Host "   ID: $($targetBranch.id)"
        Write-Host "   orgSirketSubeId: $($targetBranch.orgSirketSubeId)"
        Write-Host "   sirketSubeId: $($targetBranch.sirketSubeId)"
        Write-Host "   Name: $($targetBranch.sirketSubeAdi)"
        
        $branchIdToUse = if ($targetBranch.orgSirketSubeId) { 
            $targetBranch.orgSirketSubeId 
        } elseif ($targetBranch.sirketSubeId) { 
            $targetBranch.sirketSubeId 
        } else { 
            $targetBranch.id 
        }
    } else {
        Write-Host "❌ Branch $ForcedBranchId NOT FOUND!" -ForegroundColor Red
        Write-Host "Available branches:" -ForegroundColor Yellow
        if ($branches.data) {
            $branches.data | ForEach-Object {
                Write-Host "  - ID: $($_.id), orgSirketSubeId: $($_.orgSirketSubeId), Name: $($_.sirketSubeAdi)"
            }
        } elseif ($branches.list) {
            $branches.list | ForEach-Object { Write-Host "  - ID: $($_.id), orgSirketSubeId: $($_.orgSirketSubeId), Name: $($_.ack)" }
        }
        exit 1
    }

    # 3. CHANGE BRANCH - Test Multiple Formats
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
        Write-Host "`nAttempt $($i + 1): $($attempt | ConvertTo-Json -Compress)" -ForegroundColor Cyan

        try {
            $changeBody = $attempt | ConvertTo-Json
            
            $changeResponse = Invoke-WebRequest `
                -Uri "${BaseUrl}GuncelleYtkSirketSubeDegistir.do" `
                -Method Post `
                -Body $changeBody `
                -ContentType "application/json; charset=utf-8" `
                -WebSession $session `
                -UseBasicParsing

            $changeResult = $changeResponse.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
            Save-Response "change-branch-attempt-$($i + 1)" $changeResult

            if ($changeResult) {
                if ($changeResult.success -or $changeResult.code -eq 0 -or $changeResult."@message") {
                    Write-Host "✅ SUCCESS with attempt $($i + 1)" -ForegroundColor Green
                    $successfulAttempt = $i + 1
                    break
                } else {
                    Write-Host "⚠️ Response: $($changeResult | ConvertTo-Json -Compress)" -ForegroundColor Yellow
                }
            } else {
                Write-Host "⚠️ Non-JSON change-branch response; saved to logs" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        }

        Start-Sleep -Milliseconds 500
    }

    # 4. VERIFY
    Write-Step "4. VERIFY BRANCH SELECTION"

    $verifyResponse = Invoke-WebRequest `
        -Uri "${BaseUrl}YdlUserResponsibilityOrgSs.do" `
        -Method Post `
        -Body "{}" `
        -ContentType "application/json; charset=utf-8" `
        -WebSession $session `
        -UseBasicParsing

    $verifyData = $verifyResponse.Content | ConvertFrom-Json
    Save-Response "verify-branch" $verifyData

    $currentBranch = $null
    if ($verifyData.data) {
        $currentBranch = $verifyData.data | Where-Object { $_.secili -eq $true -or $_.selected -eq $true } | Select-Object -First 1
    } elseif ($verifyData.list) {
        $currentBranch = $verifyData.list | Where-Object { $_.selected -eq $true -or $_.secili -eq $true } | Select-Object -First 1
    }

    if ($currentBranch) {
        Write-Host "✅ CURRENT BRANCH:" -ForegroundColor Green
        Write-Host "   ID: $($currentBranch.id)"
        Write-Host "   orgSirketSubeId: $($currentBranch.orgSirketSubeId)"
        Write-Host "   Name: $($currentBranch.sirketSubeAdi)"
        Write-Host "   Selected: $($currentBranch.secili)"
        
        if ($currentBranch.orgSirketSubeId -eq $branchIdToUse -or $currentBranch.id -eq $branchIdToUse) {
            Write-Host "`n🎉 BRANCH SELECTION SUCCESSFUL!" -ForegroundColor Green
            Write-Host "Successful attempt was: #$successfulAttempt"
        } else {
            Write-Host "`n⚠️ WARNING: Different branch selected!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ NO BRANCH SELECTED!" -ForegroundColor Red
    }

    Write-Host "`n📁 All responses saved to scripts/logs/" -ForegroundColor Cyan

} catch {
    Write-Host "`n❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
}