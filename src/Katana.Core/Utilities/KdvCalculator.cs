namespace Katana.Core.Utilities;

/// <summary>
/// KDV dahil/hariç hesaplama utility
/// Koza'da kdvFlag: true = KDV dahil, false = KDV hariç
/// </summary>
public static class KdvCalculator
{
    /// <summary>
    /// KDV hariç tutardan KDV dahil tutar hesapla
    /// </summary>
    public static decimal CalculateWithVat(decimal amountExcludingVat, decimal vatRate)
    {
        if (vatRate < 0 || vatRate > 1)
        {
            throw new ArgumentException("VAT rate must be between 0 and 1 (e.g., 0.18 for 18%)", nameof(vatRate));
        }

        return amountExcludingVat * (1 + vatRate);
    }

    /// <summary>
    /// KDV dahil tutardan KDV hariç tutar hesapla
    /// </summary>
    public static decimal CalculateWithoutVat(decimal amountIncludingVat, decimal vatRate)
    {
        if (vatRate < 0 || vatRate > 1)
        {
            throw new ArgumentException("VAT rate must be between 0 and 1 (e.g., 0.18 for 18%)", nameof(vatRate));
        }

        return amountIncludingVat / (1 + vatRate);
    }

    /// <summary>
    /// KDV tutarını hesapla
    /// </summary>
    public static decimal CalculateVatAmount(decimal amountExcludingVat, decimal vatRate)
    {
        if (vatRate < 0 || vatRate > 1)
        {
            throw new ArgumentException("VAT rate must be between 0 and 1 (e.g., 0.18 for 18%)", nameof(vatRate));
        }

        return amountExcludingVat * vatRate;
    }

    /// <summary>
    /// Satır toplamı hesapla (miktar * birim fiyat)
    /// </summary>
    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice, bool includeVat, decimal vatRate)
    {
        var subtotal = quantity * unitPrice;
        
        if (includeVat)
        {
            return CalculateWithVat(subtotal, vatRate);
        }

        return subtotal;
    }

    /// <summary>
    /// Fatura toplamı hesapla (tüm satırlar)
    /// </summary>
    public static (decimal SubTotal, decimal VatAmount, decimal Total) CalculateInvoiceTotal(
        IEnumerable<(decimal Quantity, decimal UnitPrice, decimal VatRate)> lines,
        bool includeVat)
    {
        decimal subTotal = 0;
        decimal vatAmount = 0;

        foreach (var line in lines)
        {
            var lineSubtotal = line.Quantity * line.UnitPrice;
            subTotal += lineSubtotal;
            vatAmount += CalculateVatAmount(lineSubtotal, line.VatRate);
        }

        var total = includeVat ? subTotal + vatAmount : subTotal;

        return (subTotal, vatAmount, total);
    }

    /// <summary>
    /// KDV oranını yüzde formatından ondalık formatına çevir (18 -> 0.18)
    /// </summary>
    public static decimal PercentageToDecimal(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));
        }

        return percentage / 100;
    }

    /// <summary>
    /// KDV oranını ondalık formatından yüzde formatına çevir (0.18 -> 18)
    /// </summary>
    public static decimal DecimalToPercentage(decimal decimalValue)
    {
        if (decimalValue < 0 || decimalValue > 1)
        {
            throw new ArgumentException("Decimal value must be between 0 and 1", nameof(decimalValue));
        }

        return decimalValue * 100;
    }

    /// <summary>
    /// Tutar uyumsuzluğu kontrolü (tolerans ile)
    /// </summary>
    public static bool AreAmountsEqual(decimal amount1, decimal amount2, decimal tolerance = 0.01m)
    {
        return Math.Abs(amount1 - amount2) <= tolerance;
    }

    /// <summary>
    /// KDV dahil/hariç dönüşüm
    /// </summary>
    public static decimal ConvertVatInclusion(decimal amount, decimal vatRate, bool fromIncluding, bool toIncluding)
    {
        if (fromIncluding == toIncluding)
        {
            return amount; // Dönüşüm gerekmiyor
        }

        if (fromIncluding && !toIncluding)
        {
            // KDV dahil -> KDV hariç
            return CalculateWithoutVat(amount, vatRate);
        }
        else
        {
            // KDV hariç -> KDV dahil
            return CalculateWithVat(amount, vatRate);
        }
    }

    /// <summary>
    /// Satır detayı hesaplama (Koza formatı için)
    /// </summary>
    public static KozaLineCalculation CalculateKozaLine(
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        bool kdvFlag)
    {
        var subtotal = quantity * unitPrice;
        var vatAmount = CalculateVatAmount(subtotal, vatRate);
        var total = kdvFlag ? subtotal + vatAmount : subtotal;

        return new KozaLineCalculation
        {
            Quantity = quantity,
            UnitPrice = unitPrice,
            VatRate = vatRate,
            Subtotal = subtotal,
            VatAmount = vatAmount,
            Total = total,
            KdvFlag = kdvFlag
        };
    }
}

/// <summary>
/// Koza satır hesaplama sonucu
/// </summary>
public class KozaLineCalculation
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public bool KdvFlag { get; set; }
}
