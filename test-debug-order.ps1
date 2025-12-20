#!/usr/bin/env pwsh
# Debug script to analyze a specific order

param(
    [Parameter(Mandatory=$true)]
    [string]$OrderNo,
    
    [string]$BaseUrl = "http://localhost:5000",
    
    [string]$ApiKey = "your-api-key-here"
)

Write-Host "🔍 Debugging Order: $OrderNo" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sync/debug/katana-order/$OrderNo" `
        -Method GET `
        -Headers @{ "X-API-Key" = $ApiKey } `
        -ContentType "application/json"
    
    # Display results
    Write-Host "📊 Analysis Results:" -ForegroundColor Yellow
    Write-Host ""
    
    # Found status
    Write-Host "Found in Katana: " -NoNewline
    if ($response.found.inKatana) {
        Write-Host "✅ YES" -ForegroundColor Green
    } else {
        Write-Host "❌ NO" -ForegroundColor Red
    }
    
    Write-Host "Found in Database: " -NoNewline
    if ($response.found.inDatabase) {
        Write-Host "✅ YES" -ForegroundColor Green
    } else {
        Write-Host "❌ NO" -ForegroundColor Red
    }
    Write-Host ""
    
    # Katana Order
    if ($response.katanaOrder) {
        Write-Host "📦 Katana Order:" -ForegroundColor Cyan
        Write-Host "  Order No: $($response.katanaOrder.orderNo)" -ForegroundColor Gray
        Write-Host "  Katana ID: $($response.katanaOrder.id)" -ForegroundColor Gray
        Write-Host "  Customer ID (Katana): $($response.katanaOrder.katanaCustomerId)" -ForegroundColor Gray
        Write-Host "  Status: $($response.katanaOrder.status)" -ForegroundColor Gray
        Write-Host "  Total: $($response.katanaOrder.total) $($response.katanaOrder.currency)" -ForegroundColor Gray
        Write-Host "  Created: $($response.katanaOrder.orderCreatedDate)" -ForegroundColor Gray
        Write-Host "  Rows: $($response.katanaOrder.rowCount)" -ForegroundColor Gray
        Write-Host ""
    }
    
    # Database Order
    if ($response.dbOrder) {
        Write-Host "💾 Database Order:" -ForegroundColor Cyan
        Write-Host "  Order No: $($response.dbOrder.orderNo)" -ForegroundColor Gray
        Write-Host "  Local ID: $($response.dbOrder.id)" -ForegroundColor Gray
        Write-Host "  Katana Order ID: $($response.dbOrder.katanaOrderId)" -ForegroundColor Gray
        Write-Host "  Customer ID (Local): $($response.dbOrder.localCustomerId)" -ForegroundColor Gray
        Write-Host "  Customer Name: $($response.dbOrder.customerName)" -ForegroundColor Gray
        Write-Host "  Customer Reference ID: $($response.dbOrder.customerReferenceId)" -ForegroundColor Gray
        Write-Host "  Status: $($response.dbOrder.status)" -ForegroundColor Gray
        Write-Host "  Total: $($response.dbOrder.total) $($response.dbOrder.currency)" -ForegroundColor Gray
        Write-Host "  Created: $($response.dbOrder.orderCreatedDate)" -ForegroundColor Gray
        Write-Host "  Lines: $($response.dbOrder.lineCount)" -ForegroundColor Gray
        Write-Host "  Synced to Luca: $($response.dbOrder.isSyncedToLuca)" -ForegroundColor Gray
        Write-Host ""
    }
    
    # Customer Mapping
    if ($response.customerMapping) {
        Write-Host "👤 Customer Mapping:" -ForegroundColor Cyan
        Write-Host "  Local ID: $($response.customerMapping.id)" -ForegroundColor Gray
        Write-Host "  Name: $($response.customerMapping.title)" -ForegroundColor Gray
        Write-Host "  Reference ID (Katana): $($response.customerMapping.referenceId)" -ForegroundColor Gray
        Write-Host "  Email: $($response.customerMapping.email)" -ForegroundColor Gray
        Write-Host "  Active: $($response.customerMapping.isActive)" -ForegroundColor Gray
        Write-Host ""
    }
    
    # Analysis
    Write-Host "🔬 Analysis:" -ForegroundColor Yellow
    if ($response.analysis.customerIdMatch -ne "N/A") {
        Write-Host "  Customer Mapping: $($response.analysis.customerIdMatch)" -ForegroundColor Gray
    }
    if ($null -ne $response.analysis.statusMatch) {
        Write-Host "  Status Match: " -NoNewline
        if ($response.analysis.statusMatch) {
            Write-Host "✅ YES" -ForegroundColor Green
        } else {
            Write-Host "❌ NO" -ForegroundColor Red
        }
    }
    if ($null -ne $response.analysis.totalMatch) {
        Write-Host "  Total Match: " -NoNewline
        if ($response.analysis.totalMatch) {
            Write-Host "✅ YES" -ForegroundColor Green
        } else {
            Write-Host "❌ NO" -ForegroundColor Red
        }
    }
    Write-Host ""
    
    # Issues
    Write-Host "⚠️  Issues Found:" -ForegroundColor Yellow
    foreach ($issue in $response.analysis.issues) {
        if ($issue.StartsWith("✅")) {
            Write-Host "  $issue" -ForegroundColor Green
        } elseif ($issue.StartsWith("⚠️")) {
            Write-Host "  $issue" -ForegroundColor Yellow
        } else {
            Write-Host "  $issue" -ForegroundColor Red
        }
    }
    Write-Host ""
    
    # Detailed comparison if both exist
    if ($response.katanaOrder -and $response.dbOrder) {
        Write-Host "📋 Detailed Comparison:" -ForegroundColor Cyan
        
        # Compare rows/lines
        if ($response.katanaOrder.rows -and $response.dbOrder.lines) {
            Write-Host "  Order Lines:" -ForegroundColor Gray
            Write-Host "    Katana Rows: $($response.katanaOrder.rows.Count)" -ForegroundColor Gray
            Write-Host "    DB Lines: $($response.dbOrder.lines.Count)" -ForegroundColor Gray
            
            if ($response.katanaOrder.rows.Count -eq $response.dbOrder.lines.Count) {
                Write-Host "    ✅ Line count matches" -ForegroundColor Green
            } else {
                Write-Host "    ❌ Line count mismatch!" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "✅ Debug completed!" -ForegroundColor Green
    
    # Save full response to JSON file
    $jsonFile = "debug-$OrderNo-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $response | ConvertTo-Json -Depth 10 | Out-File $jsonFile
    Write-Host "📄 Full response saved to: $jsonFile" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "💡 Tip: Check your API key" -ForegroundColor Yellow
        } elseif ($statusCode -eq 403) {
            Write-Host "💡 Tip: This endpoint requires Admin role" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "Usage examples:" -ForegroundColor Cyan
Write-Host "  .\test-debug-order.ps1 -OrderNo SO-56" -ForegroundColor Gray
Write-Host "  .\test-debug-order.ps1 -OrderNo SO-41 -BaseUrl http://localhost:5000" -ForegroundColor Gray
Write-Host "  .\test-debug-order.ps1 -OrderNo SO-47 -ApiKey your-key-here" -ForegroundColor Gray
