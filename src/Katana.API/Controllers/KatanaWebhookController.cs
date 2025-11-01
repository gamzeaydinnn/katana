using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Katana.Data.Models;

namespace Katana.API.Controllers;

/// <summary>
/// Katana API'den gelen webhook isteklerini yakalar.
/// Stok değişiklikleri otomatik olarak pending adjustment'a dönüştürülür.
/// </summary>
[ApiController]
[Route("api/webhook/katana")]
[AllowAnonymous] // Katana API'den geldiği için token auth yerine API key kontrolü yapılacak
public class KatanaWebhookController : ControllerBase
{
    private readonly IPendingStockAdjustmentService _pendingService;
    private readonly ILogger<KatanaWebhookController> _logger;
    private readonly IConfiguration _configuration;

    public KatanaWebhookController(
        IPendingStockAdjustmentService pendingService,
        ILogger<KatanaWebhookController> logger,
        IConfiguration configuration)
    {
        _pendingService = pendingService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Katana API'den stok değişikliği webhook'u alır ve pending adjustment oluşturur.
    /// </summary>
    /// <remarks>
    /// Webhook Payload Örneği:
    /// {
    ///   "event": "stock.updated",
    ///   "orderId": "ORD-12345",
    ///   "productId": 123,
    ///   "sku": "SKU-ABC-001",
    ///   "quantityChange": -5,
    ///   "timestamp": "2025-11-01T10:30:00Z"
    /// }
    /// </remarks>
    [HttpPost("stock-change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveStockChange([FromBody] KatanaStockChangeWebhook webhook)
    {
        // API Key validation
        var expectedApiKey = _configuration["KatanaApi:WebhookSecret"];
        var receivedApiKey = Request.Headers["X-Katana-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedApiKey) || receivedApiKey != expectedApiKey)
        {
            _logger.LogWarning("Unauthorized webhook attempt from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { error = "Invalid webhook signature" });
        }

        try
        {
            _logger.LogInformation(
                "Received Katana webhook: Event={Event}, OrderId={OrderId}, SKU={SKU}, Quantity={Qty}",
                webhook.Event,
                webhook.OrderId,
                webhook.Sku,
                webhook.QuantityChange
            );

            // Otomatik pending adjustment oluştur
            var pendingAdjustment = new PendingStockAdjustment
            {
                ExternalOrderId = webhook.OrderId,
                ProductId = webhook.ProductId,
                Sku = webhook.Sku ?? $"PRODUCT-{webhook.ProductId}",
                Quantity = webhook.QuantityChange,
                RequestedBy = "Katana-API", // Sistem otomatik oluşturdu
                RequestedAt = webhook.Timestamp != default ? webhook.Timestamp : DateTimeOffset.UtcNow,
                Status = "Pending",
                Notes = $"Katana webhook: {webhook.Event}"
            };

            var created = await _pendingService.CreateAsync(pendingAdjustment);

            _logger.LogInformation(
                "Created pending adjustment #{PendingId} from Katana webhook (OrderId: {OrderId})",
                created.Id,
                webhook.OrderId
            );

            return Ok(new
            {
                success = true,
                pendingId = created.Id,
                message = "Stok değişikliği admin onayına gönderildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Katana webhook");
            return BadRequest(new { error = "Webhook processing failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Webhook test endpoint - development için
    /// </summary>
    [HttpPost("test")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public async Task<IActionResult> TestWebhook()
    {
        var testWebhook = new KatanaStockChangeWebhook
        {
            Event = "stock.updated",
            OrderId = $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ProductId = 999,
            Sku = "TEST-SKU-001",
            QuantityChange = -3,
            Timestamp = DateTime.UtcNow
        };

        // Mock API key for test
        Request.Headers["X-Katana-Signature"] = _configuration["KatanaApi:WebhookSecret"] ?? "test-secret";

        return await ReceiveStockChange(testWebhook);
    }
}

/// <summary>
/// Katana API webhook payload model
/// </summary>
public class KatanaStockChangeWebhook
{
    /// <summary>Event type: stock.updated, stock.created, order.completed</summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>Katana order ID (external reference)</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Product ID in Katana system</summary>
    public int ProductId { get; set; }

    /// <summary>Product SKU</summary>
    public string? Sku { get; set; }

    /// <summary>Quantity change (negative = decrease, positive = increase)</summary>
    public int QuantityChange { get; set; }

    /// <summary>Webhook timestamp from Katana</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Additional metadata (optional)</summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
