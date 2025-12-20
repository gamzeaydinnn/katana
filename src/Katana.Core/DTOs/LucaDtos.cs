using System.Text.Json;
using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

// ======================================
// Koza Integration DTOs (from Business.DTOs)
// ======================================

public class LucaKozaProductDto
{
    public string Code { get; set; } = "";          // kartKodu
    public string Name { get; set; } = "";          // kartAdi
    public string? Category { get; set; }             // kategoriAgacKod veya kategori adı
    public string? Uom { get; set; }                  // ölçü birimi (opsiyonel)
}

// ======================================
// Core Luca DTOs
// ======================================

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
    
    [JsonPropertyName("gnlOrgSsBelge")]
    public LucaBelgeDto GnlOrgSsBelge { get; set; } = new();

    
    [JsonPropertyName("faturaTur")]
    public int? FaturaTur { get; set; } 

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double? KurBedeli { get; set; }

    [JsonPropertyName("yuklemeTarihi")]
    public DateTime? YuklemeTarihi { get; set; }

	    [JsonPropertyName("babsFlag")]
	    public bool BabsFlag { get; set; }

    [JsonPropertyName("kdvFlag")]
    public bool? KdvFlag { get; set; } 

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    [JsonPropertyName("tevkifatOran")]
    public string? TevkifatOran { get; set; } 

    [JsonPropertyName("tevkifatKod")]
    public int? TevkifatKod { get; set; }

    
    [JsonPropertyName("musteriTedarikci")]
    public int? MusteriTedarikci { get; set; } 

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

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    [JsonPropertyName("cariAd")]
    public string? CariAd { get; set; }

    [JsonPropertyName("cariSoyad")]
    public string? CariSoyad { get; set; }

    
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

    
    [JsonPropertyName("siparisTarihi")]
    public DateTime? SiparisTarihi { get; set; }

    [JsonPropertyName("siparisNo")]
    public string? SiparisNo { get; set; }

    [JsonPropertyName("irsaliyeBilgisiList")]
    public List<LucaIrsaliyeBilgisiDto>? IrsaliyeBilgisiList { get; set; }

    
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

    
    [JsonPropertyName("detayList")]
    public List<LucaInvoiceItemDto> Lines { get; set; } = new();

    
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

public class LucaStockDto
{
    
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

    
    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }

    [JsonPropertyName("olcumBirimiTanim")]
    public string? Unit { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    
    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? KartSatisKdvOran { get; set; }

    [JsonPropertyName("kartToptanAlisKdvOran")]
    public double? KartToptanAlisKdvOran { get; set; }

    [JsonPropertyName("kartToptanSatisKdvOran")]
    public double? KartToptanSatisKdvOran { get; set; }

    
    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("baslangicTarihi")]
    public string? BaslangicTarihi { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }

    
    [JsonPropertyName("minStokKontrol")]
    public long? MinStokKontrol { get; set; }

    [JsonPropertyName("minStokMiktari")]
    public double? MinStokMiktari { get; set; }

    [JsonPropertyName("maxStokKontrol")]
    public long? MaxStokKontrol { get; set; }

    [JsonPropertyName("maxStokMiktari")]
    public double? MaxStokMiktari { get; set; }

    
    [JsonPropertyName("perakendeAlisBirimFiyat")]
    public double? PerakendeAlisBirimFiyat { get; set; }

    [JsonPropertyName("perakendeSatisBirimFiyat")]
    public double? PerakendeSatisBirimFiyat { get; set; }

    
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

    
    [JsonPropertyName("alisTevkifatOran")]
    public string? AlisTevkifatOran { get; set; }

    [JsonPropertyName("alisTevkifatKod")]
    public int? AlisTevkifatKod { get; set; }

    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }

    [JsonPropertyName("satisTevkifatKod")]
    public int? SatisTevkifatKod { get; set; }

    
    [JsonPropertyName("alisIskontoOran1")]
    public double? AlisIskontoOran1 { get; set; }

    [JsonPropertyName("satisIskontoOran1")]
    public double? SatisIskontoOran1 { get; set; }

    
    [JsonPropertyName("otvTipi")]
    public string? OtvTipi { get; set; }

    [JsonPropertyName("stopajOran")]
    public double? StopajOran { get; set; }

    
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

// ✅ RENAMED: LucaDepoDto -> LucaWarehouseDto (to avoid naming conflict)
// Old name kept as comment for reference: public class LucaDepoDto

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

/// <summary>
/// Luca ölçü birimi DTO - ListeleGnlOlcumBirimi.do endpoint'inden dönen veri
/// </summary>
public class LucaOlcumBirimiDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("kod")]
    public string Kod { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("kisa")]
    public string? Kisa { get; set; }

    [JsonPropertyName("aktif")]
    public bool Aktif { get; set; }

    // Backward compatibility - Tanim property maps to Ad
    [JsonIgnore]
    public string Tanim => Ad;
}

/// <summary>
/// Luca ölçü birimi API response wrapper
/// </summary>
public class LucaOlcumBirimiResponse
{
    [JsonPropertyName("data")]
    public List<LucaOlcumBirimiDto>? Data { get; set; }

    [JsonPropertyName("list")]
    public List<LucaOlcumBirimiDto>? List { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Returns the measurement units from either Data or List property
    /// </summary>
    [JsonIgnore]
    public List<LucaOlcumBirimiDto> Units => Data ?? List ?? new List<LucaOlcumBirimiDto>();
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

public class LucaCodeRangeFilter
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }

    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }
}

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
public class LucaDocumentTypeDetailFilter
{
    [JsonPropertyName("belgeTurId")]
    public long? BelgeTurId { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public long? BelgeTurDetayId { get; set; }

    [JsonPropertyName("tanim")]
    public string? Tanim { get; set; }
}
public class LucaListDocumentTypeDetailsRequest
{
    [JsonPropertyName("gnlBelgeTurDetay")]
    public LucaDocumentTypeDetailFilter? GnlBelgeTurDetay { get; set; }
}

public class LucaListDocumentSeriesRequest
{
    [JsonPropertyName("gnlBelgeTurDetay")]
    public LucaDocumentTypeDetailFilter? GnlBelgeTurDetay { get; set; }
}

public class LucaGetDocumentSeriesMaxRequest
{
    [JsonPropertyName("seriNoWs")]
    public string? SeriNoWs { get; set; }

    [JsonPropertyName("belgeTurDetayIdWs")]
    public long? BelgeTurDetayIdWs { get; set; }
}

public class LucaOrgSirketSubeFilter
{
    [JsonPropertyName("orgSirketSubeId")]
    public long? OrgSirketSubeId { get; set; }
}
public class LucaListBranchCurrenciesRequest
{
    [JsonPropertyName("gnlOrgSirketSube")]
    public LucaOrgSirketSubeFilter? GnlOrgSirketSube { get; set; }
}

public class LucaDynamicLovReference
{
    [JsonPropertyName("dynamicLovId")]
    public long? DynamicLovId { get; set; }
}

public class LucaDynamicLovValueDto
{
    [JsonPropertyName("dynamicLovValueId")]
    public long? DynamicLovValueId { get; set; }

    [JsonPropertyName("ytkDynamicLov")]
    public LucaDynamicLovReference? DynamicLov { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class LucaListDynamicLovValuesRequest
{
    [JsonPropertyName("ytkDynamicLovValue")]
    public LucaDynamicLovValueDto? DynamicLovValue { get; set; }
}

public class LucaUpdateDynamicLovValueRequest
{
    [JsonPropertyName("dynamicLovValueId")]
    public long DynamicLovValueId { get; set; }

    [JsonPropertyName("ytkDynamicLov")]
    public LucaDynamicLovReference? DynamicLov { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class LucaDynamicLovEntryRequest
{
    [JsonPropertyName("kod")]
    public string Kod { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }
}

public class LucaCreateDynamicLovRequest
{
    [JsonPropertyName("degisken")]
    public string Degisken { get; set; } = string.Empty;

    [JsonPropertyName("degerListesi")]
    public List<LucaDynamicLovEntryRequest> DegerListesi { get; set; } = new();
}

public class LucaUpdateAttributeRequest
{
    [JsonPropertyName("attributeId")]
    public long AttributeId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class LucaFinancialObjectFilter
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaCodeRangeFilter? GnlFinansalNesne { get; set; }
}

public class LucaCityFilter
{
    [JsonPropertyName("ilId")]
    public long? IlId { get; set; }
}

public class LucaCustomerPrimaryAddressFilter
{
    [JsonPropertyName("gnlIl")]
    public LucaCityFilter? GnlIl { get; set; }
}

public class LucaListCustomersRequest
{
    [JsonPropertyName("finMusteri")]
    public LucaFinancialObjectFilter? FinMusteri { get; set; }

    [JsonPropertyName("birincilFaturaAdres")]
    public LucaCustomerPrimaryAddressFilter? BirincilFaturaAdres { get; set; }
}

public class LucaListSuppliersRequest
{
    [JsonPropertyName("finTedarikci")]
    public LucaFinancialObjectFilter? FinTedarikci { get; set; }

    [JsonPropertyName("apiFilter")]
    public bool? ApiFilter { get; set; }
}

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

public class LucaStockCardCodeFilter
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }

    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }

    [JsonPropertyName("eklemeTarihiBas")]
    public string? EklemeTarihiBas { get; set; }

    [JsonPropertyName("eklemeTarihiBit")]
    public string? EklemeTarihiBit { get; set; }

    [JsonPropertyName("eklemeTarihiOp")]
    public string? EklemeTarihiOp { get; set; }

    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; }
}
public class LucaStockCardKey
{
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }
}
public class LucaListStockCardsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardCodeFilter StkSkart { get; set; } = new();
}

/// <summary>
/// Frontend için basitleştirilmiş stok kartı DTO
/// </summary>
public class KozaStokKartiListDto
{
    [JsonPropertyName("skartId")]
    public long? SkartId { get; set; }
    
    [JsonPropertyName("kod")]
    public string? KartKodu { get; set; }
    
    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }
    
    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }
    
    [JsonPropertyName("anaBirimAdi")]
    public string? Birim { get; set; }
    
    [JsonPropertyName("stokMiktari")]
    public double? Miktar { get; set; }
    
    [JsonPropertyName("kartAlisKdvOran")]
    public double? AlisKdvOran { get; set; }
    
    [JsonPropertyName("kartSatisKdvOran")]
    public double? SatisKdvOran { get; set; }
    
    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriKodu { get; set; }
    
    // Frontend compatibility
    [JsonIgnore]
    public long? StokKartId => SkartId;
}
public class LucaListStockCardPriceListsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
    
    [JsonPropertyName("tip")]
    public string Tip { get; set; } = string.Empty;
}
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

public class LucaStockCardByIdRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

public class LucaListStockCategoriesRequest
{
    
    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; }
}


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


public class LucaStockCardSuppliersRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

public class LucaListCustomerContactsRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

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

public class LucaListCashAccountsRequest
{
    [JsonPropertyName("finSsKasa")]
    public JsonElement? FinSsKasa { get; set; }
}

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


public class LucaListStockCardPurchaseTermsRequest
{
    [JsonPropertyName("stkSkart")]
    public LucaStockCardKey StkSkart { get; set; } = new();
}

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

public class LucaListSalesOrdersRequest
{
    [JsonPropertyName("stsSsSiparisBaslik")]
    public JsonElement? StsSsSiparisBaslik { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class LucaListPurchaseOrdersRequest
{
    [JsonPropertyName("stnSsSiparisBaslik")]
    public JsonElement? StnSsSiparisBaslik { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

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

/// <summary>
/// Luca'daki mevcut stok kartı detayları (karşılaştırma için)
/// </summary>
public class LucaStockCardDetails
{
    public long SkartId { get; set; }
    public string KartKodu { get; set; } = string.Empty;
    public string? KartAdi { get; set; }
    public int KartTuru { get; set; } = 1;
    public long OlcumBirimiId { get; set; } = 1;
    public double KartAlisKdvOran { get; set; }
    public double KartSatisKdvOran { get; set; }
    public int KartTipi { get; set; } = 1;
    public string? KategoriAgacKod { get; set; }
    public string? Barkod { get; set; }
    
    /// <summary>
    /// Satış fiyatı (karşılaştırma için)
    /// </summary>
    public double? SatisFiyat { get; set; }
    
    /// <summary>
    /// Alış fiyatı (karşılaştırma için)
    /// </summary>
    public double? AlisFiyat { get; set; }
    
    /// <summary>
    /// Stok miktarı (karşılaştırma için)
    /// </summary>
    public double? Miktar { get; set; }
}
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

public class LucaListInvoicesRequest
{
    [JsonPropertyName("ftrSsFaturaBaslik")]
    public LucaInvoiceOrgBelgeFilter? FtrSsFaturaBaslik { get; set; }

    [JsonPropertyName("parUstHareketTuru")]
    public int? ParUstHareketTuru { get; set; }
   
    [JsonPropertyName("parAltHareketTuru")]
    public int? ParAltHareketTuru { get; set; }
}

public class LucaCreateInvoiceDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1; 

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

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

    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

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

    [JsonPropertyName("garantiSuresi")]
    public int? GarantiSuresi { get; set; }

    [JsonPropertyName("uretimTarihi")]
    public DateTime? UretimTarihi { get; set; }

    [JsonPropertyName("konaklamaVergiOran")]
    public double? KonaklamaVergiOran { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

public class LucaCreateInvoiceHeaderRequest
{
    
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BelgeNo { get; set; }

    /// <summary>
    /// Luca API STRING tarih formatı bekliyor: "dd/MM/yyyy" (örn: "07/10/2025")
    /// </summary>
    [JsonPropertyName("belgeTarihi")]
    public string BelgeTarihi { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");

    [JsonPropertyName("duzenlemeSaati")]
    public string? DuzenlemeSaati { get; set; }

    /// <summary>
    /// Luca API STRING tarih formatı bekliyor: "dd/MM/yyyy" (örn: "07/10/2025")
    /// </summary>
    [JsonPropertyName("vadeTarihi")]
    public string? VadeTarihi { get; set; }

    [JsonPropertyName("belgeTakipNo")]
    public string? BelgeTakipNo { get; set; }

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    /// <summary>
    /// Luca API STRING bekliyor: "76" (Satış Faturası)
    /// </summary>
    [JsonPropertyName("belgeTurDetayId")]
    public string BelgeTurDetayId { get; set; } = "76";

    // Belge değişkenleri
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

    /// <summary>
    /// Luca API STRING bekliyor: "1" (Normal Fatura)
    /// </summary>
    [JsonPropertyName("faturaTur")]
    public string FaturaTur { get; set; } = "1";

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("kurBedeli")]
    public double KurBedeli { get; set; } = 1.0;

	    [JsonIgnore]
	    public bool? BabsFlag { get; set; }

    [JsonPropertyName("kdvFlag")]
    public bool KdvFlag { get; set; } = true;

    [JsonPropertyName("referansNo")]
    public string? ReferansNo { get; set; }

    /// <summary>
    /// Luca API STRING bekliyor: "1" (Müşteri)
    /// </summary>
    [JsonPropertyName("musteriTedarikci")]
    public string MusteriTedarikci { get; set; } = "1"; 

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

    // Kişi için cari alanları
    [JsonPropertyName("cariAd")]
    public string? CariAd { get; set; }

    [JsonPropertyName("cariSoyad")]
    public string? CariSoyad { get; set; }

    [JsonPropertyName("tcKimlikNo")]
    public string? TcKimlikNo { get; set; }

    // Adres alanları
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

    // İletişim alanları
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("iletisimTanim")]
    public string? IletisimTanim { get; set; }

    // E-Arşiv alanları
    [JsonPropertyName("webAdresi")]
    public string? WebAdresi { get; set; }

    [JsonPropertyName("kargoVknTckn")]
    public string? KargoVknTckn { get; set; }

    [JsonPropertyName("odemeTipi")]
    public string? OdemeTipi { get; set; }

    [JsonPropertyName("gonderimTipi")]
    public string? GonderimTipi { get; set; }

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
    public List<LucaLinkedDocument>? IrsaliyeBilgisiList { get; set; }

    // Finansal hareket değişkenleri
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

    /// <summary>
    /// E-Fatura türü: 1=TICARIFATURA, 2=TEMELFATURA, 3=YOLCUBERABERFATURA, 4=IHRACAT, 5=OZELMATRAH
    /// </summary>
    [JsonPropertyName("efaturaTuru")]
    public int? EfaturaTuru { get; set; }

    
    [JsonPropertyName("detayList")]
    public List<LucaCreateInvoiceDetailRequest> DetayList { get; set; } = new();

    [JsonIgnore]
    public List<LucaCreateInvoiceDetailRequest> DetailList
    {
        get => DetayList;
        set => DetayList = value ?? new();
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
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
public class LucaLinkedDocument
{
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public string? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime? BelgeTarihi { get; set; }
}
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
public class LucaDeleteInvoiceRequest
{
    [JsonPropertyName("ssFaturaBaslikId")]
    public long SsFaturaBaslikId { get; set; }
}

public class LucaInvoicePdfLinkRequest
{
    [JsonPropertyName("ssFaturaBaslikId")]
    public long SsFaturaBaslikId { get; set; }
}

public class LucaListCurrencyInvoicesRequest
{
    [JsonPropertyName("ftrSsFaturaBaslik")]
    public LucaInvoiceOrgBelgeFilter? FtrSsFaturaBaslik { get; set; }

    [JsonPropertyName("gnlParaBirimRapor")]
    public LucaCurrencyReport? GnlParaBirimRapor { get; set; }

    [JsonIgnore]
    public int? ParaBirimId
    {
        get => GnlParaBirimRapor?.ParaBirimId;
        set => GnlParaBirimRapor = value.HasValue ? new LucaCurrencyReport { ParaBirimId = value.Value } : null;
    }

    [JsonPropertyName("parUstHareketTuru")]
    public string? ParUstHareketTuru { get; set; }

    [JsonPropertyName("dovizGetir")]
    public int? DovizGetir { get; set; }
}
public class LucaDynamicStockServiceReportRequest
{
    [JsonPropertyName("parSiralamaKriteri")]
    public string? ParSiralamaKriteri { get; set; }

    [JsonPropertyName("parStokKartTuru")]
    public string? ParStokKartTuru { get; set; }

    [JsonPropertyName("basStokKodAd_comp")]
    public string? BasStokKodAdComp { get; set; }

    [JsonPropertyName("basStokKodAd_comp_ack")]
    public string? BasStokKodAdCompAck { get; set; }

    [JsonPropertyName("bitStokKodAd_comp")]
    public string? BitStokKodAdComp { get; set; }

    [JsonPropertyName("bitStokKodAd_comp_ack")]
    public string? BitStokKodAdCompAck { get; set; }

    [JsonPropertyName("parBaslangicStokKodu")]
    public string? ParBaslangicStokKodu { get; set; }

    [JsonPropertyName("parBitisStokKodu")]
    public string? ParBitisStokKodu { get; set; }

    [JsonPropertyName("raporFormat")]
    public string? RaporFormat { get; set; } = "XLS";

    [JsonPropertyName("request_locale")]
    public string? RequestLocale { get; set; } = "tr_TR";

    [JsonPropertyName("menuItemIslemKod")]
    public string? MenuItemIslemKod { get; set; }

    [JsonPropertyName("dovizGetir")]
    public int? DovizGetir { get; set; }

    [JsonPropertyName("outputFileName")]
    public string? OutputFileName { get; set; }
}
public class LucaInvoiceItemRequest
{
    public string stokKodu { get; set; } = string.Empty;
    public decimal miktar { get; set; }
    public decimal birimFiyat { get; set; }
    public decimal kdvOrani { get; set; }
}
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
    public bool MaliyetHesaplanacakFlag { get; set; }  // ✅ BOOLEAN - Luca dokümantasyonuna göre!

    [JsonPropertyName("kartTipi")]
    public long KartTipi { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    // NOT: stokKategoriId KALDIRILDI - Luca dokümantasyonunda bu alan YOK!

    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double KartSatisKdvOran { get; set; }

    [JsonPropertyName("bitisTarihi")]
    public DateTime? BitisTarihi { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

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
    public string? AlisTevkifatOran { get; set; }

    [JsonPropertyName("alisTevkifatTipId")]
    public int? AlisTevkifatTipId { get; set; }

    [JsonPropertyName("ihracatKategoriNo")]
    public string IhracatKategoriNo { get; set; } = string.Empty;

    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }

    [JsonPropertyName("satisTevkifatTipId")]
    public int? SatisTevkifatTipId { get; set; }

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
    
    /// <summary>
    /// Stok miktarı (karşılaştırma için - Luca API'ye gönderilmez, sadece değişiklik tespiti için)
    /// NOT: Luca'da stok miktarı stok kartı ile değil, stok hareketi (DSH) ile güncellenir
    /// </summary>
    [JsonIgnore]
    public double? Miktar { get; set; }
}

/// <summary>
/// Luca/Koza Stok Kartı Güncelleme İsteği
/// Endpoint: /GuncelleStkWsSkart.do
/// </summary>
public class LucaUpdateStokKartiRequest
{
    /// <summary>
    /// Güncellenecek stok kartının Luca ID'si (ZORUNLU)
    /// </summary>
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }

    /// <summary>
    /// Hiyerarşik kod (opsiyonel)
    /// </summary>
    [JsonPropertyName("hiyerarsikKod")]
    public string? HiyerarsikKod { get; set; }

    /// <summary>
    /// Stok Kart Kodu (SKU)
    /// </summary>
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    /// <summary>
    /// Stok Kart Adı
    /// </summary>
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    /// <summary>
    /// Uzun Ad/Açıklama
    /// </summary>
    [JsonPropertyName("uzunAdi")]
    public string? UzunAdi { get; set; }

    /// <summary>
    /// Barkod
    /// </summary>
    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    /// <summary>
    /// Kategori Ağaç Kodu
    /// </summary>
    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    /// <summary>
    /// Perakende Alış Birim Fiyat
    /// </summary>
    [JsonPropertyName("perakendeAlisBirimFiyat")]
    public decimal? PerakendeAlisBirimFiyat { get; set; }

    /// <summary>
    /// Perakende Satış Birim Fiyat
    /// </summary>
    [JsonPropertyName("perakendeSatisBirimFiyat")]
    public decimal? PerakendeSatisBirimFiyat { get; set; }

    /// <summary>
    /// GTIP Kodu (Gümrük Tarife İstatistik Pozisyonu)
    /// </summary>
    [JsonPropertyName("gtipKodu")]
    public string? GtipKodu { get; set; }

    /// <summary>
    /// Kart Tipi (1=Stok)
    /// </summary>
    [JsonPropertyName("kartTipi")]
    public long? KartTipi { get; set; }

    /// <summary>
    /// Kart Türü (1=Stok Kartı)
    /// </summary>
    [JsonPropertyName("kartTuru")]
    public long? KartTuru { get; set; }

    /// <summary>
    /// Ölçü Birimi ID
    /// </summary>
    [JsonPropertyName("olcumBirimiId")]
    public long? OlcumBirimiId { get; set; }

    /// <summary>
    /// Alış KDV Oranı
    /// </summary>
    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

    /// <summary>
    /// Satış KDV Oranı
    /// </summary>
    [JsonPropertyName("kartSatisKdvOran")]
    public double? KartSatisKdvOran { get; set; }

    /// <summary>
    /// Satılabilir Flag (1=Evet)
    /// </summary>
    [JsonPropertyName("satilabilirFlag")]
    public int? SatilabilirFlag { get; set; }

    /// <summary>
    /// Satın Alınabilir Flag (1=Evet)
    /// </summary>
    [JsonPropertyName("satinAlinabilirFlag")]
    public int? SatinAlinabilirFlag { get; set; }

    /// <summary>
    /// Maliyet Hesaplanacak Flag
    /// </summary>
    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public bool? MaliyetHesaplanacakFlag { get; set; }

    /// <summary>
    /// Aktif durumu (1=Aktif, 0=Pasif/Kullanım Dışı)
    /// Soft delete için kullanılır
    /// </summary>
    [JsonPropertyName("aktif")]
    public int Aktif { get; set; } = 1;
}

/// <summary>
/// Luca/Koza Stok Kartı Silme İsteği
/// Endpoint: /SilStkSkart.do
/// </summary>
public class LucaDeleteStokKartiRequest
{
    /// <summary>
    /// Silinecek stok kartının Luca ID'si (ZORUNLU)
    /// </summary>
    [JsonPropertyName("skartId")]
    public long SkartId { get; set; }
}


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
    public long? GiderMerkezi { get; set; }

    [JsonPropertyName("uretimMerkeziKod")]
    public string? UretimMerkeziKod { get; set; }

    [JsonPropertyName("makineKod")]
    public string? MakineKod { get; set; }

    
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

public class LucaStockCardSummaryDto
{
    // 1) Koza/Luca'dan okurken kullanılan alanlar
    // ListeleStkSkart.do genelde stokKodu / stokAdi / anaBirimAdi / stokMiktari döner.

    [JsonPropertyName("stokKodu")]
    public string StokKodu { get; set; } = string.Empty;

    [JsonPropertyName("stokAdi")]
    public string StokAdi { get; set; } = string.Empty;

    [JsonPropertyName("anaBirimAdi")]
    public string Birim { get; set; } = string.Empty;

    [JsonPropertyName("stokMiktari")]
    public double Miktar { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    // Değişiklik tespiti için ek alanlar (Luca'da güncelleme olmadığı için karşılaştırma amaçlı)
    [JsonPropertyName("alisFiyat")]
    public decimal? AlisFiyat { get; set; }

    [JsonPropertyName("satisFiyat")]
    public decimal? SatisFiyat { get; set; }

    [JsonPropertyName("kartAlisKdvOran")]
    public double? AlisKdvOran { get; set; }

    [JsonPropertyName("kartSatisKdvOran")]
    public double? SatisKdvOran { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriKodu { get; set; }

    // 2) Frontend'e giderken kullanılan alanlar (React productCode/productName/unit/stockAmount bekliyor)

    [JsonPropertyName("productCode")]
    public string ProductCode => StokKodu;

    [JsonPropertyName("productName")]
    public string ProductName => StokAdi;

    [JsonPropertyName("unit")]
    public string Unit => Birim;

    [JsonPropertyName("stockAmount")]
    public double StockAmount => Miktar;

    // Geriye dönük uyumluluk için: eski kod bazı yerlerde Code/Name bekliyor.
    // Bunlar ProductCode/ProductName alias'ı olarak bırakıldı.
    [JsonIgnore]
    public string Code => ProductCode;

    [JsonIgnore]
    public string Name => ProductName;
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

    
}






public class LucaListCustomerAddressesRequest
{
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}




public class LucaGetCustomerWorkingConditionsRequest
{
    [JsonPropertyName("calismaKosulId")]
    public long CalismaKosulId { get; set; }
}




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




public class LucaGetCustomerRiskRequest
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaFinansalNesneKey GnlFinansalNesne { get; set; } = new();
}

public class LucaBelgeTurDetayRef
{
    [JsonPropertyName("belgeTurDetayId")]
    public long? BelgeTurDetayId { get; set; }
}

public class LucaOrgBelgeDateFilter
{
    [JsonPropertyName("belgeTarihiBas")]
    public string? BelgeTarihiBas { get; set; }

    [JsonPropertyName("belgeTarihiBit")]
    public string? BelgeTarihiBit { get; set; }

    [JsonPropertyName("belgeTarihiOp")]
    public string? BelgeTarihiOp { get; set; }

    [JsonPropertyName("gnlBelgeTurDetay")]
    public LucaBelgeTurDetayRef? GnlBelgeTurDetay { get; set; }
}

public class LucaFinCariHareketBaslikFilter
{
    [JsonPropertyName("parCariKartTuru")]
    public int? ParCariKartTuru { get; set; }

    [JsonPropertyName("gnlOrgSsBelge")]
    public LucaOrgBelgeDateFilter? GnlOrgSsBelge { get; set; }
}

public class LucaListCariHareketBaslikRequest
{
    [JsonPropertyName("finCariHareketBaslik")]
    public LucaFinCariHareketBaslikFilter? FinCariHareketBaslik { get; set; }

    [JsonPropertyName("parCariKartKoduBas")]
    public string? ParCariKartKoduBas { get; set; }

    [JsonPropertyName("parCariKartKoduBit")]
    public string? ParCariKartKoduBit { get; set; }

    [JsonPropertyName("parCariKartKoduOp")]
    public string? ParCariKartKoduOp { get; set; }
}

public class LucaListOzelCariHareketBaslikRequest
{
    [JsonPropertyName("finOzelCariHareketBaslik")]
    public JsonElement? FinOzelCariHareketBaslik { get; set; }
}




public class LucaCreateCustomerContractRequest
{
    [JsonPropertyName("finMusteriSozlesme")]
    public JsonElement? FinMusteriSozlesme { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}





public class LucaStockCardAutoCompleteRequest
{
    [JsonPropertyName("kartTuru")]
    public int? KartTuru { get; set; }

    [JsonPropertyName("q")]
    public string? Query { get; set; }

    [JsonPropertyName("pageNo")]
    public int? PageNo { get; set; }

    [JsonPropertyName("pageSize")]
    public int? PageSize { get; set; }

    [JsonPropertyName("autoComplete")]
    public int? AutoComplete { get; set; }

    [JsonPropertyName("displayTagSize")]
    public int? DisplayTagSize { get; set; }
}





public class LucaUtsTransmitRequest
{
    [JsonPropertyName("ssFaturaDetayId")]
    public long SsFaturaDetayId { get; set; }

    [JsonPropertyName("iletimTarihi")]
    public string IletimTarihi { get; set; } = string.Empty;
}



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

public class LucaCreateCariHareketRequest
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
    
    [JsonPropertyName("cariTuru")]
    public int CariTuru { get; set; }

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("cariKodu")]
    public string CariKodu { get; set; } = string.Empty;

    [JsonPropertyName("detayList")]
    public List<LucaCreateCariHareketDetayRequest> DetayList { get; set; } = new();
}

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
    
    [JsonPropertyName("ozelKod")]
    public string? OzelKod { get; set; }
    
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("iletisimListesi")]
    public List<LucaContactInfoDto>? IletisimListesi { get; set; }
}

public class LucaContactInfoDto
{
    [JsonPropertyName("iletisimTipId")]
    public long? IletisimTipId { get; set; }

    [JsonPropertyName("iletisimTanim")]
    public string? IletisimTanim { get; set; }
}

/// <summary>
/// Luca cari kart tam güncelleme isteği
/// </summary>
public class LucaUpdateCustomerFullRequest
{
    [JsonPropertyName("kartKod")]
    public string KartKod { get; set; } = string.Empty;
    
    [JsonPropertyName("tip")]
    public int Tip { get; set; } = 1;
    
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
    
    [JsonPropertyName("ad")]
    public string? Ad { get; set; }
    
    [JsonPropertyName("soyad")]
    public string? Soyad { get; set; }
    
    [JsonPropertyName("ulke")]
    public string? Ulke { get; set; }
    
    [JsonPropertyName("il")]
    public string? Il { get; set; }
    
    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }
    
    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }
    
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("ozelKod")]
    public string? OzelKod { get; set; }
    
    [JsonPropertyName("kategoriKod")]
    public string? KategoriKod { get; set; }
}

/// <summary>
/// Luca cari kart kod bazlı arama filtresi
/// </summary>
public class LucaCariKartFilter
{
    [JsonPropertyName("kodBas")]
    public string? KodBas { get; set; }
    
    [JsonPropertyName("kodBit")]
    public string? KodBit { get; set; }
    
    [JsonPropertyName("kodOp")]
    public string? KodOp { get; set; } = "between";
}

/// <summary>
/// Luca cari kart listesi isteği
/// </summary>
public class LucaListCariKartRequest
{
    [JsonPropertyName("finMusteri")]
    public LucaCariKartListFilter? FinMusteri { get; set; }
}

public class LucaCariKartListFilter
{
    [JsonPropertyName("gnlFinansalNesne")]
    public LucaCariKartFilter? GnlFinansalNesne { get; set; }
}

public class LucaCreateSupplierRequest : LucaCreateCustomerRequest
{
    public LucaCreateSupplierRequest()
    {
        
        CariTipId = 2;
    }
}


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
    public int? OpsiyonTarihiFlag { get; set; }

    [JsonPropertyName("opsiyonTarihi")]
    public DateTime? OpsiyonTarihi { get; set; }

    [JsonPropertyName("teslimTarihiFlag")]
    public int? TeslimTarihiFlag { get; set; }

    [JsonPropertyName("teslimTarihi")]
    public DateTime? TeslimTarihi { get; set; }

    [JsonPropertyName("onayFlag")]
    public int? OnayFlag { get; set; }

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


public class LucaDeleteOrderRequest
{
    [JsonPropertyName("ssSiparisBaslikId")]
    public long SsSiparisBaslikId { get; set; }
}


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
    [JsonPropertyName("detayList")]
    public List<LucaSalesOrderDetailToDelete> DetayList { get; set; } = new();
}

public class LucaSalesOrderDetailToDelete
{
    [JsonPropertyName("detayId")]
    public long DetayId { get; set; }
}

public class LucaCreatePurchaseOrderRequest
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
    public int? OpsiyonTarihiFlag { get; set; }

    [JsonPropertyName("opsiyonTarihi")]
    public DateTime? OpsiyonTarihi { get; set; }

    [JsonPropertyName("teslimTarihiFlag")]
    public int? TeslimTarihiFlag { get; set; }

    [JsonPropertyName("teslimTarihi")]
    public DateTime? TeslimTarihi { get; set; }

    [JsonPropertyName("onayFlag")]
    public int? OnayFlag { get; set; }

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

    [JsonPropertyName("ozelKod")]
    public string? OzelKod { get; set; }

    [JsonPropertyName("projeKodu")]
    public string? ProjeKodu { get; set; }

    [JsonPropertyName("sevkAdresiId")]
    public long? SevkAdresiId { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreatePurchaseOrderDetailRequest> DetayList { get; set; } = new();
}

public class LucaCreatePurchaseOrderDetailRequest
{
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; }

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    [JsonPropertyName("depoKodu")]
    public string? DepoKodu { get; set; }

    [JsonPropertyName("birimKodu")]
    public string? BirimKodu { get; set; }

    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    [JsonPropertyName("birimFiyat")]
    public double BirimFiyat { get; set; }

    [JsonPropertyName("kdvOran")]
    public double KdvOran { get; set; }

    [JsonPropertyName("iskontoTutar")]
    public double? IskontoTutar { get; set; }

    [JsonPropertyName("tutar")]
    public double? Tutar { get; set; }
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
    [JsonPropertyName("detayList")]
    public List<LucaPurchaseOrderDetailToDelete> DetayList { get; set; } = new();
}

public class LucaPurchaseOrderDetailToDelete
{
    [JsonPropertyName("detayId")]
    public long DetayId { get; set; }
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
    [JsonPropertyName("belgeSeri")]
    public string? BelgeSeri { get; set; }

    [JsonPropertyName("belgeNo")]
    public int? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; }

    [JsonPropertyName("belgeTakipNo")]
    public string? BelgeTakipNo { get; set; }

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    [JsonPropertyName("belgeTurDetayId")]
    public long BelgeTurDetayId { get; set; }

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

    [JsonPropertyName("sayim1Miktar")]
    public double? Sayim1Miktar { get; set; }

    [JsonPropertyName("sayim2Miktar")]
    public double? Sayim2Miktar { get; set; }

    [JsonPropertyName("sayim3Miktar")]
    public double? Sayim3Miktar { get; set; }

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

    // Backward compatibility - eski kod için
    [JsonPropertyName("sayimMiktari")]
    public decimal? SayimMiktari { get; set; }
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

    [JsonPropertyName("belgeTakipNo")]
    public string? BelgeTakipNo { get; set; }

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

    
    
    
    [JsonPropertyName("hareketYonu")]
    public string? HareketYonu { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateDshDetayRequest> DetayList { get; set; } = new();

    
    
    
    [JsonPropertyName("uretimeGirisBaslik")]
    public LucaCreateDshBaslikRequest? UretimeGirisBaslik { get; set; }
}






public class LucaListIrsaliyeRequest
{
    [JsonPropertyName("stkSsIrsaliyeBaslik")]
    public JsonElement? StkSsIrsaliyeBaslik { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}


/// <summary>
/// Şoför bilgisi (E-İrsaliye için)
/// </summary>
public class LucaSoforDto
{
    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("soyad")]
    public string Soyad { get; set; } = string.Empty;

    [JsonPropertyName("tckn")]
    public string Tckn { get; set; } = string.Empty;
}

/// <summary>
/// Dorse bilgisi (E-İrsaliye için)
/// </summary>
public class LucaDorseDto
{
    [JsonPropertyName("plaka")]
    public string Plaka { get; set; } = string.Empty;
}

public class LucaGetEirsaliyeXmlRequest
{
    [JsonPropertyName("eirsaliyeId")]
    public long EirsaliyeId { get; set; }
}

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




public class LucaCreateIrsaliyeBaslikRequest
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
    public List<LucaSoforDto>? SoforListesi { get; set; }

    [JsonPropertyName("dorseListesi")]
    public List<LucaDorseDto>? DorseListesi { get; set; }

    [JsonPropertyName("detayList")]
    public List<LucaCreateIrsaliyeDetayRequest> DetayList { get; set; } = new();
}




public class LucaDeleteIrsaliyeRequest
{
    [JsonPropertyName("ssIrsaliyeBaslikId")]
    public long SsIrsaliyeBaslikId { get; set; }
}




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




public class LucaResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}


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

/// <summary>
/// Luca Adres DTO - Müşteri adresi için
/// </summary>
public class LucaAdresDto
{
    /// <summary>Adres tipi: 1=Fatura, 2=Teslimat</summary>
    [JsonPropertyName("adresTipId")]
    public int AdresTipId { get; set; } = 1;
    
    [JsonPropertyName("adresSerbest")]
    public string? AdresSerbest { get; set; }
    
    [JsonPropertyName("adres2")]
    public string? Adres2 { get; set; }
    
    /// <summary>Luca'da şehir = ilçe olarak gönderilir</summary>
    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }
    
    [JsonPropertyName("postaKodu")]
    public string? PostaKodu { get; set; }
    
    [JsonPropertyName("ulke")]
    public string Ulke { get; set; } = "TURKIYE";
    
    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
    
    /// <summary>Varsayılan adres mi: 1=Evet, 0=Hayır</summary>
    [JsonPropertyName("varsayilanFlag")]
    public int VarsayilanFlag { get; set; } = 1;
    
    /// <summary>FinansalNesneId - cari kart ID</summary>
    [JsonPropertyName("finansalNesneId")]
    public long FinansalNesneId { get; set; }
}

/// <summary>
/// Müşteri sync sonucu - CustomerDto'ya ek Luca alanları
/// </summary>
public class CustomerLucaSyncInfo
{
    public string? LucaCode { get; set; }
    public long? LucaFinansalNesneId { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsSynced => LucaFinansalNesneId.HasValue && string.IsNullOrEmpty(LastSyncError);
    public string SyncStatus => IsSynced ? "success" : (string.IsNullOrEmpty(LastSyncError) ? "pending" : "error");
}

#region Stock Transfer & Adjustment DTOs

/// <summary>
/// Luca Depo Transferi Request - EkleStkWsDtransferBaslik.do için
/// Belge Tür Detay ID: 33 = Standart Depo Transferi
/// </summary>
public class LucaStockTransferRequest
{
    [JsonPropertyName("stkDepoTransferBaslik")]
    public LucaTransferHeader StkDepoTransferBaslik { get; set; } = new();
}

/// <summary>
/// Depo Transfer Başlık Bilgileri
/// </summary>
public class LucaTransferHeader
{
    [JsonPropertyName("belgeSeri")]
    public string BelgeSeri { get; set; } = "TRF";

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    /// <summary>
    /// Belge Türü: 33 = Standart Depo Transferi
    /// </summary>
    [JsonPropertyName("belgeTurDetayId")]
    public int BelgeTurDetayId { get; set; } = 33;

    /// <summary>
    /// Çıkış Depo Kodu (Kaynak)
    /// </summary>
    [JsonPropertyName("cikisDepoKodu")]
    public string CikisDepoKodu { get; set; } = string.Empty;

    /// <summary>
    /// Giriş Depo Kodu (Hedef)
    /// </summary>
    [JsonPropertyName("girisDepoKodu")]
    public string GirisDepoKodu { get; set; } = string.Empty;

    [JsonPropertyName("detayList")]
    public List<LucaStockMovementRow> DetayList { get; set; } = new();
}

/// <summary>
/// Luca Stok Hareket Fişi (DSH) Request - EkleStkWsDshBaslik.do için
/// Fire, Sarf, Sayım Fazlası gibi stok düzeltmeleri için kullanılır
/// </summary>
public class LucaStockVoucherRequest
{
    [JsonPropertyName("stkDshBaslik")]
    public LucaVoucherHeader StkDshBaslik { get; set; } = new();
}

/// <summary>
/// Stok Hareket Fişi Başlık Bilgileri
/// </summary>
public class LucaVoucherHeader
{
    [JsonPropertyName("belgeSeri")]
    public string BelgeSeri { get; set; } = "DSH";

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("belgeTarihi")]
    public DateTime BelgeTarihi { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("belgeAciklama")]
    public string? BelgeAciklama { get; set; }

    /// <summary>
    /// Belge Türü Detay ID:
    /// - 150: Fire Fişi (Stok Azalış)
    /// - 151: Sayım Fazlası (Stok Artış)
    /// - 152: Sarf Fişi
    /// - 153: Devir Fişi
    /// </summary>
    [JsonPropertyName("belgeTurDetayId")]
    public int BelgeTurDetayId { get; set; }

    /// <summary>
    /// İşlemin yapıldığı depo
    /// </summary>
    [JsonPropertyName("depoKodu")]
    public string DepoKodu { get; set; } = string.Empty;

    [JsonPropertyName("paraBirimKod")]
    public string ParaBirimKod { get; set; } = "TRY";

    [JsonPropertyName("detayList")]
    public List<LucaStockMovementRow> DetayList { get; set; } = new();
}

/// <summary>
/// Transfer ve DSH için ortak satır nesnesi
/// </summary>
public class LucaStockMovementRow
{
    /// <summary>
    /// Kart Türü: 1 = Stok Kartı
    /// </summary>
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1;

    /// <summary>
    /// Stok Kart Kodu (Luca'daki kod)
    /// </summary>
    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    [JsonPropertyName("kartAdi")]
    public string? KartAdi { get; set; }

    /// <summary>
    /// Transfer/Hareket Miktarı (Her zaman pozitif)
    /// </summary>
    [JsonPropertyName("miktar")]
    public double Miktar { get; set; }

    /// <summary>
    /// Ölçü Birimi ID
    /// </summary>
    [JsonPropertyName("olcuBirimi")]
    public long? OlcuBirimi { get; set; }

    /// <summary>
    /// Birim Fiyat - DSH (Adjustment) için zorunlu, Transfer için opsiyonel
    /// </summary>
    [JsonPropertyName("birimFiyat")]
    public double? BirimFiyat { get; set; }

    [JsonPropertyName("aciklama")]
    public string? Aciklama { get; set; }

    [JsonPropertyName("lotNo")]
    public string? LotNo { get; set; }

    [JsonPropertyName("seriNo")]
    public string? SeriNo { get; set; }
}

/// <summary>
/// Stok Hareketi Belge Türleri
/// </summary>
public static class LucaStockMovementTypes
{
    /// <summary>Depo Transferi</summary>
    public const int DepoTransferi = 33;

    /// <summary>Fire Fişi - Stok Azalış (Negatif Adjustment)</summary>
    public const int FireFisi = 150;

    /// <summary>Sayım Fazlası - Stok Artış (Pozitif Adjustment)</summary>
    public const int SayimFazlasi = 151;

    /// <summary>Sarf Fişi</summary>
    public const int SarfFisi = 152;

    /// <summary>Devir Fişi</summary>
    public const int DevirFisi = 153;

    /// <summary>
    /// Miktar yönüne göre uygun belge türünü döner
    /// </summary>
    public static int GetAdjustmentType(double quantity, string? reason = null)
    {
        // Negatif = Stok Azalması
        if (quantity < 0)
        {
            // Reason bazlı seçim yapılabilir
            if (!string.IsNullOrEmpty(reason) && reason.ToLowerInvariant().Contains("sarf"))
                return SarfFisi;
            return FireFisi;
        }

        // Pozitif = Stok Artışı
        if (!string.IsNullOrEmpty(reason) && reason.ToLowerInvariant().Contains("devir"))
            return DevirFisi;
        return SayimFazlasi;
    }
}

#endregion


#region Stock Card Create V2 DTOs

/// <summary>
/// Luca API'ye stok kartı oluşturma isteği - Yeni format
/// Örnek: {"kartAdi": "Test Ürünü", "kartKodu": "00013225", "baslangicTarihi": "06/04/2022", "kartTuru": 1, ...}
/// </summary>
public class LucaCreateStockCardRequestV2
{
    // Required fields
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; } = string.Empty;

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; } = string.Empty;

    /// <summary>
    /// Başlangıç tarihi - dd/MM/yyyy formatında
    /// </summary>
    [JsonPropertyName("baslangicTarihi")]
    public string BaslangicTarihi { get; set; } = string.Empty;

    /// <summary>
    /// Kart türü: 1=Stok, 2=Hizmet
    /// </summary>
    [JsonPropertyName("kartTuru")]
    public int KartTuru { get; set; } = 1;

    // Optional fields
    [JsonPropertyName("kartTipi")]
    public int? KartTipi { get; set; }

    [JsonPropertyName("kartAlisKdvOran")]
    public double? KartAlisKdvOran { get; set; }

    [JsonPropertyName("olcumBirimiId")]
    public int? OlcumBirimiId { get; set; }

    [JsonPropertyName("kategoriAgacKod")]
    public string? KategoriAgacKod { get; set; }

    [JsonPropertyName("barkod")]
    public string? Barkod { get; set; }

    // Tevkifat fields
    /// <summary>
    /// Alış tevkifat oranı - örn: "7/10"
    /// </summary>
    [JsonPropertyName("alisTevkifatOran")]
    public string? AlisTevkifatOran { get; set; }

    /// <summary>
    /// Satış tevkifat oranı - örn: "2/10"
    /// </summary>
    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }

    [JsonPropertyName("alisTevkifatTipId")]
    public int? AlisTevkifatTipId { get; set; }

    [JsonPropertyName("satisTevkifatTipId")]
    public int? SatisTevkifatTipId { get; set; }

    // Boolean flags with defaults
    /// <summary>
    /// Satılabilir mi - varsayılan: 1 (true)
    /// </summary>
    [JsonPropertyName("satilabilirFlag")]
    public int SatilabilirFlag { get; set; } = 1;

    /// <summary>
    /// Satın alınabilir mi - varsayılan: 1 (true)
    /// </summary>
    [JsonPropertyName("satinAlinabilirFlag")]
    public int SatinAlinabilirFlag { get; set; } = 1;

    /// <summary>
    /// Lot no takibi - varsayılan: 0 (false)
    /// </summary>
    [JsonPropertyName("lotNoFlag")]
    public int LotNoFlag { get; set; } = 0;

    [JsonPropertyName("minStokKontrol")]
    public int MinStokKontrol { get; set; } = 0;

    /// <summary>
    /// Maliyet hesaplanacak mı - varsayılan: true
    /// </summary>
    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public bool MaliyetHesaplanacakFlag { get; set; } = true;
}

/// <summary>
/// Luca API stok kartı oluşturma yanıtı
/// Örnek: {"skartId": 79409, "error": false, "message": "00013225 - Test Ürünü stok kartı başarılı bir şekilde kaydedilmiştir."}
/// </summary>
public class LucaCreateStockCardResponse
{
    [JsonPropertyName("skartId")]
    public long? SkartId { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Fatura Additional Response DTOs

/// <summary>
/// Fatura Oluşturma Yanıtı
/// </summary>
public class LucaCreateInvoiceResponse
{
    [JsonPropertyName("ssFaturaBaslikId")]
    public long? SsFaturaBaslikId { get; set; }

    [JsonPropertyName("belgeNo")]
    public long? BelgeNo { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Fatura PDF Link Yanıtı
/// </summary>
public class LucaInvoicePdfLinkResponse
{
    [JsonPropertyName("pdfLink")]
    public string? PdfLink { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Fatura Kapama Yanıtı
/// </summary>
public class LucaCloseInvoiceResponse
{
    [JsonPropertyName("kapamaId")]
    public long? KapamaId { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Fatura Silme Yanıtı
/// </summary>
public class LucaDeleteInvoiceResponse
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Fatura Gönder İsteği (E-Fatura/E-Arşiv için)
/// </summary>
public class LucaSendInvoiceRequest
{
    [JsonPropertyName("ssFaturaBaslikId")]
    public long SsFaturaBaslikId { get; set; }

    [JsonPropertyName("gonderimTipi")]
    public string GonderimTipi { get; set; } = "ELEKTRONIK"; // ELEKTRONIK, KAGIT
}

/// <summary>
/// Fatura Gönder Yanıtı
/// </summary>
public class LucaSendInvoiceResponse
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Döviz Raporu için kullanılan yardımcı DTO
/// </summary>
public class LucaCurrencyReport
{
    [JsonPropertyName("paraBirimId")]
    public int ParaBirimId { get; set; } = 4; // 4: USD, 2: EUR, 1: TRY
}

#endregion
