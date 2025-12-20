# Parse LUCA SEND_STOCK_CARD_RESPONSE log files and produce a summary
# Usage: & ".\scripts\parse-luca-responses.ps1" [-OutCsv .\luca_responses.csv] [-OutJson .\luca_responses.json]
param(
    [string]$LogsPath = '.\src\Katana.API\bin\Debug\net8.0\logs',
    [string]$Filter = 'SEND_STOCK_CARD_RESPONSE*.txt',
    [string]$OutCsv = '.\luca_responses.csv',
    [string]$OutJson = '.\luca_responses.json'
)

$results = @()

Get-ChildItem -Path $LogsPath -Filter $Filter -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | ForEach-Object {
    $file = $_
    try {
        $raw = Get-Content -Raw -Path $file.FullName -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to read $($file.Name): $($_.Exception.Message)"
        return
    }

    # Try to extract first JSON object from the file
    $json = $null
    if ($raw -match '(\{[\s\S]*\})') {
        $json = $matches[1]
    }
    elseif ($raw -match '(<json>\s*(\{[\s\S]*\})\s*</json>)') {
        $json = $matches[2]
    }

    if ($json) {
        try {
            $obj = $json | ConvertFrom-Json -ErrorAction Stop
            $code = $null
            if ($obj.PSObject.Properties.Match('code')) { $code = $obj.code }
            $skartId = $null
            if ($obj.PSObject.Properties.Match('stkSkart')) {
                if ($obj.stkSkart -ne $null -and $obj.stkSkart.PSObject.Properties.Match('skartId')) { $skartId = $obj.stkSkart.skartId }
            }

            $results += [PSCustomObject]@{
                File = $file.Name
                Modified = $file.LastWriteTime
                Code = $code
                SkartId = $skartId
            }
        }
        catch {
            $results += [PSCustomObject]@{
                File = $file.Name
                Modified = $file.LastWriteTime
                Code = 'PARSE_FAILED'
                SkartId = $null
            }
        }
    }
    else {
        $results += [PSCustomObject]@{
            File = $file.Name
            Modified = $file.LastWriteTime
            Code = 'NO_JSON'
            SkartId = $null
        }
    }
}

# Output summary to console and files
if ($results.Count -eq 0) {
    Write-Output "No response files found matching $Filter under $LogsPath"
    exit 0
}

$results | Sort-Object Modified -Descending | Format-Table -AutoSize

try { $results | Export-Csv -Path $OutCsv -NoTypeInformation -Force } catch { Write-Warning "Failed to write CSV: $($_.Exception.Message)" }
try { $results | ConvertTo-Json -Depth 5 | Out-File -FilePath $OutJson -Force } catch { Write-Warning "Failed to write JSON: $($_.Exception.Message)" }

Write-Output "Summary written to: $OutCsv and $OutJson"