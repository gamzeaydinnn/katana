using System.Text;
using System.Text.Json;
using Katana.Business.DTOs.Koza;
using Katana.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// Koza Stok Kartı işlemleri
/// Endpoint'ler: ListeleStkKart.do, EkleStkWsKart.do
/// </summary>
public partial class LucaService
{
    /// <summary>
    /// Koza stok kartlarını listele (basitleştirilmiş)
    /// Frontend için sadece gerekli alanları döndürür
    /// </summary>
    public async Task<IReadOnlyList<KozaStokKartiDto>> ListStockCardsSimpleAsync(CancellationToken ct = default)
    {
        try {
            await EnsureAuthenticatedAsync();

            var req = new HttpRequestMessage(HttpMethod.Post, "ListeleStkKart.do")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("No-Paging", "true");

            ApplySessionCookie(req);
            ApplyManualSessionCookie(req);

            var client = _cookieHttpClient ?? _httpClient;
            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            // NO_JSON hatası kontrolü
            if (body.TrimStart().StartsWith("<"))
            {
                _logger.LogError("Koza NO_JSON (HTML döndü) - ListeleStkKart.do. Body: {Body}", 
                    body.Length > 500 ? body.Substring(0, 500) : body);
                throw new InvalidOperationException("Koza NO_JSON hatası");
            }

            // Response parse
            var dto = JsonSerializer.Deserialize<KozaStokKartiListResponse>(body, _jsonOptions);
            
            if (dto?.Error == true)
            {
                _logger.LogError("Koza stok kartı listeleme hatası: {Message}", dto.Message);
                throw new InvalidOperationException(dto.Message ?? "Koza stok kartı listeleme hatası");
            }

            // Koza response alanı değişken: stokKartlari veya stkKartListesi
            var stoklar = dto?.StokKartlari ?? dto?.StkKartListesi ?? new List<KozaStokKartiDto>();
            
            _logger.LogInformation("Koza'dan {Count} stok kartı listelendi", stoklar.Count);
            return stoklar;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListStockCardsSimpleAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Koza'da yeni stok kartı oluştur (basitleştirilmiş)
    /// Frontend'den gelen basit DTO'yu LucaCreateStokKartiRequest'e çevirir
    /// </summary>
    public async Task<KozaResult> CreateStockCardSimpleAsync(KozaCreateStokKartiRequest req, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // Basit DTO'yu tam DTO'ya çevir
            var fullRequest = MapToFullStokKartiRequest(req.StkKart);

            _logger.LogDebug("CreateStockCardSimpleAsync request: {Kod}", req.StkKart.KartKodu);

            // Mevcut CreateStockCardAsync metodunu kullan
            var response = await CreateStockCardAsync(fullRequest);

            // JsonElement'ten sonucu parse et
            try
            {
                if (response.ValueKind == JsonValueKind.Object)
                {
                    var errorProp = response.TryGetProperty("error", out var err) ? err : default;
                    var messageProp = response.TryGetProperty("message", out var msg) ? msg : default;

                    if (errorProp.ValueKind == JsonValueKind.True)
                    {
                        return new KozaResult
                        {
                            Success = false,
                            Message = messageProp.ValueKind == JsonValueKind.String 
                                ? messageProp.GetString() 
                                : "Koza stok kartı oluşturma hatası"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Response parse failed, assuming success");
            }

            _logger.LogInformation("Stok kartı başarıyla oluşturuldu: {Kod} - {Ad}", 
                req.StkKart.KartKodu, req.StkKart.KartAdi);
            
            return new KozaResult { Success = true, Message = "OK" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateStockCardSimpleAsync failed for: {Kod}", req.StkKart?.KartKodu ?? "unknown");
            return new KozaResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Basit DTO'yu tam LucaCreateStokKartiRequest'e çevir
    /// </summary>
    private LucaCreateStokKartiRequest MapToFullStokKartiRequest(KozaStokKartiDto simple)
    {
        return new LucaCreateStokKartiRequest
        {
            KartKodu = simple.KartKodu,
            KartAdi = simple.KartAdi,
            KartTuru = simple.KartTuru,
            KartTipi = simple.KartTipi,
            OlcumBirimiId = simple.OlcumBirimiId,
            KategoriAgacKod = simple.KategoriAgacKod,
            KartAlisKdvOran = simple.KartAlisKdvOran,
            KartSatisKdvOran = simple.KartSatisKdvOran,
            UzunAdi = simple.UzunAdi ?? simple.KartAdi,
            Barkod = simple.Barkod ?? string.Empty,
            BaslangicTarihi = simple.BaslangicTarihi ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            MinStokKontrol = simple.MinStokKontrol,
            MinStokMiktari = simple.MinStokMiktari,
            MaxStokKontrol = simple.MaxStokKontrol,
            MaxStokMiktari = simple.MaxStokMiktari,
            SatilabilirFlag = simple.SatilabilirFlag,
            SatinAlinabilirFlag = simple.SatinAlinabilirFlag,
            MaliyetHesaplanacakFlag = simple.MaliyetHesaplanacakFlag,
            
            // Varsayılan değerler
            KartToptanAlisKdvOran = simple.KartAlisKdvOran,
            KartToptanSatisKdvOran = simple.KartSatisKdvOran,
            AlisIskontoOran1 = 0,
            SatisIskontoOran1 = 0,
            RafOmru = 0,
            GarantiSuresi = 0,
            GtipKodu = string.Empty,
            AlisTevkifatOran = string.Empty,
            AlisTevkifatKod = 0,
            SatisTevkifatOran = string.Empty,
            SatisTevkifatKod = 0,
            IhracatKategoriNo = string.Empty,
            UtsVeriAktarimiFlag = 0,
            BagDerecesi = 0
        };
    }
}
