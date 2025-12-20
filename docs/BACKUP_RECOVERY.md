# Backup & Recovery Plan (SQL Server Only)

This document explains how to back up and restore Katana’s database on SQL Server.

## Overview

- Supported DB types: `sqlserver`
- Scripts:
  - `scripts/backup-db.ps1`
  - `scripts/restore-db.ps1`
- Default backup directory: `./backups` (configurable)

## Prerequisites

- PowerShell 5.1+ or PowerShell 7+
- Prefer PowerShell `SqlServer` module (Backup-SqlDatabase / Restore-SqlDatabase); fallback: `sqlcmd` CLI

## Environment Variables (optional)

- `KATANA_SQLSERVER_INSTANCE` — e.g. `localhost\SQLEXPRESS` or `tcp:server,1433`
- `KATANA_SQLSERVER_DATABASE` — e.g. `KatanaDb`
- `KATANA_SQLSERVER_USERNAME` / `KATANA_SQLSERVER_PASSWORD` — optional; if not set, Windows auth is used
- `KATANA_BACKUP_DIR` — backup output folder (default: `./backups`)
- `KATANA_BACKUP_RETENTION_DAYS` — default `30`

## Backup

Run manually:

```powershell
# Windows authentication
powershell -File scripts/backup-db.ps1 -ServerInstance localhost\SQLEXPRESS -Database KatanaDb -BackupDir C:\backups

# SQL authentication
powershell -File scripts/backup-db.ps1 -ServerInstance tcp:prod-db,1433 -Database KatanaDb -Username sa -Password "P@ssw0rd" -BackupDir C:\backups
```

Notes:
- Script prefers `SqlServer` PowerShell module; falls back to `sqlcmd` if available.
- Retention: Old backups matching `katana_*.bak` older than `KATANA_BACKUP_RETENTION_DAYS` are removed.

## Restore

```powershell
# Windows authentication
powershell -File scripts/restore-db.ps1 -ServerInstance localhost\SQLEXPRESS -Database KatanaDb -BackupFile C:\backups\katana_20250101_020000.bak

# SQL authentication
powershell -File scripts/restore-db.ps1 -ServerInstance tcp:prod-db,1433 -Database KatanaDb -Username sa -Password "P@ssw0rd" -BackupFile C:\backups\katana_20250101_020000.bak
```

Notes:
- Uses `WITH REPLACE`. Ensure you restore to a compatible environment (file paths/layout). For complex file-layout differences, prefer `Restore-SqlDatabase` with `-RelocateFile`.

## Scheduling (Windows Task Scheduler)

Create a daily task at 02:00:

```
Program/script: powershell.exe
Arguments: -NoProfile -ExecutionPolicy Bypass -File "C:\path\to\repo\scripts\backup-db.ps1" -ServerInstance localhost\SQLEXPRESS -Database KatanaDb -BackupDir C:\backups
Start in: C:\path\to\repo
```

## Verification (Recommended)

1. Perform a backup.
2. Restore into a staging/test environment.
3. Run a smoke test (API health, basic queries).
4. Document the exact steps and the tested backup file reference.

## Troubleshooting

- `sqlcmd not found` — Install SQL Server tools or use the `SqlServer` PowerShell module.
- `Access denied` on backup directory — Ensure the executing user has permissions on the backup folder.

