apply_category_mappings README

Purpose

- Add safe mapping rows to `MappingTables` for observed source category values found in integration logs: `001`, `01`, `1`, `1MAMUL`, `3YARI MAMUL`.
- This prevents internal numeric category IDs from being forwarded to Luca and should make stock cards appear correctly.

Quick steps (recommended)

1. BACK UP your database (take a full backup or export MappingTables rows first).

2. Inspect `MappingTables` to confirm schema/columns:

```sql
SELECT TOP 10 * FROM dbo.MappingTables;
```

If columns differ (e.g., `Source` instead of `SourceValue`), adapt `apply_category_mappings.sql` accordingly.

3. Edit `db/apply_category_mappings.sql` if you want a different fallback Luca code. By default the script uses `001`:

```sql
DECLARE @DefaultLucaCode NVARCHAR(200) = '001';
```

4. Run the script (example using `sqlcmd`):

PowerShell example:

```powershell
# Replace server, database, user, and password with your values
$sqlServer = 'tcp:your_sql_server\INSTANCE'
$database = 'KatanaDb'
$user = 'db_user'
$pass = 'password'
sqlcmd -S $sqlServer -d $database -U $user -P $pass -i .\db\apply_category_mappings.sql
```

If you use Windows Authentication (integrated):

```powershell
sqlcmd -S $sqlServer -d $database -E -i .\db\apply_category_mappings.sql
```

5. Verify inserted rows:

```sql
SELECT MappingType, SourceValue, TargetValue, IsActive, CreatedBy, CreatedAt
FROM dbo.MappingTables
WHERE MappingType = 'CATEGORY'
  AND SourceValue IN ('001','01','1','1MAMUL','3YARI MAMUL');
```

6. Re-run the product compatibility sync locally (or trigger your scheduled sync) and then re-run the log parser:

```powershell
# run parser from workspace root
python .\scripts\parse_luca_logs.py
# then inspect counts
Import-Csv src\Katana.API\logs\luca_parsed_report.csv | Group-Object IsNumericOnly | Format-Table Name,Count
```

Rollback

- To remove these rows:

```sql
BEGIN TRANSACTION;
DELETE FROM dbo.MappingTables
WHERE MappingType = 'CATEGORY'
  AND SourceValue IN ('001','01','1','1MAMUL','3YARI MAMUL');
COMMIT TRANSACTION;
```

Notes

- Prefer to replace the generic fallback (`001`) with real Luca tree keys if you can obtain them. This is a short-term safety fix.
- For long-term correctness, populate `MappingTables` with exact Source->Target mappings for all distinct source categories (see `src/Katana.API/logs/distinct_kategori.txt`).
- If you prefer an immediate application-level safeguard as well, I can prepare a small C# patch that forces any numeric-only `KategoriAgacKod` to the configured `_lucaSettings.DefaultKategoriKodu` before sending to Luca.
