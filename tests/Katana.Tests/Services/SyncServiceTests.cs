using System;
using Katana.Business.Interfaces;
using Katana.Business.UseCases.Sync;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace Katana.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly Mock<IExtractorService> _mockExtractor;
    private readonly Mock<ITransformerService> _mockTransformer;
    private readonly Mock<ILoaderService> _mockLoader;
    private readonly Mock<ILucaService> _mockLucaService;
    private readonly IntegrationDbContext _context;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        _mockLogger = new Mock<ILogger<SyncService>>();
        _mockExtractor = new Mock<IExtractorService>();
        _mockTransformer = new Mock<ITransformerService>();
        _mockLoader = new Mock<ILoaderService>();
        _mockLucaService = new Mock<ILucaService>();

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IntegrationDbContext(options);

        
        _mockExtractor.Setup(e => e.ExtractProductsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto> { new() { SKU = "PRD-1", Name = "Prod", Price = 10m, CategoryId = 1, IsActive = true } });
        _mockExtractor.Setup(e => e.ExtractInvoicesAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InvoiceDto> { new() { InvoiceNo = "INV-1", CustomerTaxNo = "1111111111", CustomerName = "ACME", Amount = 100m, TaxAmount = 18m, TotalAmount = 118m, InvoiceDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(7), Currency = "TRY", Items = new List<InvoiceItemDto>{ new() { ProductSKU = "PRD-1", ProductName = "Prod", Quantity = 1, UnitPrice = 100m, TaxRate = 0.18m, TaxAmount = 18m, TotalAmount = 118m } } } });
        _mockExtractor.Setup(e => e.ExtractCustomersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerDto> { new() { Id = 1, TaxNo = "1111111111", Title = "ACME", IsActive = true, CreatedAt = DateTime.UtcNow } });

        _mockTransformer.Setup(t => t.ToProductsAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ReturnsAsync((IEnumerable<ProductDto> dtos) => dtos.Select(d => new Product { SKU = d.SKU, Name = d.Name, Price = d.Price, CategoryId = d.CategoryId, IsActive = d.IsActive }).ToList());
        _mockTransformer.Setup(t => t.ToInvoicesAsync(It.IsAny<IEnumerable<InvoiceDto>>()))
            .ReturnsAsync((IEnumerable<InvoiceDto> dtos) => dtos.Select(d => new Invoice { InvoiceNo = d.InvoiceNo, Amount = d.Amount, TaxAmount = d.TaxAmount, TotalAmount = d.TotalAmount, InvoiceDate = d.InvoiceDate, DueDate = d.DueDate, Currency = d.Currency, InvoiceItems = d.Items.Select(i => new InvoiceItem{ ProductSKU = i.ProductSKU, ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice, TaxRate = i.TaxRate, TaxAmount = i.TaxAmount, TotalAmount = i.TotalAmount }).ToList() }).ToList());
        _mockTransformer.Setup(t => t.ToCustomersAsync(It.IsAny<IEnumerable<CustomerDto>>()))
            .ReturnsAsync((IEnumerable<CustomerDto> dtos) => dtos.Select(d => new Customer { Id = d.Id, TaxNo = d.TaxNo, Title = d.Title, IsActive = d.IsActive }).ToList());

        _mockLoader.Setup(l => l.LoadProductsAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Product> products, IReadOnlyDictionary<string, string> _, int __, CancellationToken ___) => products.Count());
        _mockLoader.Setup(l => l.LoadInvoicesAsync(It.IsAny<IEnumerable<Invoice>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Invoice> invoices, IReadOnlyDictionary<string, string> _, int __, CancellationToken ___) => invoices.Count());
        _mockLoader.Setup(l => l.LoadCustomersAsync(It.IsAny<IEnumerable<Customer>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Customer> customers, IReadOnlyDictionary<string, string> _, int __, CancellationToken ___) => customers.Count());

        _syncService = new SyncService(_mockExtractor.Object, _mockTransformer.Object, _mockLoader.Object, _mockLucaService.Object, _context, _mockLogger.Object);
    }

    [Fact]
    public async Task SyncStockAsync_WhenCalled_ShouldReturnMockResult()
    {
        
        var fromDate = DateTime.UtcNow.AddDays(-7);

        
        var result = await _syncService.SyncStockAsync(fromDate);

        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("STOCK");
    }

    [Fact]
    public async Task SyncInvoicesAsync_WhenCalled_ShouldReturnMockResult()
    {
        
        var fromDate = DateTime.UtcNow.AddDays(-7);

        
        var result = await _syncService.SyncInvoicesAsync(fromDate);

        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("INVOICE");
    }

    [Fact]
    public async Task SyncCustomersAsync_WhenCalled_ShouldReturnMockResult()
    {
        
        var fromDate = DateTime.UtcNow.AddDays(-7);

        
        var result = await _syncService.SyncCustomersAsync(fromDate);

        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.SyncType.Should().Be("CUSTOMER");
    }

    [Fact]
    public async Task SyncAllAsync_WhenCalled_ShouldReturnBatchResult()
    {
        
        var fromDate = DateTime.UtcNow.AddDays(-7);

        
        var result = await _syncService.SyncAllAsync(fromDate);

        
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(4);
        result.OverallSuccess.Should().BeTrue();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
