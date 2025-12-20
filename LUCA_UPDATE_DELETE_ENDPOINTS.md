# Luca update/delete endpoint behavior

This document summarizes how Luca stock-card update and delete endpoints work in the API and the underlying ILucaService implementation. It focuses on the stock card flows that actually call Luca.

## API endpoints

### Update (stock card) - Admin test endpoint
- Route: `POST /api/adminpanel/test-update-product`
- Auth: `Admin` only
- Body: `LucaUpdateStokKartiRequest`
- Core flow:
  1) `ILucaService.ListStockCardsSimpleAsync()` pulls Luca cards.
  2) SKU is normalized (trim + upper + remove spaces) and matched to Luca `KartKodu`.
  3) If no match: `400 BadRequest` with "SKU ... Luca'da bulunamadi".
  4) If match: sets `request.SkartId` to the real Luca ID.
  5) Calls `ILucaService.UpdateStockCardAsync(request)` and returns OK/BadRequest.
- Key detail: client does not need to know `skartId`; it is resolved from SKU.

### Delete (stock card) - Admin test endpoint
- Route: `POST /api/adminpanel/test-delete-product?sku=...`
- Auth: `Admin` only
- Inputs: SKU in query string (required)
- Core flow:
  1) Try local DB: find `Products` by SKU, use `LucaId` if present (fast).
  2) If not found locally: call `ListStockCardsSimpleAsync()` and match SKU in Luca (slow).
  3) If Luca ID found and local product exists, save `LucaId` for next time.
  4) Call `ILucaService.DeleteStockCardAsync(realLucaId)`.
  5) On success: optionally delete local product record (if loaded), return timing and lookup method.
  6) On failure: return BadRequest with details.

### Related: Product delete endpoint (triggers Luca delete)
- Route: `DELETE /api/products/{id}?force=false`
- Auth: `Admin`
- Core flow:
  - If `Product.LucaId` exists, calls `ILucaService.DeleteStockCardAsync`.
  - If Luca refuses delete, product is set inactive and a conflict is returned.
  - If Luca call fails and `force=false`, a conflict is thrown; `force=true` allows local-only delete (SKU is renamed with a suffix and product is deactivated).
- Note: This endpoint is not a "Luca endpoint" but it can call Luca delete.

## ILucaService behavior (stock cards)

### UpdateStockCardAsync(LucaUpdateStokKartiRequest)
- Ensures auth + branch selection before sending.
- Uses endpoint from settings: `Endpoints.StockCardUpdate` (fallback `GuncelleStkWsSkart.do`).
- Two attempts:
  1) Direct JSON serialize `request`.
  2) Wrapped JSON: `{ "stkSkart": request }`.
- Each attempt:
  - Uses `CreateKozaContent(...)` for proper encoding.
  - Sends via `SendWithAuthRetryAsync(...)` with session cookie.
  - Writes raw logs via `AppendRawLogAsync(...)`.
  - Parses response with `ParseKozaOperationResponse`.
- Returns `true` only if a success response is parsed.

### DeleteStockCardAsync(long skartId)
- Ensures auth; ensures branch selection when not using token auth.
- Uses endpoint from settings: `Endpoints.StockCardDelete`.
- Sends JSON body: `{ "skartId": <id> }`.
- Parses response:
  - If HTML -> treated as session expired -> fail.
  - If JSON `error: true` -> fail.
  - If text without "error/hata" -> success.
- If hard delete fails, falls back to `DeleteStockCardZombieAsync` (soft delete):
  - Fetches card list, sets new code/name, sets `aktif = 0`.
  - Calls update endpoint to "hide" the card.

## DTO requirements (stock card update/delete)

- `LucaUpdateStokKartiRequest`
  - Required: `skartId` (set by server in test endpoint), `kartKodu`, `kartAdi`.
  - Optional: unit, VAT, barcode, flags, etc.
- `LucaDeleteStokKartiRequest` (not used by API endpoint above)
  - Required: `skartId`.

## Source references
- API endpoints: `src/Katana.API/Controllers/AdminController.cs`
- Product delete endpoint: `src/Katana.API/Controllers/ProductsController.cs`
- Luca service update/delete: `src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs`
- Zombie delete fallback: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`
- DTOs: `src/Katana.Core/DTOs/LucaDtos.cs`
