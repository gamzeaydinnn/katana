namespace Katana.Core.Constants;

/// <summary>
/// Luca sistem sabitleri ve kategori tanımları
/// </summary>
public static class LucaConstants
{
    /// <summary>
    /// Luca'da kullanılan standart stok kategorileri
    /// Key: Kategori Kodu, Value: Kategori Adı
    /// </summary>
    public static readonly Dictionary<string, string> LUCA_CATEGORIES = new()
    {
        { "001", "Hammadde" },
        { "002", "Yarı Mamul" },
        { "003", "Mamul" },
        { "004", "Ticari Mal" },
        { "005", "Yedek Parça" },
        { "006", "Ambalaj Malzemesi" },
        { "007", "Demirbaş" },
        { "008", "Sarf Malzemesi" },
        { "009", "Hurda" },
        { "010", "Diğer" }
    };

    /// <summary>
    /// Kategori kodunun geçerliliğini kontrol eder
    /// </summary>
    public static bool IsValidCategoryCode(string code)
    {
        return !string.IsNullOrWhiteSpace(code) && LUCA_CATEGORIES.ContainsKey(code);
    }

    /// <summary>
    /// Kategori koduna göre açıklama getirir
    /// </summary>
    public static string? GetCategoryDescription(string code)
    {
        return LUCA_CATEGORIES.TryGetValue(code, out var description) ? description : null;
    }
}
