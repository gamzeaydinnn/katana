using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Katana.Core.Enums;
using Katana.Core.DTOs;
using Katana.Core.Constants;
using Katana.Core.Entities;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Katana.Business.Mappers;

public static class KatanaToLucaMapper
{
    private const string KozaDateFormat = "dd/MM/yyyy";

    public enum InvoiceDirection
    {
        Purchase,
        Sales
    }

    public static (long belgeTurId, long belgeTurDetayId) GetBelgeTurForInvoice(
        InvoiceDirection direction,
        bool isReturn,
        bool isProforma,
        bool isExchangeRate = false)
    {
        if (direction == InvoiceDirection.Purchase && !isReturn)
        {
            if (isProforma) return (KozaBelgeTurleri.AlimFaturalari, KozaBelgeTurleri.ProformaAlimFaturasi);
            if (isExchangeRate) return (KozaBelgeTurleri.AlimFaturalari, KozaBelgeTurleri.KurFarkiAlisFaturasi);
            return (KozaBelgeTurleri.AlimFaturalari, KozaBelgeTurleri.AlimFaturasi);
        }

        if (direction == InvoiceDirection.Sales && !isReturn)
        {
            if (isProforma) return (KozaBelgeTurleri.SatisFaturalari, KozaBelgeTurleri.ProformaSatisFaturasi);
            if (isExchangeRate) return (KozaBelgeTurleri.SatisFaturalari, KozaBelgeTurleri.KurFarkiSatisFaturasi);
            return (KozaBelgeTurleri.SatisFaturalari, KozaBelgeTurleri.MalSatisFaturasi);
        }

        if (direction == InvoiceDirection.Purchase && isReturn)
            return (KozaBelgeTurleri.AlimIadeFaturalari, KozaBelgeTurleri.AlimIadeFaturasi);

        if (direction == InvoiceDirection.Sales && isReturn)
            return (KozaBelgeTurleri.SatisIadeFaturalari, KozaBelgeTurleri.SatisIadeFaturasi);

        // fallback
        return (KozaBelgeTurleri.SatisFaturalari, KozaBelgeTurleri.MalSatisFaturasi);
    }

    public static LucaCreateStokKartiRequest MapFromExcelRow(
        ExcelProductDto excelRow,
        double? defaultVatRate = null,
        long? defaultOlcumBirimiId = null,
        long? defaultKartTipi = null,
        string? defaultKategoriKod = null)
    {
        if (excelRow == null) throw new ArgumentNullException(nameof(excelRow));

        var vatPurchaseDecimal = excelRow.PurchaseVatRate
            ?? excelRow.VatRate
            ?? Convert.ToDecimal(defaultVatRate ?? 0, CultureInfo.InvariantCulture);
        var vatSalesDecimal = excelRow.SalesVatRate
            ?? excelRow.VatRate
            ?? Convert.ToDecimal(defaultVatRate ?? 0, CultureInfo.InvariantCulture);

        var request = new LucaCreateStokKartiRequest
        {
            KartAdi = excelRow.Name?.Trim() ?? excelRow.SKU?.Trim() ?? string.Empty,
            KartKodu = excelRow.SKU?.Trim() ?? string.Empty,
            KartTuru = 1,
            BaslangicTarihi = excelRow.StartDate.ToString(KozaDateFormat, CultureInfo.InvariantCulture),
            OlcumBirimiId = defaultOlcumBirimiId ?? 5,
            KartTipi = defaultKartTipi ?? 4,
            KategoriAgacKod = !string.IsNullOrWhiteSpace(excelRow.CategoryCode) ? excelRow.CategoryCode : defaultKategoriKod ?? string.Empty,
            KartAlisKdvOran = ConvertToDouble(vatPurchaseDecimal),
            KartSatisKdvOran = ConvertToDouble(vatSalesDecimal),
            PerakendeAlisBirimFiyat = 0,
            PerakendeSatisBirimFiyat = 0,
            SatilabilirFlag = BoolToInt(excelRow.IsActive),
            SatinAlinabilirFlag = BoolToInt(excelRow.IsActive),
            LotNoFlag = BoolToInt(excelRow.TrackStock),
            MinStokKontrol = 0,
            MaliyetHesaplanacakFlag = BoolToInt(excelRow.CalculateCostOnPurchase),
            Barkod = excelRow.Barcode ?? string.Empty,
            UzunAdi = excelRow.Name ?? excelRow.SKU ?? string.Empty
        };

        return request;
    }

    public static (int BelgeTurId, int BelgeTurDetayId) GetInvoiceTypeIds(InvoiceType type)
    {
        return DocumentTypeMapper.GetInvoiceTypeIds(type);
    }

    public static (int BelgeTurId, int BelgeTurDetayId) GetWaybillTypeIds(WaybillType type)
    {
        return DocumentTypeMapper.GetWaybillTypeIds(type);
    }

    private static string FormatDateForKoza(DateTime date)
    {
        return date.ToString(KozaDateFormat, CultureInfo.InvariantCulture);
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;

    public static LucaCreateStokKartiRequest MapProductToStockCard(
        Product product,
        double? defaultVat = null,
        long? defaultOlcumBirimiId = null,
        long? defaultKartTipi = null,
        string? defaultKategoriKod = null)
    {
        var card = MappingHelper.MapToLucaStockCard(product, defaultOlcumBirimiId, defaultVat);

        card.KartTuru = 1;
        card.KartTipi = defaultKartTipi ?? card.KartTipi;
        card.KartAlisKdvOran = card.KartAlisKdvOran == 0 ? 1 : card.KartAlisKdvOran;
        card.KartSatisKdvOran = card.KartSatisKdvOran == 0 ? 1 : card.KartSatisKdvOran;
        card.OlcumBirimiId = defaultOlcumBirimiId ?? card.OlcumBirimiId;

        if (!string.IsNullOrWhiteSpace(defaultKategoriKod))
        {
            card.KategoriAgacKod = defaultKategoriKod;
        }

        return card;
    }

    public static LucaCreateInvoiceHeaderRequest MapInvoiceToCreateRequest(
        Invoice invoice,
        Customer customer,
        List<InvoiceItem> items,
        Dictionary<string, string> skuToAccountMapping,
        string belgeSeri,
        long belgeTurDetayId,
        string? defaultWarehouseCode)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        if (items == null) throw new ArgumentNullException(nameof(items));

        var baseDto = MappingHelper.MapToLucaInvoice(invoice, customer, items, skuToAccountMapping);

        var belge = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = belgeSeri,
            BelgeNo = null,
            BelgeTarihi = invoice.InvoiceDate == default ? DateTime.UtcNow : invoice.InvoiceDate,
            VadeTarihi = invoice.DueDate,
            BelgeTakipNo = baseDto.DocumentNo,
            BelgeAciklama = Truncate(invoice.Notes, 250),
            BelgeTurDetayId = belgeTurDetayId,
            FaturaTur = 1,
            ParaBirimKod = string.IsNullOrWhiteSpace(invoice.Currency) ? "TRY" : invoice.Currency,
            KurBedeli = 1,
            BabsFlag = false,
            KdvFlag = true,
            MusteriTedarikci = 1,
            CariKodu = string.IsNullOrWhiteSpace(baseDto.CustomerCode) ? GenerateCustomerCode(customer.TaxNo, customer.Id) : baseDto.CustomerCode,
            CariTanim = string.IsNullOrWhiteSpace(baseDto.CustomerTitle) ? customer.Title : baseDto.CustomerTitle,
            CariTip = ResolveCariTip(customer),
            CariKisaAd = Truncate(customer.Title, 50),
            CariYasalUnvan = customer.Title,
            VergiNo = NormalizeTaxNo(customer.TaxNo),
            VergiDairesi = customer.City,
            AdresSerbest = Truncate(customer.Address, 500),
            IletisimTanim = customer.Phone ?? customer.Email,
            DetayList = ConvertLines(baseDto.Lines ?? new List<LucaInvoiceItemDto>(), defaultWarehouseCode)
        };

        return belge;
    }

    public static LucaCreateCustomerRequest MapCustomerToCreateRequest(Customer customer, Dictionary<string, string>? resolvedMappings = null)
    {
        return MappingHelper.MapToLucaCustomerCreate(customer);
    }

    public static LucaCreateStokKartiRequest MapKatanaProductToStockCard(
        KatanaProductDto product,
        LucaApiSettings lucaSettings,
        IReadOnlyDictionary<string, string>? categoryMappings,
        KatanaMappingSettings katanaMapping)
    {
        var defaultVat = lucaSettings.DefaultKdvOran;
        var defaultUnitId = lucaSettings.DefaultOlcumBirimiId;
        var defaultKartTipi = lucaSettings.DefaultKartTipi;
        var defaultKategoriKod = lucaSettings.DefaultKategoriKodu;

        var card = MappingHelper.MapToLucaStockCard(product, defaultUnitId, defaultVat);

        card.KartTuru = 1;
        card.KartTipi = defaultKartTipi > 0 ? defaultKartTipi : card.KartTipi;
        card.KartAlisKdvOran = card.KartAlisKdvOran == 0 ? 1 : card.KartAlisKdvOran;
        card.KartSatisKdvOran = card.KartSatisKdvOran == 0 ? 1 : card.KartSatisKdvOran;
        card.OlcumBirimiId = defaultUnitId > 0 ? defaultUnitId : card.OlcumBirimiId;

        if (!string.IsNullOrWhiteSpace(product.Category))
        {
            var normalizedMappings = NormalizeMappingDictionary(katanaMapping.CategoryToLucaCategory);
            var key = NormalizeMappingKey(product.Category);
            if (!string.IsNullOrWhiteSpace(key) && normalizedMappings.TryGetValue(key, out var mappedCategory) && !string.IsNullOrWhiteSpace(mappedCategory))
            {
                card.KategoriAgacKod = mappedCategory;
            }
            else
            {
                card.KategoriAgacKod = defaultKategoriKod ?? card.KategoriAgacKod;
            }
        }
        else
        {
            card.KategoriAgacKod = defaultKategoriKod ?? card.KategoriAgacKod;
        }

        return card;
    }

    public static void ValidateLucaStockCard(LucaCreateStokKartiRequest dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.KartKodu))
        {
            throw new ValidationException("Stok kodu zorunlu");
        }

        if (string.IsNullOrWhiteSpace(dto.KartAdi))
        {
            throw new ValidationException("Stok tanımı zorunlu");
        }

        dto.KartAdi = EncodingHelper.ConvertToIso88599(dto.KartAdi);
        dto.UzunAdi = EncodingHelper.ConvertToIso88599(dto.UzunAdi);
    }

    public static long GetBelgeTurDetayIdForInvoiceType(Invoice invoice)
    {
        if (invoice == null) return KozaBelgeTurleri.MalSatisFaturasi;

        var hintSources = new[] { invoice.Notes, invoice.Status.ToString(), invoice.InvoiceNo };
        var isProforma = false;
        var isExchangeRate = false;
        InvoiceDirection direction = InvoiceDirection.Sales;
        var isReturn = false;

        foreach (var hint in hintSources)
        {
            var normalized = NormalizeTypeHint(hint);
            if (string.IsNullOrEmpty(normalized)) continue;

            if (normalized.Contains("PROFORMA")) isProforma = true;
            if (normalized.Contains("KUR") || normalized.Contains("EXCHANGE") || normalized.Contains("CURRENCY")) isExchangeRate = true;
            if (normalized.Contains("PURCHASE") || normalized.Contains("ALIM") || normalized.Contains("BUY") || normalized.Contains("SUPPLIER")) direction = InvoiceDirection.Purchase;
            if (normalized.Contains("RETURN") || normalized.Contains("IADE") || normalized.Contains("CREDIT")) isReturn = true;
        }

        if (invoice.TotalAmount < 0) isReturn = true;

        var (_, belgeTurDetayId) = GetBelgeTurForInvoice(direction, isReturn, isProforma, isExchangeRate);
        return belgeTurDetayId;
    }

    private static string NormalizeTypeHint(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToUpperInvariant();
        s = s.Replace('/', ' ').Replace('\\', ' ').Replace('-', ' ');
        s = RemoveDiacritics(s);
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s;
    }

    private static string NormalizeTaxNo(string? taxNo)
    {
        if (string.IsNullOrWhiteSpace(taxNo))
        {
            return "0000000000";
        }

        var normalized = new string(taxNo.Where(char.IsDigit).ToArray());
        if (normalized.Length == 10 || normalized.Length == 11)
        {
            return normalized;
        }

        return normalized.PadLeft(10, '0')[..10];
    }

    private static string GenerateCustomerCode(string? taxNo, int customerId)
    {
        if (!string.IsNullOrWhiteSpace(taxNo))
        {
            var normalizedTaxNo = NormalizeTaxNo(taxNo);
            if (normalizedTaxNo.Length == 10)
            {
                return $"CK-{normalizedTaxNo}";
            }
        }

        return $"CK-{customerId:D6}";
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static int ResolveCariTip(Customer customer)
    {
        var taxNo = customer?.TaxNo?.Trim() ?? string.Empty;
        return taxNo.Length == 11 ? 2 : 1;
    }

    private static double ConvertToDouble(decimal value)
    {
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static double ConvertToDouble(decimal? value)
    {
        return value.HasValue ? Convert.ToDouble(value.Value, CultureInfo.InvariantCulture) : 0;
    }

    private static double ConvertToDouble(int value)
    {
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, string> NormalizeMappingDictionary(IReadOnlyDictionary<string, string>? mappings)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (mappings == null)
        {
            return normalized;
        }

        foreach (var mapping in mappings)
        {
            var key = NormalizeMappingKey(mapping.Key);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized[key] = mapping.Value ?? string.Empty;
        }

        return normalized;
    }

    private static string NormalizeMappingKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToUpperInvariant();
        s = s.Replace('/', ' ').Replace('\\', ' ').Replace('-', ' ');
        s = RemoveDiacritics(s);
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static List<LucaCreateInvoiceDetailRequest> ConvertLines(List<LucaInvoiceItemDto> lines, string? defaultWarehouseCode)
    {
        return lines.Select(line => new LucaCreateInvoiceDetailRequest
        {
            KartTuru = 1,
            KartKodu = line.ProductCode ?? string.Empty,
            KartAdi = line.Description ?? string.Empty,
            Miktar = Convert.ToDouble(line.Quantity),
            BirimFiyat = Convert.ToDouble(line.UnitPrice),
            KdvOran = Convert.ToDouble(line.TaxRate),
            DepoKodu = defaultWarehouseCode ?? "MAIN"
        }).ToList();
    }
}

