using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Katana.Core.DTOs;
using Katana.Business.Mappers;
using Katana.Core.Constants;

namespace Katana.Infrastructure.Helpers;

public static class LucaRequestFactory
{
    public static LucaCreateStokKartiRequest CreateSampleStokKarti(
        string kartAdi = "ÜRÜN ADI",
        long kartTuru = 1,
        long olcumBirimiId = 5,
        string kartKodu = "PROD001",
        string kategoriAgacKod = "0000",
        double kartAlisKdvOran = 0.18,
        double kartSatisKdvOran = 0.18,
        double perakendeAlisBirimFiyat = 100.00,
        double perakendeSatisBirimFiyat = 150.00,
        long kartTipi = 4,
        bool maliyetHesaplanacakFlag = true,
        bool satilabilirFlag = true,
        bool satinAlinabilirFlag = true)
    {
        return new LucaCreateStokKartiRequest
        {
            KartAdi = kartAdi,
            KartTuru = kartTuru,
            OlcumBirimiId = olcumBirimiId,
            KartKodu = kartKodu,
            KategoriAgacKod = kategoriAgacKod,
            KartAlisKdvOran = kartAlisKdvOran,
            KartSatisKdvOran = kartSatisKdvOran,
            PerakendeAlisBirimFiyat = perakendeAlisBirimFiyat,
            PerakendeSatisBirimFiyat = perakendeSatisBirimFiyat,
            KartTipi = kartTipi,
            MaliyetHesaplanacakFlag = maliyetHesaplanacakFlag ? 1 : 0,
            SatilabilirFlag = satilabilirFlag ? 1 : 0,
            SatinAlinabilirFlag = satinAlinabilirFlag ? 1 : 0,
            BaslangicTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            UzunAdi = kartAdi
        };
    }

    public static LucaCreateInvoiceHeaderRequest CreateSampleInvoiceHeader(
        DateTime? belgeTarihi = null,
        long belgeTurDetayId = KozaBelgeTurleri.MalSatisFaturasi,
        string belgeSeri = "A",
        int? belgeNo = null,
        string? belgeTakipNo = null,
        string? belgeAciklama = null,
        int faturaTur = 1,
        string cariKodu = "CUST_123",
        int musteriTedarikci = 1,
        string paraBirimKod = "TRY",
        double kurBedeli = 1.0,
        bool kdvFlag = true,
        string productKartKodu = "PROD001",
        double miktar = 10,
        double birimFiyat = 150.00,
        double kdvOran = 0.18)
    {
        var header = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = belgeNo,
            BelgeTarihi = belgeTarihi ?? DateTime.UtcNow,
            BelgeTurDetayId = belgeTurDetayId,
            BelgeTakipNo = belgeTakipNo,
            BelgeAciklama = belgeAciklama ?? $"Sample invoice {Guid.NewGuid():N}",
            FaturaTur = faturaTur,
            CariKodu = cariKodu,
            MusteriTedarikci = musteriTedarikci,
            ParaBirimKod = paraBirimKod,
            KurBedeli = kurBedeli,
            KdvFlag = kdvFlag,
            DetayList = new List<LucaCreateInvoiceDetailRequest>
            {
                new LucaCreateInvoiceDetailRequest
                {
                    KartTuru = 1,
                    KartKodu = productKartKodu,
                    Miktar = miktar,
                    BirimFiyat = birimFiyat,
                    KdvOran = kdvOran
                }
            }
        };

        return header;
    }
    public static LucaCreateDshBaslikRequest CreateSampleOtherStockMovement(
        DateTime? belgeTarihi = null,
        long belgeTurDetayId = KozaBelgeTurleri.DigerStokGiris,
        string depoKodu = "002", // Luca MERKEZ DEPO kodu
        int kartTuru = 1,
        string kartKodu = "PROD001",
        double miktar = 10,
        double birimFiyat = 100.00)
    {
        var detay = new LucaCreateDshDetayRequest
        {
            KartKodu = kartKodu,
            Miktar = miktar,
            BirimFiyat = birimFiyat
        };

        var req = new LucaCreateDshBaslikRequest
        {
            BelgeTarihi = belgeTarihi ?? DateTime.UtcNow,
            BelgeTurDetayId = belgeTurDetayId,
            DepoKodu = depoKodu,
            DetayList = new List<LucaCreateDshDetayRequest> { detay }
        };

        return req;
    }

    public static FormUrlEncodedContent CreateStockCardRequestFromExcel(
        ExcelProductDto dto,
        double defaultVatRate = 0.20,
        long defaultOlcumBirimiId = 5,
        long defaultKartTipi = 4,
        string defaultKategoriKod = "001")
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var card = KatanaToLucaMapper.MapFromExcelRow(
            dto,
            defaultVatRate,
            defaultOlcumBirimiId,
            defaultKartTipi,
            defaultKategoriKod);

        var payload = new Dictionary<string, string>
        {
            { "kartAdi", card.KartAdi },
            { "kartKodu", card.KartKodu },
            { "kartTipi", card.KartTipi.ToString(CultureInfo.InvariantCulture) },
            { "kartAlisKdvOran", card.KartAlisKdvOran.ToString(CultureInfo.InvariantCulture) },
            { "kartSatisKdvOran", card.KartSatisKdvOran.ToString(CultureInfo.InvariantCulture) },
            { "olcumBirimiId", card.OlcumBirimiId.ToString(CultureInfo.InvariantCulture) },
            {
                "baslangicTarihi",
                card.BaslangicTarihi?.ToString() ??
                DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            },
            { "kartTuru", card.KartTuru.ToString(CultureInfo.InvariantCulture) },
            { "kategoriAgacKod", card.KategoriAgacKod ?? string.Empty },
            { "satilabilirFlag", card.SatilabilirFlag.ToString(CultureInfo.InvariantCulture) },
            { "satinAlinabilirFlag", card.SatinAlinabilirFlag.ToString(CultureInfo.InvariantCulture) },
            { "lotNoFlag", card.LotNoFlag.ToString(CultureInfo.InvariantCulture) },
            { "minStokKontrol", card.MinStokKontrol.ToString(CultureInfo.InvariantCulture) },
            { "maliyetHesaplanacakFlag", card.MaliyetHesaplanacakFlag.ToString(CultureInfo.InvariantCulture) },
            { "barkod", card.Barkod ?? string.Empty },
            { "uzunAdi", card.UzunAdi ?? card.KartAdi }
        };

        return new FormUrlEncodedContent(payload);
    }

    /// <summary>
    /// Excel ürün listesi için form-encoded istekleri üretir.
    /// </summary>
    public static List<FormUrlEncodedContent> CreateBatchStockCardRequests(
        List<ExcelProductDto> dtos,
        double defaultVatRate = 0.20,
        long defaultOlcumBirimiId = 5,
        long defaultKartTipi = 4,
        string defaultKategoriKod = "001")
    {
        var list = new List<FormUrlEncodedContent>();
        if (dtos == null) return list;

        foreach (var dto in dtos)
        {
            list.Add(CreateStockCardRequestFromExcel(
                dto,
                defaultVatRate,
                defaultOlcumBirimiId,
                defaultKartTipi,
                defaultKategoriKod));
        }

        return list;
    }
}
