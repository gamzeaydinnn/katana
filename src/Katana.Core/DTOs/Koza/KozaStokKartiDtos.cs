using System.Text.Json.Serialization;

namespace Katana.Core.DTOs.Koza;

/// <summary>
/// Basitleştirilmiş Koza Stok Kartı DTO
/// Frontend ile backend arasında kullanılır
/// Tam LucaCreateStokKartiRequest çok karmaşık, sadece gerekli alanları içerir
/// </summary>
public sealed class KozaStokKartiDto
{
    // Zorunlu alanlar
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;
    
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;
    
    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; } = 1;  // 1: Ürün, 2: Hizmet
    
    [JsonPropertyName("kartTipi")]
    public long KartTipi { get; set; } = 1;  // 1: Normal
    
    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }
    
    [JsonPropertyName("kategoriAgacKod")]
    public string KategoriAgacKod { get; set; } = string.Empty;
    
    // KDV oranları
    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; } = 0.18;
    
    [JsonPropertyName("kartSatisKdvOran")]
    public double KartSatisKdvOran { get; set; } = 0.18;
    
    // Opsiyonel alanlar
    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }
    
    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }
    
    [JsonPropertyName("baslangicTarihi")]
    public string? BaslangicTarihi { get; set; }
    
    [JsonPropertyName("bitisTarihi")]
    public string? BitisTarihi { get; set; }
    
    // Stok kontrol
    [JsonPropertyName("minStokKontrol")]
    public long MinStokKontrol { get; set; } = 0;
    
    [JsonPropertyName("minStokMiktari")]
    public double MinStokMiktari { get; set; } = 0;
    
    [JsonPropertyName("maxStokKontrol")]
    public long MaxStokKontrol { get; set; } = 0;
    
    [JsonPropertyName("maxStokMiktari")]
    public double MaxStokMiktari { get; set; } = 0;
    
    // Bayraklar
    [JsonPropertyName("satilabilirFlag")]
    public int SatilabilirFlag { get; set; } = 1;
    
    [JsonPropertyName("satinAlinabilirFlag")]
    public int SatinAlinabilirFlag { get; set; } = 1;
    
    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public int MaliyetHesaplanacakFlag { get; set; } = 1;
    
    // Listele'den gelen ID
    [JsonPropertyName("stokKartId")]
    public long? StokKartId { get; set; }
}

/// <summary>
/// Stok Kartı Listeleme Response
/// </summary>
public sealed class KozaStokKartiListResponse
{
    [JsonPropertyName("stokKartlari")]
    public List<KozaStokKartiDto>? StokKartlari { get; set; }
    
    [JsonPropertyName("stkKartListesi")]
    public List<KozaStokKartiDto>? StkKartListesi { get; set; }
    
    [JsonPropertyName("error")]
    public bool? Error { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Stok Kartı Ekleme Request
/// Koza'nın beklediği format: { stkKart: { ... } }
/// </summary>
public sealed class KozaCreateStokKartiRequest
{
    [JsonPropertyName("stkKart")]
    public KozaStokKartiDto StkKart { get; set; } = new();
}
