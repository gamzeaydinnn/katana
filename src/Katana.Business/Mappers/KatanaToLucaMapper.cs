using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Katana.Business.Enums;
using Katana.Business.Models.DTOs;
using Katana.Core.Constants;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;
using Katana.Data.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Katana.Infrastructure.Mappers;

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
        return Katana.Business.Mappers.DocumentTypeMapper.GetInvoiceTypeIds(type);
    }

    public static (int BelgeTurId, int BelgeTurDetayId) GetWaybillTypeIds(WaybillType type)
    {
        return Katana.Business.Mappers.DocumentTypeMapper.GetWaybillTypeIds(type);
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

        // ========================================================================
        // SENİN ATTIĞIN "DOĞRU VERİ" ÖRNEĞİNE GÖRE SABİTLENMİŞ AYARLAR
        // ========================================================================

        // 1. Kart Tipi
        card.KartTipi = 1;

        // 2. KDV Oran ID'leri
        card.KartAlisKdvOran = 1;
        card.KartSatisKdvOran = 1;

        // 3. Ölçüm Birimi
        card.OlcumBirimiId = 1;

        // 4. Kart Türü (Stok)
        card.KartTuru = 1;

        // 5. Kategori Ağaç Kodu - NULL bırak (Luca örnekte gösterildiği gibi)
        // Eğer defaultKategoriKod numeric kod ise ("001" gibi) kullan, yoksa null
        card.KategoriAgacKod = (!string.IsNullOrWhiteSpace(defaultKategoriKod) && 
                                 defaultKategoriKod!.All(c => char.IsDigit(c) || c == '.')) 
                                 ? defaultKategoriKod 
                                 : null;

        // 6. Tarih Formatı (gg/aa/yyyy)
        card.BaslangicTarihi = DateTime.Now.ToString(KozaDateFormat, CultureInfo.InvariantCulture);

        // 7. Diğer flag'ler
        card.SatilabilirFlag = 1;
        card.SatinAlinabilirFlag = 1;
        card.MaliyetHesaplanacakFlag = 1;

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
        if (invoice == null) return KozaBelgeTurleri.MalSatisFaturasi;

        // try to infer direction/isReturn/isProforma/isExchangeRate from invoice hints
        var hintSources = new[] { invoice.Notes, invoice.Status, invoice.InvoiceNo };
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

        // negative total usually indicates a return
        if (invoice.TotalAmount < 0) isReturn = true;

        var (belgeTurId, belgeTurDetayId) = GetBelgeTurForInvoice(direction, isReturn, isProforma, isExchangeRate);
        return belgeTurDetayId;
    }

    private static List<LucaCreateInvoiceDetailRequest> ConvertLines(
        IEnumerable<LucaInvoiceItemDto> lines,
        string? defaultWarehouseCode)
    {
        var list = new List<LucaCreateInvoiceDetailRequest>();
        foreach (var line in lines)
        {
            // Depo kodu validation - boş olamaz
            var depoKodu = defaultWarehouseCode;
            if (string.IsNullOrWhiteSpace(depoKodu))
            {
                throw new InvalidOperationException(
                    $"Depo kodu boş olamaz. Fatura satırı eklenirken depo kodu zorunludur. " +
                    $"Ürün: {line.ProductCode}, Açıklama: {line.Description}");
            }

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
                DepoKodu = depoKodu,
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

    public static LucaCreateStokKartiRequest MapKatanaProductToStockCard(
        KatanaProductDto product,
        LucaApiSettings lucaSettings,
        IReadOnlyDictionary<string, string>? productCategoryMappings = null,
        KatanaMappingSettings? katanaMapping = null)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));
        if (lucaSettings == null) throw new ArgumentNullException(nameof(lucaSettings));

        var sku = string.IsNullOrWhiteSpace(product.SKU) ? product.GetProductCode() : product.SKU.Trim();
        
        // 🔥 KRİTİK FİX: Katana'dan Name boş gelirse SKU kullan, ama LOG'A YAZ!
        var rawName = string.IsNullOrWhiteSpace(product.Name) ? sku : product.Name.Trim();
        
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            // UYARI: Katana'dan ürün ismi boş geldi, SKU kullanılıyor!
            // Bu durumda Luca'da "COOLING WATER PIPE" varsa ama biz "81.06301-8211" gönderiyorsak
            // sistem isim değişikliği algılar ve gereksiz versiyon oluşturur!
            Console.WriteLine($"⚠️ MAPPING HATASI: Katana'dan Name boş geldi, SKU kullanılıyor: {sku}");
            Console.WriteLine($"   Bu durum Luca'da gereksiz versiyon oluşturabilir!");
            Console.WriteLine($"   ÇÖZÜM: Katana API'sinden 'name' alanını dolu gönder veya database'den ürün ismini çek.");
        }
        
        // 🔥 ENCODING SORUNLARINI ÇÖZME: Ø, ?, ?? gibi karakterleri normalize et
        // Luca API'si ISO-8859-9 (Turkish) encoding kullanıyor
        // UTF-8'den gelen Ø karakteri Luca'da ?? olarak görünebilir
        // Karşılaştırma sırasında sorun yaratmamak için normalize ediyoruz
        var name = NormalizeProductNameForLuca(rawName);
        
        if (rawName != name)
        {
            Console.WriteLine($"🔧 ENCODING FIX: Ürün ismi normalize edildi");
            Console.WriteLine($"   Orijinal: '{rawName}'");
            Console.WriteLine($"   Normalize: '{name}'");
            Console.WriteLine($"   SKU: {sku}");
        }
        
        // Prefer product.Category if provided; else fall back to configured default; otherwise leave null (Koza accepts null).
        // However, some products get assigned an internal default Category.Id (e.g. "1") which is NOT a valid
        // Luca tree code. Treat that as missing and use the configured DefaultKategoriKodu when available.
        // Resolve product category to Luca KategoriAgacKod using provided mapping dictionary
        string? category = null;
        var rawCategory = !string.IsNullOrWhiteSpace(product.Category) ? product.Category : null;

        // 🔥 ÖNCE: Database mapping tablosundan kontrol et (productCategoryMappings)
        if (productCategoryMappings != null && !string.IsNullOrWhiteSpace(rawCategory))
        {
            var lookupKey = NormalizeMappingKey(rawCategory);
            if (productCategoryMappings.TryGetValue(lookupKey, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            {
                category = mapped;
            }
        }

        // 🔥 SONRA: appsettings.json CategoryMapping'den kontrol et (fallback)
        if (string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(rawCategory))
        {
            var lookupKey = NormalizeMappingKey(rawCategory);
            if (lucaSettings.CategoryMapping != null && 
                lucaSettings.CategoryMapping.TryGetValue(lookupKey, out var configMapped) && 
                !string.IsNullOrWhiteSpace(configMapped))
            {
                category = configMapped;
            }
        }

        // If mapping not found, DO NOT use raw category name as code
        // Only use numeric codes like "001", "220", etc. Never use category names!
        if (string.IsNullOrWhiteSpace(category))
        {
            if (!string.IsNullOrWhiteSpace(rawCategory))
            {
                // If rawCategory is numeric-only (internal id), it's still not a valid Luca code
                // Only use DefaultKategoriKodu if it looks like a numeric code ("001" format)
                if (IsNumericOnly(rawCategory))
                {
                    // Numeric internal ID - use default if it's a valid code format
                    if (!string.IsNullOrWhiteSpace(lucaSettings.DefaultKategoriKodu) && 
                        lucaSettings.DefaultKategoriKodu!.All(c => char.IsDigit(c) || c == '.'))
                    {
                        category = lucaSettings.DefaultKategoriKodu;
                    }
                    else
                    {
                        category = null; // No valid code, leave null
                    }
                }
                else
                {
                    // rawCategory is a NAME (like "3YARI MAMUL") - Try appsettings fallback first
                    if (lucaSettings.CategoryMapping != null && 
                        lucaSettings.CategoryMapping.TryGetValue("default", out var defaultCategory) && 
                        !string.IsNullOrWhiteSpace(defaultCategory))
                    {
                        category = defaultCategory;
                    }
                    else
                    {
                        // Last resort: use DefaultKategoriKodu
                        category = null;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(lucaSettings.DefaultKategoriKodu) &&
                     lucaSettings.DefaultKategoriKodu!.All(c => char.IsDigit(c) || c == '.'))
            {
                category = lucaSettings.DefaultKategoriKodu;
            }
        }

        // 🔥 KRİTİK FİX: Versiyonlu SKU'lar için barkod NULL olmalı (Duplicate Barcode hatasını önlemek için)
        // Eğer SKU "-V" ile bitiyorsa (örn: "PIPE-V2", "silll12344-V3"), bu yeni bir versiyon demektir
        // Luca'da aynı barkod birden fazla stok kartında olamaz, bu yüzden versiyonlu kartlarda barkod boş gönderilmeli
        bool isVersionedSku = System.Text.RegularExpressions.Regex.IsMatch(sku, @"-V\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        string? barcodeToSend = null;
        
        if (isVersionedSku)
        {
            // Versiyonlu SKU - Barkod NULL gönder
            barcodeToSend = null;
            Console.WriteLine($"⚠️ VERSIYONLU SKU TESPİT EDİLDİ: {sku} - Barkod NULL gönderiliyor (Duplicate Barcode hatasını önlemek için)");
        }
        else
        {
            // Normal SKU - Barkod gönder
            barcodeToSend = string.IsNullOrWhiteSpace(product.Barcode) ? sku : product.Barcode.Trim();
        }
        
        var dto = new LucaCreateStokKartiRequest
        {
            KartAdi = name,
            KartTuru = 1, // 1=Stok, 2=Hizmet
            BaslangicTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            OlcumBirimiId = ResolveMeasurementUnitId(product, lucaSettings, katanaMapping),
            KartKodu = sku,
            MaliyetHesaplanacakFlag = 1,
            KartTipi = lucaSettings.DefaultKartTipi,
            // kategoriAgacKod - mapping varsa kullan, yoksa null
            KategoriAgacKod = category,
            KartAlisKdvOran = 1,
            KartSatisKdvOran = 1,
            Barkod = barcodeToSend, // Versiyonlu SKU'lar için NULL
            UzunAdi = name,
            SatilabilirFlag = 1,
            SatinAlinabilirFlag = 1,
            LotNoFlag = 0,
            MinStokKontrol = 0,
            // 🔥 FIX: Tevkifat alanları - Luca dokümantasyonuna göre doğru isimler
            AlisTevkifatOran = null,           // "7/10" formatında string veya null
            SatisTevkifatOran = null,          // "2/10" formatında string veya null
            AlisTevkifatTipId = null,          // NOT: alisTevkifatKod DEĞİL!
            SatisTevkifatTipId = null,         // NOT: satisTevkifatKod DEĞİL!
            PerakendeAlisBirimFiyat = ConvertToDouble(product.CostPrice ?? product.PurchasePrice ?? 0),
            PerakendeSatisBirimFiyat = ConvertToDouble(product.SalesPrice ?? product.Price)
        };

        dto.KartAdi = EncodingHelper.ConvertToIso88599(dto.KartAdi);
        dto.UzunAdi = EncodingHelper.ConvertToIso88599(dto.UzunAdi);
        return dto;
    }

    private static long ResolveMeasurementUnitId(
        KatanaProductDto product,
        LucaApiSettings lucaSettings,
        KatanaMappingSettings? katanaMapping)
    {
        var defaultId = lucaSettings.DefaultOlcumBirimiId > 0 ? lucaSettings.DefaultOlcumBirimiId : 1;
        if (katanaMapping == null)
        {
            return defaultId;
        }

        var unitToResolve = !string.IsNullOrWhiteSpace(product.Unit)
            ? product.Unit
            : katanaMapping.DefaultUnit;

        if (string.IsNullOrWhiteSpace(unitToResolve))
        {
            return defaultId;
        }

        var normalizedUnit = NormalizeMappingKey(unitToResolve);
        if (string.IsNullOrWhiteSpace(normalizedUnit))
        {
            return defaultId;
        }

        var normalizedMappings = NormalizeMappingDictionary(katanaMapping.UnitToMeasurementUnit);
        if (normalizedMappings.TryGetValue(normalizedUnit, out var mappedValue) &&
            !string.IsNullOrWhiteSpace(mappedValue) &&
            long.TryParse(mappedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultId;
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

    // Use regex to reliably detect numeric-only strings (avoid culture/parsing surprises)
    private static bool IsNumericOnly(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(input.Trim(), "^\\d+$");
    }
    
    /// <summary>
    /// Ürün ismini Luca API'si için normalize eder (Encoding sorunlarını çözer)
    /// Luca ISO-8859-9 (Turkish) encoding kullanıyor, UTF-8'den gelen bazı karakterler bozuluyor
    /// Ø → O, ?? → temizle, özel karakterleri ASCII'ye çevir
    /// </summary>
    private static string NormalizeProductNameForLuca(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = input.Trim();

        // 1. DIAMETER (Çap) sembolü varyantları → O'ya çevir
        result = result
            .Replace("Ø", "O")   // Unicode U+00D8 (Latin Capital Letter O with Stroke)
            .Replace("ø", "o")   // Unicode U+00F8 (Latin Small Letter O with Stroke)
            .Replace("Φ", "O")   // Unicode U+03A6 (Greek Capital Letter Phi)
            .Replace("φ", "o")   // Unicode U+03C6 (Greek Small Letter Phi)
            .Replace("⌀", "O");  // Unicode U+2300 (Diameter Sign)

        // 2. ENCODING HATASI karakterlerini temizle
        // Luca'da ?? olarak görünen karakterler için fallback
        result = result
            .Replace("�", "")    // Unicode Replacement Character (U+FFFD)
            .Replace("?", "");   // Soru işareti (encoding bozukluğu göstergesi)

        // 3. TÜRKÇE KARAKTERLER - Luca API'si zaten ISO-8859-9 destekliyor, dokunma!
        // Ü, Ö, Ş, Ç, Ğ, İ karakterlerini KORUYORUZ (Luca bunları destekliyor)

        // 4. WINDOWS-1254 <-> UTF-8 encoding sorunlarını düzelt
        result = result
            .Replace("Ã‡", "Ç")  // Ç encoding hatası
            .Replace("Ã–", "Ö")  // Ö encoding hatası
            .Replace("Ãœ", "Ü")  // Ü encoding hatası
            .Replace("Å�", "İ")  // İ encoding hatası
            .Replace("Ã§", "ç")  // ç encoding hatası
            .Replace("Ã¶", "ö")  // ö encoding hatası
            .Replace("Ã¼", "ü")  // ü encoding hatası
            .Replace("Ä±", "ı"); // ı encoding hatası

        // 5. FAZLA BOŞLUKLARI TEMİZLE
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();

        return result;
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
    
}
