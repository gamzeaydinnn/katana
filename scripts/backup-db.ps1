Param(
  # SQL Server settings
  [Parameter(Mandatory=$true)] [string]$ServerInstance,
  [Parameter(Mandatory=$true)] [string]$Database,
  [string]$Username,
  [string]$Password,

  # Backup output
  [string]$BackupDir = $env:KATANA_BACKUP_DIR ?? (Join-Path -Path (Get-Location) -ChildPath "backups"),
  [int]$RetentionDays = [int]($env:KATANA_BACKUP_RETENTION_DAYS ?? 30)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $BackupDir)) {
  New-Item -ItemType Directory -Path $BackupDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDir "katana_$timestamp.bak"

Write-Host "[Backup] Starting backup (SQL Server) -> $backupFile"

if (-not $ServerInstance -or -not $Database) {
  throw "Provide -ServerInstance and -Database (or set KATANA_SQLSERVER_INSTANCE/KATANA_SQLSERVER_DATABASE)."
}

# Prefer SqlServer module's Backup-SqlDatabase if available, else use sqlcmd
$hasSqlModule = Get-Module -ListAvailable -Name SqlServer | Measure-Object | Select-Object -ExpandProperty Count
if ($hasSqlModule -gt 0) {
  Import-Module SqlServer -ErrorAction Stop
  Write-Host "[Backup][SQL] Using Backup-SqlDatabase"
  if ($Username -and $Password) {
    $sec = ConvertTo-SecureString $Password -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential($Username, $sec)
    Backup-SqlDatabase -ServerInstance $ServerInstance -Database $Database -BackupFile $backupFile -CopyOnly -Initialize -Credential $cred
  } else {
    Backup-SqlDatabase -ServerInstance $ServerInstance -Database $Database -BackupFile $backupFile -CopyOnly -Initialize
  }
} else {
  $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
  if (-not $sqlcmd) { throw "sqlcmd not found. Install SQL Server tools or SqlServer PowerShell module." }
  Write-Host "[Backup][SQL] Using sqlcmd BACKUP DATABASE"
  $quoted = $backupFile.Replace("'","''")
  $tsql = "BACKUP DATABASE [$Database] TO DISK = N'$quoted' WITH INIT, COPY_ONLY, STATS = 10;"
  if ($Username -and $Password) {
    & $sqlcmd.Path -S $ServerInstance -U $Username -P $Password -Q $tsql
  } else {
    & $sqlcmd.Path -S $ServerInstance -E -Q $tsql
  }
}

Write-Host "[Backup] Backup created: $backupFile"

# Retention cleanup
if ($RetentionDays -gt 0) {
  Write-Host "[Backup] Cleaning backups older than $RetentionDays days in $BackupDir"
  Get-ChildItem -Path $BackupDir -Filter "katana_*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } |
    ForEach-Object {
      Write-Host "[Backup] Deleting old backup: $($_.FullName)"
      Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[Backup] Done."
