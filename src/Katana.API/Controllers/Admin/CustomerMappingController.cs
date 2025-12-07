using Katana.Business.Services;
using Katana.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers.Admin;

[ApiController]
[Route("api/admin/customer-mappings")]
public class CustomerMappingController : ControllerBase
{
    private readonly ICustomerMappingService _customerMappingService;
    private readonly ILogger<CustomerMappingController> _logger;

    public CustomerMappingController(
        ICustomerMappingService customerMappingService,
        ILogger<CustomerMappingController> logger)
    {
        _customerMappingService = customerMappingService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm customer mapping'leri getir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CustomerKozaCariMapping>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMappings()
    {
        try
        {
            var mappings = await _customerMappingService.GetAllMappingsAsync();
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customer mappings");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Customer ID'ye göre mapping getir
    /// </summary>
    [HttpGet("{customerId:int}")]
    [ProducesResponseType(typeof(CustomerKozaCariMapping), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappingByCustomerId(int customerId)
    {
        try
        {
            var mapping = await _customerMappingService.GetMappingByCustomerIdAsync(customerId);
            
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found", customerId });
            }

            return Ok(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mapping for Customer {CustomerId}", customerId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Customer ID'ye göre cari kodu getir
    /// </summary>
    [HttpGet("{customerId:int}/cari-kodu")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCariKodu(int customerId)
    {
        try
        {
            var cariKodu = await _customerMappingService.GetCariKoduByCustomerIdAsync(customerId);
            return Ok(new { customerId, cariKodu });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cari kodu for Customer {CustomerId}", customerId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Customer ID'ye göre finansal nesne ID getir
    /// </summary>
    [HttpGet("{customerId:int}/finansal-nesne-id")]
    [ProducesResponseType(typeof(long?), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinansalNesneId(int customerId)
    {
        try
        {
            var finansalNesneId = await _customerMappingService.GetFinansalNesneIdByCustomerIdAsync(customerId);
            return Ok(new { customerId, finansalNesneId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting finansal nesne ID for Customer {CustomerId}", customerId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Vergi numarasına göre mapping getir (duplicate kontrolü)
    /// </summary>
    [HttpGet("by-tax-no/{taxNo}")]
    [ProducesResponseType(typeof(CustomerKozaCariMapping), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappingByTaxNo(string taxNo)
    {
        try
        {
            var mapping = await _customerMappingService.GetMappingByTaxNoAsync(taxNo);
            
            if (mapping == null)
            {
                return NotFound(new { error = "Mapping not found", taxNo });
            }

            return Ok(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mapping for TaxNo {TaxNo}", taxNo);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Customer mapping oluştur veya güncelle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerKozaCariMapping), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateMapping([FromBody] CreateCustomerMappingRequest request)
    {
        try
        {
            if (request.KatanaCustomerId <= 0)
            {
                return BadRequest(new { error = "KatanaCustomerId must be greater than 0" });
            }

            if (string.IsNullOrWhiteSpace(request.KozaCariKodu))
            {
                return BadRequest(new { error = "KozaCariKodu is required" });
            }

            var mapping = await _customerMappingService.CreateOrUpdateMappingAsync(
                request.KatanaCustomerId,
                request.KozaCariKodu,
                request.KozaFinansalNesneId,
                request.KatanaCustomerName,
                request.KozaCariTanim,
                request.KatanaCustomerTaxNo);

            return Ok(mapping);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating customer mapping");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Duplicate customer kontrolü
    /// </summary>
    [HttpPost("check-duplicate")]
    [ProducesResponseType(typeof(DuplicateCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequest request)
    {
        try
        {
            var isDuplicate = await _customerMappingService.IsDuplicateCustomerAsync(
                request.CustomerId, 
                request.TaxNo);
            
            return Ok(new DuplicateCheckResponse
            {
                CustomerId = request.CustomerId,
                TaxNo = request.TaxNo,
                IsDuplicate = isDuplicate,
                Message = isDuplicate 
                    ? "Bu vergi numarası ile başka bir müşteri kaydı bulundu" 
                    : "Duplicate müşteri bulunamadı"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate for Customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Customer → Cari Kodu dictionary'si getir
    /// </summary>
    [HttpGet("dictionary")]
    [ProducesResponseType(typeof(Dictionary<int, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerToCariKoduMap()
    {
        try
        {
            var map = await _customerMappingService.GetCustomerToCariKoduMapAsync();
            return Ok(map);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer to cari kodu map");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}

// DTOs
public class CreateCustomerMappingRequest
{
    public int KatanaCustomerId { get; set; }
    public string KozaCariKodu { get; set; } = string.Empty;
    public long? KozaFinansalNesneId { get; set; }
    public string? KatanaCustomerName { get; set; }
    public string? KozaCariTanim { get; set; }
    public string? KatanaCustomerTaxNo { get; set; }
}

public class CheckDuplicateRequest
{
    public int CustomerId { get; set; }
    public string TaxNo { get; set; } = string.Empty;
}

public class DuplicateCheckResponse
{
    public int CustomerId { get; set; }
    public string TaxNo { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public string Message { get; set; } = string.Empty;
}
