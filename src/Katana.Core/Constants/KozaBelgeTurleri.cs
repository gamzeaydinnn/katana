namespace Katana.Core.Constants;

/// <summary>
/// Luca/Koza tarafında kullanılan belge türü ID sabitleri.
/// </summary>
public static class KozaBelgeTurleri
{
    // Stok hareket türleri (Excel'den gelen ID'lerle doldurulacaksa burada güncellenir)
    public const long DigerStokGiris = 123; // TODO: Excel'deki gerçek ID ile güncelle
    public const long DigerStokCikis = 124; // TODO: Excel'deki gerçek ID ile güncelle

    // Fatura türleri
    public const long SatisFaturasi = 76;
    public const long AlimFaturasi = 77;
}
