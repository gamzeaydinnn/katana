# Luca Stock Card Creation - Diagnosis

## Current Status

✅ Docker build completed successfully (18:09)  
✅ Container is running the new code  
❌ Luca API is rejecting all stock card creation requests with `{"error":true}`

## Evidence from Logs (18:14:47)

### JSON Being Sent

```json
{
  "kartAdi":"Presli Boru",
  "kartKodu":"PUT. Ø22*1,5",
  "kartTipi":4,
  "kartAlisKdvOran":1,
  "kartSatisKdvOran":1,
  "olcumBirimiId":5,
  "baslangicTarihi":"06/12/2025",
  "kartTuru":1,
  "barkod":"PUT. Ø22*1,5",
  "satilabilirFlag":1,
  "satinAlinabilirFlag":1,
  "lotNoFlag":0,
  "minStokKontrol":0,
  "maliyetHesaplanacakFlag":true  ← WRONG TYPE (should be 1, not true)
}
```

### Luca Response

```json
{ "error": true }
```

## Problems Identified

### 1. Wrong Data Type

- **Field**: `maliyetHesaplanacakFlag`
- **Expected**: `1` (int)
- **Actual**: `true` (boolean)
- **Impact**: Luca API might be rejecting due to type mismatch

### 2. Missing Fields

The JSON is missing many fields that are defined in the DTO with default values:

**Missing String Fields** (should be empty string `""`):

- `gtipKodu`
- `ihracatKategoriNo`
- `detayAciklama`
- `otvTipi`
- `alisTevkifatOran` ← **CRITICAL** - We added this field!
- `satisTevkifatOran` ← **CRITICAL** - We added this field!

**Missing Numeric Fields** (should be `0` or `0.0`):

- `alisTevkifatKod` ← **CRITICAL** - We added this field!
- `satisTevkifatKod` ← **CRITICAL** - We added this field!
- `stopajOran`
- `alisIskontoOran1` through `alisIskontoOran5`
- `satisIskontoOran1` through `satisIskontoOran5`
- `perakendeAlisBirimFiyat` ← Set in mapper but missing!
- `perakendeSatisBirimFiyat` ← Set in mapper but missing!
- `rafOmru`
- `garantiSuresi`
- `minStokMiktari`
- `maxStokKontrol`
- `maxStokMiktari`
- `utsVeriAktarimiFlag`
- `bagDerecesi`
- `uretimSuresi`
- `uretimSuresiBirim`
- `seriNoFlag`
- `otvMaliyetFlag`
- `otvTutarKdvFlag`
- `otvIskontoFlag`
- `satisAlternatifFlag`

## Root Cause Analysis

### Why is `maliyetHesaplanacakFlag` boolean?

1. DTO defines it as `int` (line 1560 of LucaDtos.cs)
2. Mapper sets it to `1` (line 484 of KatanaToLucaMapper.cs)
3. But JSON shows `true`

**Hypothesis**: There might be implicit conversion happening, OR the serializer is treating `1` as `true` for some reason.

### Why are fields missing?

1. Serialization uses `DefaultIgnoreCondition.Never` (line 30 of LucaService.StockCards.cs)
2. This SHOULD include all fields, even with default values
3. But fields with `0`, `0.0`, or `""` are being omitted

**Hypothesis**: The `DefaultIgnoreCondition.Never` setting might not be working as expected, OR there's another serialization happening somewhere.

## Possible Solutions

### Option 1: Fix the Serialization

Ensure that `DefaultIgnoreCondition.Never` actually works by:

- Explicitly setting all properties in the DTO before serialization
- Using a custom JSON converter if needed

### Option 2: Match Luca's Expected Format

Research what Luca API actually expects:

- Does it require ALL fields to be present?
- Does it accept `true`/`false` for flag fields or only `0`/`1`?
- Are there required fields we're missing?

### Option 3: Debug the Actual Serialization

Add logging to see:

- What the DTO object looks like before serialization
- What the JsonSerializerOptions actually are
- Whether there's any transformation happening

## Next Steps

1. **Verify the DTO is correct** - Check that `MaliyetHesaplanacakFlag` is actually `int` in the compiled code
2. **Test serialization directly** - Create a unit test that serializes the DTO and checks the output
3. **Check Luca API documentation** - Find out what fields are actually required
4. **Add more logging** - Log the DTO object before serialization to see its actual state

## Questions

1. Why is `maliyetHesaplanacakFlag` being serialized as boolean when it's defined as int?
2. Why are the tevkifat fields we added not appearing in the JSON?
3. Why are price fields (`perakendeAlisBirimFiyat`, `perakendeSatisBirimFiyat`) missing when they're explicitly set in the mapper?
4. Does Luca API require ALL fields to be present, or only certain ones?
5. Is there a working example of a successful Luca stock card creation request we can compare against?
