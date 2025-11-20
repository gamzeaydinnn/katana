Param(
    [string]$ApiBase = 'http://localhost:5055',
    [int]$ProductId = 1
)

# Improved send-luca script:
# - writes UTF-8 payload file
# - sets explicit Content-Type with charset
# - sends payload, prints response
# - if response indicates missing 'dto', retries with { "dto": <payload> }

function Write-Utf8File {
    param(
        [string]$Path,
        [object]$Content
    )
    $json = $Content | ConvertTo-Json -Depth 10 -Compress
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.Encoding]::UTF8)
}

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$Url,
        [string]$BodyFile,
        [hashtable]$Headers
    )
    $content = Get-Content -Raw -Encoding UTF8 $BodyFile
    $hdr = @{}
    foreach ($k in $Headers.Keys) { $hdr[$k] = $Headers[$k] }
    try {
        $resp = Invoke-WebRequest -Uri $Url -Method $Method -Body $content -Headers $hdr -ContentType 'application/json; charset=utf-8' -UseBasicParsing
        $status = $resp.StatusCode
        $body = $resp.Content
    }
    catch {
        # Try to extract body and status from the web exception
        $we = $_.Exception
        if ($we -and $we.Response) {
            try {
                $stream = $we.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $body = $reader.ReadToEnd()
            }
            catch { $body = $_.ToString() }
            try { $status = $we.Response.StatusCode } catch { $status = 0 }
        }
        else {
            throw
        }
    }
    return @{ Status = $status; Body = $body }
}

try {
    Write-Host "Using API base: $ApiBase"

    # 1) login to API and get token
    $loginUrl = "$ApiBase/api/auth/login"
    $loginBody = @{ username = 'admin'; password = 'Katana2025!' }
    $loginFile = Join-Path -Path $PSScriptRoot -ChildPath 'login.json'
    Write-Utf8File -Path $loginFile -Content $loginBody
    Write-Host "Logging in..."
    $loginResp = Invoke-JsonRequest -Method 'POST' -Url $loginUrl -BodyFile $loginFile -Headers @{}
    Write-Host "Login Status: $($loginResp.Status) $($loginResp.Reason)"
    if ($loginResp.Status -ne 200) { Write-Host $loginResp.Body; throw 'Login failed' }
    $tokenObj = $loginResp.Body | ConvertFrom-Json
    $token = $tokenObj.token
    if (-not $token) { Write-Host $loginResp.Body; throw 'No token in login response' }
    Write-Host "Token received (length $($token.Length))"

    # 2) prepare correctly-enveloped Luca payload (include both general and Koza-specific fields)
    $payloadInner = @{
        productCode = 'TEST-001'
        productName = 'Test Ürünü'
        unit = 'ADET'
        quantity = 10
        unitPrice = 99.9
        vatRate = 18
        kartAdi = 'Test Ürün'
        kartTuru = 1
        olcumBirimiId = 5
        kartKodu = 'TEST-001'
        kategoriAgacKod = '0000'
        uzunAdi = 'Test Ürün Uzun Adı'
    }

    $putUrl = "$ApiBase/api/products/luca/$ProductId"
    $headers = @{ Authorization = "Bearer $token" }

    # First attempt: send DTO as top-level object (controllers commonly expect this)
    $directFile = Join-Path -Path $PSScriptRoot -ChildPath 'put.json'
    Write-Utf8File -Path $directFile -Content $payloadInner
    Write-Host "Sending direct DTO PUT to $putUrl... (file: $directFile)"
    $putResp = Invoke-JsonRequest -Method 'PUT' -Url $putUrl -BodyFile $directFile -Headers $headers
    Write-Host "Direct PUT Status: $($putResp.Status)"
    Write-Host "Direct Response Body:`n$($putResp.Body)`n"

    if ($putResp.Status -eq 200) {
        Write-Host "Direct PUT succeeded."
    }
    else {
        # If server complains about missing 'dto' or direct binding, try enveloped
        if ($putResp.Body -match 'dto' -or $putResp.Body -match 'The dto field is required') {
            Write-Host "Server expects envelope. Retrying with { dto: ... }"
            $envelope = @{ dto = $payloadInner }
            $envFile = Join-Path -Path $PSScriptRoot -ChildPath 'put-enveloped.json'
            Write-Utf8File -Path $envFile -Content $envelope
            $putResp2 = Invoke-JsonRequest -Method 'PUT' -Url $putUrl -BodyFile $envFile -Headers $headers
            Write-Host "Enveloped PUT Status: $($putResp2.Status)"
            Write-Host "Enveloped Response Body:`n$($putResp2.Body)`n"
        }
        elseif ([int]$putResp.Status -eq 404 -or ($putResp.Body -match 'Ürün bulunamadı' -or $putResp.Body -match 'not found')) {
            Write-Host "Product $ProductId not found in local DB. Creating a test product and retrying..."
            # Create a test product via API
            $createUrl = "$ApiBase/api/products"
            $createBody = @{
                name = $payloadInner.productName
                sku = $payloadInner.productCode
                price = $payloadInner.unitPrice
                stock = $payloadInner.quantity
                categoryId = 1
            }
            $createFile = Join-Path -Path $PSScriptRoot -ChildPath 'create-product.json'
            Write-Utf8File -Path $createFile -Content $createBody
            Write-Host "Creating product via $createUrl (file: $createFile)"
            $createResp = Invoke-JsonRequest -Method 'POST' -Url $createUrl -BodyFile $createFile -Headers $headers
            Write-Host "Create Status: $($createResp.Status)"
            Write-Host "Create Response Body:`n$($createResp.Body)`n"
            if ([int]$createResp.Status -eq 201 -or [int]$createResp.Status -eq 200) {
                try {
                    $created = $createResp.Body | ConvertFrom-Json
                    $newId = $created.id
                }
                catch {
                    # Try alternative property name
                    $created = $createResp.Body | ConvertFrom-Json -ErrorAction SilentlyContinue
                    $newId = $created.Id
                }

                if (-not $newId) {
                    Write-Host "Failed to determine created product id. Aborting."
                }
                else {
                    Write-Host "Created product with id $newId. Retrying Luca PUT..."
                    $putUrl2 = "$ApiBase/api/products/luca/$newId"
                    # Update payload productCode/productName to match created product
                    $payloadInner.productCode = $createBody.sku
                    $payloadInner.productName = $createBody.name
                    $directFile2 = Join-Path -Path $PSScriptRoot -ChildPath 'put.json'
                    Write-Utf8File -Path $directFile2 -Content $payloadInner
                    $putRespRetry = Invoke-JsonRequest -Method 'PUT' -Url $putUrl2 -BodyFile $directFile2 -Headers $headers
                    Write-Host "Retry PUT Status: $($putRespRetry.Status)"
                    Write-Host "Retry Response Body:`n$($putRespRetry.Body)`n"
                }
            }
            else {
                Write-Host "Product creation failed; not retrying PUT."
            }
        }
        else {
            Write-Host "Server returned error for direct PUT. Not retrying with envelope."
        }
    }
}
catch {
    Write-Host "ERROR: $_"
    exit 1
}

Write-Host "Done. Files created: login.json, put-enveloped.json."