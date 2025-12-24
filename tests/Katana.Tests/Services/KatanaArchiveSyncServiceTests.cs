using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Katana.Business.Interfaces;
using Katana.Business.Services;
using Katana.Core.DTOs;
using Katana.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

/// <summary>
/// Property-based tests for KatanaArchiveSyncService
/// Feature: katana-product-archive-sync
/// </summary>
public class KatanaArchiveSyncServiceTests
{
    private readonly Mock<IKatanaService> _mockKatanaService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<KatanaArchiveSyncService>> _mockLogger;

    public KatanaArchiveSyncServiceTests()
    {
        _mockKatanaService = new Mock<IKatanaService>();
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<KatanaArchiveSyncService>>();
    }

    /// <summary>
    /// **Feature: katana-product-archive-sync, Property 2: Preview Mode Immutability**
    /// For any preview operation, the number of Katana API PATCH calls should be zero,
    /// and the returned list should contain all products that would be archived.
    /// **Validates: Requirements 2.1, 3.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PreviewMode_ShouldNotCallArchiveApi()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyArray<string>>(),
            Arb.From<NonEmptyArray<string>>(),
            (localSkus, katanaSkus) =>
            {
                // Arrange
                var localProducts = localSkus.Get.Select((sku, i) => new ProductDto 
                { 
                    Id = i + 1, 
                    SKU = sku.Trim().ToUpperInvariant() 
                }).ToList();

                var katanaProducts = katanaSkus.Get.Select((sku, i) => new KatanaProductDto 
                { 
                    Id = (i + 1).ToString(), 
                    SKU = sku.Trim().ToUpperInvariant(),
                    Name = $"Product {i + 1}"
                }).ToList();

                _mockProductService.Setup(x => x.GetAllProductsAsync())
                    .ReturnsAsync(localProducts);
                _mockKatanaService.Setup(x => x.GetProductsAsync())
                    .ReturnsAsync(katanaProducts);

                var archiveCallCount = 0;
                _mockKatanaService.Setup(x => x.ArchiveProductAsync(It.IsAny<int>()))
                    .Callback(() => archiveCallCount++)
                    .ReturnsAsync(true);

                var service = new KatanaArchiveSyncService(
                    _mockKatanaService.Object,
                    _mockProductService.Object,
                    _mockLogger.Object);

                // Act
                var result = service.SyncArchiveAsync(previewOnly: true).GetAwaiter().GetResult();

                // Assert - No archive calls should be made in preview mode
                return (archiveCallCount == 0 && result.IsPreviewOnly)
                    .Label($"Archive calls: {archiveCallCount}, IsPreviewOnly: {result.IsPreviewOnly}");
            });
    }

    /// <summary>
    /// **Feature: katana-product-archive-sync, Property 1: SKU Matching Correctness**
    /// For any set of local products and Katana products, a Katana product should be marked 
    /// for archiving if and only if its SKU (case-insensitive) does not exist in the local product set.
    /// **Validates: Requirements 1.2, 1.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SkuMatching_ShouldBeCaseInsensitive()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyArray<string>>(),
            (skus) =>
            {
                // Arrange - Create products with same SKUs but different cases
                var localSkus = skus.Get.Select(s => s.ToUpperInvariant()).Distinct().ToArray();
                var katanaSkus = skus.Get.Select(s => s.ToLowerInvariant()).Distinct().ToArray();

                var localProducts = localSkus.Select((sku, i) => new ProductDto 
                { 
                    Id = i + 1, 
                    SKU = sku 
                }).ToList();

                var katanaProducts = katanaSkus.Select((sku, i) => new KatanaProductDto 
                { 
                    Id = (i + 1).ToString(), 
                    SKU = sku,
                    Name = $"Product {i + 1}"
                }).ToList();

                _mockProductService.Setup(x => x.GetAllProductsAsync())
                    .ReturnsAsync(localProducts);
                _mockKatanaService.Setup(x => x.GetProductsAsync())
                    .ReturnsAsync(katanaProducts);

                var service = new KatanaArchiveSyncService(
                    _mockKatanaService.Object,
                    _mockProductService.Object,
                    _mockLogger.Object);

                // Act
                var preview = service.GetArchivePreviewAsync().GetAwaiter().GetResult();

                // Assert - Same SKUs (case-insensitive) should not be marked for archiving
                var localSkuSet = new HashSet<string>(localSkus, StringComparer.OrdinalIgnoreCase);
                var expectedToArchive = katanaProducts
                    .Where(k => !localSkuSet.Contains(k.SKU))
                    .Count();

                return (preview.Count == expectedToArchive)
                    .Label($"Expected: {expectedToArchive}, Actual: {preview.Count}");
            });
    }

    /// <summary>
    /// **Feature: katana-product-archive-sync, Property 3: Archive Summary Consistency**
    /// For any archive operation, the sum of ArchivedSuccessfully and ArchiveFailed 
    /// should equal ProductsToArchive.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ArchiveSummary_ShouldBeConsistent()
    {
        return Prop.ForAll(
            Gen.Choose(0, 10).ToArbitrary(),
            Gen.Choose(0, 10).ToArbitrary(),
            (successCount, failCount) =>
            {
                // Arrange
                var totalToArchive = successCount + failCount;
                var katanaProducts = Enumerable.Range(1, totalToArchive)
                    .Select(i => new KatanaProductDto 
                    { 
                        Id = i.ToString(), 
                        SKU = $"KATANA-{i}",
                        Name = $"Katana Product {i}"
                    }).ToList();

                var localProducts = new List<ProductDto>(); // Empty - all Katana products should be archived

                _mockProductService.Setup(x => x.GetAllProductsAsync())
                    .ReturnsAsync(localProducts);
                _mockKatanaService.Setup(x => x.GetProductsAsync())
                    .ReturnsAsync(katanaProducts);

                var callIndex = 0;
                _mockKatanaService.Setup(x => x.ArchiveProductAsync(It.IsAny<int>()))
                    .ReturnsAsync(() => 
                    {
                        var result = callIndex < successCount;
                        callIndex++;
                        return result;
                    });

                var service = new KatanaArchiveSyncService(
                    _mockKatanaService.Object,
                    _mockProductService.Object,
                    _mockLogger.Object);

                // Act
                var result = service.SyncArchiveAsync(previewOnly: false).GetAwaiter().GetResult();

                // Assert
                var sumEquals = result.ArchivedSuccessfully + result.ArchiveFailed == result.ProductsToArchive;
                return sumEquals
                    .Label($"Success: {result.ArchivedSuccessfully}, Failed: {result.ArchiveFailed}, Total: {result.ProductsToArchive}");
            });
    }

    /// <summary>
    /// **Feature: katana-product-archive-sync, Property 4: Partial Failure Resilience**
    /// For any archive operation where some products fail to archive, 
    /// the remaining products should still be processed and the operation should complete with a summary.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PartialFailure_ShouldContinueProcessing()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            Gen.Choose(0, 5).ToArbitrary(),
            (totalProducts, failAtIndex) =>
            {
                // Arrange
                var katanaProducts = Enumerable.Range(1, totalProducts)
                    .Select(i => new KatanaProductDto 
                    { 
                        Id = i.ToString(), 
                        SKU = $"KATANA-{i}",
                        Name = $"Katana Product {i}"
                    }).ToList();

                var localProducts = new List<ProductDto>(); // Empty - all should be archived

                _mockProductService.Setup(x => x.GetAllProductsAsync())
                    .ReturnsAsync(localProducts);
                _mockKatanaService.Setup(x => x.GetProductsAsync())
                    .ReturnsAsync(katanaProducts);

                var callIndex = 0;
                var actualFailIndex = failAtIndex % totalProducts;
                _mockKatanaService.Setup(x => x.ArchiveProductAsync(It.IsAny<int>()))
                    .ReturnsAsync(() => 
                    {
                        var shouldFail = callIndex == actualFailIndex;
                        callIndex++;
                        return !shouldFail;
                    });

                var service = new KatanaArchiveSyncService(
                    _mockKatanaService.Object,
                    _mockProductService.Object,
                    _mockLogger.Object);

                // Act
                var result = service.SyncArchiveAsync(previewOnly: false).GetAwaiter().GetResult();

                // Assert - All products should be processed (success + fail = total)
                var allProcessed = result.ArchivedSuccessfully + result.ArchiveFailed == totalProducts;
                var hasExpectedFailure = result.ArchiveFailed >= 1;
                
                return (allProcessed && hasExpectedFailure)
                    .Label($"Processed: {result.ArchivedSuccessfully + result.ArchiveFailed}/{totalProducts}, Failures: {result.ArchiveFailed}");
            });
    }

    /// <summary>
    /// **Feature: katana-product-archive-sync, Property 5: Preview Result Completeness**
    /// For any preview result, each item should contain non-empty KatanaProductId, SKU, and Name fields.
    /// **Validates: Requirements 2.2, 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PreviewResult_ShouldContainRequiredFields()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyArray<NonEmptyString>>(),
            (productNames) =>
            {
                // Arrange
                var katanaProducts = productNames.Get.Select((name, i) => new KatanaProductDto 
                { 
                    Id = (i + 1).ToString(), 
                    SKU = $"SKU-{i + 1}",
                    Name = name.Get
                }).ToList();

                var localProducts = new List<ProductDto>(); // Empty - all should appear in preview

                _mockProductService.Setup(x => x.GetAllProductsAsync())
                    .ReturnsAsync(localProducts);
                _mockKatanaService.Setup(x => x.GetProductsAsync())
                    .ReturnsAsync(katanaProducts);

                var service = new KatanaArchiveSyncService(
                    _mockKatanaService.Object,
                    _mockProductService.Object,
                    _mockLogger.Object);

                // Act
                var preview = service.GetArchivePreviewAsync().GetAwaiter().GetResult();

                // Assert - All preview items should have required fields
                var allHaveRequiredFields = preview.All(p => 
                    p.KatanaProductId > 0 && 
                    !string.IsNullOrWhiteSpace(p.SKU) && 
                    !string.IsNullOrWhiteSpace(p.Name));

                return allHaveRequiredFields
                    .Label($"Preview count: {preview.Count}, All have required fields: {allHaveRequiredFields}");
            });
    }
}
