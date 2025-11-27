using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Katana.Core.DTOs;
using Katana.Core.Constants;

namespace Katana.Core.Helper
{
    /// <summary>
    /// Helper factory to create sample Luca request DTOs populated with required fields.
    /// Use these helpers as a starting point when assembling payloads to send to Luca/Koza.
    /// </summary>
    public static class LucaRequestFactory
    {
        /// <summary>
        /// Creates a sample `LucaCreateStokKartiRequest` populated with the typical required fields.
        /// Customize values as needed before sending to the API.
        /// </summary>
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
                // sensible defaults for optional fields if not provided
                BaslangicTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                UzunAdi = kartAdi,
            };
        }

        /// <summary>
        /// Creates a sample `LucaCreateInvoiceHeaderRequest` populated with required fields and one detail line.
        /// Adjust or expand the `DetayList` as needed for real invoices.
        /// </summary>
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

        /// <summary>
        /// Creates a sample `LucaCreateDshBaslikRequest` (Other Stock Movement) populated with required fields and one detail line.
        /// </summary>
        public static LucaCreateDshBaslikRequest CreateSampleOtherStockMovement(
            DateTime? belgeTarihi = null,
            long belgeTurDetayId = KozaBelgeTurleri.DigerStokGiris,
            string depoKodu = "0001-0001",
            int kartTuru = 1,
            string kartKodu = "PROD001",
            double miktar = 10,
            double birimFiyat = 100.00)
        {
            var detay = new LucaCreateDshDetayRequest
            {
                //KartTuru = kartTuru,
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

    }
}
