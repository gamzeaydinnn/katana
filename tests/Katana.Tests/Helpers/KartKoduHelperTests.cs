using FluentAssertions;
using Katana.Core.Helpers;
using Xunit;

namespace Katana.Tests.Helpers;

/// <summary>
/// KartKoduHelper.CanonicalizeKartKodu fonksiyonu için unit testler.
/// Cache lookup, payload oluşturma ve duplicate kontrolü için aynı canonical form kullanılmalı.
/// </summary>
public class KartKoduHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  test  ", "TEST")]
    public void CanonicalizeKartKodu_HandlesNullAndWhitespace(string? input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("PUT. O22*1,5", "PUT. O22*1,5")] // Ø already converted to O
    [InlineData("O6 boru", "O6 BORU")]
    public void CanonicalizeKartKodu_ReplacesScandinavianO(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CELIK CEKME BORU", "CELIK CEKME BORU")]
    [InlineData("sise", "SISE")]
    [InlineData("SISE", "SISE")]
    [InlineData("guzel", "GUZEL")]
    [InlineData("GUZEL", "GUZEL")]
    [InlineData("ozel", "OZEL")]
    [InlineData("OZEL", "OZEL")]
    [InlineData("islak", "ISLAK")]
    [InlineData("ISTANBUL", "ISTANBUL")]
    public void CanonicalizeKartKodu_NormalizesTurkishCharacters(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test  product", "TEST PRODUCT")]
    [InlineData("test   product", "TEST PRODUCT")]
    [InlineData("  test   product  ", "TEST PRODUCT")]
    [InlineData("a    b    c", "A B C")]
    public void CanonicalizeKartKodu_CollapsesMultipleSpaces(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "TEST")]
    [InlineData("TEST", "TEST")]
    [InlineData("TeSt", "TEST")]
    [InlineData("abc123", "ABC123")]
    public void CanonicalizeKartKodu_ConvertsToUppercase(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("cafe", "CAFE")]
    [InlineData("naive", "NAIVE")]
    [InlineData("resume", "RESUME")]
    [InlineData("pinata", "PINATA")]
    [InlineData("uber", "UBER")]
    public void CanonicalizeKartKodu_RemovesDiacritics(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void CanonicalizeKartKodu_ComplexExample_PutO22()
    {
        // Gerçek dünya örneği: "PUT. O22*1,5" (Ø zaten O'ya çevrilmiş)
        var input = "PUT. O22*1,5";
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be("PUT. O22*1,5");
    }

    [Fact]
    public void CanonicalizeKartKodu_ComplexExample_CelikBoruO6()
    {
        // Gerçek dünya örneği: "CELIK CEKME BORU O6" (Türkçe karakterler zaten çevrilmiş)
        var input = "CELIK CEKME BORU O6";
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be("CELIK CEKME BORU O6");
    }

    [Fact]
    public void CanonicalizeKartKodu_SameInputProducesSameOutput()
    {
        // Cache consistency: aynı input her zaman aynı output vermeli
        var input = "PUT. O22*1,5";
        var result1 = KartKoduHelper.CanonicalizeKartKodu(input);
        var result2 = KartKoduHelper.CanonicalizeKartKodu(input);
        var result3 = KartKoduHelper.CanonicalizeKartKodu(input);
        
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Fact]
    public void NormalizeForPayload_SameAsCanonicalizeKartKodu()
    {
        var input = "CELIK CEKME BORU O6";
        var canonical = KartKoduHelper.CanonicalizeKartKodu(input);
        var payload = KartKoduHelper.NormalizeForPayload(input);
        
        payload.Should().Be(canonical);
    }

    [Fact]
    public void NormalizeForCacheKey_SameAsCanonicalizeKartKodu()
    {
        var input = "CELIK CEKME BORU O6";
        var canonical = KartKoduHelper.CanonicalizeKartKodu(input);
        var cacheKey = KartKoduHelper.NormalizeForCacheKey(input);
        
        cacheKey.Should().Be(canonical);
    }

    [Fact]
    public void CacheKeyAndPayload_AreIdentical()
    {
        // Bu test cache mismatch sorununu önler
        var inputs = new[]
        {
            "PUT. O22*1,5",
            "CELIK CEKME BORU O6",
            "SEKER KAMISI",
            "OZEL URUN",
            "  spaced  input  "
        };

        foreach (var input in inputs)
        {
            var cacheKey = KartKoduHelper.NormalizeForCacheKey(input);
            var payload = KartKoduHelper.NormalizeForPayload(input);
            
            cacheKey.Should().Be(payload, $"Cache key and payload should match for input: '{input}'");
        }
    }

    [Theory]
    [InlineData("ABC-123", "ABC-123")]
    [InlineData("ABC_123", "ABC_123")]
    [InlineData("ABC.123", "ABC.123")]
    [InlineData("ABC*123", "ABC*123")]
    [InlineData("ABC,123", "ABC,123")]
    public void CanonicalizeKartKodu_PreservesSpecialCharacters(string input, string expected)
    {
        var result = KartKoduHelper.CanonicalizeKartKodu(input);
        result.Should().Be(expected);
    }
}
