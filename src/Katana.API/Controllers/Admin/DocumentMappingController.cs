using Katana.Business.Services;
using Katana.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

[ApiController]
[Route("api/admin/document-mapping")]
public class DocumentMappingController : ControllerBase
{
    private readonly IDocumentMappingService _documentMappingService;
    private readonly ILogger<DocumentMappingController> _logger;

    public DocumentMappingController(
        IDocumentMappingService documentMappingService,
        ILogger<DocumentMappingController> logger)
    {
        _documentMappingService = documentMappingService;
        _logger = logger;
    }

    /// <summary>
    /// Katana belge tipini Koza'ya map et
    /// </summary>
    [HttpPost("map")]
    [ProducesResponseType(typeof(DocumentMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult MapDocument([FromBody] MapDocumentRequest request)
    {
        try
        {
            var (documentType, belgeTurDetayId) = _documentMappingService.MapKatanaToKoza(
                request.KatanaDocumentType,
                request.Metadata);

            var description = BelgeTuruValidator.GetDescription(belgeTurDetayId);

            return Ok(new DocumentMappingResponse
            {
                KatanaDocumentType = request.KatanaDocumentType,
                KozaDocumentType = documentType,
                BelgeTurDetayId = belgeTurDetayId,
                Description = description
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping document type: {DocumentType}", request.KatanaDocumentType);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Sales Order için belge türü al
    /// </summary>
    [HttpGet("sales-order")]
    [ProducesResponseType(typeof(BelgeTuruResponse), StatusCodes.Status200OK)]
    public IActionResult GetSalesOrderBelgeTuru([FromQuery] bool isFulfilled = false, [FromQuery] bool isReturn = false)
    {
        try
        {
            var belgeTurDetayId = _documentMappingService.GetBelgeTurDetayIdForSalesOrder(isFulfilled, isReturn);
            var description = BelgeTuruValidator.GetDescription(belgeTurDetayId);

            return Ok(new BelgeTuruResponse
            {
                BelgeTurDetayId = belgeTurDetayId,
                Description = description,
                IsFatura = BelgeTuruValidator.IsFatura(belgeTurDetayId),
                IsIrsaliye = BelgeTuruValidator.IsIrsaliye(belgeTurDetayId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order belge turu");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Purchase Order için belge türü al
    /// </summary>
    [HttpGet("purchase-order")]
    [ProducesResponseType(typeof(BelgeTuruResponse), StatusCodes.Status200OK)]
    public IActionResult GetPurchaseOrderBelgeTuru([FromQuery] bool isReceived = false, [FromQuery] bool isReturn = false)
    {
        try
        {
            var belgeTurDetayId = _documentMappingService.GetBelgeTurDetayIdForPurchaseOrder(isReceived, isReturn);
            var description = BelgeTuruValidator.GetDescription(belgeTurDetayId);

            return Ok(new BelgeTuruResponse
            {
                BelgeTurDetayId = belgeTurDetayId,
                Description = description,
                IsFatura = BelgeTuruValidator.IsFatura(belgeTurDetayId),
                IsIrsaliye = BelgeTuruValidator.IsIrsaliye(belgeTurDetayId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order belge turu");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Stock Adjustment için belge türü al
    /// </summary>
    [HttpGet("stock-adjustment")]
    [ProducesResponseType(typeof(BelgeTuruResponse), StatusCodes.Status200OK)]
    public IActionResult GetStockAdjustmentBelgeTuru([FromQuery] string adjustmentType = "")
    {
        try
        {
            var belgeTurDetayId = _documentMappingService.GetBelgeTurDetayIdForStockAdjustment(adjustmentType);
            var description = BelgeTuruValidator.GetDescription(belgeTurDetayId);

            return Ok(new BelgeTuruResponse
            {
                BelgeTurDetayId = belgeTurDetayId,
                Description = description,
                IsStokHareketi = BelgeTuruValidator.IsStokHareketi(belgeTurDetayId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock adjustment belge turu");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Belge türü validate et
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidateBelgeTuru([FromBody] ValidateBelgeTuruRequest request)
    {
        try
        {
            var (isValid, errorMessage) = BelgeTuruValidator.Validate(request.BelgeTurDetayId, request.DocumentType);
            
            return Ok(new ValidationResponse
            {
                IsValid = isValid,
                ErrorMessage = errorMessage,
                BelgeTurDetayId = request.BelgeTurDetayId,
                DocumentType = request.DocumentType,
                Description = BelgeTuruValidator.GetDescription(request.BelgeTurDetayId)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating belge turu");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Tüm belge türlerini listele
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(typeof(BelgeTuruListResponse), StatusCodes.Status200OK)]
    public IActionResult ListBelgeTurleri()
    {
        try
        {
            var response = new BelgeTuruListResponse
            {
                Fatura = new List<BelgeTuruItem>
                {
                    new() { Id = BelgeTuruValidator.Fatura.MalSatisFaturasi, Description = "Mal Satış Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.HizmetSatisFaturasi, Description = "Hizmet Satış Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.MalAlimFaturasi, Description = "Mal Alım Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.HizmetAlimFaturasi, Description = "Hizmet Alım Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.SatisIadeFaturasi, Description = "Satış İade Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.AlimIadeFaturasi, Description = "Alım İade Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.ProformaSatisFaturasi, Description = "Proforma Satış Faturası" },
                    new() { Id = BelgeTuruValidator.Fatura.ProformaAlimFaturasi, Description = "Proforma Alım Faturası" }
                },
                Irsaliye = new List<BelgeTuruItem>
                {
                    new() { Id = BelgeTuruValidator.Irsaliye.SatisIrsaliyesi, Description = "Satış İrsaliyesi" },
                    new() { Id = BelgeTuruValidator.Irsaliye.AlimIrsaliyesi, Description = "Alım İrsaliyesi" },
                    new() { Id = BelgeTuruValidator.Irsaliye.SatisIadeIrsaliyesi, Description = "Satış İade İrsaliyesi" },
                    new() { Id = BelgeTuruValidator.Irsaliye.AlimIadeIrsaliyesi, Description = "Alım İade İrsaliyesi" }
                },
                StokHareketi = new List<BelgeTuruItem>
                {
                    new() { Id = BelgeTuruValidator.StokHareketi.DepoTransferi, Description = "Depo Transferi" },
                    new() { Id = BelgeTuruValidator.StokHareketi.Fire, Description = "Fire" },
                    new() { Id = BelgeTuruValidator.StokHareketi.Sarf, Description = "Sarf" },
                    new() { Id = BelgeTuruValidator.StokHareketi.SayimFazlasi, Description = "Sayım Fazlası" },
                    new() { Id = BelgeTuruValidator.StokHareketi.SayimEksigi, Description = "Sayım Eksiği" },
                    new() { Id = BelgeTuruValidator.StokHareketi.DigerGiris, Description = "Diğer Giriş" },
                    new() { Id = BelgeTuruValidator.StokHareketi.DigerCikis, Description = "Diğer Çıkış" }
                },
                Sayim = new List<BelgeTuruItem>
                {
                    new() { Id = BelgeTuruValidator.Sayim.SayimSonucFisi, Description = "Sayım Sonuç Fişi" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing belge turleri");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}

// DTOs
public class MapDocumentRequest
{
    public string KatanaDocumentType { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class DocumentMappingResponse
{
    public string KatanaDocumentType { get; set; } = string.Empty;
    public string KozaDocumentType { get; set; } = string.Empty;
    public long BelgeTurDetayId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class BelgeTuruResponse
{
    public long BelgeTurDetayId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsFatura { get; set; }
    public bool IsIrsaliye { get; set; }
    public bool IsStokHareketi { get; set; }
}

public class ValidateBelgeTuruRequest
{
    public long BelgeTurDetayId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

public class ValidationResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public long BelgeTurDetayId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BelgeTuruListResponse
{
    public List<BelgeTuruItem> Fatura { get; set; } = new();
    public List<BelgeTuruItem> Irsaliye { get; set; } = new();
    public List<BelgeTuruItem> StokHareketi { get; set; } = new();
    public List<BelgeTuruItem> Sayim { get; set; } = new();
}

public class BelgeTuruItem
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
}
