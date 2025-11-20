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

// --- Ortak listeleme filtreleri ---

public class LucaCodeRangeFilter
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }

    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }
}

// --- 3.2.1 Vergi Dairesi Listesi ---

public class LucaTaxOfficeFilter
{
    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }

    [JsonPropertyName("kod")]
    public string? Kod { get; set; }
}

public class LucaListTaxOfficesRequest
{
    [JsonPropertyName("gnlVergiDairesi")]
    public LucaTaxOfficeFilter? GnlVergiDairesi { get; set; }
}

// --- 3.2.2 Ölçü Birimi Listesi ---

public class LucaMeasurementUnitFilter
{
    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
}

public class LucaListMeasurementUnitsRequest
{
    [JsonPropertyName("gnlOlcumBirimi")]
    public LucaMeasurementUnitFilter? GnlOlcumBirimi { get; set; }
}

// --- 3.2.3 / 3.2.4 Cari ve Tedarikçi Listesi ---

public class LucaFinancialObjectFilter
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaCodeRangeFilter? GnlFinansalNesne { get; set; }
}

public class LucaListCustomersRequest
{
    [JsonPropertyName("finMusteri")]
    public LucaFinancialObjectFilter? FinMusteri { get; set; }
}

public class LucaListSuppliersRequest
{
    [JsonPropertyName("finTedarikci")]
    public LucaFinancialObjectFilter? FinTedarikci { get; set; }
}

// --- 3.2.16 Depo Listesi ---

public class LucaWarehouseFilter
{
    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }

    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }
}

public class LucaListWarehousesRequest
{
    [JsonPropertyName("stkDepo")]
    public LucaWarehouseFilter? StkDepo { get; set; }
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

// --- 3.2.29 / 3.2.30 Cari ve Tedarikçi Kart Ekleme ---

public class LucaCreateCustomerRequest
{
    [JsonPropertyName("tip")]
    public int Tip { get; set; } = 1;

    [JsonPropertyName("cariTipId")]
    public long CariTipId { get; set; } = 1;

    [JsonPropertyName("takipNoFlag")]
    public bool? TakipNoFlag { get; set; }

    [JsonPropertyName("efaturaTuru")]
    public int? EfaturaTuru { get; set; }

    [JsonPropertyName("kategoriKod")]
    public string? KategoriKod { get; set; }

    [JsonPropertyName("kartKod")]
    public string? KartKod { get; set; }

    [JsonPropertyName("tanim")]
    public string Tanim { get; set; } = string.Empty;

    [JsonPropertyName("mutabakatMektubuGonderilecek")]
    public bool? MutabakatMektubuGonderilecek { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("kisaAd")]
    public string? KisaAd { get; set; }

    [JsonPropertyName("yasalUnvan")]
    public string? YasalUnvan { get; set; }

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    [JsonPropertyName("ad")]
    public string? Ad { get; set; }

    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }

    [JsonPropertyName("dogumTarihi")]
    public string? DogumTarihi { get; set; }

    [JsonPropertyName("mustahsil")]
    public bool? Mustahsil { get; set; }

    [JsonPropertyName("tcUyruklu")]
    public bool? TcUyruklu { get; set; }

    [JsonPropertyName("vergiDairesiId")]
    public long? VergiDairesiId { get; set; }

    [JsonPropertyName("adresTipId")]
    public long? AdresTipId { get; set; }

    [JsonPropertyName("ulke")]
    public string? Ulke { get; set; }

    [JsonPropertyName("il")]
    public string? Il { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }

    [JsonPropertyName("iletisimTipId")]
    public long? IletisimTipId { get; set; }

    [JsonPropertyName("iletisimTanim")]
    public string? IletisimTanim { get; set; }
}

public class LucaCreateSupplierRequest : LucaCreateCustomerRequest
{
    public LucaCreateSupplierRequest()
    {
        // 2 genellikle tedarikçi kart tipini temsil eder
        CariTipId = 2;
    }
}

// --- 3.2.40 Diğer Stok Hareketi (Dsh) ---

public class LucaCreateDshDetayRequest
{
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("depoKodu")]
    public string? DepoKodu { get; set; }

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double? BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double? KdvOran { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaCreateDshBaslikRequest
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("depoKodu")]
    public string? DepoKodu { get; set; }

    /// <summary>
    /// Giriş/çıkış yönü: "GIRIS" veya "CIKIS" (dokümandaki alt belge türüne göre de belirlenebilir)
    /// </summary>
    [JsonPropertyName("hareketYonu")]
    public string? HareketYonu { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateDshDetayRequest> DetayList { get; set; } = new();
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

/// <summary>
/// Basit İrsaliye (Delivery Note) DTO - Luca'dan gelen listeyi parse edip kullanmak için özet alanlar.
/// </summary>
public class LucaDespatchItemDto
{
    [JsonPropertyName("kartKodu")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string? ProductName { get; set; }

    [JsonPropertyName("miktar")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("birimFiyat")]
    public decimal? UnitPrice { get; set; }

    [JsonPropertyName("kdvOran")]
    public double? TaxRate { get; set; }
}

public class LucaDespatchDto
{
    [JsonPropertyName("belgeNo")]
    public string DocumentNo { get; set; } = string.Empty;

    [JsonPropertyName("belgeTarihi")]
    public DateTime DocumentDate { get; set; }

    [JsonPropertyName("cariKodu")]
    public string? CustomerCode { get; set; }

    [JsonPropertyName("cariTanim")]
    public string? CustomerTitle { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaDespatchItemDto> Lines { get; set; } = new();
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

