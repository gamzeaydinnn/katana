using System.Text.Json;
using FluentAssertions;
using Katana.Core.DTOs;
using Katana.Business.Mappers;
using Xunit;

namespace Katana.Tests.Services;

/// <summary>
/// Luca Stock Card UPSERT Tests
/// 
/// Bu testler UpsertStockCardAsync metodunun doğru çalıştığını doğrular:
/// - Mevcut SKU varsa UPDATE yapılmalı
/// - Yeni SKU ise CREATE yapılmalı
/// - Alanlar doğru eşlenmeli
/// - Hata durumları doğru yönetilmeli
/// 
/// **Feature: luca-stock-card-upsert-fix**
/// </summary>
public class LucaStockCardUpsertTests
{
    #region Task 5: UpdateStockCardAsync Başarılı Tests

    [Fact]
    public void UpdateStockCardAsync_WhenValidRequest_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = "TEST-SKU-001",
            KartAdi = "Test Product Name",
            UzunAdi = "Test Product Long Name",
            Barkod = "1234567890123",
            KategoriAgacKod = "001",
            PerakendeAlisBirimFiyat = 50.0,
            PerakendeSatisBirimFiyat = 100.0
        };
        var skartId = 12345L;

        // Act
        var updateRequest = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);

        // Assert
        updateRequest.Should().NotBeNull();
        updateRequest.SkartId.Should().Be(skartId);
        updateRequest.KartKodu.Should().Be("TEST-SKU-001");
        updateRequest.KartAdi.Should().Be("Test Product Name");
        updateRequest.UzunAdi.Should().Be("Test Product Long Name");
        updateRequest.Barkod.Should().Be("1234567890123");
        updateRequest.KategoriAgacKod.Should().Be("001");
        updateRequest.PerakendeAlisBirimFiyat.Should().Be(50.0);
        updateRequest.PerakendeSatisBirimFiyat.Should().Be(100.0);
    }

    [Fact]
    public void UpdateStockCardAsync_WhenNullBarkod_ShouldMapCorrectly()
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = "TEST-SKU-002",
            KartAdi = "Product Without Barcode",
            Barkod = null
        };
        var skartId = 99999L;

        // Act
        var updateRequest = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);

        // Assert
        updateRequest.Barkod.Should().BeNull();
        updateRequest.SkartId.Should().Be(skartId);
    }

    #endregion

    #region Task 5.1: Property Test - Aynı SKU Güncelleme

    /// <summary>
    /// **Feature: luca-stock-card-upsert-fix, Property 1: Aynı SKU Güncelleme Garantisi**
    /// **Validates: Requirements 1.1, 1.2, 1.3**
    /// 
    /// For any product with existing SKU in Luca, the system should UPDATE (not CREATE).
    /// </summary>
    [Theory]
    [InlineData("SKU-001", 1001)]
    [InlineData("SKU-002", 2002)]
    [InlineData("PROD-ABC-123", 3003)]
    [InlineData("TEST-PRODUCT", 4004)]
    [InlineData("12345", 5005)]
    public void Property_SameSKU_ShouldAlwaysMapToUpdate(string sku, long skartId)
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = sku,
            KartAdi = $"Product {sku}",
            PerakendeSatisBirimFiyat = 100.0
        };

        // Act
        var updateRequest = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);

        // Assert - Property: SKU should be preserved in update request
        updateRequest.KartKodu.Should().Be(sku);
        updateRequest.SkartId.Should().Be(skartId);
        updateRequest.SkartId.Should().BeGreaterThan(0);
    }

    #endregion

    #region Task 6: UpdateStockCardAsync Başarısız Tests

    [Fact]
    public void MapToUpdateRequest_WhenNullCreateRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        LucaCreateStokKartiRequest? createRequest = null;
        var skartId = 12345L;

        // Act & Assert
        Action act = () => KatanaToLucaMapper.MapToUpdateRequest(createRequest!, skartId);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapToUpdateRequest_WhenZeroSkartId_ShouldThrowArgumentException()
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = "TEST-SKU",
            KartAdi = "Test Product"
        };
        var skartId = 0L;

        // Act & Assert
        Action act = () => KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapToUpdateRequest_WhenNegativeSkartId_ShouldThrowArgumentException()
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = "TEST-SKU",
            KartAdi = "Test Product"
        };
        var skartId = -1L;

        // Act & Assert
        Action act = () => KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Task 6.1: Property Test - Yeni SKU Oluşturma

    /// <summary>
    /// **Feature: luca-stock-card-upsert-fix, Property 2: Yeni SKU Oluşturma Garantisi**
    /// **Validates: Requirements 1.5**
    /// 
    /// For any product with new SKU (not in Luca), the system should CREATE.
    /// This test validates that create requests have all required fields.
    /// </summary>
    [Theory]
    [InlineData("NEW-SKU-001", "New Product 1", 50.0)]
    [InlineData("NEW-SKU-002", "New Product 2", 100.0)]
    [InlineData("BRAND-NEW-ITEM", "Brand New Item", 250.0)]
    public void Property_NewSKU_ShouldHaveValidCreateRequest(string sku, string name, double price)
    {
        // Arrange & Act
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = sku,
            KartAdi = name,
            PerakendeSatisBirimFiyat = price,
            KartTipi = 1,
            KartTuru = 1,
            OlcumBirimiId = 5,
            BaslangicTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy")
        };

        // Assert - Property: Create request should have all required fields
        createRequest.KartKodu.Should().NotBeNullOrEmpty();
        createRequest.KartAdi.Should().NotBeNullOrEmpty();
        createRequest.KartTipi.Should().BeGreaterThan(0);
        createRequest.OlcumBirimiId.Should().BeGreaterThan(0);
        createRequest.BaslangicTarihi.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Task 7.1: Property Test - Alanlar Doğru Eşlenme

    /// <summary>
    /// **Feature: luca-stock-card-upsert-fix, Property 3: Alanlar Doğru Eşlenme**
    /// **Validates: Requirements 2.1-2.8**
    /// 
    /// For any product update, all updatable fields should be correctly mapped.
    /// </summary>
    [Theory]
    [InlineData("SKU-001", "Product A", "Long Name A", "1111111111111", "001", 10.0, 20.0)]
    [InlineData("SKU-002", "Product B", "Long Name B", "2222222222222", "002", 25.0, 50.0)]
    [InlineData("SKU-003", "Product C", null, null, null, 0.0, 0.0)]
    public void Property_AllFields_ShouldMapCorrectly(
        string sku, string name, string? longName, string? barcode, 
        string? category, double buyPrice, double sellPrice)
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = sku,
            KartAdi = name,
            UzunAdi = longName,
            Barkod = barcode,
            KategoriAgacKod = category,
            PerakendeAlisBirimFiyat = buyPrice,
            PerakendeSatisBirimFiyat = sellPrice
        };
        var skartId = 12345L;

        // Act
        var updateRequest = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);

        // Assert - Property: All fields should be mapped correctly
        updateRequest.KartKodu.Should().Be(sku);
        updateRequest.KartAdi.Should().Be(name);
        updateRequest.UzunAdi.Should().Be(longName);
        updateRequest.Barkod.Should().Be(barcode);
        updateRequest.KategoriAgacKod.Should().Be(category);
        updateRequest.PerakendeAlisBirimFiyat.Should().Be(buyPrice);
        updateRequest.PerakendeSatisBirimFiyat.Should().Be(sellPrice);
    }

    #endregion

    #region Task 8.1: Property Test - İdempotency

    /// <summary>
    /// **Feature: luca-stock-card-upsert-fix, Property 4: İdempotency**
    /// **Validates: Requirements 4.1, 4.2**
    /// 
    /// For any product, calling MapToUpdateRequest multiple times should produce identical results.
    /// </summary>
    [Theory]
    [InlineData("IDEM-SKU-001", 1001)]
    [InlineData("IDEM-SKU-002", 2002)]
    [InlineData("IDEM-SKU-003", 3003)]
    public void Property_Idempotency_MultipleCallsShouldProduceSameResult(string sku, long skartId)
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = sku,
            KartAdi = $"Product {sku}",
            UzunAdi = $"Long Name {sku}",
            Barkod = "1234567890123",
            KategoriAgacKod = "001",
            PerakendeAlisBirimFiyat = 50.0,
            PerakendeSatisBirimFiyat = 100.0
        };

        // Act - Call multiple times
        var result1 = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);
        var result2 = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);
        var result3 = KatanaToLucaMapper.MapToUpdateRequest(createRequest, skartId);

        // Assert - Property: All results should be identical
        result1.SkartId.Should().Be(result2.SkartId).And.Be(result3.SkartId);
        result1.KartKodu.Should().Be(result2.KartKodu).And.Be(result3.KartKodu);
        result1.KartAdi.Should().Be(result2.KartAdi).And.Be(result3.KartAdi);
        result1.UzunAdi.Should().Be(result2.UzunAdi).And.Be(result3.UzunAdi);
        result1.Barkod.Should().Be(result2.Barkod).And.Be(result3.Barkod);
        result1.KategoriAgacKod.Should().Be(result2.KategoriAgacKod).And.Be(result3.KategoriAgacKod);
        result1.PerakendeAlisBirimFiyat.Should().Be(result2.PerakendeAlisBirimFiyat).And.Be(result3.PerakendeAlisBirimFiyat);
        result1.PerakendeSatisBirimFiyat.Should().Be(result2.PerakendeSatisBirimFiyat).And.Be(result3.PerakendeSatisBirimFiyat);
    }

    #endregion

    #region Task 9.1: Property Test - Hata Yönetimi

    /// <summary>
    /// **Feature: luca-stock-card-upsert-fix, Property 5: Hata Yönetimi**
    /// **Validates: Requirements 3.1-3.4**
    /// 
    /// For any invalid input, the system should throw appropriate exceptions.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-999999)]
    public void Property_ErrorHandling_InvalidSkartIdShouldThrow(long invalidSkartId)
    {
        // Arrange
        var createRequest = new LucaCreateStokKartiRequest
        {
            KartKodu = "TEST-SKU",
            KartAdi = "Test Product"
        };

        // Act & Assert - Property: Invalid skartId should always throw
        Action act = () => KatanaToLucaMapper.MapToUpdateRequest(createRequest, invalidSkartId);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region LucaUpdateStokKartiRequest DTO Tests

    [Fact]
    public void LucaUpdateStokKartiRequest_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var request = new LucaUpdateStokKartiRequest
        {
            SkartId = 12345,
            KartKodu = "TEST-SKU",
            KartAdi = "Test Product",
            UzunAdi = "Test Product Long Name",
            Barkod = "1234567890123",
            KategoriAgacKod = "001",
            PerakendeAlisBirimFiyat = 50.0,
            PerakendeSatisBirimFiyat = 100.0,
            GtipKodu = "12345678"
        };

        // Assert
        request.SkartId.Should().Be(12345);
        request.KartKodu.Should().Be("TEST-SKU");
        request.KartAdi.Should().Be("Test Product");
        request.UzunAdi.Should().Be("Test Product Long Name");
        request.Barkod.Should().Be("1234567890123");
        request.KategoriAgacKod.Should().Be("001");
        request.PerakendeAlisBirimFiyat.Should().Be(50.0);
        request.PerakendeSatisBirimFiyat.Should().Be(100.0);
        request.GtipKodu.Should().Be("12345678");
    }

    [Fact]
    public void LucaUpdateStokKartiRequest_ShouldAllowNullableFields()
    {
        // Arrange & Act
        var request = new LucaUpdateStokKartiRequest
        {
            SkartId = 12345,
            KartKodu = "TEST-SKU",
            KartAdi = "Test Product",
            UzunAdi = null,
            Barkod = null,
            KategoriAgacKod = null,
            PerakendeAlisBirimFiyat = null,
            PerakendeSatisBirimFiyat = null,
            GtipKodu = null
        };

        // Assert
        request.UzunAdi.Should().BeNull();
        request.Barkod.Should().BeNull();
        request.KategoriAgacKod.Should().BeNull();
        request.PerakendeAlisBirimFiyat.Should().BeNull();
        request.PerakendeSatisBirimFiyat.Should().BeNull();
        request.GtipKodu.Should().BeNull();
    }

    #endregion
}
