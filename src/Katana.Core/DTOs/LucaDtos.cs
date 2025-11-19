using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class LucaInvoiceDto
{
    public string DocumentNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerTitle { get; set; } = string.Empty;
    public string CustomerTaxNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string DocumentType { get; set; } = "SALES_INVOICE";
    public List<LucaInvoiceItemDto> Lines { get; set; } = new();
}

public class LucaInvoiceItemDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal UnitPrice { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
}

public class LucaStockDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string EntryWarehouseCode { get; set; } = string.Empty;
    public string ExitWarehouseCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty; // IN, OUT
    public DateTime MovementDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
}

public class LucaCustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

// --- Koza / Luca stok kartı listeleme ve yardımcı DTO'lar ---

public class LucaStockCardCodeFilter
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }

    /// <summary>
    /// Operatör: between, ge, le vb.
    /// </summary>
    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }
}

public class LucaStockCardKey
{
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }
}

/// <summary>
/// 3.2.5 Stok Kartları Listesi isteği gövdesi.
/// </summary>
public class LucaListStockCardsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardCodeFilter? StkSkart { get; set; }
}

/// <summary>
/// 3.2.9 Stok Kartı Alış/Satış Fiyat Listesi isteği.
/// </summary>
public class LucaListStockCardPriceListsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();

    /// <summary>
    /// "alis" veya "satis"
    /// </summary>
    [JsonPropertyName("tip")]
    public string Tip { get; set; } = string.Empty;
}

/// <summary>
/// skartId ile çalışan basit istek gövdesi (alternatif OB, alternatif stoklar, maliyet vb.).
/// </summary>
public class LucaStockCardByIdRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

/// <summary>
/// 3.2.23 Stok/Hizmet Kategori Listesi isteği.
/// </summary>
public class LucaListStockCategoriesRequest
{
    /// <summary>
    /// 1 = Stok Kartı, 2 = Hizmet Kartı
    /// </summary>
    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; }
}

// --- Koza / Luca fatura listeleme ve ekleme DTO'ları ---

public class LucaInvoiceBelgeFilter
{
    [JsonPropertyName("belgeNoBas")]
    public long? BelgeNoBas { get; set; }

    [JsonPropertyName("belgeNoBit")]
    public long? BelgeNoBit { get; set; }

    [JsonPropertyName("belgeNoOp")]
    public string? BelgeNoOp { get; set; }

    [JsonPropertyName("belgeTarihiBas")]
    public string? BelgeTarihiBas { get; set; }

    [JsonPropertyName("belgeTarihiBit")]
    public string? BelgeTarihiBit { get; set; }

    [JsonPropertyName("belgeTarihiOp")]
    public string? BelgeTarihiOp { get; set; }
}

public class LucaInvoiceOrgBelgeFilter
{
    [JsonPropertyName("gnlOrgSsBelge")]
    public LucaInvoiceBelgeFilter? GnlOrgSsBelge { get; set; }
}

/// <summary>
/// 3.2.6 Fatura Listesi (ListeleFtrSsFaturaBaslik.do) istek DTO'su.
/// </summary>
public class LucaListInvoicesRequest
{
    [JsonPropertyName("ftrSsFaturaBaslik")]
    public LucaInvoiceOrgBelgeFilter? FtrSsFaturaBaslik { get; set; }

    /// <summary>
    /// Üst hareket türü (16,17,18,19 ...)
    /// </summary>
    [JsonPropertyName("parUstHareketTuru")]
    public int? ParUstHareketTuru { get; set; }

    /// <summary>
    /// Alt hareket türü (dokümandaki ID listesine göre).
    /// </summary>
    [JsonPropertyName("parAltHareketTuru")]
    public int? ParAltHareketTuru { get; set; }
}

/// <summary>
/// 3.2.24 Fatura Ekleme – Fatura detay satırı DTO'su.
/// Bu DTO sadece temel mal/hizmet satırı alanlarını içerir; ihtiyaç halinde genişletilebilir.
/// </summary>
public class LucaCreateInvoiceDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1; // 1: Stok, 2: Hizmet

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("kartTipi")]
    public long? KartTipi { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    [JsonPropertyName("olcuBirimi")]
    public long? OlcuBirimi { get; set; }

    [JsonPropertyName("kdvOran")]
    public double KdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? KartSatisKdvOran { get; set; }

    [JsonPropertyName("depoKodu")]
    public string? DepoKodu { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double BirimFiyat { get; set; }

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("tutar")]
    public double? Tutar { get; set; }
}

/// <summary>
/// 3.2.24 Fatura Ekleme – Fatura başlık DTO'su.
/// Pek çok alan opsiyonel bırakıldı; zorunlu olanlar iş kuralına göre doldurulmalı.
/// </summary>
public class LucaCreateInvoiceHeaderRequest
{
    // Belge bilgileri
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

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    // Fatura başlık
    [JsonPropertyName("faturaTur")]
    public int FaturaTur { get; set; } = 1;

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double KurBedeli { get; set; } = 1.0;

    [JsonPropertyName("babsFlag")]
    public bool BabsFlag { get; set; }

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    [JsonPropertyName("musteriTedarikci")]
    public int MusteriTedarikci { get; set; } = 1; // 1:Müşteri, 2:Tedarikçi

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariTanim")]
    public string? CariTanim { get; set; }

    // Detay listesi
    [JsonPropertyName("detayList")]
    public List<LucaCreateInvoiceDetailRequest> DetayList { get; set; } = new();
}

/// <summary>
/// 3.2.25 Fatura Kapama isteği.
/// </summary>
public class LucaCloseInvoiceRequest
{
    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("faturaId")]
    public long FaturaId { get; set; }

    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarih")]
    public DateTime BelgeTarih { get; set; }

    [JsonPropertyName("vadeTarih")]
    public DateTime? VadeTarih { get; set; }

    [JsonPropertyName("takipNo")]
    public string? TakipNo { get; set; }

    [JsonPropertyName("Aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("cariKod")]
    public string CariKod { get; set; } = string.Empty;

    [JsonPropertyName("cariTur")]
    public int CariTur { get; set; }

    [JsonPropertyName("kurBedeli")]
    public double KurBedeli { get; set; } = 1.0;
}

/// <summary>
/// 3.2.26 Fatura Silme isteği.
/// </summary>
public class LucaDeleteInvoiceRequest
{
    [JsonPropertyName("ssFaturaBaslikId")]
    public long SsFaturaBaslikId { get; set; }
}

/// <summary>
/// Koza / Luca stok kartı oluşturma isteği (EkleStkWsSkart.do).
/// Tüm alanlar Luca tarafının beklediği JSON alan adlarıyla eşleştirilmiştir.
/// </summary>
public class LucaCreateStokKartiRequest
{
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; }

    [JsonPropertyName("baslangicTarihi")]
    public DateTime? BaslangicTarihi { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public bool MaliyetHesaplanacakFlag { get; set; }

    [JsonPropertyName("kartTipi")]
    public long KartTipi { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string KategoriAgacKod { get; set; } = string.Empty;

    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double KartSatisKdvOran { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }

    [JsonPropertyName("barkod")]
    public string Barkod { get; set; } = string.Empty;

    [JsonPropertyName("kartToptanAlisKdvOran")]
    public double KartToptanAlisKdvOran { get; set; }

    [JsonPropertyName("rafOmru")]
    public double RafOmru { get; set; }

    [JsonPropertyName("uzunAdi")]
    public string UzunAdi { get; set; } = string.Empty;

    [JsonPropertyName("kartToptanSatisKdvOran")]
    public double KartToptanSatisKdvOran { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int GarantiSuresi { get; set; }

    [JsonPropertyName("gtipKodu")]
    public string GtipKodu { get; set; } = string.Empty;

    [JsonPropertyName("alisTevkifatOran")]
    public string AlisTevkifatOran { get; set; } = string.Empty;

    [JsonPropertyName("alisTevkifatKod")]
    public int AlisTevkifatKod { get; set; }

    [JsonPropertyName("ihracatKategoriNo")]
    public string IhracatKategoriNo { get; set; } = string.Empty;

    [JsonPropertyName("satisTevkifatOran")]
    public string SatisTevkifatOran { get; set; } = string.Empty;

    [JsonPropertyName("satisTevkifatKod")]
    public int SatisTevkifatKod { get; set; }

    [JsonPropertyName("minStokKontrol")]
    public long MinStokKontrol { get; set; }

    [JsonPropertyName("minStokMiktari")]
    public double MinStokMiktari { get; set; }

    [JsonPropertyName("alisIskontoOran1")]
    public double AlisIskontoOran1 { get; set; }

    [JsonPropertyName("satilabilirFlag")]
    public bool SatilabilirFlag { get; set; }

    [JsonPropertyName("satinAlinabilirFlag")]
    public bool SatinAlinabilirFlag { get; set; }

    [JsonPropertyName("utsVeriAktarimiFlag")]
    public bool UtsVeriAktarimiFlag { get; set; }

    [JsonPropertyName("bagDerecesi")]
    public long BagDerecesi { get; set; }

    [JsonPropertyName("maxStokKontrol")]
    public long MaxStokKontrol { get; set; }

    [JsonPropertyName("maxStokMiktari")]
    public double MaxStokMiktari { get; set; }

    [JsonPropertyName("satisIskontoOran1")]
    public double SatisIskontoOran1 { get; set; }

    [JsonPropertyName("satisAlternatifFlag")]
    public bool SatisAlternatifFlag { get; set; }

    [JsonPropertyName("uretimSuresi")]
    public double UretimSuresi { get; set; }

    [JsonPropertyName("uretimSuresiBirim")]
    public long UretimSuresiBirim { get; set; }

    [JsonPropertyName("seriNoFlag")]
    public bool SeriNoFlag { get; set; }

    [JsonPropertyName("lotNoFlag")]
    public bool LotNoFlag { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string DetayAciklama { get; set; } = string.Empty;

    // Oranlar Alanı

    [JsonPropertyName("otvMaliyetFlag")]
    public bool OtvMaliyetFlag { get; set; }

    [JsonPropertyName("otvTutarKdvFlag")]
    public bool OtvTutarKdvFlag { get; set; }

    [JsonPropertyName("otvIskontoFlag")]
    public bool OtvIskontoFlag { get; set; }

    [JsonPropertyName("otvTipi")]
    public string OtvTipi { get; set; } = string.Empty;

    [JsonPropertyName("stopajOran")]
    public double StopajOran { get; set; }

    [JsonPropertyName("alisIskontoOran2")]
    public double AlisIskontoOran2 { get; set; }

    [JsonPropertyName("alisIskontoOran3")]
    public double AlisIskontoOran3 { get; set; }

    [JsonPropertyName("alisIskontoOran4")]
    public double AlisIskontoOran4 { get; set; }

    [JsonPropertyName("alisIskontoOran5")]
    public double AlisIskontoOran5 { get; set; }

    [JsonPropertyName("satisIskontoOran2")]
    public double SatisIskontoOran2 { get; set; }

    [JsonPropertyName("satisIskontoOran3")]
    public double SatisIskontoOran3 { get; set; }

    [JsonPropertyName("satisIskontoOran4")]
    public double SatisIskontoOran4 { get; set; }

    [JsonPropertyName("satisIskontoOran5")]
    public double SatisIskontoOran5 { get; set; }

    [JsonPropertyName("alisMaktuVergi")]
    public double AlisMaktuVergi { get; set; }

    [JsonPropertyName("satisMaktuVergi")]
    public double SatisMaktuVergi { get; set; }

    [JsonPropertyName("alisOtvOran")]
    public double AlisOtvOran { get; set; }

    [JsonPropertyName("alisOtvTutar")]
    public double AlisOtvTutar { get; set; }

    [JsonPropertyName("alisTecilOtv")]
    public double AlisTecilOtv { get; set; }

    [JsonPropertyName("satisOtvOran")]
    public double SatisOtvOran { get; set; }

    [JsonPropertyName("satisOtvTutar")]
    public double SatisOtvTutar { get; set; }

    [JsonPropertyName("satisTecilOtv")]
    public double SatisTecilOtv { get; set; }

    [JsonPropertyName("perakendeAlisBirimFiyat")]
    public double PerakendeAlisBirimFiyat { get; set; }

    [JsonPropertyName("perakendeSatisBirimFiyat")]
    public double PerakendeSatisBirimFiyat { get; set; }
}

public class LucaProductUpdateDto
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;
    
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;
    
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
    
    [JsonPropertyName("vatRate")]
    public int? VatRate { get; set; }

    // Koza (EkleStkWsSkart.do) specific fields
    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("kartTuru")]
    public long? KartTuru { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long? OlcumBirimiId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string? KartKodu { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }
}

/// <summary>
/// Luca stok kartı / ürün DTO (ListeleStkSkart.do response için)
/// </summary>
public class LucaProductDto
{
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }

    [JsonPropertyName("stokKartKodu")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("stokKartAdi")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("barkod")]
    public string? Barcode { get; set; }

    [JsonPropertyName("olcumBirimi")]
    public string? Unit { get; set; }

    [JsonPropertyName("kartAlisKdvOran")]
    public double? PurchaseVatRate { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? SalesVatRate { get; set; }

    // Ek alanlar gerektiğinde genişletilebilir: rafOmru, detayAciklama vb.
}

// --- Cari / Finansal Nesne yardımcı DTO'lar ---

/// <summary>
/// 3.2.13 Cari Adres Listesi isteği.
/// </summary>
public class LucaListCustomerAddressesRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

/// <summary>
/// 3.2.14 Cari Çalışma Koşulları isteği.
/// </summary>
public class LucaGetCustomerWorkingConditionsRequest
{
    [JsonPropertyName("calismaKosulId")]
    public long CalismaKosulId { get; set; }
}

/// <summary>
/// 3.2.15 Cari Yetkili Kişiler isteği.
/// </summary>
public class LucaListCustomerAuthorizedPersonsRequest
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaFinansalNesneKey GnlFinansalNesne { get; set; } = new();
}

public class LucaFinansalNesneKey
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

/// <summary>
/// 3.2.31 Cari Hareket Ekleme - detay satırı.
/// </summary>
public class LucaCreateCariHareketDetayRequest
{
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

/// <summary>
/// 3.2.31 Cari Hareket Ekleme - başlık DTO'su.
/// </summary>
public class LucaCreateCariHareketRequest
{
    // Belge bilgileri
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

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    // Cari hareket başlık
    [JsonPropertyName("cariTuru")]
    public int CariTuru { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("detayList")]
    public List<LucaCreateCariHareketDetayRequest> DetayList { get; set; } = new();
}

// --- İrsaliye DTO'ları ---

/// <summary>
/// 3.2.21 İrsaliye Listesi isteği (body boş; sadece gövde için placeholder).
/// </summary>
public class LucaListIrsaliyeRequest
{
}

/// <summary>
/// 3.2.27 İrsaliye Detay satırı.
/// </summary>
public class LucaCreateIrsaliyeDetayRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("kartTipi")]
    public long? KartTipi { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    [JsonPropertyName("olcuBirimi")]
    public long? OlcuBirimi { get; set; }

    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? KartSatisKdvOran { get; set; }

    [JsonPropertyName("depoKodu")]
    public string? DepoKodu { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double BirimFiyat { get; set; }

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("kdvOran")]
    public double KdvOran { get; set; }
}

/// <summary>
/// 3.2.27 İrsaliye Ekleme – başlık DTO'su (temel alanlar).
/// </summary>
public class LucaCreateIrsaliyeBaslikRequest
{
    // Belge bilgileri
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

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    // Başlık bilgileri
    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double KurBedeli { get; set; } = 1.0;

    [JsonPropertyName("yuklemeTarihi")]
    public DateTime? YuklemeTarihi { get; set; }

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    [JsonPropertyName("tevkifatKod")]
    public int? TevkifatKod { get; set; }

    [JsonPropertyName("musteriTedarikci")]
    public int MusteriTedarikci { get; set; } = 1;

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariTanim")]
    public string? CariTanim { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateIrsaliyeDetayRequest> DetayList { get; set; } = new();
}

/// <summary>
/// 3.2.28 İrsaliye Silme isteği.
/// </summary>
public class LucaDeleteIrsaliyeRequest
{
    [JsonPropertyName("ssIrsaliyeBaslikId")]
    public long SsIrsaliyeBaslikId { get; set; }
}

// --- Basit fatura ve cari DTO'ları (özet kullanımlar için) ---

/// <summary>
/// 3.3 kısa örneğe uygun sadeleştirilmiş fatura isteği.
/// Detaylı EkleFtrWsFaturaBaslik.do DTO'larına ek olarak, basit senaryolar için kullanılabilir.
/// </summary>
public class LucaInvoiceRequest
{
    [JsonPropertyName("belgeTurId")]
    public int BelgeTurId { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public int BelgeTurDetayId { get; set; }

    [JsonPropertyName("cariKod")]
    public string CariKod { get; set; } = string.Empty;

    [JsonPropertyName("faturaTarihi")]
    public DateTime FaturaTarihi { get; set; }

    [JsonPropertyName("lines")]
    public List<LucaInvoiceLine> Lines { get; set; } = new();
}

public class LucaInvoiceLine
{
    [JsonPropertyName("stokKodu")]
    public string StokKodu { get; set; } = string.Empty;

    [JsonPropertyName("miktar")]
    public decimal Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public decimal BirimFiyat { get; set; }
}

/// <summary>
/// Cari (müşteri/tedarikçi) ekleme/güncelleme için özet DTO.
/// </summary>
public class LucaCustomerRequest
{
    [JsonPropertyName("cariKod")]
    public string CariKod { get; set; } = string.Empty;

    [JsonPropertyName("unvan")]
    public string Unvan { get; set; } = string.Empty;

    [JsonPropertyName("vergiNo")]
    public string VergiNo { get; set; } = string.Empty;

    [JsonPropertyName("adres")]
    public string Adres { get; set; } = string.Empty;

    [JsonPropertyName("telefon")]
    public string Telefon { get; set; } = string.Empty;
}

/// <summary>
/// Luca/Koza endpoint'leri için ortak response zarfı.
/// </summary>
public class LucaResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

