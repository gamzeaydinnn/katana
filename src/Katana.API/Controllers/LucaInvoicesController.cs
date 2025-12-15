using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Katana.API.Controllers;

/// <summary>
/// Luca Fatura API Controller
/// 
/// Bu controller, Luca API'nin fatura modülünü expose eder.
/// Tüm endpoint'ler Luca API'ye direkt istek gönderir.
/// 
/// Kullanım:
/// - POST /api/luca-invoices/pdf-link - Fatura PDF linki al
/// - POST /api/luca-invoices/list - Fatura listesi
/// - POST /api/luca-invoices/list-currency - Dövizli fatura listesi
/// - POST /api/luca-invoices/create - Yeni fatura oluştur
/// - POST /api/luca-invoices/close - Fatura kapat/ödeme
/// - DELETE /api/luca-invoices/{invoiceId} - Fatura sil
/// - POST /api/luca-invoices/send - Fatura gönder (E-Fatura/E-Arşiv)
/// </summary>
[ApiController]
[Route("api/luca-invoices")]
public class LucaInvoicesController : ControllerBase
{
    private readonly ILucaService _lucaService;
    private readonly ILogger<LucaInvoicesController> _logger;

    public LucaInvoicesController(ILucaService lucaService, ILogger<LucaInvoicesController> logger)
    {
        _lucaService = lucaService;
        _logger = logger;
    }

    /// <summary>
    /// Fatura PDF Linki Al
    /// 
    /// Faturanın PDF çıktısını almak için kullanılan servistir.
    /// </summary>
    /// <param name="request">Fatura ID içeren istek</param>
    /// <returns>PDF link bilgisi</returns>
    [HttpPost("pdf-link")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvoicePdfLink([FromBody] LucaInvoicePdfLinkRequest request)
    {
        try
        {
            _logger.LogInformation("📄 Getting PDF link for invoice {InvoiceId}", request.SsFaturaBaslikId);

            var response = await _lucaService.GetInvoicePdfLinkAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get PDF link for invoice {InvoiceId}", request.SsFaturaBaslikId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fatura Listesi
    /// 
    /// Mevcut faturaları listelemek için kullanılır.
    /// </summary>
    /// <param name="request">Fatura listeleme parametreleri</param>
    /// <param name="detayliListe">Detaylı liste getirilsin mi?</param>
    /// <returns>Fatura listesi</returns>
    [HttpPost("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListInvoices(
        [FromBody] LucaListInvoicesRequest request, 
        [FromQuery] bool detayliListe = false)
    {
        try
        {
            _logger.LogInformation("📋 Listing invoices with ParUstHareketTuru={ParUstHareketTuru}", 
                request.ParUstHareketTuru);

            var response = await _lucaService.ListInvoicesAsync(request, detayliListe);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list invoices");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Dövizli Fatura Listesi
    /// 
    /// Dövizli faturaları listelemek için özelleştirilmiş bir istek.
    /// </summary>
    /// <param name="request">Dövizli fatura listeleme parametreleri</param>
    /// <returns>Dövizli fatura listesi</returns>
    [HttpPost("list-currency")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListCurrencyInvoices([FromBody] LucaListCurrencyInvoicesRequest request)
    {
        try
        {
            _logger.LogInformation("💱 Listing currency invoices");

            var response = await _lucaService.ListCurrencyInvoicesAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to list currency invoices");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fatura Oluştur
    /// 
    /// Yeni bir fatura oluşturmak için kullanılır. Tüm detaylar (cari, ürünler, vergiler) burada tanımlanır.
    /// 
    /// ÖNEMLI: Eğer HTML response alıyorsanız, session kaybı var demektir!
    /// </summary>
    /// <param name="request">Fatura oluşturma isteği (detaylar dahil)</param>
    /// <returns>Oluşturulan fatura bilgisi</returns>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateInvoice([FromBody] JsonElement request)
    {
        try
        {
            var rawJson = request.GetRawText();
            _logger.LogInformation("🧾 Creating invoice (passthrough) - payload length={Length}", rawJson?.Length ?? 0);

            var response = await _lucaService.CreateInvoiceRawJsonAsync(rawJson);
            
            // HTML response kontrolü
            if (response.ValueKind == JsonValueKind.String)
            {
                var content = response.GetString();
                if (!string.IsNullOrEmpty(content) && content.Contains("<html", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("❌ Received HTML response instead of JSON - session lost!");
                    return StatusCode(500, new
                    {
                        success = false,
                        error = "Session lost - received HTML response instead of JSON. Please try again.",
                        htmlPreview = content.Substring(0, Math.Min(200, content.Length))
                    });
                }
            }
            
            return StatusCode(201, new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create invoice (passthrough)");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fatura Kapat / Ödeme
    /// 
    /// Faturaya bağlı ödeme/kapama kaydı girmek için kullanılır.
    /// </summary>
    /// <param name="request">Fatura kapama isteği</param>
    /// <returns>Kapama işlemi sonucu</returns>
    [HttpPost("close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CloseInvoice([FromBody] LucaCloseInvoiceRequest request)
    {
        try
        {
            _logger.LogInformation("💰 Closing invoice FaturaId={FaturaId}, Tutar={Tutar}", 
                request.FaturaId, request.Tutar);

            if (request.FaturaId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid FaturaId"
                });
            }

            var response = await _lucaService.CloseInvoiceAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to close invoice FaturaId={FaturaId}", request.FaturaId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fatura Sil
    /// 
    /// Belirtilen faturayı silmek için kullanılır.
    /// </summary>
    /// <param name="invoiceId">Silinecek fatura ID</param>
    /// <returns>Silme işlemi sonucu</returns>
    [HttpDelete("{invoiceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteInvoice(long invoiceId)
    {
        try
        {
            _logger.LogInformation("🗑️ Deleting invoice {InvoiceId}", invoiceId);

            if (invoiceId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid invoice ID"
                });
            }

            var request = new LucaDeleteInvoiceRequest
            {
                SsFaturaBaslikId = invoiceId
            };

            var response = await _lucaService.DeleteInvoiceAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Fatura Gönder
    /// 
    /// E-Fatura veya E-Arşiv olarak fatura gönderir.
    /// </summary>
    /// <param name="request">Fatura gönderme isteği</param>
    /// <returns>Gönderim işlemi sonucu</returns>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendInvoice([FromBody] LucaSendInvoiceRequest request)
    {
        try
        {
            _logger.LogInformation("📤 Sending invoice {InvoiceId} with GonderimTipi={GonderimTipi}", 
                request.SsFaturaBaslikId, request.GonderimTipi);

            if (request.SsFaturaBaslikId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid invoice ID"
                });
            }

            // SendInvoiceAsync için LucaService.Queries.cs'deki yeni overload kullanılıyor
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var response = await _lucaService.SendInvoiceAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send invoice {InvoiceId}", request.SsFaturaBaslikId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Session Durumu Kontrol
    /// 
    /// Luca API session'ının durumunu kontrol eder.
    /// </summary>
    /// <returns>Session durumu</returns>
    [HttpGet("session-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionStatus()
    {
        try
        {
            var cacheStatus = await _lucaService.GetCacheStatusAsync();
            
            return Ok(new
            {
                success = true,
                data = cacheStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get session status");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Session Yenile
    /// 
    /// Luca API session'ını zorla yeniler (HTML response sorunu için).
    /// </summary>
    /// <returns>Yenileme sonucu</returns>
    [HttpPost("refresh-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshSession()
    {
        try
        {
            _logger.LogInformation("🔄 Forcing session refresh");
            
            await _lucaService.ForceSessionRefreshAsync();
            
            return Ok(new
            {
                success = true,
                message = "Session refreshed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to refresh session");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
