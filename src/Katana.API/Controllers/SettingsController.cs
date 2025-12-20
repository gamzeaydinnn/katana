using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Katana.Data.Configuration;

namespace Katana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IOptionsSnapshot<SyncSettings> _syncSettings;
    private readonly IOptionsSnapshot<KatanaApiSettings> _katanaSettings;
    private readonly IOptionsSnapshot<LucaApiSettings> _lucaSettings;
    private readonly IOptionsSnapshot<CatalogVisibilitySettings> _catalogVisibility;
    private readonly ILogger<SettingsController> _logger;
    private static SettingsDto? _cachedSettings;

    public static SettingsDto? GetCachedSettings() => _cachedSettings;

    public SettingsController(
        IOptionsSnapshot<SyncSettings> syncSettings,
        IOptionsSnapshot<KatanaApiSettings> katanaSettings,
        IOptionsSnapshot<LucaApiSettings> lucaSettings,
        IOptionsSnapshot<CatalogVisibilitySettings> catalogVisibility,
        ILogger<SettingsController> logger)
    {
        _syncSettings = syncSettings;
        _katanaSettings = katanaSettings;
        _lucaSettings = lucaSettings;
        _catalogVisibility = catalogVisibility;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<SettingsDto> GetSettings()
    {
        try
        {
            if (_cachedSettings != null)
            {
                return Ok(_cachedSettings);
            }

            var sync = _syncSettings.Value;
            var settings = new SettingsDto
            {
                KatanaApiKey = _katanaSettings.Value.ApiKey ?? "",
                LucaApiKey = _lucaSettings.Value.ApiKey ?? "",
                AutoSync = sync.EnableAutoSync,
                SyncInterval = sync.Stock.SyncIntervalMinutes,
                HideZeroStockProducts = _catalogVisibility.Value.HideZeroStockProducts
            };

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings");
            return StatusCode(500, new { message = "Ayarlar alınırken hata oluştu" });
        }
    }

    [HttpPost]
    public ActionResult UpdateSettings([FromBody] SettingsDto settings)
    {
        try
        {
            _cachedSettings = settings;
            _logger.LogInformation("Settings updated: AutoSync={AutoSync}, SyncInterval={SyncInterval}", 
                settings.AutoSync, settings.SyncInterval);

            return Ok(new { message = "Ayarlar başarıyla kaydedildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { message = "Ayarlar kaydedilirken hata oluştu" });
        }
    }
}

public class SettingsDto
{
    public string KatanaApiKey { get; set; } = "";
    public string LucaApiKey { get; set; } = "";
    public bool AutoSync { get; set; }
    public int SyncInterval { get; set; }
    public bool HideZeroStockProducts { get; set; } = true;
}
