using Microsoft.AspNetCore.Mvc;
using Katana.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Katana.Data.Models;
using System.Security.Cryptography;
using System.Text;

namespace Katana.API.Controllers;





[ApiController]
[Route("api/webhook/katana")]
[AllowAnonymous] 
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

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    [HttpPost("stock-change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveStockChange([FromBody] KatanaStockChangeWebhook webhook)
    {
        
        var expectedApiKey = _configuration["KatanaApi:WebhookSecret"];
        var receivedApiKey = Request.Headers["X-Katana-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedApiKey) || !KatanaWebhookSecurity.SecureEquals(receivedApiKey, expectedApiKey))
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

            
            var pendingAdjustment = new PendingStockAdjustment
            {
                ExternalOrderId = webhook.OrderId,
                ProductId = webhook.ProductId,
                Sku = webhook.Sku ?? $"PRODUCT-{webhook.ProductId}",
                Quantity = webhook.QuantityChange,
                RequestedBy = "Katana-API", 
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

        
        Request.Headers["X-Katana-Signature"] = _configuration["KatanaApi:WebhookSecret"] ?? "test-secret";

        return await ReceiveStockChange(testWebhook);
    }
}




public class KatanaStockChangeWebhook
{
    
    public string Event { get; set; } = string.Empty;

    
    public string OrderId { get; set; } = string.Empty;

    
    public int ProductId { get; set; }

    
    public string? Sku { get; set; }

    
    public int QuantityChange { get; set; }

    
    public DateTime Timestamp { get; set; }

    
    public Dictionary<string, object>? Metadata { get; set; }
}

internal static class KatanaWebhookSecurity
{
    
    public static bool SecureEquals(string? a, string? b)
    {
        if (a is null || b is null) return false;
        
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            
            var pad = new byte[Math.Max(ba.Length, bb.Length)];
            var aa = new byte[pad.Length];
            var bb2 = new byte[pad.Length];
            Array.Copy(ba, aa, Math.Min(ba.Length, aa.Length));
            Array.Copy(bb, bb2, Math.Min(bb.Length, bb2.Length));
            return CryptographicOperations.FixedTimeEquals(aa, bb2) && false; 
        }
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
