using Katana.Business.Interfaces;
using Katana.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Katana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IntegrationTestController : ControllerBase
{
    private readonly IIntegrationTestService _testService;
    private readonly ILogger<IntegrationTestController> _logger;

    public IntegrationTestController(IIntegrationTestService testService, ILogger<IntegrationTestController> logger)
    {
        _testService = testService;
        _logger = logger;
    }

    [HttpPost("stock-flow")]
    public async Task<ActionResult<IntegrationTestResultDto>> TestStockFlow([FromQuery] int sampleSize = 10)
    {
        try
        {
            _logger.LogInformation("Stok entegrasyon testi başlatıldı: {SampleSize} kayıt", sampleSize);
            var result = await _testService.TestKatanaToLucaStockFlowAsync(sampleSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stok entegrasyon testi hatası");
            return StatusCode(500, new { message = "Test hatası", error = ex.Message });
        }
    }

    [HttpPost("invoice-flow")]
    public async Task<ActionResult<IntegrationTestResultDto>> TestInvoiceFlow([FromQuery] int sampleSize = 10)
    {
        try
        {
            _logger.LogInformation("Fatura entegrasyon testi başlatıldı: {SampleSize} kayıt", sampleSize);
            var result = await _testService.TestKatanaToLucaInvoiceFlowAsync(sampleSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura entegrasyon testi hatası");
            return StatusCode(500, new { message = "Test hatası", error = ex.Message });
        }
    }

    [HttpPost("mapping-consistency")]
    public async Task<ActionResult<IntegrationTestResultDto>> TestMappingConsistency()
    {
        try
        {
            _logger.LogInformation("Mapping tutarlılık testi başlatıldı");
            var result = await _testService.TestMappingConsistencyAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mapping tutarlılık testi hatası");
            return StatusCode(500, new { message = "Test hatası", error = ex.Message });
        }
    }

    [HttpPost("uat-suite")]
    public async Task<ActionResult<List<IntegrationTestResultDto>>> RunUATSuite()
    {
        try
        {
            _logger.LogInformation("UAT test paketi başlatıldı");
            var results = await _testService.RunFullUATSuiteAsync();
            
            var allSuccess = results.All(r => r.Success);
            return Ok(new 
            { 
                success = allSuccess, 
                totalTests = results.Count,
                passedTests = results.Count(r => r.Success),
                failedTests = results.Count(r => !r.Success),
                results 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UAT test paketi hatası");
            return StatusCode(500, new { message = "Test hatası", error = ex.Message });
        }
    }
}
