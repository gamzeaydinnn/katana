namespace Katana.Core.Utilities;

/// <summary>
/// Koza belge türü validation
/// Yanlış belge türü seçimi kontrolü
/// </summary>
public static class BelgeTuruValidator
{
    // Fatura belge türleri
    public static class Fatura
    {
        public const long MalSatisFaturasi = 68;
        public const long HizmetSatisFaturasi = 70;
        public const long MalAlimFaturasi = 69;
        public const long HizmetAlimFaturasi = 71;
        public const long SatisIadeFaturasi = 72;
        public const long AlimIadeFaturasi = 73;
        public const long ProformaSatisFaturasi = 74;
        public const long ProformaAlimFaturasi = 75;
    }

    // İrsaliye belge türleri
    public static class Irsaliye
    {
        public const long SatisIrsaliyesi = 1;
        public const long AlimIrsaliyesi = 2;
        public const long SatisIadeIrsaliyesi = 3;
        public const long AlimIadeIrsaliyesi = 4;
    }

    // Stok hareketi belge türleri
    public static class StokHareketi
    {
        public const long DepoTransferi = 33;
        public const long Fire = 34;
        public const long Sarf = 35;
        public const long SayimFazlasi = 36;
        public const long SayimEksigi = 37;
        public const long DigerGiris = 38;
        public const long DigerCikis = 39;
    }

    // Sayım belge türleri
    public static class Sayim
    {
        public const long SayimSonucFisi = 40;
    }

    /// <summary>
    /// Belge türünün fatura olup olmadığını kontrol et
    /// </summary>
    public static bool IsFatura(long belgeTurDetayId)
    {
        return belgeTurDetayId >= 68 && belgeTurDetayId <= 75;
    }

    /// <summary>
    /// Belge türünün irsaliye olup olmadığını kontrol et
    /// </summary>
    public static bool IsIrsaliye(long belgeTurDetayId)
    {
        return belgeTurDetayId >= 1 && belgeTurDetayId <= 4;
    }

    /// <summary>
    /// Belge türünün stok hareketi olup olmadığını kontrol et
    /// </summary>
    public static bool IsStokHareketi(long belgeTurDetayId)
    {
        return belgeTurDetayId >= 33 && belgeTurDetayId <= 39;
    }

    /// <summary>
    /// Belge türünün sayım olup olmadığını kontrol et
    /// </summary>
    public static bool IsSayim(long belgeTurDetayId)
    {
        return belgeTurDetayId == 40;
    }

    /// <summary>
    /// Belge türünün satış yönlü olup olmadığını kontrol et
    /// </summary>
    public static bool IsSatis(long belgeTurDetayId)
    {
        return belgeTurDetayId == Fatura.MalSatisFaturasi ||
               belgeTurDetayId == Fatura.HizmetSatisFaturasi ||
               belgeTurDetayId == Fatura.ProformaSatisFaturasi ||
               belgeTurDetayId == Irsaliye.SatisIrsaliyesi;
    }

    /// <summary>
    /// Belge türünün alım yönlü olup olmadığını kontrol et
    /// </summary>
    public static bool IsAlim(long belgeTurDetayId)
    {
        return belgeTurDetayId == Fatura.MalAlimFaturasi ||
               belgeTurDetayId == Fatura.HizmetAlimFaturasi ||
               belgeTurDetayId == Fatura.ProformaAlimFaturasi ||
               belgeTurDetayId == Irsaliye.AlimIrsaliyesi;
    }

    /// <summary>
    /// Belge türünün iade olup olmadığını kontrol et
    /// </summary>
    public static bool IsIade(long belgeTurDetayId)
    {
        return belgeTurDetayId == Fatura.SatisIadeFaturasi ||
               belgeTurDetayId == Fatura.AlimIadeFaturasi ||
               belgeTurDetayId == Irsaliye.SatisIadeIrsaliyesi ||
               belgeTurDetayId == Irsaliye.AlimIadeIrsaliyesi;
    }

    /// <summary>
    /// Belge türü validation - geçerli mi?
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) Validate(long belgeTurDetayId, string documentType)
    {
        switch (documentType.ToUpperInvariant())
        {
            case "FATURA":
            case "INVOICE":
                if (!IsFatura(belgeTurDetayId))
                {
                    return (false, $"Belge türü {belgeTurDetayId} fatura için geçerli değil. Fatura belge türleri: 68-75");
                }
                break;

            case "IRSALIYE":
            case "WAYBILL":
                if (!IsIrsaliye(belgeTurDetayId))
                {
                    return (false, $"Belge türü {belgeTurDetayId} irsaliye için geçerli değil. İrsaliye belge türleri: 1-4");
                }
                break;

            case "STOK_HAREKETI":
            case "STOCK_MOVEMENT":
                if (!IsStokHareketi(belgeTurDetayId))
                {
                    return (false, $"Belge türü {belgeTurDetayId} stok hareketi için geçerli değil. Stok hareketi belge türleri: 33-39");
                }
                break;

            case "SAYIM":
            case "STOCKTAKE":
                if (!IsSayim(belgeTurDetayId))
                {
                    return (false, $"Belge türü {belgeTurDetayId} sayım için geçerli değil. Sayım belge türü: 40");
                }
                break;

            default:
                return (false, $"Bilinmeyen belge tipi: {documentType}");
        }

        return (true, null);
    }

    /// <summary>
    /// Belge türü validation - exception fırlat
    /// </summary>
    public static void ValidateOrThrow(long belgeTurDetayId, string documentType)
    {
        var (isValid, errorMessage) = Validate(belgeTurDetayId, documentType);
        
        if (!isValid)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Belge türü açıklaması al
    /// </summary>
    public static string GetDescription(long belgeTurDetayId)
    {
        return belgeTurDetayId switch
        {
            Fatura.MalSatisFaturasi => "Mal Satış Faturası",
            Fatura.HizmetSatisFaturasi => "Hizmet Satış Faturası",
            Fatura.MalAlimFaturasi => "Mal Alım Faturası",
            Fatura.HizmetAlimFaturasi => "Hizmet Alım Faturası",
            Fatura.SatisIadeFaturasi => "Satış İade Faturası",
            Fatura.AlimIadeFaturasi => "Alım İade Faturası",
            Fatura.ProformaSatisFaturasi => "Proforma Satış Faturası",
            Fatura.ProformaAlimFaturasi => "Proforma Alım Faturası",
            Irsaliye.SatisIrsaliyesi => "Satış İrsaliyesi",
            Irsaliye.AlimIrsaliyesi => "Alım İrsaliyesi",
            Irsaliye.SatisIadeIrsaliyesi => "Satış İade İrsaliyesi",
            Irsaliye.AlimIadeIrsaliyesi => "Alım İade İrsaliyesi",
            StokHareketi.DepoTransferi => "Depo Transferi",
            StokHareketi.Fire => "Fire",
            StokHareketi.Sarf => "Sarf",
            StokHareketi.SayimFazlasi => "Sayım Fazlası",
            StokHareketi.SayimEksigi => "Sayım Eksiği",
            StokHareketi.DigerGiris => "Diğer Giriş",
            StokHareketi.DigerCikis => "Diğer Çıkış",
            Sayim.SayimSonucFisi => "Sayım Sonuç Fişi",
            _ => $"Bilinmeyen Belge Türü ({belgeTurDetayId})"
        };
    }
}
