using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Katana.Core.Utilities;

/// <summary>
/// Mapping tabloları için hash hesaplama ve değişiklik tespiti
/// </summary>
public static class MappingHashHelper
{
    /// <summary>
    /// Verilen nesneyi JSON'a çevirip SHA256 hash'ini hesapla
    /// </summary>
    public static string ComputeHash(object? data)
    {
        if (data == null)
            return ComputeSha256("");

        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
            return ComputeSha256(json);
        }
        catch
        {
            // Serialization başarısız olursa, ToString() kullan
            return ComputeSha256(data.ToString() ?? "");
        }
    }

    /// <summary>
    /// String'in SHA256 hash'ini hesapla
    /// </summary>
    public static string ComputeSha256(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Mapping'in değişip değişmediğini kontrol et
    /// </summary>
    public static bool HasChanged(string? lastHash, object? currentData)
    {
        var currentHash = ComputeHash(currentData);
        return lastHash != currentHash;
    }

    /// <summary>
    /// Supplier mapping'i için hash hesapla
    /// </summary>
    public static string ComputeSupplierMappingHash(string katanaSupplierId, string kozaCariKodu, long? kozaFinansalNesneId)
    {
        var data = new { katanaSupplierId, kozaCariKodu, kozaFinansalNesneId };
        return ComputeHash(data);
    }

    /// <summary>
    /// Customer mapping'i için hash hesapla
    /// </summary>
    public static string ComputeCustomerMappingHash(string katanaCustomerId, string kozaCariKodu, long? kozaFinansalNesneId, string? taxNo = null)
    {
        var data = new { katanaCustomerId, kozaCariKodu, kozaFinansalNesneId, taxNo };
        return ComputeHash(data);
    }

    /// <summary>
    /// Location mapping'i için hash hesapla
    /// </summary>
    public static string ComputeLocationMappingHash(string katanaLocationId, string kozaDepoKodu, long? kozaDepoId)
    {
        var data = new { katanaLocationId, kozaDepoKodu, kozaDepoId };
        return ComputeHash(data);
    }

    /// <summary>
    /// Product mapping'i için hash hesapla
    /// </summary>
    public static string ComputeProductMappingHash(string katanaProductId, string katanaSku, string lucaStockCode, long? lucaStockId)
    {
        var data = new { katanaProductId, katanaSku, lucaStockCode, lucaStockId };
        return ComputeHash(data);
    }

    /// <summary>
    /// Order mapping'i için hash hesapla
    /// </summary>
    public static string ComputeOrderMappingHash(int orderId, long lucaInvoiceId, string entityType, string? externalOrderId)
    {
        var data = new { orderId, lucaInvoiceId, entityType, externalOrderId };
        return ComputeHash(data);
    }
}
