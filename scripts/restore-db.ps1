Param(
  [Parameter(Mandatory=$true)] [string]$BackupFile,
  # SQL Server
  [Parameter(Mandatory=$true)] [string]$ServerInstance,
  [Parameter(Mandatory=$true)] [string]$Database,
  [string]$Username,
  [string]$Password
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $BackupFile)) {
  throw "Backup file not found: $BackupFile"
}

Write-Host "[Restore] Starting restore (SQL Server) from $BackupFile"

if (-not $ServerInstance -or -not $Database) {
  throw "Provide -ServerInstance and -Database (or set KATANA_SQLSERVER_INSTANCE/KATANA_SQLSERVER_DATABASE)."
}

# Prefer SqlServer module's Restore-SqlDatabase if available, else use sqlcmd
$hasSqlModule = Get-Module -ListAvailable -Name SqlServer | Measure-Object | Select-Object -ExpandProperty Count
if ($hasSqlModule -gt 0) {
  Import-Module SqlServer -ErrorAction Stop
  Write-Host "[Restore][SQL] Using Restore-SqlDatabase (WITH REPLACE)"
  if ($Username -and $Password) {
    $sec = ConvertTo-SecureString $Password -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential($Username, $sec)
    Restore-SqlDatabase -ServerInstance $ServerInstance -Database $Database -BackupFile $BackupFile -ReplaceDatabase -Credential $cred
  } else {
    Restore-SqlDatabase -ServerInstance $ServerInstance -Database $Database -BackupFile $BackupFile -ReplaceDatabase
  }
} else {
  $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
  if (-not $sqlcmd) { throw "sqlcmd not found. Install SQL Server tools or SqlServer PowerShell module." }
  Write-Host "[Restore][SQL] Using sqlcmd RESTORE DATABASE (WITH REPLACE)"
  $quoted = $BackupFile.Replace("'","''")
  $tsql = "RESTORE DATABASE [$Database] FROM DISK = N'$quoted' WITH REPLACE, RECOVERY;"
  if ($Username -and $Password) {
    & $sqlcmd.Path -S $ServerInstance -U $Username -P $Password -Q $tsql
  } else {
    & $sqlcmd.Path -S $ServerInstance -E -Q $tsql
  }
}

Write-Host "[Restore] Done."
