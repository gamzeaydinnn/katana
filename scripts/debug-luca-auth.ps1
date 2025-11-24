$base = 'http://85.111.1.49:57005/Yetki'
$auth = "$base/Giris.do"
$outDir = 'scripts\logs\luca-auth-debug'
New-Item -Path $outDir -ItemType Directory -Force | Out-Null

# Credentials as provided
$user = 'Admin'
$pass = 'WebServis'

$attempts = @(
  @{ Name='form_txtKullanici_txtSifre'; Body = @{ 'txtKullanici'=$user; 'txtSifre'=$pass }; ContentType='application/x-www-form-urlencoded' },
  @{ Name='json_user_pwd'; Body = @{ 'username'=$user; 'password'=$pass } ; ContentType='application/json' },
  @{ Name='form_user_pwd'; Body = @{ 'userName'=$user; 'password'=$pass } ; ContentType='application/x-www-form-urlencoded' }
)

foreach ($a in $attempts) {
  $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
  try {
    if ($a.ContentType -eq 'application/json') {
      $bodyRaw = ($a.Body | ConvertTo-Json -Depth 4)
      $resp = Invoke-WebRequest -Uri $auth -Method Post -Body $bodyRaw -ContentType 'application/json' -WebSession $session -UseBasicParsing -ErrorAction Stop
    } else {
      $resp = Invoke-WebRequest -Uri $auth -Method Post -Body $a.Body -ContentType $a.ContentType -WebSession $session -UseBasicParsing -ErrorAction Stop
    }
  } catch {
    $resp = $_.Exception.Response
  }

  $fileBase = Join-Path $outDir $a.Name
  # Save status + headers
  $hdrs = @()
  if ($resp -and $resp.StatusCode) { $hdrs += "StatusCode: $($resp.StatusCode)" } else { $hdrs += "StatusCode: N/A" }
  if ($resp -and $resp.Headers) { $resp.Headers.GetEnumerator() | ForEach-Object { $hdrs += "$($_.Key): $($_.Value)" } }
  $hdrs | Out-File ($fileBase + '.headers.txt') -Encoding utf8

  # Save body
  if ($resp -and $resp.Content) { $resp.Content | Out-File ($fileBase + '.body.txt') -Encoding utf8 } else { "<no content>" | Out-File ($fileBase + '.body.txt') -Encoding utf8 }

  # Save cookies for the base URL
  $cookies = @()
  try { $session.Cookies.GetCookies($base) | ForEach-Object { $cookies += "{0}={1}; Domain={2}; Path={3}; Expires={4}; HttpOnly={5}; Secure={6}" -f $_.Name, $_.Value, $_.Domain, $_.Path, $_.Expires, $_.HttpOnly, $_.Secure } } catch { }
  if ($cookies.Count -eq 0) { "<no cookies>" | Out-File ($fileBase + '.cookies.txt') -Encoding utf8 } else { $cookies | Out-File ($fileBase + '.cookies.txt') -Encoding utf8 }

  Write-Host "Saved attempt $($a.Name) -> $fileBase.*"
}

Write-Host 'All attempts complete. Files in' $outDir
