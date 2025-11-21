param(
    [string]$BaseUrl = "http://85.111.1.49:57005",
    [string]$OrgCode = "7374953",
    [string]$Username = "Admin",
    [string]$Password = "2009Bfm"
)

Write-Host "=== KOZA LOGIN TEST (CORRECT FIELD NAMES) ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "OrgCode: $OrgCode"
Write-Host "Username: $Username"
Write-Host ""

try {
    $formBody = @{
        orgCode = $OrgCode
        user = $Username
        "girisForm.userPassword" = $Password
        "girisForm.captchaInput" = ""
    }

    $resp = Invoke-WebRequest `
        -Uri "$BaseUrl/Yetki/Giris.do" `
        -Method POST `
        -Body $formBody `
        -ContentType "application/x-www-form-urlencoded" `
        -SessionVariable session -ErrorAction Stop

    Write-Host "Status: $($resp.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($resp.Content)" -ForegroundColor White

    if ($session.Cookies) {
        Write-Host "Cookies received:" -ForegroundColor Green
        $session.Cookies.GetCookies($BaseUrl) | ForEach-Object { Write-Host "  $($_.Name)=$($_.Value)" }
    }
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        Write-Host "Body: $($reader.ReadToEnd())" -ForegroundColor DarkGray
    }
}

Write-Host "=== TEST DONE ===" -ForegroundColor Cyan
