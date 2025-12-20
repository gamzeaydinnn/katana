using Katana.Core.Helpers;

var tests = new (string? input, string expected)[] {
    ("PUT. Ø22*1,5", "PUT. O22*1,5"),
    ("çelik çekme boru Ø6", "CELIK CEKME BORU O6"),
    ("ŞEKER KAMIŞI", "SEKER KAMISI"),
    ("özel ürün", "OZEL URUN"),
    ("  spaced  input  ", "SPACED INPUT"),
    ("İSTANBUL", "ISTANBUL"),
    ("café", "CAFE"),
    (null, ""),
    ("", ""),
    ("   ", ""),
    ("ABC-123", "ABC-123"),
    ("test  product", "TEST PRODUCT"),
};

var passed = 0;
var failed = 0;

Console.WriteLine("🧪 KartKoduHelper.CanonicalizeKartKodu Tests\n");

foreach (var (input, expected) in tests)
{
    var result = KartKoduHelper.CanonicalizeKartKodu(input);
    if (result == expected)
    {
        Console.WriteLine($"✅ PASS: '{input ?? "null"}' → '{result}'");
        passed++;
    }
    else
    {
        Console.WriteLine($"❌ FAIL: '{input ?? "null"}' → '{result}' (expected: '{expected}')");
        failed++;
    }
}

Console.WriteLine($"\n📊 Results: {passed} passed, {failed} failed");

// Cache consistency test
Console.WriteLine("\n🔄 Cache Consistency Test:");
var testInput = "PUT. Ø22*1,5";
var cacheKey = KartKoduHelper.NormalizeForCacheKey(testInput);
var payload = KartKoduHelper.NormalizeForPayload(testInput);
if (cacheKey == payload)
{
    Console.WriteLine($"✅ Cache key and payload match: '{cacheKey}'");
}
else
{
    Console.WriteLine($"❌ MISMATCH! Cache: '{cacheKey}', Payload: '{payload}'");
}

return failed > 0 ? 1 : 0;
