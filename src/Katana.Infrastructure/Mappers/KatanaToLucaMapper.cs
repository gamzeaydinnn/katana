using System;
using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Infrastructure.Mappers;

public static class KatanaToLucaMapper
{
    public static LucaStockCardDto MapToLucaStockCard(
        Product katanaProduct,
        long defaultOlcumBirimiId = 5,
        string? kategoriKodu = null)
    {
        return new LucaStockCardDto
        {
            KartAdi = katanaProduct.Name,
            KartTuru = 1,
            KartKodu = katanaProduct.SKU,
            OlcumBirimiId = defaultOlcumBirimiId,
            BaslangicTarihi = DateTime.Now,
            KartAlisKdvOran = 0.20,
            KartSatisKdvOran = 0.20,
            KategoriAgacKod = kategoriKodu,
            KartTipi = 4,
            Barkod = katanaProduct.SKU,
            DetayAciklama = katanaProduct.Description,
            UzunAdi = katanaProduct.Name.Length > 50
                ? katanaProduct.Name.Substring(0, 50)
                : katanaProduct.Name,
            SatilabilirFlag = true,
            SatinAlinabilirFlag = true,
            MaliyetHesaplanacakFlag = true
        };
    }

    public static LucaInvoiceDto MapToLucaInvoice(
        object katanaInvoice,
        string belgeSeri,
        long belgeTurDetayId)
    {
        // TODO: Katana'dan fatura entity'si geldiğinde implement et
        throw new NotImplementedException();
    }
}
