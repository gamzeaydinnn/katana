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

    // Accept Koza variations (kod, stokKartKodu, code, skartKod, stokKodu)
    [JsonPropertyName("kod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Kod
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartKodu = value;
            }
        }
    }

    [JsonPropertyName("stokKartKodu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? StokKartKodu
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartKodu = value;
            }
        }
    }

    [JsonPropertyName("skartKod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SkartKod
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartKodu = value;
            }
        }
    }
    
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    // Accept Koza variations (adi, stokKartAdi, tanim)
    [JsonPropertyName("adi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Adi
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartAdi = value;
            }
        }
    }

    [JsonPropertyName("stokKartAdi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? StokKartAdi
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartAdi = value;
            }
        }
    }

    [JsonPropertyName("tanim")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Tanim
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KartAdi = value;
            }
        }
    }
    
    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; } = 1;  // 1: Ürün, 2: Hizmet
    
    [JsonPropertyName("kartTipi")]
    public long KartTipi { get; set; } = 1;  // 1: Normal
    
    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }
    
    [JsonPropertyName("kategoriAgacKod")]
    public string KategoriAgacKod { get; set; } = string.Empty;

    // Accept Koza variations (kategoriKodu, hiyerarsikKod, category)
    [JsonPropertyName("kategoriKodu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? KategoriKodu
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KategoriAgacKod = value;
            }
        }
    }

    [JsonPropertyName("hiyerarsikKod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? HiyerarsikKod
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KategoriAgacKod = value;
            }
        }
    }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Category
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                KategoriAgacKod = value;
            }
        }
    }
    
    // KDV oranları
    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; } = 0.18;

    [JsonPropertyName("alisKdvOran")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? AlisKdvOran
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                KartAlisKdvOran = value.Value;
            }
        }
    }
    
    [JsonPropertyName("kartSatisKdvOran")]
    public double KartSatisKdvOran { get; set; } = 0.18;

    [JsonPropertyName("satisKdvOran")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? SatisKdvOran
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                KartSatisKdvOran = value.Value;
            }
        }
    }
    
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

    [JsonPropertyName("skartId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? SkartId
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                StokKartId = value.Value;
            }
        }
    }

    [JsonPropertyName("skartID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? SkartIdUpper
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                StokKartId = value.Value;
            }
        }
    }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? GenericId
    {
        get => null;
        set
        {
            if (value.HasValue)
            {
                StokKartId = value.Value;
            }
        }
    }
}

/// <summary>
/// Stok Kartı Listeleme Response
/// </summary>
public sealed class KozaStokKartiListResponse
{
    // Koza'nın yeni formatı: { "list": [ ... ] }
    [JsonPropertyName("list")]
    public List<KozaStokKartiDto>? List { get; set; }
    
    [JsonPropertyName("stokKartlari")]
    public List<KozaStokKartiDto>? StokKartlari { get; set; }
    
    [JsonPropertyName("stkKartListesi")]
    public List<KozaStokKartiDto>? StkKartListesi { get; set; }

    // Common Koza variations captured for resilience
    [JsonPropertyName("stkSkart")]
    public List<KozaStokKartiDto>? StkSkart { get; set; }

    [JsonPropertyName("data")]
    public List<KozaStokKartiDto>? Data { get; set; }
    
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
