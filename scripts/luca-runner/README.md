Luca Runner

This small console app calls `ILucaService.CreateStockCardAsync` using the repo's existing infrastructure with a manual session cookie and optional forced branch id.

Build & run (from repo root):

1. Build

```powershell
cd scripts\luca-runner
dotnet build
```

2. Run (example with manual JSESSIONID):

```powershell
cd scripts\luca-runner
dotnet run -- --baseUrl "http://85.111.1.49:57005/Yetki/" --sessionCookie "JSESSIONID=ts-..." --forcedBranch "864" --sku "MY-SKU-1" --name "My Test Product"
```

Notes:

- The runner configures `LucaApiSettings.ManualSessionCookie` from the `--sessionCookie` argument. It sets `UseTokenAuth=false` so cookie-based Koza flows are used.
- The runner prints the response and suggests you inspect `scripts/logs/` and `src/Katana.API/logs/luca-raw.log` for the raw request/response artifacts that the client writes.
- If the runner reports `code==1003` or HTML responses, collect the files under `scripts/logs/` (`stock-create-request.json`, `stock-create-response*.txt`, cookies JSON) and share them for analysis.
