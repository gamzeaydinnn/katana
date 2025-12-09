using System.Text.Json.Serialization;

namespace Katana.Core.DTOs.Koza;

/// <summary>
/// Koza Stok Depo Kartı
/// Endpoint: ListeleStkDepo.do, EkleStkWsDepo.do
/// </summary>
public sealed class KozaDepoDto
{
    [JsonPropertyName("depoId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]  // ✅ null ise gönderme
    public long? DepoId { get; set; }
    
    [JsonPropertyName("kod")] 
    public string Kod { get; set; } = string.Empty;
    
    [JsonPropertyName("tanim")] 
    public string Tanim { get; set; } = string.Empty;
    
    [JsonPropertyName("kategoriKod")] 
    public string KategoriKod { get; set; } = string.Empty;
    
    /// <summary>Luca depo kategori ağacı ID'si</summary>
    [JsonPropertyName("depoKategoriAgacId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]  // ✅ null ise gönderme
    public long? DepoKategoriAgacId { get; set; }
    
    /// <summary>Luca depo kategori ağacı kodu</summary>
    [JsonPropertyName("sisDepoKategoriAgacKodu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]  // ✅ null ise gönderme
    public string? SisDepoKategoriAgacKodu { get; set; }
    
    [JsonPropertyName("ulke")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ulke { get; set; }
    
    [JsonPropertyName("il")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Il { get; set; }
    
    [JsonPropertyName("ilce")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ilce { get; set; }
    
    [JsonPropertyName("adresSerbest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
/// FIX: Frontend wrapper gönderiyor {"stkDepo":{...}}, biz karşılıyoruz
/// Ama Luca'ya sadece içini (StkDepo) göndereceğiz - düz format
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
