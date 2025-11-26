using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.Business.Interfaces;
using Katana.Business.UseCases.Sync;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Katana.Tests.Services;

public class SyncServiceEdgeCaseTests : IDisposable
{
    private readonly IntegrationDbContext _db;
    private readonly Mock<IExtractorService> _extractor = new();
    private readonly Mock<ITransformerService> _transformer = new();
    private readonly Mock<ILoaderService> _loader = new();
    private readonly Mock<ILogger<SyncService>> _logger = new();

    public SyncServiceEdgeCaseTests()
    {
        var opts = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IntegrationDbContext(opts);
    }

    [Fact]
    public async Task SyncStock_EmptyExtraction_IsSuccessTrueWithZeroCounts()
    {
        
        _extractor.Setup(e => e.ExtractProductsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto>());
        _transformer.Setup(t => t.ToProductsAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ReturnsAsync(new List<Product>());
        _loader.Setup(l => l.LoadProductsAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var mockLucaService = new Mock<ILucaService>();
        var svc = new SyncService(_extractor.Object, _transformer.Object, _loader.Object, mockLucaService.Object, _db, _logger.Object);

        
        var result = await svc.SyncStockAsync(DateTime.UtcNow.AddDays(-1));

        
        result.IsSuccess.Should().BeTrue();
        result.ProcessedRecords.Should().Be(0);
        result.SuccessfulRecords.Should().Be(0);
        result.FailedRecords.Should().Be(0);

        var log = _db.SyncOperationLogs.OrderByDescending(x => x.StartTime).FirstOrDefault();
        log.Should().NotBeNull();
        log!.Status.Should().Be("SUCCESS");
        log.ProcessedRecords.Should().Be(0);
        log.SuccessfulRecords.Should().Be(0);
        log.FailedRecords.Should().Be(0);
    }

    [Fact]
    public async Task SyncStock_TransformerThrows_ReturnsFailureAndLogsFailed()
    {
        
        _extractor.Setup(e => e.ExtractProductsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductDto> { new() { SKU = "SKU-1", Name = "P", Price = 10m, CategoryId = 1, IsActive = true } });
        _transformer.Setup(t => t.ToProductsAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ThrowsAsync(new InvalidOperationException("Transform error"));

        var mockLucaService = new Mock<ILucaService>();
        var svc = new SyncService(_extractor.Object, _transformer.Object, _loader.Object, mockLucaService.Object, _db, _logger.Object);

        
        var result = await svc.SyncStockAsync();

        
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Transform error");

        var log = _db.SyncOperationLogs.OrderByDescending(x => x.StartTime).First();
        log.Status.Should().Be("FAILED");
        log.ErrorMessage.Should().Contain("Transform error");
        log.ProcessedRecords.Should().Be(0);
        log.SuccessfulRecords.Should().Be(0);
        log.FailedRecords.Should().Be(0);
    }

    [Fact]
    public async Task SyncStock_LoaderPartialSuccess_ReturnsPartialAndLogsCounts()
    {
        
        var dtos = new List<ProductDto>
        {
            new() { SKU = "A", Name = "A", Price = 1m, CategoryId = 1, IsActive = true },
            new() { SKU = "B", Name = "B", Price = 2m, CategoryId = 1, IsActive = true },
            new() { SKU = "C", Name = "C", Price = 3m, CategoryId = 1, IsActive = true }
        };
        _extractor.Setup(e => e.ExtractProductsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);
        _transformer.Setup(t => t.ToProductsAsync(It.IsAny<IEnumerable<ProductDto>>()))
            .ReturnsAsync((IEnumerable<ProductDto> p) => p.Select(d => new Product { SKU = d.SKU, Name = d.Name, Price = d.Price, CategoryId = d.CategoryId, IsActive = d.IsActive }).ToList());
        _loader.Setup(l => l.LoadProductsAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var mockLucaService = new Mock<ILucaService>();
        var svc = new SyncService(_extractor.Object, _transformer.Object, _loader.Object, mockLucaService.Object, _db, _logger.Object);

        
        var result = await svc.SyncStockAsync();

        
        result.IsSuccess.Should().BeFalse();
        result.ProcessedRecords.Should().Be(3);
        result.SuccessfulRecords.Should().Be(2);
        result.FailedRecords.Should().Be(1);

        var log = _db.SyncOperationLogs.OrderByDescending(x => x.StartTime).First();
        log.Status.Should().Be("FAILED");
        log.ProcessedRecords.Should().Be(3);
        log.SuccessfulRecords.Should().Be(2);
        log.FailedRecords.Should().Be(1);
    }

    [Fact]
    public async Task SyncCustomers_ExtractorThrows_OneFailedLog_NoRetry()
    {
        
        _extractor.Setup(e => e.ExtractCustomersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Extractor failure"));

        var mockLucaService = new Mock<ILucaService>();
        var svc = new SyncService(_extractor.Object, _transformer.Object, _loader.Object, mockLucaService.Object, _db, _logger.Object);

        
        var result = await svc.SyncCustomersAsync();

        
        result.IsSuccess.Should().BeFalse();
        var logs = _db.SyncOperationLogs.Where(l => l.SyncType == "CUSTOMER").ToList();
        logs.Count.Should().Be(1);
        logs[0].Status.Should().Be("FAILED");
        logs[0].ErrorMessage.Should().Contain("Extractor failure");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
