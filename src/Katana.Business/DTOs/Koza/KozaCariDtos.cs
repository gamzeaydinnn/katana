using System.Text.Json.Serialization;

namespace Katana.Business.DTOs.Koza;

#region Cari Adres İşlemleri

/// <summary>
/// Cari Adres Listesi Request
/// Endpoint: ListeleWSGnlSsAdres.do
/// </summary>
public sealed class KozaCariAdresListRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

/// <summary>
/// Cari Adres DTO
/// </summary>
public sealed class KozaCariAdresDto
{
    [JsonPropertyName("adresId")]
    public long? AdresId { get; set; }
    
    [JsonPropertyName("adresTipId")]
    public long? AdresTipId { get; set; }
    
    [JsonPropertyName("adresTipTanim")]
    public string? AdresTipTanim { get; set; }
    
    [JsonPropertyName("ulke")]
    public string? Ulke { get; set; }
    
    [JsonPropertyName("il")]
    public string? Il { get; set; }
    
    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }
    
    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }
    
    [JsonPropertyName("postaKodu")]
    public string? PostaKodu { get; set; }
}

#endregion

#region Cari Yetkili Kişiler

/// <summary>
/// Cari Yetkili Kişiler Listesi Request
/// Endpoint: ListeleFinFinansalNesneYetkili.do
/// Body: { "gnlFinansalNesne": { "finansalNesneId": 137212 } }
/// </summary>
public sealed class KozaCariYetkiliListRequest
{
    [JsonPropertyName("gnlFinansalNesne")]
    public KozaFinansalNesneFilter GnlFinansalNesne { get; set; } = new();
}

public sealed class KozaFinansalNesneFilter
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

/// <summary>
/// Cari Yetkili DTO
/// </summary>
public sealed class KozaCariYetkiliDto
{
    [JsonPropertyName("yetkiliId")]
    public long? YetkiliId { get; set; }
    
    [JsonPropertyName("ad")]
    public string? Ad { get; set; }
    
    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }
    
    [JsonPropertyName("unvan")]
    public string? Unvan { get; set; }
    
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

#endregion

#region Cari Çalışma Koşulları

/// <summary>
/// Cari Çalışma Koşulları Request
/// Endpoint: GetirFinCalismaKosul.do
/// Body: { "calismaKosulId": 998 }
/// </summary>
public sealed class KozaCalismaKosulRequest
{
    [JsonPropertyName("calismaKosulId")]
    public long CalismaKosulId { get; set; }
}

/// <summary>
/// Çalışma Koşulu DTO
/// </summary>
public sealed class KozaCalismaKosulDto
{
    [JsonPropertyName("calismaKosulId")]
    public long? CalismaKosulId { get; set; }
    
    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
    
    [JsonPropertyName("vadeSure")]
    public int? VadeSure { get; set; }
    
    [JsonPropertyName("iskontoOran")]
    public double? IskontoOran { get; set; }
}

#endregion

#region Cari Hareket İşlemleri

/// <summary>
/// Cari Hareket Ekleme Request
/// Endpoint: EkleFinCariHareketBaslikWS.do
/// </summary>
public sealed class KozaCariHareketRequest
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }
    
    [JsonPropertyName("belgeNo")]
    public int? BelgeNo { get; set; }
    
    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }
    
    [JsonPropertyName("duzenlemeSaati")]
    public string? DuzenlemeSaati { get; set; }
    
    [JsonPropertyName("vadeTarihi")]
    public DateTime? VadeTarihi { get; set; }
    
    [JsonPropertyName("belgeTakipNo")]
    public string? BelgeTakipNo { get; set; }
    
    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }
    
    /// <summary>
    /// Belge Tür Detay ID
    /// Örnek değerler için Luca dökümantasyonuna bakın
    /// </summary>
    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }
    
    /// <summary>
    /// Cari Türü: 1 = Müşteri, 2 = Tedarikçi
    /// </summary>
    [JsonPropertyName("cariTuru")]
    public int CariTuru { get; set; }
    
    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";
    
    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;
    
    [JsonPropertyName("detayList")]
    public List<KozaCariHareketDetay> DetayList { get; set; } = new();
}

/// <summary>
/// Cari Hareket Detay Satırı
/// </summary>
public sealed class KozaCariHareketDetay
{
    /// <summary>
    /// Kart Türü: 1 = Kasa, 2 = Banka, 3 = Çek/Senet, vb.
    /// </summary>
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }
    
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;
    
    [JsonPropertyName("avansFlag")]
    public bool AvansFlag { get; set; }
    
    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }
    
    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

#endregion

#region Tedarikçi Sync

/// <summary>
/// Tedarikçi Sync Sonucu
/// </summary>
public sealed class SupplierSyncResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<SupplierSyncItem> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class SupplierSyncItem
{
    public string KatanaSupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? KozaCariKodu { get; set; }
    public long? KozaFinansalNesneId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Katana Supplier'dan Koza Cari'ye dönüşüm için DTO
/// </summary>
public sealed class KatanaSupplierToCariDto
{
    public string KatanaSupplierId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxNo { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Katana Location'dan Koza Depo'ya dönüşüm için DTO
/// </summary>
public sealed class KatanaLocationToDepoDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

/// <summary>
/// Katana Customer'dan Luca Cari'ye dönüşüm için DTO
/// </summary>
public sealed class KatanaCustomerToCariDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

#endregion

#region Koza Cari (Müşteri/Tedarikçi) Response

/// <summary>
/// Koza Cari Listesi Response
/// Endpoint: ListeleFinTedarikci.do, ListeleFinMusteri.do
/// </summary>
public sealed class KozaCariListResponse
{
    [JsonPropertyName("finTedarikciListesi")]
    public List<KozaCariDto>? FinTedarikciListesi { get; set; }
    
    [JsonPropertyName("finMusteriListesi")]
    public List<KozaCariDto>? FinMusteriListesi { get; set; }
    
    [JsonPropertyName("error")]
    public bool? Error { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Koza Cari (Müşteri/Tedarikçi) Kartı DTO
/// </summary>
public sealed class KozaCariDto
{
    [JsonPropertyName("finansalNesneId")]
    public long? FinansalNesneId { get; set; }
    
    [JsonPropertyName("kod")]
    public string? Kod { get; set; }
    
    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
    
    [JsonPropertyName("kisaAd")]
    public string? KisaAd { get; set; }
    
    [JsonPropertyName("yasalUnvan")]
    public string? YasalUnvan { get; set; }
    
    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }
    
    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
    
    [JsonPropertyName("cariTipId")]
    public long? CariTipId { get; set; }
    
    [JsonPropertyName("paraBirimKod")]
    public string? ParaBirimKod { get; set; }
}

#endregion
