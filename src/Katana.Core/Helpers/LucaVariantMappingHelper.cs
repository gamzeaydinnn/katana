using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Core.Helpers;

/// <summary>
/// Luca fatura oluştururken varyant bilgilerini doğru şekilde map eden helper sınıfı.
/// VariantMapping tablosunu kullanarak Katana varyantlarını Luca stok kartlarına eşler.
/// </summary>
public static class LucaVariantMappingHelper
{
    /// <summary>
    /// SalesOrderLine için varyant bilgilerini içeren Luca fatura detay satırı oluşturur.
    /// </summary>
    /// <param name="line">SalesOrder satırı</param>
    /// <param name="variantMapping">VariantMapping (nullable)</param>
    /// <param name="product">Ana ürün (nullable)</param>
    /// <param name="productVariant">Ürün varyantı (nullable)</param>
    /// <param name="depoKodu">Luca depo kodu</param>
    /// <returns>Luca fatura detay request</returns>
    public static LucaCreateInvoiceDetailRequest BuildInvoiceDetailWithVariant(
        SalesOrderLine line,
        VariantMapping? variantMapping,
        Product? product,
        ProductVariant? productVariant,
        string depoKodu)
    {
        // 1. KartKodu'nu belirle (varyant varsa varyant SKU, yoksa ana ürün SKU)
        string kartKodu;
        string kartAdi;
        string? aciklama;

        if (productVariant != null)
        {
            // VARYANTLI ÜRÜN
            kartKodu = MappingHelper.NormalizeSku(productVariant.SKU);
            kartAdi = BuildVariantProductName(product?.Name, productVariant);
            aciklama = MappingHelper.BuildInvoiceLineDescriptionWithVariant(
                product?.Name ?? line.ProductName,
                productVariant.SKU,
                productVariant.Attributes);
        }
        else if (product != null)
        {
            // VARYANT OLMAYAN ÜRÜN
            kartKodu = MappingHelper.NormalizeSku(product.SKU);
            kartAdi = product.Name;
            aciklama = MappingHelper.BuildInvoiceLineDescriptionWithVariant(
                product.Name,
                product.SKU,
                null);
        }
        else
        {
            // FALLBACK: SKU veya ürün adı
            kartKodu = MappingHelper.NormalizeSku(line.SKU);
            kartAdi = line.ProductName ?? $"Ürün ({line.SKU})";
            aciklama = MappingHelper.BuildInvoiceLineDescriptionWithVariant(
                line.ProductName,
                line.SKU,
                null);
        }

        // KDV oranı normalize et (0.18 formatında olmalı)
        var kdvOran = NormalizeKdvOran(line.TaxRate);

        return new LucaCreateInvoiceDetailRequest
        {
            KartTuru = 1, // Stok kartı
            KartKodu = kartKodu,
            HesapKod = null,
            KartAdi = MappingHelper.NormalizeTurkishText(kartAdi),
            KartTipi = 1,
            Barkod = productVariant?.Barcode ?? product?.Barcode,
            OlcuBirimi = 1, // Adet
            KdvOran = kdvOran,
            KartSatisKdvOran = kdvOran,
            KartAlisKdvOran = kdvOran,
            DepoKodu = depoKodu,
            BirimFiyat = (double)(line.PricePerUnit ?? 0),
            Miktar = (double)line.Quantity,
            Tutar = null, // Luca hesaplar
            IskontoOran1 = 0.0,
            Aciklama = aciklama,
            LotNo = null
        };
    }

    /// <summary>
    /// Varyantlı ürün adı oluşturur: "Ana Ürün - Varyant Adı" formatında
    /// </summary>
    private static string BuildVariantProductName(string? productName, ProductVariant variant)
    {
        var baseName = !string.IsNullOrWhiteSpace(productName) ? productName : "Ürün";
        
        // Attributes'tan varyant bilgisi çıkar
        if (!string.IsNullOrWhiteSpace(variant.Attributes))
        {
            try
            {
                if (variant.Attributes.TrimStart().StartsWith("{"))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(variant.Attributes);
                    var values = jsonDoc.RootElement.EnumerateObject()
                        .Select(p => p.Value.GetString() ?? p.Value.ToString())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToList();
                    
                    if (values.Any())
                    {
                        return $"{baseName} - {string.Join(" / ", values)}";
                    }
                }
                else
                {
                    return $"{baseName} - {variant.Attributes}";
                }
            }
            catch
            {
                // JSON parse başarısız
            }
        }
        
        // SKU'dan varyant bilgisi çıkar
        var skuParts = variant.SKU.Split('-');
        if (skuParts.Length >= 2)
        {
            var variantParts = skuParts.Skip(1).Where(p => !string.IsNullOrWhiteSpace(p));
            if (variantParts.Any())
            {
                return $"{baseName} - {string.Join(" / ", variantParts)}";
            }
        }
        
        return baseName;
    }

    /// <summary>
    /// KDV oranını normalize eder (0.18 formatında)
    /// </summary>
    private static double NormalizeKdvOran(decimal? taxRatePercentOrDecimal)
    {
        var rate = (double)(taxRatePercentOrDecimal ?? 18m);
        if (rate > 1.0) rate /= 100.0;
        return Math.Round(rate, 2);
    }

    /// <summary>
    /// Varyant mapping bilgilerini kullanarak Luca stok kodunu resolve eder.
    /// Öncelik sırası:
    /// 1. ProductVariant.SKU (varyant varsa)
    /// 2. Product.SKU (ana ürün)
    /// 3. VariantMapping.Sku
    /// 4. SalesOrderLine.SKU (fallback)
    /// </summary>
    public static string ResolveKartKodu(
        SalesOrderLine line,
        VariantMapping? variantMapping,
        Product? product,
        ProductVariant? productVariant)
    {
        // 1. Varyant SKU
        if (productVariant != null && !string.IsNullOrWhiteSpace(productVariant.SKU))
        {
            return MappingHelper.NormalizeSku(productVariant.SKU);
        }
        
        // 2. Ana ürün SKU
        if (product != null && !string.IsNullOrWhiteSpace(product.SKU))
        {
            return MappingHelper.NormalizeSku(product.SKU);
        }
        
        // 3. VariantMapping SKU
        if (variantMapping != null && !string.IsNullOrWhiteSpace(variantMapping.Sku))
        {
            return MappingHelper.NormalizeSku(variantMapping.Sku);
        }
        
        // 4. Fallback: Line SKU
        return MappingHelper.NormalizeSku(line.SKU);
    }
}
