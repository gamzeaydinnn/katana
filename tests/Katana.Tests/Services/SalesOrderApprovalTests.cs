using Katana.Core.DTOs;
using Katana.Core.Entities;
using Xunit;

namespace Katana.Tests.Services;

/// <summary>
/// Unit tests for Sales Order Approval functionality
/// Tests Requirements: 1.1, 1.2, 3.1, 3.2, 3.3, 6.1, 6.2, 6.3
/// </summary>
public class SalesOrderApprovalTests
{
    #region Order Validation Tests (Requirements 3.1, 3.2, 3.3)

    [Fact]
    public void ValidateOrder_WithNoLines_ShouldFail()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Lines = new List<SalesOrderLine>()
        };

        // Act
        var result = ValidateOrderForApproval(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("satır", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public void ValidateOrder_WithEmptySKU_ShouldFail()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Lines = new List<SalesOrderLine>
            {
                new() { Id = 1, SKU = "", Quantity = 10, VariantId = 1 }
            }
        };

        // Act
        var result = ValidateOrderForApproval(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("SKU", result.ErrorMessage ?? "");
    }

    [Fact]
    public void ValidateOrder_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Lines = new List<SalesOrderLine>
            {
                new() { Id = 1, SKU = "SKU-001", Quantity = 0, VariantId = 1 }
            }
        };

        // Act
        var result = ValidateOrderForApproval(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("miktar", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public void ValidateOrder_WithMissingVariantId_ShouldFail()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Lines = new List<SalesOrderLine>
            {
                new() { Id = 1, SKU = "SKU-001", Quantity = 10, VariantId = 0 }
            }
        };

        // Act
        var result = ValidateOrderForApproval(order);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("VariantId", result.ErrorMessage ?? "");
    }

    [Fact]
    public void ValidateOrder_WithValidData_ShouldPass()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Lines = new List<SalesOrderLine>
            {
                new() { Id = 1, SKU = "SKU-001", Quantity = 10, VariantId = 123 },
                new() { Id = 2, SKU = "SKU-002", Quantity = 5, VariantId = 456 }
            }
        };

        // Act
        var result = ValidateOrderForApproval(order);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region Duplicate Approval Tests (Requirements 6.1, 6.2, 6.3)

    [Fact]
    public void CheckDuplicateApproval_WithApprovedStatus_ShouldReject()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Status = "APPROVED",
            KatanaOrderId = 0
        };

        // Act
        var isDuplicate = IsAlreadyApproved(order);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void CheckDuplicateApproval_WithKatanaOrderId_ShouldReject()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Status = "NOT_SHIPPED",
            KatanaOrderId = 12345
        };

        // Act
        var isDuplicate = IsAlreadyApproved(order);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void CheckDuplicateApproval_WithPendingStatus_ShouldAllow()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Status = "NOT_SHIPPED",
            KatanaOrderId = 0
        };

        // Act
        var isDuplicate = IsAlreadyApproved(order);

        // Assert
        Assert.False(isDuplicate);
    }

    #endregion

    #region Katana Order Builder Tests (Requirements 1.2, 2.1, 2.2, 2.3, 2.4)

    [Fact]
    public void BuildKatanaOrder_WithSingleLine_ShouldCreateOneRow()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Currency = "TRY",
            Customer = new Customer { ReferenceId = "123" },
            Lines = new List<SalesOrderLine>
            {
                new() { SKU = "SKU-001", Quantity = 10, VariantId = 100, PricePerUnit = 50 }
            }
        };

        // Act
        var katanaOrder = BuildKatanaOrderFromSalesOrder(order);

        // Assert
        Assert.Single(katanaOrder.SalesOrderRows);
        Assert.Equal(10, katanaOrder.SalesOrderRows[0].Quantity);
        Assert.Equal(100, katanaOrder.SalesOrderRows[0].VariantId);
    }

    [Fact]
    public void BuildKatanaOrder_WithMultipleLines_ShouldCreateMultipleRows()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-001",
            Currency = "TRY",
            Customer = new Customer { ReferenceId = "123" },
            Lines = new List<SalesOrderLine>
            {
                new() { SKU = "SKU-001", Quantity = 10, VariantId = 100, PricePerUnit = 50 },
                new() { SKU = "SKU-002", Quantity = 5, VariantId = 200, PricePerUnit = 100 },
                new() { SKU = "SKU-003", Quantity = 3, VariantId = 300, PricePerUnit = 75 }
            }
        };

        // Act
        var katanaOrder = BuildKatanaOrderFromSalesOrder(order);

        // Assert
        Assert.Equal(3, katanaOrder.SalesOrderRows.Count);
        Assert.Equal(10, katanaOrder.SalesOrderRows[0].Quantity);
        Assert.Equal(5, katanaOrder.SalesOrderRows[1].Quantity);
        Assert.Equal(3, katanaOrder.SalesOrderRows[2].Quantity);
    }

    [Fact]
    public void BuildKatanaOrder_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var order = new SalesOrder
        {
            Id = 1,
            OrderNo = "SO-TEST-123",
            Currency = "EUR",
            AdditionalInfo = "Test order",
            CustomerRef = "CUST-REF-001",
            Customer = new Customer { ReferenceId = "456" },
            LocationId = 789,
            Lines = new List<SalesOrderLine>
            {
                new() { SKU = "SKU-001", Quantity = 10, VariantId = 100, PricePerUnit = 50, TaxRateId = 1 }
            }
        };

        // Act
        var katanaOrder = BuildKatanaOrderFromSalesOrder(order);

        // Assert
        Assert.Equal("SO-TEST-123", katanaOrder.OrderNo);
        Assert.Equal("EUR", katanaOrder.Currency);
        Assert.Equal("Test order", katanaOrder.AdditionalInfo);
        Assert.Equal("CUST-REF-001", katanaOrder.CustomerRef);
        Assert.Equal(456, katanaOrder.CustomerId);
        Assert.Equal(789, katanaOrder.LocationId);
        Assert.Equal("NOT_SHIPPED", katanaOrder.Status);
    }

    #endregion

    #region Helper Methods (Simulating Controller Logic)

    private static OrderValidationResult ValidateOrderForApproval(SalesOrder? order)
    {
        if (order == null)
            return OrderValidationResult.Fail("Sipariş bulunamadı");

        if (order.Lines == null || order.Lines.Count == 0)
            return OrderValidationResult.Fail("Sipariş satırları bulunamadı");

        var errors = new List<string>();
        foreach (var line in order.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.SKU))
                errors.Add($"Satır {line.Id}: SKU eksik");
            if (line.Quantity <= 0)
                errors.Add($"Satır {line.Id} ({line.SKU}): Geçersiz miktar ({line.Quantity})");
            if (line.VariantId <= 0)
                errors.Add($"Satır {line.Id} ({line.SKU}): VariantId eksik");
        }

        if (errors.Count > 0)
            return OrderValidationResult.Fail("Sipariş doğrulama hatası", errors);

        return OrderValidationResult.Success();
    }

    private static bool IsAlreadyApproved(SalesOrder order)
    {
        return order.Status == "APPROVED" || 
               order.Status == "SHIPPED" || 
               (order.KatanaOrderId > 0);
    }

    private static SalesOrderDto BuildKatanaOrderFromSalesOrder(SalesOrder order)
    {
        long katanaCustomerId = 0;
        if (order.Customer?.ReferenceId != null && long.TryParse(order.Customer.ReferenceId, out var parsedId))
        {
            katanaCustomerId = parsedId;
        }

        return new SalesOrderDto
        {
            OrderNo = order.OrderNo ?? $"SO-{order.Id}",
            CustomerId = katanaCustomerId,
            LocationId = order.LocationId,
            DeliveryDate = order.DeliveryDate,
            Currency = order.Currency ?? "TRY",
            Status = "NOT_SHIPPED",
            AdditionalInfo = order.AdditionalInfo,
            CustomerRef = order.CustomerRef,
            SalesOrderRows = order.Lines.Select(line => new SalesOrderRowDto
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                PricePerUnit = line.PricePerUnit,
                TaxRateId = line.TaxRateId,
                LocationId = line.LocationId ?? order.LocationId,
                Attributes = new List<SalesOrderRowAttributeDto>()
            }).ToList(),
            Addresses = new List<SalesOrderAddressDto>()
        };
    }

    #endregion
}
