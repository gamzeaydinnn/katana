using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Katana.Business.Interfaces;
using Katana.Business.Models.DTOs;
using Katana.Data.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Katana.API.Controllers
{
    [ApiController]
    [Route("api/luca")]
    public class LucaExcelController : ControllerBase
    {
        private readonly ILucaService _lucaService;
        private readonly LucaApiSettings _settings;
        private readonly ILogger<LucaExcelController> _logger;

        public LucaExcelController(
            ILucaService lucaService,
            IOptions<LucaApiSettings> lucaOptions,
            ILogger<LucaExcelController> logger)
        {
            _lucaService = lucaService;
            _settings = lucaOptions.Value;
            _logger = logger;
        }

        [HttpPost("send-product-from-excel")]
        public async Task<IActionResult> SendProductFromExcel([FromBody] ExcelProductDto product, CancellationToken ct)
        {
            if (product == null)
            {
                return BadRequest(new { error = "Body is required" });
            }

            if (!product.IsValid(out var error))
            {
                return BadRequest(new { error });
            }

            _logger.LogInformation("Excel product push requested. SKU={SKU}", product.SKU);

            var result = await _lucaService.SendProductsFromExcelAsync(new List<ExcelProductDto> { product }, ct);

            var success = result.IsSuccess || result.SuccessfulRecords > 0;
            return Ok(new
            {
                success,
                processed = result.ProcessedRecords,
                successful = result.SuccessfulRecords,
                failed = result.FailedRecords,
                message = result.Message,
                sku = product.SKU
            });
        }
    }
}
