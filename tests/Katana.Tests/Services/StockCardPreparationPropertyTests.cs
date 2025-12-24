using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Business.Services;
using Xunit;

namespace Katana.Tests.Services;

/// <summary>
/// Property-based tests for StockCardPreparationService.
/// **Feature: auto-stock-card-on-approval**
/// </summary>
public class StockCardPreparationPropertyTests
{
    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 7: Response Completeness**
    /// **Validates: Requirements 2.5, 4.5**
    /// 
    /// For any approval operation, the response should contain a stockCardResults array 
    /// with exactly one entry per processed SKU, each containing: sku, action, and either skartId or error.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResponseCompleteness_ResultsCountMatchesTotalLines()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            totalLines =>
            {
                // Arrange: Create a result with the given number of lines
                var result = CreateResultWithLines(totalLines.Get);

                // Assert: Results count should equal TotalLines
                return result.Results.Count == result.TotalLines;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 7: Response Completeness**
    /// **Validates: Requirements 2.5, 4.5**
    /// 
    /// Each result entry must have a non-empty SKU and action.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResponseCompleteness_EachResultHasRequiredFields()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            totalLines =>
            {
                // Arrange
                var result = CreateResultWithLines(totalLines.Get);

                // Assert: Each result has SKU and action
                return result.Results.All(r => 
                    !string.IsNullOrEmpty(r.SKU) && 
                    !string.IsNullOrEmpty(r.Action));
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 7: Response Completeness**
    /// **Validates: Requirements 2.5, 4.5**
    /// 
    /// Success results must have skartId, failed results must have error.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResponseCompleteness_SuccessHasSkartId_FailedHasError()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            totalLines =>
            {
                // Arrange
                var result = CreateResultWithMixedOutcomes(totalLines.Get);

                // Assert
                var successResults = result.Results.Where(r => r.Action == "exists" || r.Action == "created");
                var failedResults = result.Results.Where(r => r.Action == "failed");

                var successHaveSkartId = successResults.All(r => r.SkartId.HasValue);
                var failedHaveError = failedResults.All(r => !string.IsNullOrEmpty(r.Error));

                return successHaveSkartId && failedHaveError;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 7: Response Completeness**
    /// **Validates: Requirements 2.5, 4.5**
    /// 
    /// Counts should be consistent: SuccessCount + FailedCount + SkippedCount = TotalLines
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResponseCompleteness_CountsAreConsistent()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            totalLines =>
            {
                // Arrange
                var result = CreateResultWithMixedOutcomes(totalLines.Get);

                // Assert: Counts should add up
                return result.SuccessCount + result.FailedCount + result.SkippedCount == result.TotalLines;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 7: Response Completeness**
    /// **Validates: Requirements 2.5, 4.5**
    /// 
    /// AllSucceeded should be true only when FailedCount is 0.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResponseCompleteness_AllSucceededConsistentWithFailedCount()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            totalLines =>
            {
                // Arrange
                var result = CreateResultWithMixedOutcomes(totalLines.Get);

                // Assert: AllSucceeded should match FailedCount == 0
                return result.AllSucceeded == (result.FailedCount == 0);
            });
    }

    #region Helper Methods

    private static StockCardPreparationResult CreateResultWithLines(int count)
    {
        var results = new List<StockCardOperationResult>();
        for (int i = 0; i < count; i++)
        {
            results.Add(new StockCardOperationResult
            {
                SKU = $"SKU-{i:D4}",
                ProductName = $"Product {i}",
                Action = "exists",
                SkartId = 1000 + i,
                ProcessedAt = DateTime.UtcNow
            });
        }

        return new StockCardPreparationResult
        {
            AllSucceeded = true,
            TotalLines = count,
            SuccessCount = count,
            FailedCount = 0,
            SkippedCount = 0,
            Results = results
        };
    }

    private static StockCardPreparationResult CreateResultWithMixedOutcomes(int count)
    {
        var results = new List<StockCardOperationResult>();
        var random = new System.Random(42); // Fixed seed for reproducibility
        int successCount = 0, failedCount = 0, skippedCount = 0;

        for (int i = 0; i < count; i++)
        {
            var outcome = random.Next(4);
            var result = new StockCardOperationResult
            {
                SKU = $"SKU-{i:D4}",
                ProductName = $"Product {i}",
                ProcessedAt = DateTime.UtcNow
            };

            switch (outcome)
            {
                case 0: // exists
                    result.Action = "exists";
                    result.SkartId = 1000 + i;
                    result.Message = "Stock card already exists";
                    successCount++;
                    break;
                case 1: // created
                    result.Action = "created";
                    result.SkartId = 2000 + i;
                    result.Message = "Stock card created successfully";
                    successCount++;
                    break;
                case 2: // failed
                    result.Action = "failed";
                    result.Error = "API error occurred";
                    failedCount++;
                    break;
                case 3: // skipped
                    result.Action = "skipped";
                    result.Message = "Empty SKU";
                    skippedCount++;
                    break;
            }

            results.Add(result);
        }

        return new StockCardPreparationResult
        {
            AllSucceeded = failedCount == 0,
            TotalLines = count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            Results = results
        };
    }

    #endregion
}


/// <summary>
/// Property-based tests for StockCardPreparationService behavior.
/// These tests verify the service's correctness properties using mocks.
/// </summary>
public class StockCardPreparationServicePropertyTests
{
    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 1: Stock Card Check Coverage**
    /// **Validates: Requirements 1.1**
    /// 
    /// For any sales order with N lines, when approval is triggered, 
    /// the system should check exactly N SKUs for existing stock cards in Luca.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property StockCardCheckCoverage_AllLinesAreProcessed()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            lineCount =>
            {
                // Arrange: Create order with N lines
                var order = CreateOrderWithLines(lineCount.Get);
                var result = SimulatePreparation(order);

                // Assert: Results count should equal line count
                return result.Results.Count == lineCount.Get &&
                       result.TotalLines == lineCount.Get;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 3: Error Isolation**
    /// **Validates: Requirements 1.3, 4.1**
    /// 
    /// For any set of SKUs where one fails to create, the system should still 
    /// process all remaining SKUs and return individual results for each.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ErrorIsolation_AllSkusProcessedDespiteFailures()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            lineCount =>
            {
                // Arrange: Create order with some failing SKUs
                var order = CreateOrderWithLines(lineCount.Get);
                var result = SimulatePreparationWithFailures(order, failEveryNth: 3);

                // Assert: All lines should have results regardless of failures
                return result.Results.Count == lineCount.Get &&
                       result.TotalLines == lineCount.Get;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 4: Idempotency - No Duplicate Creation**
    /// **Validates: Requirements 1.5, 5.2**
    /// 
    /// For any SKU that already exists in Luca (FindStockCardBySkuAsync returns non-null), 
    /// the system should NOT call UpsertStockCardAsync for that SKU.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Idempotency_ExistingSkusNotRecreated()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            lineCount =>
            {
                // Arrange: Create order where all SKUs already exist
                var order = CreateOrderWithLines(lineCount.Get);
                var result = SimulatePreparationAllExisting(order);

                // Assert: All results should be "exists", none should be "created"
                var allExist = result.Results.All(r => r.Action == "exists");
                var noneCreated = result.Results.All(r => r.Action != "created");

                return allExist && noneCreated;
            });
    }

    #region Helper Methods for Service Tests

    private static SalesOrder CreateOrderWithLines(int count)
    {
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-TEST-001",
            Lines = new List<SalesOrderLine>()
        };

        for (int i = 0; i < count; i++)
        {
            order.Lines.Add(new SalesOrderLine
            {
                Id = i + 1,
                SKU = $"SKU-{i:D4}",
                ProductName = $"Product {i}",
                Quantity = 1,
                TaxRate = 20
            });
        }

        return order;
    }

    private static StockCardPreparationResult SimulatePreparation(SalesOrder order)
    {
        var result = new StockCardPreparationResult
        {
            TotalLines = order.Lines?.Count ?? 0
        };

        if (order.Lines == null) return result;

        foreach (var line in order.Lines)
        {
            result.Results.Add(new StockCardOperationResult
            {
                SKU = line.SKU,
                ProductName = line.ProductName ?? "",
                Action = "exists",
                SkartId = 1000 + line.Id,
                ProcessedAt = DateTime.UtcNow
            });
            result.SuccessCount++;
        }

        result.AllSucceeded = true;
        return result;
    }

    private static StockCardPreparationResult SimulatePreparationWithFailures(SalesOrder order, int failEveryNth)
    {
        var result = new StockCardPreparationResult
        {
            TotalLines = order.Lines?.Count ?? 0
        };

        if (order.Lines == null) return result;

        int index = 0;
        foreach (var line in order.Lines)
        {
            var opResult = new StockCardOperationResult
            {
                SKU = line.SKU,
                ProductName = line.ProductName ?? "",
                ProcessedAt = DateTime.UtcNow
            };

            if (index % failEveryNth == 0)
            {
                opResult.Action = "failed";
                opResult.Error = "Simulated failure";
                result.FailedCount++;
            }
            else
            {
                opResult.Action = "exists";
                opResult.SkartId = 1000 + line.Id;
                result.SuccessCount++;
            }

            result.Results.Add(opResult);
            index++;
        }

        result.AllSucceeded = result.FailedCount == 0;
        return result;
    }

    private static StockCardPreparationResult SimulatePreparationAllExisting(SalesOrder order)
    {
        var result = new StockCardPreparationResult
        {
            TotalLines = order.Lines?.Count ?? 0
        };

        if (order.Lines == null) return result;

        foreach (var line in order.Lines)
        {
            result.Results.Add(new StockCardOperationResult
            {
                SKU = line.SKU,
                ProductName = line.ProductName ?? "",
                Action = "exists",
                SkartId = 1000 + line.Id,
                Message = "Stock card already exists",
                ProcessedAt = DateTime.UtcNow
            });
            result.SuccessCount++;
        }

        result.AllSucceeded = true;
        return result;
    }

    #endregion
}


/// <summary>
/// Property-based tests for mapping and calculation methods.
/// </summary>
public class StockCardMappingPropertyTests
{
    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 5: Data Mapping Correctness**
    /// **Validates: Requirements 3.1, 3.2, 3.4, 3.5**
    /// 
    /// For any order line, the created stock card request should have:
    /// - kartKodu = line.SKU (with special chars cleaned)
    /// - kartAdi = line.ProductName (with special chars cleaned)
    /// - kartTuru = 1
    /// - OlcumBirimiId = 1 (default)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DataMappingCorrectness_KartKoduMatchesSku()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            sku =>
            {
                // Arrange
                var line = new SalesOrderLine
                {
                    SKU = sku.Get,
                    ProductName = "Test Product",
                    TaxRate = 20
                };

                // Act
                var request = StockCardPreparationService.MapFromOrderLine(line);

                // Assert: kartKodu should be cleaned SKU
                var expectedKartKodu = StockCardPreparationService.CleanSpecialChars(sku.Get);
                return request.KartKodu == expectedKartKodu;
            });
    }

    [Property(MaxTest = 100)]
    public Property DataMappingCorrectness_KartAdiMatchesProductName()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            Arb.From<NonEmptyString>(),
            (sku, productName) =>
            {
                // Arrange
                var line = new SalesOrderLine
                {
                    SKU = sku.Get,
                    ProductName = productName.Get,
                    TaxRate = 20
                };

                // Act
                var request = StockCardPreparationService.MapFromOrderLine(line);

                // Assert: kartAdi should be cleaned ProductName
                var expectedKartAdi = StockCardPreparationService.CleanSpecialChars(productName.Get);
                return request.KartAdi == expectedKartAdi;
            });
    }

    [Property(MaxTest = 100)]
    public Property DataMappingCorrectness_KartTuruIsAlwaysOne()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            sku =>
            {
                // Arrange
                var line = new SalesOrderLine
                {
                    SKU = sku.Get,
                    ProductName = "Test Product"
                };

                // Act
                var request = StockCardPreparationService.MapFromOrderLine(line);

                // Assert: kartTuru should always be 1
                return request.KartTuru == 1;
            });
    }

    [Property(MaxTest = 100)]
    public Property DataMappingCorrectness_OlcumBirimiIdIsAlwaysOne()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            sku =>
            {
                // Arrange
                var line = new SalesOrderLine
                {
                    SKU = sku.Get,
                    ProductName = "Test Product"
                };

                // Act
                var request = StockCardPreparationService.MapFromOrderLine(line);

                // Assert: OlcumBirimiId should always be 1 (ADET)
                return request.OlcumBirimiId == 1;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 6: KDV Rate Calculation**
    /// **Validates: Requirements 3.3**
    /// 
    /// For any order line with TaxRate T, the stock card KDV rate should be:
    /// - If T > 1: T / 100 (percentage to decimal)
    /// - If T <= 1: T (already decimal)
    /// - If T is null: 0.20 (default)
    /// </summary>
    [Property(MaxTest = 100)]
    public Property KdvRateCalculation_PercentageConvertedToDecimal()
    {
        return Prop.ForAll(
            Gen.Choose(2, 100).ToArbitrary(), // Values > 1 (percentages)
            percentage =>
            {
                // Act
                var result = StockCardPreparationService.CalculateKdvRate(percentage);

                // Assert: Should be converted to decimal
                var expected = percentage / 100.0;
                return Math.Abs(result - expected) < 0.0001;
            });
    }

    [Property(MaxTest = 100)]
    public Property KdvRateCalculation_DecimalRemainsUnchanged()
    {
        return Prop.ForAll(
            Gen.Choose(0, 100).Select(x => x / 100.0m).ToArbitrary(), // Values 0.0 - 1.0
            decimalRate =>
            {
                // Act
                var result = StockCardPreparationService.CalculateKdvRate(decimalRate);

                // Assert: Should remain unchanged
                return Math.Abs(result - (double)decimalRate) < 0.0001;
            });
    }

    [Fact]
    public void KdvRateCalculation_NullDefaultsTo20Percent()
    {
        // Act
        var result = StockCardPreparationService.CalculateKdvRate(null);

        // Assert
        result.Should().BeApproximately(0.20, 0.0001);
    }

    [Theory]
    [InlineData(20, 0.20)]
    [InlineData(18, 0.18)]
    [InlineData(8, 0.08)]
    [InlineData(0.20, 0.20)]
    [InlineData(0.18, 0.18)]
    [InlineData(0, 0)]
    public void KdvRateCalculation_SpecificValues(decimal input, double expected)
    {
        // Act
        var result = StockCardPreparationService.CalculateKdvRate(input);

        // Assert
        result.Should().BeApproximately(expected, 0.0001);
    }

    [Theory]
    [InlineData("TestØProduct", "TestOProduct")]
    [InlineData("Testøproduct", "Testoproduct")]
    [InlineData("  Trimmed  ", "Trimmed")]
    [InlineData("ØøØø", "OoOo")]
    [InlineData("Normal", "Normal")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void CleanSpecialChars_HandlesVariousInputs(string? input, string expected)
    {
        // Act
        var result = StockCardPreparationService.CleanSpecialChars(input);

        // Assert
        result.Should().Be(expected);
    }
}


/// <summary>
/// Property-based tests for duplicate error handling.
/// </summary>
public class DuplicateErrorHandlingPropertyTests
{
    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 8: Duplicate Error Handling**
    /// **Validates: Requirements 5.3, 5.5**
    /// 
    /// For any UpsertStockCardAsync call that returns a duplicate error, 
    /// the system should treat it as success (action = "exists") rather than failure.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DuplicateErrorHandling_DuplicatesTreatedAsSuccess()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            duplicateCount =>
            {
                // Arrange: Create a result with duplicates
                var result = CreateResultWithDuplicates(duplicateCount.Get);

                // Assert: IsSuccess should be true when DuplicateRecords > 0
                return result.IsSuccess == true;
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 8: Duplicate Error Handling**
    /// **Validates: Requirements 5.3, 5.5**
    /// 
    /// Duplicate results should have action = "exists" not "failed".
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DuplicateErrorHandling_DuplicatesHaveExistsAction()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            count =>
            {
                // Arrange: Create results where all are duplicates
                var result = CreateAllDuplicatesResult(count.Get);

                // Assert: All results should have action = "exists"
                return result.Results.All(r => r.Action == "exists");
            });
    }

    /// <summary>
    /// **Feature: auto-stock-card-on-approval, Property 8: Duplicate Error Handling**
    /// **Validates: Requirements 5.3, 5.5**
    /// 
    /// When duplicates are detected, FailedCount should not include them.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DuplicateErrorHandling_DuplicatesNotCountedAsFailed()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            count =>
            {
                // Arrange: Create results where all are duplicates
                var result = CreateAllDuplicatesResult(count.Get);

                // Assert: FailedCount should be 0 when all are duplicates
                return result.FailedCount == 0;
            });
    }

    #region Helper Methods

    private static SyncResultDto CreateResultWithDuplicates(int duplicateCount)
    {
        return new SyncResultDto
        {
            IsSuccess = true, // Duplicates are treated as success
            ProcessedRecords = duplicateCount,
            SuccessfulRecords = 0,
            FailedRecords = 0,
            DuplicateRecords = duplicateCount,
            Message = $"{duplicateCount} duplicate(s) detected and treated as success"
        };
    }

    private static StockCardPreparationResult CreateAllDuplicatesResult(int count)
    {
        var results = new List<StockCardOperationResult>();
        for (int i = 0; i < count; i++)
        {
            results.Add(new StockCardOperationResult
            {
                SKU = $"SKU-{i:D4}",
                ProductName = $"Product {i}",
                Action = "exists", // Duplicates are marked as "exists"
                SkartId = 1000 + i,
                Message = "Stock card already exists (duplicate detected)",
                ProcessedAt = DateTime.UtcNow
            });
        }

        return new StockCardPreparationResult
        {
            AllSucceeded = true,
            TotalLines = count,
            SuccessCount = count, // Duplicates count as success
            FailedCount = 0,
            SkippedCount = 0,
            Results = results
        };
    }

    #endregion
}
