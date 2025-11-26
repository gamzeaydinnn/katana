using System.Text.Json;
using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

/// <summary>
/// Belge ana bilgileri (fatura/irsaliye/sipariş başlıkları için).
/// </summary>
public class LucaBelgeDto
{
    [JsonPropertyName("belgeSeri")]
    public string BelgeSeri { get; set; } = string.Empty;

    [JsonPropertyName("belgeNo")]
    public int? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; } = DateTime.Now;

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

    // Custom Attributes
    [JsonPropertyName("belgeAttribute1Deger")]
    public string? BelgeAttribute1Deger { get; set; }
    [JsonPropertyName("belgeAttribute1Ack")]
    public string? BelgeAttribute1Ack { get; set; }
    [JsonPropertyName("belgeAttribute2Deger")]
    public string? BelgeAttribute2Deger { get; set; }
    [JsonPropertyName("belgeAttribute2Ack")]
    public string? BelgeAttribute2Ack { get; set; }
    [JsonPropertyName("belgeAttribute3Deger")]
    public string? BelgeAttribute3Deger { get; set; }
    [JsonPropertyName("belgeAttribute3Ack")]
    public string? BelgeAttribute3Ack { get; set; }
    [JsonPropertyName("belgeAttribute4Deger")]
    public string? BelgeAttribute4Deger { get; set; }
    [JsonPropertyName("belgeAttribute4Ack")]
    public string? BelgeAttribute4Ack { get; set; }
    [JsonPropertyName("belgeAttribute5Deger")]
    public string? BelgeAttribute5Deger { get; set; }
    [JsonPropertyName("belgeAttribute5Ack")]
    public string? BelgeAttribute5Ack { get; set; }
}

public class LucaInvoiceDto
{
    // =============== BELGE BİLGİLERİ ===============
    [JsonPropertyName("gnlOrgSsBelge")]
    public LucaBelgeDto GnlOrgSsBelge { get; set; } = new();

    // =============== FATURA BAŞLIK ===============
    [JsonPropertyName("faturaTur")]
    public int? FaturaTur { get; set; } // 1:Mal/Hizmet, 2:Gider, 3:Gelir

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double? KurBedeli { get; set; }

    [JsonPropertyName("yuklemeTarihi")]
    public DateTime? YuklemeTarihi { get; set; }

    [JsonPropertyName("babsFlag")]
    public bool BabsFlag { get; set; }

    [JsonPropertyName("kdvFlag")]
    public bool? KdvFlag { get; set; } // false: KDV Hariç, true: KDV Dahil

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    [JsonPropertyName("tevkifatOran")]
    public string? TevkifatOran { get; set; } // "1/2", "1/3", "/10"

    [JsonPropertyName("tevkifatKod")]
    public int? TevkifatKod { get; set; }

    // =============== CARİ BİLGİLERİ ===============
    [JsonPropertyName("musteriTedarikci")]
    public int? MusteriTedarikci { get; set; } // 1:Müşteri, 2:Tedarikçi

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    // Yeni cari açılıyorsa opsiyonel alanlar
    [JsonPropertyName("cariTanim")]
    public string? CariTanim { get; set; }

    [JsonPropertyName("cariTip")]
    public int? CariTip { get; set; }

    [JsonPropertyName("cariKisaAd")]
    public string? CariKisaAd { get; set; }

    [JsonPropertyName("cariYasalUnvan")]
    public string? CariYasalUnvan { get; set; }

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("vergiDairesi")]
    public string? VergiDairesi { get; set; }

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    [JsonPropertyName("cariAd")]
    public string? CariAd { get; set; }

    [JsonPropertyName("cariSoyad")]
    public string? CariSoyad { get; set; }

    // =============== ADRES BİLGİLERİ ===============
    [JsonPropertyName("il")]
    public string? Il { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("mahallesemt")]
    public string? Mahallesemt { get; set; }

    [JsonPropertyName("caddesokak")]
    public string? Caddesokak { get; set; }

    [JsonPropertyName("diskapino")]
    public string? Diskapino { get; set; }

    [JsonPropertyName("ickapino")]
    public string? Ickapino { get; set; }

    [JsonPropertyName("postaKodu")]
    public string? PostaKodu { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    // =============== E-ARŞİV / E-FATURA ===============
    [JsonPropertyName("webAdresi")]
    public string? WebAdresi { get; set; }

    [JsonPropertyName("kargoVknTckn")]
    public string? KargoVknTckn { get; set; }

    [JsonPropertyName("odemeTipi")]
    public string? OdemeTipi { get; set; }

    [JsonPropertyName("gonderimTipi")]
    public string? GonderimTipi { get; set; }

    [JsonPropertyName("earsivNo")]
    public string? EarsivNo { get; set; }

    [JsonPropertyName("efaturaNo")]
    public string? EfaturaNo { get; set; }

    // =============== SİPARİŞ / İRSALİYE BİLGİLERİ ===============
    [JsonPropertyName("siparisTarihi")]
    public DateTime? SiparisTarihi { get; set; }

    [JsonPropertyName("siparisNo")]
    public string? SiparisNo { get; set; }

    [JsonPropertyName("irsaliyeBilgisiList")]
    public List<LucaIrsaliyeBilgisiDto>? IrsaliyeBilgisiList { get; set; }

    // =============== CUSTOM ATTRIBUTES (5'e kadar) ===============
    [JsonPropertyName("fhAttribute1Deger")]
    public string? FhAttribute1Deger { get; set; }

    [JsonPropertyName("fhAttribute1Ack")]
    public string? FhAttribute1Ack { get; set; }

    [JsonPropertyName("fhAttribute2Deger")]
    public string? FhAttribute2Deger { get; set; }

    [JsonPropertyName("fhAttribute2Ack")]
    public string? FhAttribute2Ack { get; set; }

    [JsonPropertyName("fhAttribute3Deger")]
    public string? FhAttribute3Deger { get; set; }

    [JsonPropertyName("fhAttribute3Ack")]
    public string? FhAttribute3Ack { get; set; }

    [JsonPropertyName("fhAttribute4Deger")]
    public string? FhAttribute4Deger { get; set; }

    [JsonPropertyName("fhAttribute4Ack")]
    public string? FhAttribute4Ack { get; set; }

    [JsonPropertyName("fhAttribute5Deger")]
    public string? FhAttribute5Deger { get; set; }

    [JsonPropertyName("fhAttribute5Ack")]
    public string? FhAttribute5Ack { get; set; }

    // =============== DETAYLAR ===============
    [JsonPropertyName("detayList")]
    public List<LucaInvoiceItemDto> Lines { get; set; } = new();

    // Geriye dönük uyumluluk alanları
    [JsonIgnore]
    public string DocumentNo
    {
        get => GnlOrgSsBelge?.BelgeNo?.ToString() ?? string.Empty;
        set => GnlOrgSsBelge.BelgeNo = int.TryParse(value, out var no) ? no : null;
    }

    [JsonIgnore]
    public string CustomerCode
    {
        get => CariKodu;
        set => CariKodu = value;
    }

    [JsonIgnore]
    public string CustomerTitle
    {
        get => CariTanim ?? string.Empty;
        set => CariTanim = value;
    }

    [JsonIgnore]
    public string CustomerTaxNo
    {
        get => VergiNo ?? string.Empty;
        set => VergiNo = value;
    }

    [JsonIgnore]
    public DateTime DocumentDate
    {
        get => GnlOrgSsBelge?.BelgeTarihi ?? DateTime.Now;
        set => GnlOrgSsBelge.BelgeTarihi = value;
    }

    [JsonIgnore]
    public DateTime? DueDate
    {
        get => GnlOrgSsBelge?.VadeTarihi;
        set => GnlOrgSsBelge.VadeTarihi = value;
    }

    [JsonIgnore]
    public decimal NetAmount { get; set; }

    [JsonIgnore]
    public decimal TaxAmount { get; set; }

    [JsonIgnore]
    public decimal GrossAmount { get; set; }

    [JsonIgnore]
    public string Currency
    {
        get => ParaBirimKod;
        set => ParaBirimKod = value;
    }

    [JsonIgnore]
    public string DocumentType { get; set; } = "SALES_INVOICE";
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

    // Yeni kart açma / detay alanları
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

    [JsonPropertyName("iskontoOran1")]
    public double? IskontoOran1 { get; set; }
    [JsonPropertyName("iskontoOran2")]
    public double? IskontoOran2 { get; set; }
    [JsonPropertyName("iskontoOran3")]
    public double? IskontoOran3 { get; set; }
    [JsonPropertyName("iskontoOran4")]
    public double? IskontoOran4 { get; set; }
    [JsonPropertyName("iskontoOran5")]
    public double? IskontoOran5 { get; set; }
    [JsonPropertyName("iskontoOran6")]
    public double? IskontoOran6 { get; set; }
    [JsonPropertyName("iskontoOran7")]
    public double? IskontoOran7 { get; set; }
    [JsonPropertyName("iskontoOran8")]
    public double? IskontoOran8 { get; set; }
    [JsonPropertyName("iskontoOran9")]
    public double? IskontoOran9 { get; set; }
    [JsonPropertyName("iskontoOran10")]
    public double? IskontoOran10 { get; set; }

    [JsonPropertyName("otvOran")]
    public double? OtvOran { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("garantiBitisTarihi")]
    public DateTime? GarantiBitisTarihi { get; set; }

    [JsonPropertyName("uretimTarihi")]
    public DateTime? UretimTarihi { get; set; }

    [JsonPropertyName("konaklamaVergiOran")]
    public double? KonaklamaVergiOran { get; set; }

    // Custom attributes (satır bazında)
    [JsonPropertyName("shAttribute1Deger")]
    public string? ShAttribute1Deger { get; set; }
    [JsonPropertyName("shAttribute1Ack")]
    public string? ShAttribute1Ack { get; set; }
    [JsonPropertyName("shAttribute2Deger")]
    public string? ShAttribute2Deger { get; set; }
    [JsonPropertyName("shAttribute2Ack")]
    public string? ShAttribute2Ack { get; set; }
    [JsonPropertyName("shAttribute3Deger")]
    public string? ShAttribute3Deger { get; set; }
    [JsonPropertyName("shAttribute3Ack")]
    public string? ShAttribute3Ack { get; set; }
    [JsonPropertyName("shAttribute4Deger")]
    public string? ShAttribute4Deger { get; set; }
    [JsonPropertyName("shAttribute4Ack")]
    public string? ShAttribute4Ack { get; set; }
    [JsonPropertyName("shAttribute5Deger")]
    public string? ShAttribute5Deger { get; set; }
    [JsonPropertyName("shAttribute5Ack")]
    public string? ShAttribute5Ack { get; set; }
}

/// <summary>
/// ListeleStkSkart.do response için detaylı stok kartı DTO'su.
/// Hareket alanları geriye dönük uyumluluk için JsonIgnore ile tutulur.
/// </summary>
public class LucaStockDto
{
    // Temel bilgiler
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; }

    [JsonPropertyName("kartTipi")]
    public long? KartTipi { get; set; }

    // Ölçüm ve barkod
    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("olcumBirimiTanim")]
    public string? Unit { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    // KDV oranları
    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? KartSatisKdvOran { get; set; }

    [JsonPropertyName("kartToptanAlisKdvOran")]
    public double? KartToptanAlisKdvOran { get; set; }

    [JsonPropertyName("kartToptanSatisKdvOran")]
    public double? KartToptanSatisKdvOran { get; set; }

    // Kategori ve tarihler
    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("baslangicTarihi")]
    public string? BaslangicTarihi { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }

    // Stok kontrol
    [JsonPropertyName("minStokKontrol")]
    public long? MinStokKontrol { get; set; }

    [JsonPropertyName("minStokMiktari")]
    public double? MinStokMiktari { get; set; }

    [JsonPropertyName("maxStokKontrol")]
    public long? MaxStokKontrol { get; set; }

    [JsonPropertyName("maxStokMiktari")]
    public double? MaxStokMiktari { get; set; }

    // Fiyatlar
    [JsonPropertyName("perakendeAlisBirimFiyat")]
    public double? PerakendeAlisBirimFiyat { get; set; }

    [JsonPropertyName("perakendeSatisBirimFiyat")]
    public double? PerakendeSatisBirimFiyat { get; set; }

    // Flagler
    [JsonPropertyName("satilabilirFlag")]
    public bool SatilabilirFlag { get; set; }

    [JsonPropertyName("satinAlinabilirFlag")]
    public bool SatinAlinabilirFlag { get; set; }

    [JsonPropertyName("seriNoFlag")]
    public bool SeriNoFlag { get; set; }

    [JsonPropertyName("lotNoFlag")]
    public bool LotNoFlag { get; set; }

    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public int MaliyetHesaplanacakFlag { get; set; }

    // Detay alanları
    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string? DetayAciklama { get; set; }

    [JsonPropertyName("gtipKodu")]
    public string? GtipKodu { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("rafOmru")]
    public double? RafOmru { get; set; }

    // Tevkifat
    [JsonPropertyName("alisTevkifatOran")]
    public string? AlisTevkifatOran { get; set; }

    [JsonPropertyName("alisTevkifatKod")]
    public int? AlisTevkifatKod { get; set; }

    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }

    [JsonPropertyName("satisTevkifatKod")]
    public int? SatisTevkifatKod { get; set; }

    // Iskonto
    [JsonPropertyName("alisIskontoOran1")]
    public double? AlisIskontoOran1 { get; set; }

    [JsonPropertyName("satisIskontoOran1")]
    public double? SatisIskontoOran1 { get; set; }

    // ÖTV / Stopaj
    [JsonPropertyName("otvTipi")]
    public string? OtvTipi { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    // Hareket alanları (geriye dönük)
    [JsonIgnore]
    public string WarehouseCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string EntryWarehouseCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string ExitWarehouseCode { get; set; } = string.Empty;

    [JsonIgnore]
    public int Quantity { get; set; }

    [JsonIgnore]
    public string MovementType { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime MovementDate { get; set; }

    [JsonIgnore]
    public string? Reference { get; set; }

    [JsonIgnore]
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

public class LucaDepoDto
{
    public string Kod { get; set; } = string.Empty;
    public string Tanim { get; set; } = string.Empty;
}

public class LucaWarehouseDto
{
    [JsonPropertyName("depoId")]
    public long? DepoId { get; set; }

    [JsonPropertyName("kod")]
    public string? Kod { get; set; }

    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
}

public class LucaBranchDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("ack")]
    public string? Ack { get; set; }

    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
}

public class LucaVergiDairesiDto
{
    public string Kod { get; set; } = string.Empty;
    public string Tanim { get; set; } = string.Empty;
}

public class LucaOlcumBirimiDto
{
    public long Id { get; set; }
    public string Tanim { get; set; } = string.Empty;
}

public class LucaMeasurementUnitDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("tanim")]
    public string Tanim { get; set; } = string.Empty;
}

public class LucaCariHareketDto
{
    public int CariTuru { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public double Tutar { get; set; }
    public DateTime BelgeTarihi { get; set; }
}

public class LucaFaturaKapamaDto
{
    public long FaturaId { get; set; }
    public double Tutar { get; set; }
    public string CariKod { get; set; } = string.Empty;
    public int CariTur { get; set; }
    public DateTime BelgeTarih { get; set; }
}

public class LucaIrsaliyeDto
{
    public string BelgeSeri { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public DateTime BelgeTarihi { get; set; }
    public long BelgeTurDetayId { get; set; }
    public int MusteriTedarikci { get; set; } = 1;
    public bool KdvFlag { get; set; } = true;
    public DateTime? YuklemeTarihi { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string ParaBirimKod { get; set; } = "TRY";
    public List<LucaIrsaliyeDetayDto> DetayList { get; set; } = new();
    public string? TasiyiciPlaka { get; set; }
    public string? SoforAd { get; set; }
    public string? SoforTckn { get; set; }
}

public class LucaIrsaliyeDetayDto
{
    public int KartTuru { get; set; } = 1;
    public string KartKodu { get; set; } = string.Empty;
    public double Miktar { get; set; }
    public double BirimFiyat { get; set; }
    public string? OlcuBirimi { get; set; }
    public double KdvOran { get; set; }
    public string? DepoKodu { get; set; }
}

/// <summary>
/// Fatura içinde irsaliye referans bilgisi.
/// </summary>
public class LucaIrsaliyeBilgisiDto
{
    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("seriNo")]
    public string SeriNo { get; set; } = string.Empty;

    [JsonPropertyName("yuklemeTarihi")]
    public DateTime? YuklemeTarihi { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaSatinalmaSiparisDto
{
    public string BelgeSeri { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public DateTime TeslimTarihi { get; set; }
    public string TedarikciKodu { get; set; } = string.Empty;
    public int TeklifSiparisTur { get; set; } = 1;
    public int OnayFlag { get; set; }
    public List<LucaSipariDetayDto> DetayList { get; set; } = new();
}

public class LucaSipariDetayDto
{
    public string KartKodu { get; set; } = string.Empty;
    public double Miktar { get; set; }
    public double BirimFiyat { get; set; }
}

public class LucaDepoTransferDto
{
    public string GirisDepoKodu { get; set; } = string.Empty;
    public string CikisDepoKodu { get; set; } = string.Empty;
    public DateTime BelgeTarihi { get; set; }
    public List<LucaTransferDetayDto> DetayList { get; set; } = new();
}

public class LucaTransferDetayDto
{
    public string KartKodu { get; set; } = string.Empty;
    public double Miktar { get; set; }
}

public class LucaTedarikciDto
{
    public string Kod { get; set; } = string.Empty;
    public string Tanim { get; set; } = string.Empty;
    public string? VergiNo { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }
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
/// 3.2.9 Stok Kartı Alış/Satış Fiyat Listesi response DTO'su.
/// </summary>
public class LucaStockCardPriceListDto
{
    [JsonPropertyName("fiyatListesiId")]
    public long FiyatListesiId { get; set; }

    [JsonPropertyName("fiyatListesiAdi")]
    public string FiyatListesiAdi { get; set; } = string.Empty;

    [JsonPropertyName("birimFiyat")]
    public double BirimFiyat { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("baslangicTarihi")]
    public string? BaslangicTarihi { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }
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

/// <summary>
/// 3.2.23 Stok/Hizmet Kategori Listesi response DTO'su.
/// ListeleStkSkartKategoriAgac.do sonucu dönen her bir kategori düğümünü temsil eder.
/// </summary>
public class LucaStockCategoryDto
{
    [JsonPropertyName("kategoriKod")]
    public string KategoriKod { get; set; } = string.Empty;

    [JsonPropertyName("kategoriAdi")]
    public string KategoriAdi { get; set; } = string.Empty;

    [JsonPropertyName("ustKategoriKod")]
    public string? UstKategoriKod { get; set; }

    [JsonPropertyName("seviye")]
    public int Seviye { get; set; }
}

// --- 3.2.7 Stok Kartı Temin Yerleri (Tedarikçi) ---

public class LucaStockCardSuppliersRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

// --- 3.2.8 Cari İletişim Listesi ---

public class LucaListCustomerContactsRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

// --- 3.2.12 Banka Kartları Listesi ---

public class LucaBankFilter
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaCodeRangeFilter? GnlFinansalNesne { get; set; }
}

public class LucaListBanksRequest
{
    [JsonPropertyName("finSsBanka")]
    public LucaBankFilter? FinSsBanka { get; set; }
}

// --- 3.2.17 Depo Eldeki Miktar ---

public class LucaGetWarehouseStockRequest
{
    [JsonPropertyName("cagirilanKart")]
    public string CagirilanKart { get; set; } = "depo";

    [JsonPropertyName("stkDepo")]
    public LucaWarehouseKey StkDepo { get; set; } = new();
}

public class LucaWarehouseKey
{
    [JsonPropertyName("depoId")]
    public long DepoId { get; set; }
}

// --- 3.2.20 Stok Kartı Alım Şartları ---

public class LucaListStockCardPurchaseTermsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

/// <summary>
/// 3.2.10 Stok Kartı Alternatif Ölçü Birimi response DTO'su.
/// </summary>
public class LucaStockCardAlternativeUnitDto
{
    [JsonPropertyName("alternatifObId")]
    public long AlternatifObId { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("olcumBirimiTanim")]
    public string OlcumBirimiTanim { get; set; } = string.Empty;

    [JsonPropertyName("carpan")]
    public double Carpan { get; set; }

    [JsonPropertyName("bolen")]
    public double Bolen { get; set; }
}

/// <summary>
/// 3.2.11 Stok Kartı Alternatif Stoklar response DTO'su.
/// </summary>
public class LucaStockCardAlternativeDto
{
    [JsonPropertyName("alternatifSkartId")]
    public long AlternatifSkartId { get; set; }

    [JsonPropertyName("alternatifKartKodu")]
    public string AlternatifKartKodu { get; set; } = string.Empty;

    [JsonPropertyName("alternatifKartAdi")]
    public string AlternatifKartAdi { get; set; } = string.Empty;

    [JsonPropertyName("oncelikSirasi")]
    public int? OncelikSirasi { get; set; }
}

// --- 3.2.22 Satış Sipariş Listesi ---

public class LucaListSalesOrdersRequest
{
    /// <summary>
    /// Tarih aralığı veya kod aralığı gibi opsiyonel alanlar gerekirse genişletilebilir.
    /// Şimdilik boş gövde ile çağrı yapılabiliyor.
    /// </summary>
}

/// <summary>
/// 3.2.18 Stok Kartı Maliyet Bilgisi response DTO'su.
/// </summary>
public class LucaStockCardCostDto
{
    [JsonPropertyName("maliyetTuru")]
    public string MaliyetTuru { get; set; } = string.Empty;

    [JsonPropertyName("maliyetBedeli")]
    public double MaliyetBedeli { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("sonGuncellemeTarihi")]
    public DateTime? SonGuncellemeTarihi { get; set; }
}

/// <summary>
/// 3.2.20 Stok Kartı Alım Şartları response DTO'su.
/// </summary>
public class LucaStockCardPurchaseTermDto
{
    [JsonPropertyName("tedarikciKodu")]
    public string TedarikciKodu { get; set; } = string.Empty;

    [JsonPropertyName("tedarikciAdi")]
    public string TedarikciAdi { get; set; } = string.Empty;

    [JsonPropertyName("minSiparisMiktari")]
    public double? MinSiparisMiktari { get; set; }

    [JsonPropertyName("teslimatSuresi")]
    public int? TeslimatSuresi { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double? BirimFiyat { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";
}

// ======================= STOK KARTI DETAY =======================

public class LucaGetStockCardDetailRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

public class LucaStockCardDetailDto
{
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    [JsonPropertyName("alisKdvOran")]
    public double? AlisKdvOran { get; set; }

    [JsonPropertyName("satisKdvOran")]
    public double? SatisKdvOran { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string? DetayAciklama { get; set; }

    [JsonPropertyName("rafOmru")]
    public double? RafOmru { get; set; }
}

// ======================= STOK KARTI GÜNCELLEME =======================

public class LucaUpdateStockCardRequest
{
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long? OlcumBirimiId { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string? DetayAciklama { get; set; }
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

    /// <summary>
    /// Hesap planı kodu (SKU → Luca hesap kodu eşlemesi için kullanılır).
    /// </summary>
    [JsonPropertyName("hesapKod")]
    public string? HesapKod { get; set; }

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

    [JsonPropertyName("iskontoOran1")]
    public double? IskontoOran1 { get; set; }

    [JsonPropertyName("iskontoOran2")]
    public double? IskontoOran2 { get; set; }

    [JsonPropertyName("iskontoOran3")]
    public double? IskontoOran3 { get; set; }

    [JsonPropertyName("iskontoOran4")]
    public double? IskontoOran4 { get; set; }

    [JsonPropertyName("iskontoOran5")]
    public double? IskontoOran5 { get; set; }

    [JsonPropertyName("otvOran")]
    public double? OtvOran { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("uretimTarihi")]
    public DateTime? UretimTarihi { get; set; }

    [JsonPropertyName("konaklamaVergiOran")]
    public double? KonaklamaVergiOran { get; set; }

    /// <summary>
    /// Dokümanda geçen ek alanlar için esnek taşıyıcı.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
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

    [JsonPropertyName("cariTip")]
    public int? CariTip { get; set; }

    [JsonPropertyName("cariKisaAd")]
    public string? CariKisaAd { get; set; }

    [JsonPropertyName("cariYasalUnvan")]
    public string? CariYasalUnvan { get; set; }

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("vergiDairesi")]
    public string? VergiDairesi { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }

    [JsonPropertyName("iletisimTanim")]
    public string? IletisimTanim { get; set; }

    [JsonPropertyName("kargoVknTckn")]
    public string? KargoVknTckn { get; set; }

    [JsonPropertyName("odemeTipi")]
    public int? OdemeTipi { get; set; }

    [JsonPropertyName("gonderimTipi")]
    public int? GonderimTipi { get; set; }

    [JsonPropertyName("siparisTarihi")]
    public DateTime? SiparisTarihi { get; set; }

    [JsonPropertyName("siparisNo")]
    public string? SiparisNo { get; set; }

    [JsonPropertyName("yuklemeTarihi")]
    public DateTime? YuklemeTarihi { get; set; }

    [JsonPropertyName("tevkifatOran")]
    public string? TevkifatOran { get; set; }

    [JsonPropertyName("tevkifatKod")]
    public int? TevkifatKod { get; set; }

    [JsonPropertyName("earsivNo")]
    public string? EarsivNo { get; set; }

    [JsonPropertyName("efaturaNo")]
    public string? EfaturaNo { get; set; }

    [JsonPropertyName("irsaliyeBilgisiList")]
    public List<LucaLinkedDocument> IrsaliyeBilgisiList { get; set; } = new();

    // Detay listesi
    [JsonPropertyName("detayList")]
    public List<LucaCreateInvoiceDetailRequest> DetayList { get; set; } = new();

    /// <summary>
    /// Dokümanda yer alan diğer başlık alanlarını taşımak için esnek alan (ör. belgeAttribute*, fhAttribute*).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

// ======================= FATURA DETAY =======================

public class LucaGetInvoiceDetailRequest
{
    [JsonPropertyName("faturaId")]
    public long FaturaId { get; set; }
}

public class LucaInvoiceDetailDto
{
    [JsonPropertyName("faturaId")]
    public long FaturaId { get; set; }

    [JsonPropertyName("belgeSeri")]
    public string BelgeSeri { get; set; } = string.Empty;

    [JsonPropertyName("belgeNo")]
    public long BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string CariAdi { get; set; } = string.Empty;

    [JsonPropertyName("toplamTutar")]
    public double ToplamTutar { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaInvoiceItemDto> DetayList { get; set; } = new();
}

// ======================= FATURA GÜNCELLEME =======================

public class LucaUpdateInvoiceRequest
{
    [JsonPropertyName("faturaId")]
    public long FaturaId { get; set; }

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    [JsonPropertyName("vadeTarihi")]
    public DateTime? VadeTarihi { get; set; }

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }
}

/// <summary>
/// İrsaliye veya sipariş referanslarını fatura ile ilişkilendirmek için basit belge DTO'su.
/// </summary>
public class LucaLinkedDocument
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime? BelgeTarihi { get; set; }
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
/// Luca fatura satır request'i (basit şekil).
/// </summary>
public class LucaInvoiceItemRequest
{
    public string stokKodu { get; set; } = string.Empty;
    public decimal miktar { get; set; }
    public decimal birimFiyat { get; set; }
    public decimal kdvOrani { get; set; }
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
    public string? BaslangicTarihi { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public int MaliyetHesaplanacakFlag { get; set; }

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
    public int SatilabilirFlag { get; set; }

    [JsonPropertyName("satinAlinabilirFlag")]
    public int SatinAlinabilirFlag { get; set; }

    [JsonPropertyName("utsVeriAktarimiFlag")]
    public int UtsVeriAktarimiFlag { get; set; }

    [JsonPropertyName("bagDerecesi")]
    public long BagDerecesi { get; set; }

    [JsonPropertyName("maxStokKontrol")]
    public long MaxStokKontrol { get; set; }

    [JsonPropertyName("maxStokMiktari")]
    public double MaxStokMiktari { get; set; }

    [JsonPropertyName("satisIskontoOran1")]
    public double SatisIskontoOran1 { get; set; }

    [JsonPropertyName("satisAlternatifFlag")]
    public int SatisAlternatifFlag { get; set; }

    [JsonPropertyName("uretimSuresi")]
    public double UretimSuresi { get; set; }

    [JsonPropertyName("uretimSuresiBirim")]
    public long UretimSuresiBirim { get; set; }

    [JsonPropertyName("seriNoFlag")]
    public int SeriNoFlag { get; set; }

    [JsonPropertyName("lotNoFlag")]
    public int LotNoFlag { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string DetayAciklama { get; set; } = string.Empty;

    // Oranlar Alanı

    [JsonPropertyName("otvMaliyetFlag")]
    public int OtvMaliyetFlag { get; set; }

    [JsonPropertyName("otvTutarKdvFlag")]
    public int OtvTutarKdvFlag { get; set; }

    [JsonPropertyName("otvIskontoFlag")]
    public int OtvIskontoFlag { get; set; }

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

/// <summary>
/// Koza stok kartı DTO (detaylı) – ekleme/güncelleme için.
/// </summary>
public class LucaStockCardDto
{
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; } = 1;

    [JsonPropertyName("kartKodu")]
    public string? KartKodu { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("baslangicTarihi")]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Now;

    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double KartSatisKdvOran { get; set; }

    [JsonPropertyName("kartToptanAlisKdvOran")]
    public double? KartToptanAlisKdvOran { get; set; }

    [JsonPropertyName("kartToptanSatisKdvOran")]
    public double? KartToptanSatisKdvOran { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("kartTipi")]
    public long? KartTipi { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }

    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public bool MaliyetHesaplanacakFlag { get; set; } = true;

    [JsonPropertyName("gtipKodu")]
    public string? GtipKodu { get; set; }

    [JsonPropertyName("ihracatKategoriNo")]
    public string? IhracatKategoriNo { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("rafOmru")]
    public double? RafOmru { get; set; }

    [JsonPropertyName("alisTevkifatOran")]
    public string? AlisTevkifatOran { get; set; }

    [JsonPropertyName("alisTevkifatKod")]
    public int? AlisTevkifatKod { get; set; }

    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }

    [JsonPropertyName("satisTevkifatKod")]
    public int? SatisTevkifatKod { get; set; }

    [JsonPropertyName("minStokKontrol")]
    public long? MinStokKontrol { get; set; }

    [JsonPropertyName("minStokMiktari")]
    public double? MinStokMiktari { get; set; }

    [JsonPropertyName("maxStokKontrol")]
    public long? MaxStokKontrol { get; set; }

    [JsonPropertyName("maxStokMiktari")]
    public double? MaxStokMiktari { get; set; }

    [JsonPropertyName("alisIskontoOran1")]
    public double? AlisIskontoOran1 { get; set; }

    [JsonPropertyName("satisIskontoOran1")]
    public double? SatisIskontoOran1 { get; set; }

    [JsonPropertyName("satilabilirFlag")]
    public bool SatilabilirFlag { get; set; } = true;

    [JsonPropertyName("satinAlinabilirFlag")]
    public bool SatinAlinabilirFlag { get; set; } = true;

    [JsonPropertyName("utsVeriAktarimiFlag")]
    public bool UtsVeriAktarimiFlag { get; set; }

    [JsonPropertyName("bagDerecesi")]
    public long? BagDerecesi { get; set; }

    [JsonPropertyName("satisAlternatifFlag")]
    public bool SatisAlternatifFlag { get; set; }

    [JsonPropertyName("seriNoFlag")]
    public bool SeriNoFlag { get; set; }

    [JsonPropertyName("lotNoFlag")]
    public bool LotNoFlag { get; set; }

    [JsonPropertyName("uretimSuresi")]
    public double? UretimSuresi { get; set; }

    [JsonPropertyName("uretimSuresiBirim")]
    public long? UretimSuresiBirim { get; set; }

    [JsonPropertyName("perakendeAlisBirimFiyat")]
    public double? PerakendeAlisBirimFiyat { get; set; }

    [JsonPropertyName("perakendeSatisBirimFiyat")]
    public double? PerakendeSatisBirimFiyat { get; set; }

    [JsonPropertyName("detayAciklama")]
    public string? DetayAciklama { get; set; }

    // Hizmet kartı için ek alanlar
    [JsonPropertyName("kategoriAgacTanim")]
    public string? KategoriAgacTanim { get; set; }

    [JsonPropertyName("oivOran")]
    public double? OivOran { get; set; }

    [JsonPropertyName("bagliFaturadaKullanilabilir")]
    public bool? BagliFaturadaKullanilabilir { get; set; }

    [JsonPropertyName("uretimdeKullanilacak")]
    public bool? UretimdeKullanilacak { get; set; }

    [JsonPropertyName("uretimAnalizindeKullanilacak")]
    public bool? UretimAnalizindeKullanilacak { get; set; }

    [JsonPropertyName("giderMerkezi")]
    public string? GiderMerkezi { get; set; }

    [JsonPropertyName("uretimMerkeziKod")]
    public string? UretimMerkeziKod { get; set; }

    [JsonPropertyName("makineKod")]
    public string? MakineKod { get; set; }

    // Oranlar alanı
    [JsonPropertyName("otvMaliyetFlag")]
    public bool? OtvMaliyetFlag { get; set; }

    [JsonPropertyName("otvTutarKdvFlag")]
    public bool? OtvTutarKdvFlag { get; set; }

    [JsonPropertyName("otvIskontoFlag")]
    public bool? OtvIskontoFlag { get; set; }

    [JsonPropertyName("otvTipi")]
    public string? OtvTipi { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    [JsonPropertyName("alisIskontoOran2")]
    public double? AlisIskontoOran2 { get; set; }

    [JsonPropertyName("alisIskontoOran3")]
    public double? AlisIskontoOran3 { get; set; }

    [JsonPropertyName("alisIskontoOran4")]
    public double? AlisIskontoOran4 { get; set; }

    [JsonPropertyName("alisIskontoOran5")]
    public double? AlisIskontoOran5 { get; set; }

    [JsonPropertyName("satisIskontoOran2")]
    public double? SatisIskontoOran2 { get; set; }

    [JsonPropertyName("satisIskontoOran3")]
    public double? SatisIskontoOran3 { get; set; }

    [JsonPropertyName("satisIskontoOran4")]
    public double? SatisIskontoOran4 { get; set; }

    [JsonPropertyName("satisIskontoOran5")]
    public double? SatisIskontoOran5 { get; set; }

    [JsonPropertyName("alisMaktuVergi")]
    public double? AlisMaktuVergi { get; set; }

    [JsonPropertyName("satisMaktuVergi")]
    public double? SatisMaktuVergi { get; set; }

    [JsonPropertyName("alisOtvOran")]
    public double? AlisOtvOran { get; set; }

    [JsonPropertyName("alisOtvTutar")]
    public double? AlisOtvTutar { get; set; }

    [JsonPropertyName("alisTecilOtv")]
    public double? AlisTecilOtv { get; set; }

    [JsonPropertyName("satisOtvOran")]
    public double? SatisOtvOran { get; set; }

    [JsonPropertyName("satisOtvTutar")]
    public double? SatisOtvTutar { get; set; }

    [JsonPropertyName("satisTecilOtv")]
    public double? SatisTecilOtv { get; set; }
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
/// 3.2.19 Cari Risk Bilgileri isteği (isteğe bağlı body kullanımı).
/// </summary>
public class LucaGetCustomerRiskRequest
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaFinansalNesneKey GnlFinansalNesne { get; set; } = new();
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

// ======================= CARI HAREKET LİSTESİ =======================

public class LucaListCustomerTransactionsRequest
{
    [JsonPropertyName("cariKodu")]
    public string? CariKodu { get; set; }

    [JsonPropertyName("belgeTarihiBas")]
    public string? BelgeTarihiBas { get; set; }

    [JsonPropertyName("belgeTarihiBit")]
    public string? BelgeTarihiBit { get; set; }

    [JsonPropertyName("belgeTarihiOp")]
    public string? BelgeTarihiOp { get; set; }
}

public class LucaCustomerTransactionDto
{
    [JsonPropertyName("hareketId")]
    public long HareketId { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("belgeNo")]
    public string BelgeNo { get; set; } = string.Empty;

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

// ======================= CARI DETAY =======================

public class LucaGetCustomerDetailRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

public class LucaCustomerDetailDto
{
    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string CariAdi { get; set; } = string.Empty;

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("vergiDairesi")]
    public string? VergiDairesi { get; set; }

    [JsonPropertyName("adres")]
    public string? Adres { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

// ======================= CARI GÜNCELLEME =======================

public class LucaUpdateCustomerRequest
{
    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string? CariAdi { get; set; }

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("vergiDairesi")]
    public string? VergiDairesi { get; set; }

    [JsonPropertyName("adres")]
    public string? Adres { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

// ======================= TEDARIKÇI GÜNCELLEME =======================

public class LucaUpdateSupplierRequest
{
    [JsonPropertyName("tedarikciKodu")]
    public string TedarikciKodu { get; set; } = string.Empty;

    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
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

// --- 3.2.35 / 3.2.36 Satış / Satın Alma Siparişi ---

/// <summary>
/// 3.2.35 Satış veya 3.2.36 Satınalma siparişi detay satırı.
/// </summary>
public class LucaCreateOrderDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1;

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

    [JsonPropertyName("iskontoOran1")]
    public double? IskontoOran1 { get; set; }
    [JsonPropertyName("iskontoOran2")]
    public double? IskontoOran2 { get; set; }
    [JsonPropertyName("iskontoOran3")]
    public double? IskontoOran3 { get; set; }
    [JsonPropertyName("iskontoOran4")]
    public double? IskontoOran4 { get; set; }
    [JsonPropertyName("iskontoOran5")]
    public double? IskontoOran5 { get; set; }
    [JsonPropertyName("iskontoOran6")]
    public double? IskontoOran6 { get; set; }
    [JsonPropertyName("iskontoOran7")]
    public double? IskontoOran7 { get; set; }
    [JsonPropertyName("iskontoOran8")]
    public double? IskontoOran8 { get; set; }
    [JsonPropertyName("iskontoOran9")]
    public double? IskontoOran9 { get; set; }
    [JsonPropertyName("iskontoOran10")]
    public double? IskontoOran10 { get; set; }

    [JsonPropertyName("otvOran")]
    public double? OtvOran { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("shAttribute1Deger")]
    public string? ShAttribute1Deger { get; set; }
    [JsonPropertyName("shAttribute1Ack")]
    public string? ShAttribute1Ack { get; set; }
    [JsonPropertyName("shAttribute2Deger")]
    public string? ShAttribute2Deger { get; set; }
    [JsonPropertyName("shAttribute2Ack")]
    public string? ShAttribute2Ack { get; set; }
    [JsonPropertyName("shAttribute3Deger")]
    public string? ShAttribute3Deger { get; set; }
    [JsonPropertyName("shAttribute3Ack")]
    public string? ShAttribute3Ack { get; set; }
    [JsonPropertyName("shAttribute4Deger")]
    public string? ShAttribute4Deger { get; set; }
    [JsonPropertyName("shAttribute4Ack")]
    public string? ShAttribute4Ack { get; set; }
    [JsonPropertyName("shAttribute5Deger")]
    public string? ShAttribute5Deger { get; set; }
    [JsonPropertyName("shAttribute5Ack")]
    public string? ShAttribute5Ack { get; set; }
}

/// <summary>
/// 3.2.35 / 3.2.36 sipariş başlık DTO'su.
/// </summary>
public class LucaCreateOrderHeaderRequest
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

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("teklifSiparisTur")]
    public int TeklifSiparisTur { get; set; } = 1;

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double KurBedeli { get; set; } = 1.0;

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("opsiyonTarihiFlag")]
    public bool? OpsiyonTarihiFlag { get; set; }

    [JsonPropertyName("opsiyonTarihi")]
    public DateTime? OpsiyonTarihi { get; set; }

    [JsonPropertyName("teslimTarihiFlag")]
    public bool? TeslimTarihiFlag { get; set; }

    [JsonPropertyName("teslimTarihi")]
    public DateTime? TeslimTarihi { get; set; }

    [JsonPropertyName("onayFlag")]
    public bool? OnayFlag { get; set; }

    [JsonPropertyName("onayTarihi")]
    public DateTime? OnayTarihi { get; set; }

    [JsonPropertyName("onayAciklama")]
    public string? OnayAciklama { get; set; }

    [JsonPropertyName("saticiNotu")]
    public string? SaticiNotu { get; set; }

    [JsonPropertyName("finansmanNotu")]
    public string? FinansmanNotu { get; set; }

    [JsonPropertyName("nakliyeBedeliTuru")]
    public int? NakliyeBedeliTuru { get; set; }

    [JsonPropertyName("perakendeKdvFlag")]
    public bool? PerakendeKdvFlag { get; set; }

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    [JsonPropertyName("tevkifatOran")]
    public string? TevkifatOran { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariTanim")]
    public string? CariTanim { get; set; }

    [JsonPropertyName("cariTip")]
    public int? CariTip { get; set; }

    [JsonPropertyName("cariKisaAd")]
    public string? CariKisaAd { get; set; }

    [JsonPropertyName("cariYasalUnvan")]
    public string? CariYasalUnvan { get; set; }

    [JsonPropertyName("vergiNo")]
    public string? VergiNo { get; set; }

    [JsonPropertyName("vergiDairesi")]
    public string? VergiDairesi { get; set; }

    [JsonPropertyName("cariAd")]
    public string? CariAd { get; set; }

    [JsonPropertyName("cariSoyad")]
    public string? CariSoyad { get; set; }

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    [JsonPropertyName("il")]
    public string? Il { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("mahalle")]
    public string? Mahalle { get; set; }

    [JsonPropertyName("cadde")]
    public string? Cadde { get; set; }

    [JsonPropertyName("disKapiNo")]
    public string? DisKapiNo { get; set; }

    [JsonPropertyName("icKapiNo")]
    public string? IcKapiNo { get; set; }

    [JsonPropertyName("postaKodu")]
    public string? PostaKodu { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateOrderDetailRequest> DetayList { get; set; } = new();
}

/// <summary>
/// 3.2.38 sipariş silme isteği.
/// </summary>
public class LucaDeleteOrderRequest
{
    [JsonPropertyName("ssSiparisBaslikId")]
    public long SsSiparisBaslikId { get; set; }
}

/// <summary>
/// 3.2.38 sipariş detay silme isteği.
/// </summary>
public class LucaDeleteOrderDetailRequest
{
    [JsonPropertyName("detayList")]
    public List<LucaOrderDetailToDelete> DetayList { get; set; } = new();
}

public class LucaOrderDetailToDelete
{
    [JsonPropertyName("detayId")]
    public long DetayId { get; set; }
}

// --- Mevcut sipariş DTO'ları (basitleştirilmiş) ---

public class LucaCreateSalesOrderRequest
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("detayList")]
    public List<LucaSalesOrderDetailRequest> DetayList { get; set; } = new();
}

public class LucaSalesOrderDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("miktar")]
    public decimal Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public decimal BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double KdvOran { get; set; }
}

// ======================= SİPARİŞ GÜNCELLEME =======================

public class LucaUpdateSalesOrderRequest
{
    [JsonPropertyName("siparisId")]
    public long SiparisId { get; set; }

    [JsonPropertyName("teslimTarihi")]
    public DateTime? TeslimTarihi { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaDeleteSalesOrderRequest
{
    [JsonPropertyName("ssSiparisBaslikId")]
    public long SsSiparisBaslikId { get; set; }
}

public class LucaDeleteSalesOrderDetailRequest
{
    [JsonPropertyName("ssSiparisDetayId")]
    public long SsSiparisDetayId { get; set; }
}

public class LucaCreatePurchaseOrderRequest
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("detayList")]
    public List<LucaPurchaseOrderDetailRequest> DetayList { get; set; } = new();
}

public class LucaPurchaseOrderDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("miktar")]
    public decimal Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public decimal BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double KdvOran { get; set; }
}

public class LucaDeletePurchaseOrderRequest
{
    [JsonPropertyName("ssSiparisBaslikId")]
    public long SsSiparisBaslikId { get; set; }
}

public class LucaDeletePurchaseOrderDetailRequest
{
    [JsonPropertyName("ssSiparisDetayId")]
    public long SsSiparisDetayId { get; set; }
}

public class LucaPurchaseOrderDto
{
    [JsonPropertyName("siparisId")]
    public long SiparisId { get; set; }

    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("teslimTarihi")]
    public DateTime? TeslimTarihi { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string? CariAdi { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string? ParaBirimKod { get; set; }

    [JsonPropertyName("toplamTutar")]
    public double? ToplamTutar { get; set; }

    [JsonPropertyName("durum")]
    public string? Durum { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaPurchaseOrderLineDto> DetayList { get; set; } = new();
}

public class LucaPurchaseOrderLineDto
{
    [JsonPropertyName("detayId")]
    public long? DetayId { get; set; }

    [JsonPropertyName("kartKodu")]
    public string? KartKodu { get; set; }

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double? KdvOran { get; set; }
}

// --- Depo hareket / sayım ve kredi kartı ---

public class LucaCreateWarehouseTransferRequest
{
    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("belgeTakipNo")]
    public string? BelgeTakipNo { get; set; }

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    [JsonPropertyName("girisDepoKodu")]
    public string GirisDepoKodu { get; set; } = string.Empty;

    [JsonPropertyName("cikisDepoKodu")]
    public string CikisDepoKodu { get; set; } = string.Empty;

    [JsonPropertyName("detayList")]
    public List<LucaWarehouseTransferDetailRequest> DetayList { get; set; } = new();
}

public class LucaWarehouseTransferDetailRequest
{
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("miktar")]
    public decimal Miktar { get; set; }

    [JsonPropertyName("olcuBirimi")]
    public long? OlcuBirimi { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("shAttribute1Deger")]
    public string? ShAttribute1Deger { get; set; }
    [JsonPropertyName("shAttribute1Ack")]
    public string? ShAttribute1Ack { get; set; }
    [JsonPropertyName("shAttribute2Deger")]
    public string? ShAttribute2Deger { get; set; }
    [JsonPropertyName("shAttribute2Ack")]
    public string? ShAttribute2Ack { get; set; }
    [JsonPropertyName("shAttribute3Deger")]
    public string? ShAttribute3Deger { get; set; }
    [JsonPropertyName("shAttribute3Ack")]
    public string? ShAttribute3Ack { get; set; }
    [JsonPropertyName("shAttribute4Deger")]
    public string? ShAttribute4Deger { get; set; }
    [JsonPropertyName("shAttribute4Ack")]
    public string? ShAttribute4Ack { get; set; }
    [JsonPropertyName("shAttribute5Deger")]
    public string? ShAttribute5Deger { get; set; }
    [JsonPropertyName("shAttribute5Ack")]
    public string? ShAttribute5Ack { get; set; }
}

public class LucaCreateStockCountRequest
{
    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("depoKodu")]
    public string DepoKodu { get; set; } = string.Empty;

    [JsonPropertyName("kapamaBelgeOlustur")]
    public bool KapamaBelgeOlustur { get; set; } = true;

    [JsonPropertyName("detayList")]
    public List<LucaStockCountDetailRequest> DetayList { get; set; } = new();
}

public class LucaStockCountDetailRequest
{
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("sayimMiktari")]
    public decimal SayimMiktari { get; set; }
}

public class LucaCreateWarehouseRequest
{
    [JsonPropertyName("kod")]
    public string Kod { get; set; } = string.Empty;

    [JsonPropertyName("tanim")]
    public string Tanim { get; set; } = string.Empty;

    [JsonPropertyName("kategoriKod")]
    public string? KategoriKod { get; set; }

    [JsonPropertyName("ulke")]
    public string? Ulke { get; set; }

    [JsonPropertyName("il")]
    public string? Il { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }
}

public class LucaCreateCreditCardEntryRequest
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

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

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("detayList")]
    public List<LucaCreditCardEntryDetailRequest> DetayList { get; set; } = new();
}

public class LucaCreditCardEntryDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("avansFlag")]
    public bool? AvansFlag { get; set; }

    [JsonPropertyName("tutar")]
    public decimal Tutar { get; set; }

    [JsonPropertyName("vadeTarihi")]
    public DateTime? VadeTarihi { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

// --- 3.2.40 Diğer Stok Hareketi (Dsh) ---

public class LucaCreateDshDetayRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1;

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

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double? BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double? KdvOran { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("seriNo")]
    public string? SeriNo { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }
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

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("siparisBelgeSeri")]
    public string? SiparisBelgeSeri { get; set; }

    [JsonPropertyName("siparisBelgeNo")]
    public string? SiparisBelgeNo { get; set; }

    /// <summary>
    /// Giriş/çıkış yönü: "GIRIS" veya "CIKIS" (dokümandaki alt belge türüne göre de belirlenebilir)
    /// </summary>
    [JsonPropertyName("hareketYonu")]
    public string? HareketYonu { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateDshDetayRequest> DetayList { get; set; } = new();

    /// <summary>
    /// Çıkış belgesine bağlı giriş hareketi.
    /// </summary>
    [JsonPropertyName("uretimeGirisBaslik")]
    public LucaCreateDshBaslikRequest? UretimeGirisBaslik { get; set; }
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

    // İskonto alanları
    [JsonPropertyName("iskontoOran1")]
    public double? IskontoOran1 { get; set; }
    [JsonPropertyName("iskontoOran2")]
    public double? IskontoOran2 { get; set; }
    [JsonPropertyName("iskontoOran3")]
    public double? IskontoOran3 { get; set; }
    [JsonPropertyName("iskontoOran4")]
    public double? IskontoOran4 { get; set; }
    [JsonPropertyName("iskontoOran5")]
    public double? IskontoOran5 { get; set; }
    [JsonPropertyName("iskontoOran6")]
    public double? IskontoOran6 { get; set; }
    [JsonPropertyName("iskontoOran7")]
    public double? IskontoOran7 { get; set; }
    [JsonPropertyName("iskontoOran8")]
    public double? IskontoOran8 { get; set; }
    [JsonPropertyName("iskontoOran9")]
    public double? IskontoOran9 { get; set; }
    [JsonPropertyName("iskontoOran10")]
    public double? IskontoOran10 { get; set; }

    [JsonPropertyName("otvOran")]
    public double? OtvOran { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("garantiBitisTarihi")]
    public DateTime? GarantiBitisTarihi { get; set; }

    [JsonPropertyName("uretimTarihi")]
    public DateTime? UretimTarihi { get; set; }

    // Satır custom attributes
    [JsonPropertyName("shAttribute1Deger")]
    public string? ShAttribute1Deger { get; set; }
    [JsonPropertyName("shAttribute1Ack")]
    public string? ShAttribute1Ack { get; set; }
    [JsonPropertyName("shAttribute2Deger")]
    public string? ShAttribute2Deger { get; set; }
    [JsonPropertyName("shAttribute2Ack")]
    public string? ShAttribute2Ack { get; set; }
    [JsonPropertyName("shAttribute3Deger")]
    public string? ShAttribute3Deger { get; set; }
    [JsonPropertyName("shAttribute3Ack")]
    public string? ShAttribute3Ack { get; set; }
    [JsonPropertyName("shAttribute4Deger")]
    public string? ShAttribute4Deger { get; set; }
    [JsonPropertyName("shAttribute4Ack")]
    public string? ShAttribute4Ack { get; set; }
    [JsonPropertyName("shAttribute5Deger")]
    public string? ShAttribute5Deger { get; set; }
    [JsonPropertyName("shAttribute5Ack")]
    public string? ShAttribute5Ack { get; set; }
}

/// <summary>
/// 3.2.27 İrsaliye Ekleme – başlık DTO'su.
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

    // Belge custom attributes
    [JsonPropertyName("belgeAttribute1Deger")]
    public string? BelgeAttribute1Deger { get; set; }
    [JsonPropertyName("belgeAttribute1Ack")]
    public string? BelgeAttribute1Ack { get; set; }
    [JsonPropertyName("belgeAttribute2Deger")]
    public string? BelgeAttribute2Deger { get; set; }
    [JsonPropertyName("belgeAttribute2Ack")]
    public string? BelgeAttribute2Ack { get; set; }
    [JsonPropertyName("belgeAttribute3Deger")]
    public string? BelgeAttribute3Deger { get; set; }
    [JsonPropertyName("belgeAttribute3Ack")]
    public string? BelgeAttribute3Ack { get; set; }
    [JsonPropertyName("belgeAttribute4Deger")]
    public string? BelgeAttribute4Deger { get; set; }
    [JsonPropertyName("belgeAttribute4Ack")]
    public string? BelgeAttribute4Ack { get; set; }
    [JsonPropertyName("belgeAttribute5Deger")]
    public string? BelgeAttribute5Deger { get; set; }
    [JsonPropertyName("belgeAttribute5Ack")]
    public string? BelgeAttribute5Ack { get; set; }

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

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    [JsonPropertyName("il")]
    public string? Il { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("mahallesemt")]
    public string? Mahallesemt { get; set; }

    [JsonPropertyName("caddesokak")]
    public string? Caddesokak { get; set; }

    [JsonPropertyName("diskapino")]
    public string? Diskapino { get; set; }

    [JsonPropertyName("ickapino")]
    public string? Ickapino { get; set; }

    [JsonPropertyName("postaKodu")]
    public string? PostaKodu { get; set; }

    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }

    // E-İrsaliye taşıyıcı bilgileri
    [JsonPropertyName("tasiyiciPlaka")]
    public string? TasiyiciPlaka { get; set; }

    [JsonPropertyName("tasiyiciVkn")]
    public string? TasiyiciVkn { get; set; }

    [JsonPropertyName("tasiyiciUnvan")]
    public string? TasiyiciUnvan { get; set; }

    [JsonPropertyName("kargoNumarasi")]
    public string? KargoNumarasi { get; set; }

    [JsonPropertyName("eirsaliyeNo")]
    public string? EirsaliyeNo { get; set; }

    [JsonPropertyName("soforListesi")]
    public List<string>? SoforListesi { get; set; }

    [JsonPropertyName("dorseListesi")]
    public List<string>? DorseListesi { get; set; }

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
// ======================= CHECK (ÇEK) =======================

public class LucaCreateCheckRequest
{
    [JsonPropertyName("cekNo")]
    public string CekNo { get; set; } = string.Empty;

    [JsonPropertyName("vadeTarihi")]
    public DateTime VadeTarihi { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("bankaAdi")]
    public string? BankaAdi { get; set; }

    [JsonPropertyName("subeAdi")]
    public string? SubeAdi { get; set; }

    [JsonPropertyName("hesapNo")]
    public string? HesapNo { get; set; }
}

public class LucaCheckDto
{
    [JsonPropertyName("cekId")]
    public long CekId { get; set; }

    [JsonPropertyName("cekNo")]
    public string CekNo { get; set; } = string.Empty;

    [JsonPropertyName("vadeTarihi")]
    public DateTime VadeTarihi { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string? CariAdi { get; set; }

    [JsonPropertyName("durum")]
    public string Durum { get; set; } = string.Empty;
}

public class LucaListChecksRequest
{
    [JsonPropertyName("cekNoBas")]
    public string? CekNoBas { get; set; }

    [JsonPropertyName("cekNoBit")]
    public string? CekNoBit { get; set; }

    [JsonPropertyName("cekNoOp")]
    public string? CekNoOp { get; set; }

    [JsonPropertyName("vadeTarihiBas")]
    public string? VadeTarihiBas { get; set; }

    [JsonPropertyName("vadeTarihiBit")]
    public string? VadeTarihiBit { get; set; }

    [JsonPropertyName("vadeTarihiOp")]
    public string? VadeTarihiOp { get; set; }
}
// ======================= BOND (SENET) =======================

public class LucaCreateBondRequest
{
    [JsonPropertyName("senetNo")]
    public string SenetNo { get; set; } = string.Empty;

    [JsonPropertyName("vadeTarihi")]
    public DateTime VadeTarihi { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaBondDto
{
    [JsonPropertyName("senetId")]
    public long SenetId { get; set; }

    [JsonPropertyName("senetNo")]
    public string SenetNo { get; set; } = string.Empty;

    [JsonPropertyName("vadeTarihi")]
    public DateTime VadeTarihi { get; set; }

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("cariAdi")]
    public string? CariAdi { get; set; }

    [JsonPropertyName("durum")]
    public string Durum { get; set; } = string.Empty;
}

public class LucaListBondsRequest
{
    [JsonPropertyName("senetNoBas")]
    public string? SenetNoBas { get; set; }

    [JsonPropertyName("senetNoBit")]
    public string? SenetNoBit { get; set; }

    [JsonPropertyName("senetNoOp")]
    public string? SenetNoOp { get; set; }

    [JsonPropertyName("vadeTarihiBas")]
    public string? VadeTarihiBas { get; set; }

    [JsonPropertyName("vadeTarihiBit")]
    public string? VadeTarihiBit { get; set; }

    [JsonPropertyName("vadeTarihiOp")]
    public string? VadeTarihiOp { get; set; }
}

// ======================= CASH (KASA) =======================

public class LucaCashDto
{
    [JsonPropertyName("kasaId")]
    public long KasaId { get; set; }

    [JsonPropertyName("kasaKodu")]
    public string KasaKodu { get; set; } = string.Empty;

    [JsonPropertyName("kasaAdi")]
    public string KasaAdi { get; set; } = string.Empty;

    [JsonPropertyName("bakiye")]
    public double Bakiye { get; set; }
}

public class LucaListCashRequest
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }

    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }
}

// ------- Kasa Hareketi ---------

public class LucaCashTransactionDto
{
    [JsonPropertyName("hareketId")]
    public long HareketId { get; set; }

    [JsonPropertyName("kasaKodu")]
    public string KasaKodu { get; set; } = string.Empty;

    [JsonPropertyName("belgeNo")]
    public string BelgeNo { get; set; } = string.Empty;

    [JsonPropertyName("tutar")]
    public double Tutar { get; set; }

    [JsonPropertyName("tarih")]
    public DateTime Tarih { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaListCashTransactionsRequest
{
    [JsonPropertyName("kasaKodu")]
    public string? KasaKodu { get; set; }

    [JsonPropertyName("tarihBas")]
    public string? TarihBas { get; set; }

    [JsonPropertyName("tarihBit")]
    public string? TarihBit { get; set; }

    [JsonPropertyName("tarihOp")]
    public string? TarihOp { get; set; }
}
