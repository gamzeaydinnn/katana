using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Katana.Core.Constants;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;

namespace Katana.Infrastructure.Mappers;

public static class KatanaToLucaMapper
{
    public static LucaCreateStokKartiRequest MapProductToStockCard(
        Product product,
        double? defaultVat = null,
        long? defaultOlcumBirimiId = null,
        long? defaultKartTipi = null,
        string? defaultKategoriKod = null)
    {
        var card = MappingHelper.MapToLucaStockCard(product, defaultOlcumBirimiId, defaultVat);

        if (defaultKartTipi.HasValue && defaultKartTipi.Value > 0)
        {
            card.KartTipi = defaultKartTipi.Value;
        }

        if (!string.IsNullOrWhiteSpace(defaultKategoriKod) &&
            string.IsNullOrWhiteSpace(card.KategoriAgacKod))
        {
            card.KategoriAgacKod = defaultKategoriKod!;
        }

        return card;
    }

    public static LucaCreateCustomerRequest MapCustomerToCreateRequest(
        Customer customer,
        IReadOnlyDictionary<string, string>? customerTypeMappings = null)
    {
        var request = MappingHelper.MapToLucaCustomerCreate(customer);

        if (customerTypeMappings != null && customerTypeMappings.Count > 0)
        {
            foreach (var key in ResolveCustomerTypeKeys(customer))
            {
                if (customerTypeMappings.TryGetValue(key, out var mappedValue) &&
                    long.TryParse(mappedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    request.CariTipId = parsed;
                    break;
                }
            }
        }

        return request;
    }

    public static LucaCreateInvoiceHeaderRequest MapInvoiceToCreateRequest(
        Invoice invoice,
        Customer customer,
        IEnumerable<InvoiceItem> items,
        IReadOnlyDictionary<string, string> skuAccountMappings,
        string belgeSeri,
        long belgeTurDetayId,
        string? defaultWarehouseCode = null)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        if (items == null) throw new ArgumentNullException(nameof(items));

        var invoiceItems = items.ToList();
        var accountMappings = skuAccountMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var baseDto = MappingHelper.MapToLucaInvoice(invoice, customer, invoiceItems, accountMappings.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase));
        var belge = new LucaCreateInvoiceHeaderRequest
        {
            BelgeSeri = string.IsNullOrWhiteSpace(belgeSeri) ? "A" : belgeSeri.Trim(),
            BelgeNo = ParseDocumentNo(baseDto.DocumentNo),
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

    public static long GetBelgeTurDetayIdForInvoiceType(Invoice invoice)
    {
        if (invoice == null)
        {
            return KozaBelgeTurleri.MalSatisFaturasi;
        }

        var hintSources = new[]
        {
            invoice.Notes,
            invoice.Status,
            invoice.InvoiceNo
        };

        foreach (var hint in hintSources)
        {
            var normalized = NormalizeTypeHint(hint);
            if (string.IsNullOrEmpty(normalized)) continue;

            switch (normalized)
            {
                case "PURCHASEINVOICE":
                case "PURCHASE":
                case "ALIM":
                case "BUY":
                case "SUPPLIER":
                    return KozaBelgeTurleri.AlimFaturasi;
                case "SALESRETURN":
                case "SALE_RETURN":
                case "SATISIADE":
                case "SALES-CREDIT":
                    return KozaBelgeTurleri.SatisIadeFaturasi;
                case "PURCHASERETURN":
                case "ALIMIADE":
                case "PURCHASE-CREDIT":
                    return KozaBelgeTurleri.AlimIadeFaturasi;
                case "PURCHASEPROFORMA":
                case "PURCHASEORDER":
                    return KozaBelgeTurleri.AlimFaturasi;
                case "SALESPROFORMA":
                    return KozaBelgeTurleri.MalSatisFaturasi;
                case "SALESINVOICE":
                    return KozaBelgeTurleri.MalSatisFaturasi;
            }
        }

        if (invoice.TotalAmount < 0)
        {
            return KozaBelgeTurleri.SatisIadeFaturasi;
        }

        return KozaBelgeTurleri.MalSatisFaturasi;
    }

    private static List<LucaCreateInvoiceDetailRequest> ConvertLines(
        IEnumerable<LucaInvoiceItemDto> lines,
        string? defaultWarehouseCode)
    {
        var list = new List<LucaCreateInvoiceDetailRequest>();
        foreach (var line in lines)
        {
            var detail = new LucaCreateInvoiceDetailRequest
            {
                KartTuru = 1,
                KartKodu = line.ProductCode,
                KartAdi = Truncate(line.Description, 200),
                BirimFiyat = ConvertToDouble(line.UnitPrice),
                Miktar = ConvertToDouble(line.Quantity),
                KdvOran = ConvertToDouble(line.TaxRate),
                Tutar = ConvertToDouble(line.NetAmount),
                HesapKod = string.IsNullOrWhiteSpace(line.AccountCode) ? null : line.AccountCode,
                DepoKodu = string.IsNullOrWhiteSpace(defaultWarehouseCode) ? null : defaultWarehouseCode,
                OlcuBirimi = null,
                Barkod = null
            };

            list.Add(detail);
        }

        return list;
    }

    private static IEnumerable<string> ResolveCustomerTypeKeys(Customer customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.Country))
        {
            yield return customer.Country.Trim();
        }

        if (!string.IsNullOrWhiteSpace(customer.City))
        {
            yield return customer.City.Trim();
        }

        if (!string.IsNullOrWhiteSpace(customer.TaxNo))
        {
            yield return customer.TaxNo.Trim();
            yield return customer.TaxNo.Length == 11 ? "PERSON" : "COMPANY";
        }
        else
        {
            yield return "COMPANY";
        }
    }

    private static int ResolveCariTip(Customer customer)
    {
        var taxNo = customer?.TaxNo?.Trim() ?? string.Empty;
        return taxNo.Length == 11 ? 2 : 1;
    }

    private static int? ParseDocumentNo(string? documentNo)
    {
        if (string.IsNullOrWhiteSpace(documentNo))
        {
            return null;
        }

        if (int.TryParse(documentNo, out var parsed))
        {
            return parsed;
        }

        var digits = new string(documentNo.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var fallback) ? fallback : null;
    }

    private static string NormalizeTypeHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("-", string.Empty).ToUpperInvariant();
    }

    private static double ConvertToDouble(decimal value) =>
        Convert.ToDouble(value, CultureInfo.InvariantCulture);

    private static double ConvertToDouble(decimal? value) =>
        value.HasValue ? Convert.ToDouble(value.Value, CultureInfo.InvariantCulture) : 0d;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    private static string GenerateCustomerCode(string? taxNo, int customerId)
    {
        if (!string.IsNullOrWhiteSpace(taxNo))
        {
            return $"CUST_{taxNo}";
        }

        return $"CUST_{customerId:000000}";
    }

    private static string? NormalizeTaxNo(string? taxNo)
    {
        if (string.IsNullOrWhiteSpace(taxNo)) return null;
        var digits = new string(taxNo.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
