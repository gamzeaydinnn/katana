using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Katana.Core.Entities;
using Katana.Core.DTOs;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Katana.API.Controllers;




[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TestController : ControllerBase
{
    private readonly IKatanaService _katanaService;
    private readonly ILucaService _lucaService;
    private readonly IntegrationDbContext _dbContext;
    private readonly IOptions<KatanaApiSettings> _settings;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IKatanaService katanaService,
        ILucaService lucaService,
        IntegrationDbContext dbContext,
        IOptions<KatanaApiSettings> settings,
        ILogger<TestController> logger)
    {
        _katanaService = katanaService;
        _lucaService = lucaService;
        _dbContext = dbContext;
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("katana-config")]
    public IActionResult GetKatanaConfig()
    {
        var config = _settings.Value;
        return Ok(new
        {
            baseUrl = config.BaseUrl,
            productsEndpoint = config.Endpoints.Products,
            fullUrl = $"{config.BaseUrl.TrimEnd('/')}/{config.Endpoints.Products}",
            apiKeyLength = config.ApiKey?.Length ?? 0,
            apiKeyFirst10 = config.ApiKey?.Substring(0, Math.Min(10, config.ApiKey.Length)),
            hasApiKey = !string.IsNullOrEmpty(config.ApiKey)
        });
    }

    [HttpGet("katana-direct")]
    public async Task<IActionResult> TestKatanaDirect()
    {
        try
        {
            _logger.LogWarning("=== DIRECT KATANA API TEST START ===");

            var products = await _katanaService.GetProductsAsync();

            _logger.LogWarning("=== DIRECT KATANA API TEST END - Products Count: {Count} ===", products.Count);

            return Ok(new
            {
                success = true,
                count = products.Count,
                firstProduct = products.FirstOrDefault()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test failed");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    
    
    
    [HttpPost("sync-one-product")]
    public async Task<IActionResult> SyncOneProduct([FromQuery] string? sku = null)
    {
        try
        {
            _logger.LogInformation("🧪 Test: Syncing single product to Luca. SKU={SKU}", sku ?? "first-available");

            var product = await _dbContext.Products
                .Where(p => p.IsActive && (string.IsNullOrEmpty(sku) || p.SKU == sku))
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { success = false, message = "Ürün bulunamadı", sku });
            }

            var stockCard = MappingHelper.MapToLucaStockCard(product);
            var payload = JsonSerializer.Serialize(stockCard, new JsonSerializerOptions { WriteIndented = false });
            _logger.LogInformation("📤 Sending product to Luca. SKU={SKU} Payload={Payload}", product.SKU, payload);

            var result = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ürün Luca'ya gönderilemedi",
                    errors = result.Errors,
                    response = result
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Ürün Luca'ya gönderildi: {product.SKU}",
                product = new { product.Id, product.SKU, product.Name, product.Price, product.IsActive },
                lucaResponse = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Test sync failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Test ürün gönderimi başarısız",
                error = ex.Message
            });
        }
    }

    
    
    
    [HttpPost("create-test-product")]
    public async Task<IActionResult> CreateTestProduct()
    {
        try
        {
            _logger.LogInformation("🧪 Creating test product for Luca sync");

            var testProduct = new Product
            {
                SKU = $"TEST_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Name = "Test Ürün - Otomatik",
                Description = "Katana-Luca entegrasyon test ürünü",
                Price = 123.45m,
                IsActive = true,
                CategoryId = 1,
                StockSnapshot = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Products.Add(testProduct);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Test product created in DB: {SKU}", testProduct.SKU);

            var stockCard = MappingHelper.MapToLucaStockCard(testProduct);
            var payload = JsonSerializer.Serialize(stockCard, new JsonSerializerOptions { WriteIndented = false });
            _logger.LogInformation("📤 Sending test product to Luca. SKU={SKU} Payload={Payload}", testProduct.SKU, payload);

            var result = await _lucaService.SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Test ürünü DB'ye kaydedildi ama Luca'ya gönderilemedi",
                    errors = result.Errors,
                    response = result
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Test ürünü oluşturuldu ve Luca'ya gönderildi: {testProduct.SKU}",
                product = new { testProduct.Id, testProduct.SKU, testProduct.Name, testProduct.Price },
                lucaResponse = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Test product creation failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Test ürünü oluşturulamadı",
                error = ex.Message
            });
        }
    }
}
