using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Frontend'den gelen ürün güncelleme isteği - Luca'da güncellenebilen alanları içerir
/// </summary>
public class UpdateProductRequest
{
    /// <summary>Ürün adı (kartAdi)</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>Uzun adı (uzunAdi)</summary>
    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }
    
    /// <summary>Barkod</summary>
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }
    
    /// <summary>Kategori ID (DB'den KategoriAgacKod'a mapping yapılır)</summary>
    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }
    
    /// <summary>Kategori Ağaç Kodu - Luca'ya direkt gönderilir (örn: "01", "02")</summary>
    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }
    
    /// <summary>Ölçü birimi ID (DB'den OlcumBirimiId'ye mapping yapılır)</summary>
    [JsonPropertyName("unitId")]
    public int? UnitId { get; set; }
    
    /// <summary>Miktar</summary>
    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }
    
    /// <summary>Alış fiyatı (perakendeAlisBirimFiyat)</summary>
    [JsonPropertyName("purchasePrice")]
    public decimal? PurchasePrice { get; set; }
    
    /// <summary>Satış fiyatı (perakendeSatisBirimFiyat)</summary>
    [JsonPropertyName("salesPrice")]
    public decimal? SalesPrice { get; set; }
    
    /// <summary>KDV oranı</summary>
    [JsonPropertyName("kdvRate")]
    public decimal? KdvRate { get; set; }
    
    /// <summary>GTIP kodu</summary>
    [JsonPropertyName("gtipCode")]
    public string? GtipCode { get; set; }
}
