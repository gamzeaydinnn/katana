using System.Text.Json.Serialization;

namespace Katana.Business.DTOs.Koza;

/// <summary>
/// Koza Stok Depo Kartı
/// Endpoint: ListeleStkDepo.do, EkleStkWsDepo.do
/// </summary>
public sealed class KozaDepoDto
{
    [JsonPropertyName("depoId")] 
    public long? DepoId { get; set; }
    
    [JsonPropertyName("kod")] 
    public string Kod { get; set; } = string.Empty;
    
    [JsonPropertyName("tanim")] 
    public string Tanim { get; set; } = string.Empty;
    
    [JsonPropertyName("kategoriKod")] 
    public string KategoriKod { get; set; } = string.Empty;
    
    /// <summary>Luca depo kategori ağacı ID'si</summary>
    [JsonPropertyName("depoKategoriAgacId")] 
    public long? DepoKategoriAgacId { get; set; }
    
    /// <summary>Luca depo kategori ağacı kodu</summary>
    [JsonPropertyName("sisDepoKategoriAgacKodu")] 
    public string? SisDepoKategoriAgacKodu { get; set; }
    
    [JsonPropertyName("ulke")] 
    public string? Ulke { get; set; }
    
    [JsonPropertyName("il")] 
    public string? Il { get; set; }
    
    [JsonPropertyName("ilce")] 
    public string? Ilce { get; set; }
    
    [JsonPropertyName("adresSerbest")] 
    public string? AdresSerbest { get; set; }
}

/// <summary>
/// Depo Listeleme Response
/// Koza response alanı değişken olabiliyor (depolar veya stkDepoListesi)
/// </summary>
public sealed class KozaDepoListResponse
{
    [JsonPropertyName("depolar")] 
    public List<KozaDepoDto>? Depolar { get; set; }
    
    [JsonPropertyName("stkDepoListesi")] 
    public List<KozaDepoDto>? StkDepoListesi { get; set; }
    
    [JsonPropertyName("error")] 
    public bool? Error { get; set; }
    
    [JsonPropertyName("message")] 
    public string? Message { get; set; }
}

/// <summary>
/// Depo Ekleme Request
/// Koza'nın beklediği format: { stkDepo: { kod, tanim, kategoriKod, ... } }
/// </summary>
public sealed class KozaCreateDepotRequest
{
    [JsonPropertyName("stkDepo")] 
    public KozaDepoDto StkDepo { get; set; } = new();
}

/// <summary>
/// Genel Koza işlem sonucu
/// </summary>
public sealed class KozaResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
