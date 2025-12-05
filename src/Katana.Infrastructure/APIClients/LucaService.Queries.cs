using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Katana.Business.DTOs;
using Katana.Business.Models.DTOs;
using Katana.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using System.Net.Http.Headers;
using System.Net;
using System.Globalization;
using Katana.Business.Interfaces;
using Katana.Infrastructure.Mappers;
using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Helpers;

namespace Katana.Infrastructure.APIClients;

/// <summary>
/// LucaService - PART 3: Queries (List/Fetch methods, Helpers, Upsert methods)
/// </summary>
public partial class LucaService
{
    public async Task<JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request ?? new LucaListInvoicesRequest(), _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.InvoiceList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.Invoices, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceClose, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.InvoiceDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAddresses)
        {
            Content = content
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");
        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> GetCustomerWorkingConditionsAsync(LucaGetCustomerWorkingConditionsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerWorkingConditions, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> ListCustomerAuthorizedPersonsAsync(LucaListCustomerAuthorizedPersonsRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.CustomerAuthorizedPersons)
        {
            Content = content
        };
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> GetCustomerRiskAsync(LucaGetCustomerRiskRequest request)
    {
        await EnsureAuthenticatedAsync();

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = $"{_settings.Endpoints.CustomerRisk}?gnlFinansalNesne.finansalNesneId={request.GnlFinansalNesne.FinansalNesneId}";
        var response = await client.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerTransaction, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCustomerTransactionAsync(
        Payment payment,
        Customer customer,
        long belgeTurDetayId,
        int cariTuru,
        string belgeSeri,
        bool avansFlag,
        string? aciklama = null)
    {
        var request = MappingHelper.MapToLucaCariHareketCreate(payment, customer, belgeTurDetayId, cariTuru, belgeSeri, avansFlag, aciklama);
        return await CreateCustomerTransactionAsync(request);
    }
    public async Task<JsonElement> ListDeliveryNotesAsync(bool detayliListe = false)
    {
        await EnsureAuthenticatedAsync();

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var url = _settings.Endpoints.IrsaliyeList + (detayliListe ? "?detayliListe=true" : string.Empty);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = CreateKozaContent("{}")
        };
        ApplyManualSessionCookie(httpRequest);
        httpRequest.Headers.Add("No-Paging", "true");

        var response = await client.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request)
    {
        await EnsureAuthenticatedAsync();
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.IrsaliyeDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCustomerAsync(LucaCreateCustomerRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CustomerCreate, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    public async Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.OtherStockMovement, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateSalesOrderAsync(LucaCreateSalesOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateSalesOrderHeaderAsync(LucaCreateOrderHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);
        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateSalesOrderHeaderAsync(
        Order order,
        Customer customer,
        List<OrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var request = MappingHelper.MapToLucaSalesOrderHeader(order, customer, items, belgeTurDetayId, belgeSeri);
        return await CreateSalesOrderHeaderAsync(request);
    }
    public async Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrderDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> DeleteSalesOrderDetailAsync(LucaDeleteSalesOrderDetailRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.SalesOrderDetailDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderAsync(LucaCreatePurchaseOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderHeaderAsync(LucaCreateOrderHeaderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrder, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreatePurchaseOrderHeaderAsync(
        PurchaseOrder purchaseOrder,
        Supplier supplier,
        List<PurchaseOrderItem> items,
        long belgeTurDetayId,
        string belgeSeri)
    {
        var request = MappingHelper.MapToLucaPurchaseOrderHeader(purchaseOrder, supplier, items, belgeTurDetayId, belgeSeri);
        return await CreatePurchaseOrderHeaderAsync(request);
    }
    public async Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrderDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> DeletePurchaseOrderDetailAsync(LucaDeletePurchaseOrderDetailRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.PurchaseOrderDetailDelete, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    
    public async Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.WarehouseTransfer, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    
    /// <summary>
    /// Luca Depo Transferi - LucaStockTransferRequest wrapper
    /// </summary>
    public async Task<long> CreateWarehouseTransferAsync(LucaStockTransferRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            
            // LucaStockTransferRequest → LucaCreateWarehouseTransferRequest dönüşümü
            var transferRequest = new LucaCreateWarehouseTransferRequest
            {
                BelgeTurDetayId = request.StkDepoTransferBaslik.BelgeTurDetayId,
                BelgeSeri = request.StkDepoTransferBaslik.BelgeSeri,
                BelgeNo = request.StkDepoTransferBaslik.BelgeNo,
                BelgeTarihi = request.StkDepoTransferBaslik.BelgeTarihi,
                BelgeAciklama = request.StkDepoTransferBaslik.BelgeAciklama,
                GirisDepoKodu = request.StkDepoTransferBaslik.GirisDepoKodu,
                CikisDepoKodu = request.StkDepoTransferBaslik.CikisDepoKodu,
                DetayList = request.StkDepoTransferBaslik.DetayList
                    .Select(r => new LucaWarehouseTransferDetailRequest
                    {
                        KartKodu = r.KartKodu,
                        Miktar = (decimal)r.Miktar,
                        OlcuBirimi = r.OlcuBirimi,
                        Aciklama = r.Aciklama
                    }).ToList()
            };
            
            var result = await CreateWarehouseTransferAsync(transferRequest);
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssBelgeId", out idProp))
            {
                return idProp.GetInt64();
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                return idProp.GetInt64();
            }
            
            _logger.LogWarning("Depo transfer response'dan ID çıkarılamadı: {Response}", result.GetRawText());
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Depo transfer oluşturma hatası");
            throw;
        }
    }
    
    /// <summary>
    /// Luca DSH Stok Hareketi Fişi (Fire, Sarf, Sayım Fazlası vb.)
    /// </summary>
    public async Task<long> CreateStockVoucherAsync(LucaStockVoucherRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            
            // LucaStockVoucherRequest → LucaCreateDshBaslikRequest dönüşümü
            var dshRequest = new LucaCreateDshBaslikRequest
            {
                BelgeSeri = request.StkDshBaslik.BelgeSeri,
                BelgeNo = request.StkDshBaslik.BelgeNo,
                BelgeTarihi = request.StkDshBaslik.BelgeTarihi,
                BelgeAciklama = request.StkDshBaslik.BelgeAciklama,
                BelgeTurDetayId = request.StkDshBaslik.BelgeTurDetayId,
                DepoKodu = request.StkDshBaslik.DepoKodu,
                ParaBirimKod = request.StkDshBaslik.ParaBirimKod,
                DetayList = request.StkDshBaslik.DetayList
                    .Select(r => new LucaCreateDshDetayRequest
                    {
                        KartTuru = r.KartTuru,
                        KartKodu = r.KartKodu,
                        KartAdi = r.KartAdi,
                        Miktar = r.Miktar,
                        OlcuBirimi = r.OlcuBirimi,
                        BirimFiyat = r.BirimFiyat,
                        Aciklama = r.Aciklama,
                        LotNo = r.LotNo,
                        SeriNo = r.SeriNo
                    }).ToList()
            };
            
            var result = await CreateOtherStockMovementAsync(dshRequest);
            
            // Response'dan ID çıkar
            if (result.TryGetProperty("id", out var idProp) || result.TryGetProperty("ssDshBaslikId", out idProp))
            {
                return idProp.GetInt64();
            }
            
            // Alternatif: data.id
            if (result.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("id", out idProp))
            {
                return idProp.GetInt64();
            }
            
            _logger.LogWarning("DSH stok fişi response'dan ID çıkarılamadı: {Response}", result.GetRawText());
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DSH stok fişi oluşturma hatası");
            throw;
        }
    }
    
    public async Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.StockCountResult, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateWarehouseAsync(LucaCreateWarehouseRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.Warehouse, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request)
    {
        await EnsureAuthenticatedAsync();

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = CreateKozaContent(json);

        var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
        var response = await client.PostAsync(_settings.Endpoints.CreditCardEntry, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }
    public async Task<JsonElement> CreateCreditCardEntryAsync(
        Payment payment,
        Customer customer,
        string belgeSeri,
        string kasaCariKodu,
        DateTime? vadeTarihi = null,
        bool? avansFlag = null)
    {
        var request = MappingHelper.MapToLucaKrediKartiGiris(payment, customer, belgeSeri, kasaCariKodu, vadeTarihi, avansFlag);
        return await CreateCreditCardEntryAsync(request);
    }
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing connection to Luca API");

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(_settings.Endpoints.Health);
            var isConnected = response.IsSuccessStatusCode;

            _logger.LogInformation("Luca API connection test result: {IsConnected}", isConnected);
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to Luca API");
            return false;
        }
    }
    private void EnsureInvoiceDefaults(IEnumerable<LucaInvoiceDto> invoices)
    {
        if (invoices == null)
        {
            return;
        }

        foreach (var invoice in invoices)
        {
            EnsureInvoiceDefaults(invoice);
        }
    }
    private void EnsureInvoiceDefaults(LucaInvoiceDto? invoice)
    {
        if (invoice == null)
        {
            return;
        }

        invoice.GnlOrgSsBelge ??= new LucaBelgeDto();
        var belge = invoice.GnlOrgSsBelge;

        if (string.IsNullOrWhiteSpace(belge.BelgeSeri))
        {
            belge.BelgeSeri = string.IsNullOrWhiteSpace(_settings.DefaultBelgeSeri)
                ? "A"
                : _settings.DefaultBelgeSeri;
        }

        if (belge.BelgeTurDetayId <= 0)
        {
            var defaultBelgeTurDetayId = TryGetDefaultBelgeTurDetayId("SalesInvoice");
            if (defaultBelgeTurDetayId.HasValue)
            {
                belge.BelgeTurDetayId = defaultBelgeTurDetayId.Value;
            }
        }

        if (belge.BelgeTarihi == default)
        {
            belge.BelgeTarihi = invoice.DocumentDate;
        }

        if (!belge.VadeTarihi.HasValue)
        {
            belge.VadeTarihi = invoice.DueDate;
        }

        if (!belge.BelgeNo.HasValue && int.TryParse(invoice.DocumentNo, out var parsedNo))
        {
            belge.BelgeNo = parsedNo;
        }

        if (string.IsNullOrWhiteSpace(belge.BelgeAciklama))
        {
            belge.BelgeAciklama = Truncate($"Invoice {invoice.DocumentNo}", 250);
        }

        if (!invoice.FaturaTur.HasValue || invoice.FaturaTur.Value <= 0)
        {
            invoice.FaturaTur = 1;
        }

        if (!invoice.MusteriTedarikci.HasValue || invoice.MusteriTedarikci.Value <= 0)
        {
            invoice.MusteriTedarikci = 1;
        }

        if (string.IsNullOrWhiteSpace(invoice.ParaBirimKod))
        {
            invoice.ParaBirimKod = "TRY";
        }

        
        if (!invoice.KdvFlag.HasValue)
        {
            invoice.KdvFlag = true;
        }

        if (string.IsNullOrWhiteSpace(invoice.CariKodu))
        {
            throw new InvalidOperationException("CariKodu (müşteri kodu) zorunludur");
        }

        if (invoice.Lines == null || !invoice.Lines.Any())
        {
            throw new InvalidOperationException("Fatura detayları (Lines) zorunludur");
        }

        foreach (var line in invoice.Lines)
        {
            EnsureLineDefaults(line);
        }

        void EnsureLineDefaults(LucaInvoiceItemDto line)
        {
            if (line == null)
            {
                throw new InvalidOperationException("Fatura detay satırı boş olamaz");
            }

            SetNumericLineProperty(line, "KartTuru", 1);

            if (string.IsNullOrWhiteSpace(line.Unit))
            {
                line.Unit = "ADET";
            }

            var measurementProperty = line.GetType().GetProperty("OlcuBirimi");
            if (measurementProperty != null &&
                measurementProperty.PropertyType == typeof(string))
            {
                var measurementValue = measurementProperty.GetValue(line) as string;
                if (string.IsNullOrWhiteSpace(measurementValue))
                {
                    measurementProperty.SetValue(line, "ADET");
                }
            }
            else if (!line.OlcuBirimi.HasValue || line.OlcuBirimi <= 0)
            {
                if (_settings.DefaultOlcumBirimiId > 0)
                {
                    line.OlcuBirimi = _settings.DefaultOlcumBirimiId;
                }
            }

            var unitPrice = ReadDecimalProperty(line, "BirimFiyat", line.UnitPrice);
            var quantity = ReadDecimalProperty(line, "Miktar", line.Quantity);

            if (unitPrice <= 0 || quantity <= 0)
            {
                var code = ReadStringProperty(line, "KartKodu");
                if (string.IsNullOrWhiteSpace(code))
                {
                    code = line.ProductCode;
                }

                throw new InvalidOperationException($"Satır için birim fiyat ve miktar zorunludur: {code}");
            }
        }

        void SetNumericLineProperty(object lineItem, string propertyName, int defaultValue)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead || !property.CanWrite)
            {
                return;
            }

            var raw = property.GetValue(lineItem);
            var numeric = ConvertToNullableLong(raw);
            if (!numeric.HasValue || numeric.Value <= 0)
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var converted = Convert.ChangeType(defaultValue, targetType, CultureInfo.InvariantCulture);
                property.SetValue(lineItem, converted);
            }
        }

        decimal ReadDecimalProperty(object lineItem, string propertyName, decimal fallback)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead)
            {
                return fallback;
            }

            var raw = property.GetValue(lineItem);
            if (raw == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToDecimal(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        string? ReadStringProperty(object lineItem, string propertyName)
        {
            var property = lineItem.GetType().GetProperty(propertyName);
            if (property == null || !property.CanRead)
            {
                return null;
            }

            var raw = property.GetValue(lineItem);
            return raw?.ToString();
        }

        long? ConvertToNullableLong(object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
    }

    private long? TryGetDefaultBelgeTurDetayId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var defaultsProperty = _settings.GetType().GetProperty("DefaultBelgeTurDetayId");
        if (defaultsProperty == null)
        {
            return null;
        }

        var defaults = defaultsProperty.GetValue(_settings);
        if (defaults == null)
        {
            return null;
        }

        if (defaults is IDictionary<string, long> typedDict && typedDict.TryGetValue(key, out var typedValue))
        {
            return typedValue;
        }

        if (defaults is IDictionary dictionary)
        {
            if (dictionary.Contains(key))
            {
                return Convert.ToInt64(dictionary[key]);
            }

            var lowered = key.ToLowerInvariant();
            if (dictionary.Contains(lowered))
            {
                return Convert.ToInt64(dictionary[lowered]);
            }
        }

        var matchingProperty = defaults.GetType().GetProperty(key);
        if (matchingProperty != null)
        {
            var propertyValue = matchingProperty.GetValue(defaults);
            if (propertyValue != null && long.TryParse(propertyValue.ToString(), out var result))
            {
                return result;
            }
        }

        return null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

        private ByteArrayContent CreateFormContentCp1254(string payloadJson)
        {
            var pairs = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        string valueStr;
                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                valueStr = prop.Value.GetString() ?? string.Empty;
                                break;
                            case JsonValueKind.Number:
                                valueStr = prop.Value.GetRawText();
                                break;
                            case JsonValueKind.True:
                                valueStr = "true";
                                break;
                            case JsonValueKind.False:
                                valueStr = "false";
                                break;
                            case JsonValueKind.Null:
                                valueStr = string.Empty;
                                break;
                            default:
                                valueStr = prop.Value.GetRawText();
                                break;
                        }

                        var k = UrlEncodeCp1254(prop.Name ?? string.Empty);
                        var v = UrlEncodeCp1254(valueStr ?? string.Empty);
                        pairs.Add(k + "=" + v);
                    }
                }
            }
            catch
            {
                // fallback: send raw JSON as single field 'payload'
                var k = UrlEncodeCp1254("payload");
                var v = UrlEncodeCp1254(payloadJson ?? string.Empty);
                pairs.Add(k + "=" + v);
            }

            var form = string.Join("&", pairs);
            var bytes = _encoding.GetBytes(form);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "windows-1254" };
            return content;
        }

    private HttpContent CreateKozaContent(string json)
    {
        var payload = json ?? string.Empty;
        var content = new ByteArrayContent(_encoding.GetBytes(payload));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = _encoding.WebName
        };
        return content;
    }

    private void ApplyManualSessionCookie(HttpRequestMessage? request)
    {
        try
        {
            if (request == null) return;
            
            // 🔥 DEBUG: Cookie durumunu logla
            var cookieSource = "none";
            string? cookieToApply = null;
            
            // Öncelik sırası: 1) _manualJSessionId (login'den gelen), 2) _sessionCookie, 3) CookieContainer, 4) ManualSessionCookie (config)
            if (!string.IsNullOrWhiteSpace(_manualJSessionId))
            {
                cookieToApply = _manualJSessionId;
                cookieSource = "_manualJSessionId";
            }
            else if (!string.IsNullOrWhiteSpace(_sessionCookie))
            {
                cookieToApply = _sessionCookie;
                cookieSource = "_sessionCookie";
            }
            else
            {
                // CookieContainer'dan almayı dene
                var containerCookie = TryGetJSessionFromContainer();
                if (!string.IsNullOrWhiteSpace(containerCookie))
                {
                    cookieToApply = containerCookie.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase) 
                        ? containerCookie 
                        : "JSESSIONID=" + containerCookie;
                    cookieSource = "CookieContainer";
                }
                else if (!string.IsNullOrWhiteSpace(_settings?.ManualSessionCookie))
                {
                    cookieToApply = _settings.ManualSessionCookie;
                    cookieSource = "ManualSessionCookie(config)";
                }
            }
            
            if (string.IsNullOrWhiteSpace(cookieToApply)) 
            {
                _logger.LogDebug("🍪 ApplyManualSessionCookie: No cookie available to apply");
                return;
            }

            var trimmed = cookieToApply.Trim();
            if (trimmed.IndexOf("FILL_ME", StringComparison.OrdinalIgnoreCase) >= 0) 
            {
                _logger.LogDebug("🍪 ApplyManualSessionCookie: Cookie contains FILL_ME placeholder, skipping");
                return;
            }

            // Cookie formatını normalize et
            if (!trimmed.StartsWith("JSESSIONID=", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "JSESSIONID=" + trimmed;
            }

            if (!request.Headers.Contains("Cookie"))
            {
                request.Headers.TryAddWithoutValidation("Cookie", trimmed);
                _logger.LogDebug("🍪 ApplyManualSessionCookie: Applied cookie from {Source} (preview: {Preview})", 
                    cookieSource, 
                    trimmed.Length > 50 ? trimmed.Substring(0, 50) + "..." : trimmed);
            }
            else
            {
                _logger.LogDebug("🍪 ApplyManualSessionCookie: Cookie header already exists, skipping");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to apply manual session cookie to outgoing request");
        }
    }
    private void ValidateFaturaKapama(LucaFaturaKapamaDto dto, long belgeTurDetayId)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (FaturaKapamaCariRules.TryGetValue(belgeTurDetayId, out var rule) && dto.CariTur != rule.ExpectedCariTur)
        {
            throw new InvalidOperationException(rule.ErrorMessage);
        }
    }
    private static async Task<string> ReadContentPreviewAsync(HttpContent content)
    {
        if (content == null)
        {
            return string.Empty;
        }

        try
        {
            return await content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
        public async Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var queryDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endpoint = $"{_settings.Endpoints.Invoices}?fromDate={queryDate}";

            _logger.LogInformation("Fetching invoices from Luca since {Date}", queryDate);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var invoices = JsonSerializer.Deserialize<List<LucaInvoiceDto>>(content, _jsonOptions) ?? new List<LucaInvoiceDto>();

                _logger.LogInformation("Successfully fetched {Count} invoices from Luca", invoices.Count);
                return invoices;
            }
            else
            {
                _logger.LogError("Failed to fetch invoices from Luca. Status: {StatusCode}", response.StatusCode);
                return new List<LucaInvoiceDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoices from Luca");
            return new List<LucaInvoiceDto>();
        }
    }
    public async Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var queryDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            var endpoint = $"{_settings.Endpoints.Stock}?fromDate={queryDate}";

            _logger.LogInformation("Fetching stock movements from Luca since {Date}", queryDate);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var stockMovements = JsonSerializer.Deserialize<List<LucaStockDto>>(content, _jsonOptions) ?? new List<LucaStockDto>();

                _logger.LogInformation("Successfully fetched {Count} stock movements from Luca", stockMovements.Count);
                return stockMovements;
            }
            else
            {
                _logger.LogError("Failed to fetch stock movements from Luca. Status: {StatusCode}", response.StatusCode);
                return new List<LucaStockDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock movements from Luca");
            return new List<LucaStockDto>();
        }
    }
    public async Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null)
    {
        try
        {
            _logger.LogInformation("Fetching customers from Luca (fromDate={FromDate})", fromDate);
            var element = await ListCustomersAsync();
            var customers = new List<LucaCustomerDto>();

            JsonElement arrayEl = default;
            if (element.ValueKind == JsonValueKind.Array)
            {
                arrayEl = element;
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = data;
                }
                else if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    arrayEl = list;
                }
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Customer list response from Luca did not contain an array; returning empty list");
                return customers;
            }

            foreach (var item in arrayEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var dto = new LucaCustomerDto
                {
                    CustomerCode = TryGetProperty(item, "kod", "cariKodu") ?? string.Empty,
                    Title = TryGetProperty(item, "tanim", "cariTanim") ?? string.Empty,
                    TaxNo = TryGetProperty(item, "vergiNo", "vkn", "tcKimlikNo") ?? string.Empty,
                    ContactPerson = TryGetProperty(item, "yetkili", "yetkiliKisi"),
                    Phone = TryGetProperty(item, "telefon"),
                    Email = TryGetProperty(item, "email"),
                    Address = TryGetProperty(item, "adresSerbest", "adres"),
                    City = TryGetProperty(item, "il"),
                    Country = TryGetProperty(item, "ulke", "country")
                };

                customers.Add(dto);
            }

            _logger.LogInformation("Successfully fetched {Count} customers from Luca", customers.Count);
            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers from Luca");
            return new List<LucaCustomerDto>();
        }
    }
    public async Task<List<LucaProductDto>> FetchProductsAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Fetching products (stock cards) from Luca (Koza)...");

            
            var json = JsonSerializer.Serialize(new { }, _jsonOptions);
            var content = CreateKozaContent(json);

            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.StockCards)
            {
                Content = content
            };
            ApplyManualSessionCookie(httpRequest);
            httpRequest.Headers.Add("No-Paging", "true");

            var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch products from Luca. Status: {Status}", response.StatusCode);
                return new List<LucaProductDto>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(responseContent);

                
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("list", out var listEl) && listEl.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(listEl.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }

                    if (doc.RootElement.TryGetProperty("stkSkartList", out var skartList) && skartList.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(skartList.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }

                    if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<LucaProductDto>>(dataEl.GetRawText(), _jsonOptions) ?? new List<LucaProductDto>();
                    }
                }

                
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse products response from Luca; attempting generic deserialize");
            }

            
            return JsonSerializer.Deserialize<List<LucaProductDto>>(responseContent, _jsonOptions) ?? new List<LucaProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from Luca");
            return new List<LucaProductDto>();
        }
    }

    public async Task<List<LucaProductDto>> FetchProductsAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        var result = new List<LucaProductDto>();

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true
        };

        var baseAddr = !string.IsNullOrWhiteSpace(_settings.BaseUrl) ? new Uri(_settings.BaseUrl.TrimEnd('/') + "/") : null;

        using var client = new HttpClient(handler)
        {
            BaseAddress = baseAddr
        };

        try
        {
            var loggedIn = await PerformLoginOnClientAsync(client, cookieContainer, cancellationToken);
            if (!loggedIn)
            {
                _logger.LogError("[Luca] FetchProductsAsync: Login/branch selection failed.");
                return result;
            }

            var url = !string.IsNullOrWhiteSpace(_settings.Endpoints?.StockCards) ? _settings.Endpoints.StockCards : "ListeleStkSkart.do";

            var formPairs = new List<KeyValuePair<string, string>>();

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formPairs)
            };

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Luca] FetchProductsAsync: HTTP request failed.");
                return result;
            }

            var statusCode = (int)response.StatusCode;
            var rawBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            var encoding1254 = Encoding.GetEncoding(1254);
            var bodyText = encoding1254.GetString(rawBytes);

            try { await AppendRawLogAsync("FetchProducts", (client.BaseAddress?.ToString() ?? string.Empty) + url, 
                $"FORM:{string.Join("&", formPairs.Select(p => $"{p.Key}={p.Value}"))}",
                response.StatusCode, bodyText); } catch (Exception) { }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Luca] FetchProductsAsync: Response not successful. Status: {Status}", statusCode);
                return result;
            }

            if (IsJson(bodyText))
            {
                result = ParseKozaProductJson(bodyText);
            }
            else
            {
                result = ParseKozaProductHtml(bodyText);
            }

            _logger.LogInformation("[Luca] FetchProductsAsync: Parsed {Count} products from Koza.", result.Count);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Luca] FetchProductsAsync: cancelled");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Luca] FetchProductsAsync: unexpected error");
            return result;
        }
    }

    private bool IsJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        return (text.StartsWith("{") && text.EndsWith("}")) ||
               (text.StartsWith("[") && text.EndsWith("]"));
    }

    private List<Katana.Core.DTOs.LucaProductDto> ParseKozaProductJson(string json)
    {
        var list = new List<Katana.Core.DTOs.LucaProductDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement dataEl = default;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array) dataEl = d;
                else if (root.TryGetProperty("list", out var l) && l.ValueKind == JsonValueKind.Array) dataEl = l;
                else if (root.TryGetProperty("stkSkartList", out var s) && s.ValueKind == JsonValueKind.Array) dataEl = s;
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                dataEl = root;
            }

            if (dataEl.ValueKind != JsonValueKind.Array) return list;

            foreach (var item in dataEl.EnumerateArray())
            {
                var code = item.TryGetProperty("kartKodu", out var codeEl) ? codeEl.GetString() ?? string.Empty : string.Empty;
                var name = item.TryGetProperty("kartAdi", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                var category = item.TryGetProperty("kategoriAgacKod", out var catEl) ? catEl.GetString() : (item.TryGetProperty("kategori", out var cat2) ? cat2.GetString() : null);

                if (string.IsNullOrWhiteSpace(code)) continue;

                var dto = new Katana.Core.DTOs.LucaProductDto
                {
                    ProductCode = code,
                    ProductName = name,
                    Unit = item.TryGetProperty("olcumBirimi", out var u) ? u.GetString() : null
                };
                list.Add(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ParseKozaProductJson failed");
        }

        return list;
    }

    private List<Katana.Core.DTOs.LucaProductDto> ParseKozaProductHtml(string html)
    {
        var list = new List<Katana.Core.DTOs.LucaProductDto>();
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table[@id='grid']//tr[position()>1]")
                       ?? doc.DocumentNode.SelectNodes("//table//tr[position()>1]");
            if (rows == null) return list;

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 2) continue;

                var code = cells[0].InnerText.Trim();
                var name = cells.Count > 1 ? cells[1].InnerText.Trim() : string.Empty;
                var category = cells.Count > 2 ? cells[2].InnerText.Trim() : null;

                if (string.IsNullOrWhiteSpace(code)) continue;

                list.Add(new Katana.Core.DTOs.LucaProductDto
                {
                    ProductCode = WebUtility.HtmlDecode(code),
                    ProductName = WebUtility.HtmlDecode(name),
                    Unit = cells.Count > 3 ? WebUtility.HtmlDecode(cells[3].InnerText.Trim()) : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ParseKozaProductHtml failed");
        }
        return list;
    }

    private async Task<bool> PerformLoginOnClientAsync(HttpClient client, CookieContainer cookieContainer, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUri = client.BaseAddress ?? new Uri(_settings.BaseUrl?.TrimEnd('/') + "/");

            try
            {
                var getResp = await client.GetAsync(_settings.Endpoints.Auth ?? "Giris.do", cancellationToken);
                var getBody = await ReadResponseContentAsync(getResp);
                await AppendRawLogAsync("AUTH_LOGIN_GET_ONCLIENT", _settings.Endpoints.Auth, string.Empty, getResp.StatusCode, getBody);
            }
            catch (Exception)
            {
            }

            var loginAttempts = new List<(string desc, HttpContent content)>
            {
                ("JSON:orgCode_userName_userPassword", CreateKozaContent(
                    JsonSerializer.Serialize(new
                    {
                        orgCode = _settings.MemberNumber,
                        userName = _settings.Username,
                        userPassword = _settings.Password
                    }, _jsonOptions))),
                ("FORM:orgCode_user_girisForm.userPassword", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "orgCode", _settings.MemberNumber },
                    { "user", _settings.Username },
                    { "girisForm.userPassword", _settings.Password },
                    { "girisForm.captchaInput", string.Empty }
                })),
                ("FORM:orgCode_userName_userPassword", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "orgCode", _settings.MemberNumber },
                    { "userName", _settings.Username },
                    { "userPassword", _settings.Password }
                }))
            };

            foreach (var (desc, payload) in loginAttempts)
            {
                try
                {
                    var payloadText = await ReadContentPreviewAsync(payload);
                    var resp = await client.PostAsync(_settings.Endpoints.Auth, payload, cancellationToken);
                    var body = await ReadResponseContentAsync(resp);
                    await AppendRawLogAsync($"AUTH_LOGIN_ONCLIENT:{desc}", _settings.Endpoints.Auth, payloadText, resp.StatusCode, body);

                    try
                    {
                        if (cookieContainer != null)
                        {
                            var cookies = cookieContainer.GetCookies(baseUri);
                            var c = cookies.Cast<System.Net.Cookie>().FirstOrDefault(x => string.Equals(x.Name, "JSESSIONID", StringComparison.OrdinalIgnoreCase));
                            if (c != null && !string.IsNullOrWhiteSpace(c.Value))
                            {
                                return true;
                            }
                        }
                    }
                    catch { }

                    if (resp.IsSuccessStatusCode && IsKozaLoginSuccess(body))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Login attempt on client failed: {Desc}", desc);
                }
            }

            _logger.LogWarning("PerformLoginOnClientAsync: login attempts failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PerformLoginOnClientAsync threw");
            return false;
        }
    }
    
    public async Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _logger.LogInformation("Fetching delivery notes (irsaliye) from Luca");
            var element = await ListDeliveryNotesAsync(true);

            var results = new List<LucaDespatchDto>();

            
            JsonElement arrayEl = default;
            if (element.ValueKind == JsonValueKind.Array)
            {
                arrayEl = element;
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                    arrayEl = list;
                else if (element.TryGetProperty("irsaliyeList", out var il) && il.ValueKind == JsonValueKind.Array)
                    arrayEl = il;
                else if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    arrayEl = data;
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Delivery notes response did not contain an array; returning empty list");
                return results;
            }

            foreach (var item in arrayEl.EnumerateArray())
            {
                try
                {
                    var dto = new LucaDespatchDto();

                    if (item.TryGetProperty("belgeNo", out var bno))
                        dto.DocumentNo = bno.GetString() ?? string.Empty;

                    if (item.TryGetProperty("belgeTarihi", out var bdt))
                    {
                        if (bdt.ValueKind == JsonValueKind.String && DateTime.TryParse(bdt.GetString(), out var dt))
                            dto.DocumentDate = dt;
                        else if (bdt.ValueKind == JsonValueKind.Number && bdt.TryGetInt64(out var unix))
                            dto.DocumentDate = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
                    }

                    if (item.TryGetProperty("cariKodu", out var ck))
                        dto.CustomerCode = ck.GetString();

                    if (item.TryGetProperty("cariTanim", out var ct))
                        dto.CustomerTitle = ct.GetString();

                    
                    if (item.TryGetProperty("detayList", out var detay) && detay.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var line in detay.EnumerateArray())
                        {
                            try
                            {
                                var li = new LucaDespatchItemDto();
                                if (line.TryGetProperty("kartKodu", out var pk))
                                    li.ProductCode = pk.GetString() ?? string.Empty;
                                if (line.TryGetProperty("kartAdi", out var pn))
                                    li.ProductName = pn.GetString();
                                if (line.TryGetProperty("miktar", out var mq) && mq.ValueKind == JsonValueKind.Number)
                                    li.Quantity = mq.GetDecimal();
                                if (line.TryGetProperty("birimFiyat", out var up) && up.ValueKind == JsonValueKind.Number)
                                    li.UnitPrice = up.GetDecimal();
                                if (line.TryGetProperty("kdvOran", out var tr) && tr.ValueKind == JsonValueKind.Number)
                                    li.TaxRate = tr.GetDouble();

                                dto.Lines.Add(li);
                            }
                            catch (Exception) {  }
                        }
                    }

                    results.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse one delivery note item");
                }
            }

            _logger.LogInformation("Parsed {Count} delivery notes from Luca", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching delivery notes from Luca");
            return new List<LucaDespatchDto>();
        }
    }
    private bool NeedsBranchSelection(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;

        var lower = body.ToLowerInvariant();
        if (lower.Contains("şirket şube seçimi") || lower.Contains("sirket sube secimi") || lower.Contains("sube secimi yapilmali"))
            return true;

        if (lower.Contains("\"code\":1003") || lower.Contains("code\":1003") || lower.Contains("code\": 1003"))
            return true;

        return false;
    }

    /// <summary>
    /// Response'un HTML olup olmadığını kontrol eder (session timeout/login sayfası)
    /// </summary>
    private bool IsHtmlResponse(string? responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
            return false;

        var trimmed = responseContent.TrimStart();
        
        // HTML başlangıç tag'leri
        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Login sayfası veya error sayfası göstergeleri
        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("<title>") && lower.Contains("</title>") &&
            (lower.Contains("login") || lower.Contains("giriş") || lower.Contains("oturum") || lower.Contains("error")))
        {
            return true;
        }

        // HTML body tag'i varsa
        if (lower.Contains("<body") || lower.Contains("<head"))
        {
            return true;
        }

        return false;
    }

    private async Task AppendRawLogAsync(string tag, string? url, string requestBody, System.Net.HttpStatusCode? status, string responseBody)
    {
        // 🔥 FILE LOCK: Concurrent yazma sorununu önle
        await _fileLock.WaitAsync();
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);
            var file = Path.Combine(logDir, "luca-raw.log");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);
            sb.AppendLine("URL: " + (url ?? string.Empty));
            sb.AppendLine("Request:");
            sb.AppendLine(requestBody ?? string.Empty);
            sb.AppendLine("ResponseStatus: " + (status?.ToString() ?? "(null)"));
            sb.AppendLine("Response:");
            sb.AppendLine(responseBody ?? string.Empty);
            sb.AppendLine("----");

            await File.AppendAllTextAsync(file, sb.ToString());

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, "luca-raw.log");
                    await File.AppendAllTextAsync(repoFile, sb.ToString());
                }
            }
            catch (Exception)
            {
                // Repo log yazılamadı - kritik değil
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append raw Luca log");
        }
        finally
        {
            _fileLock.Release();
        }
    }
    private async Task SaveHttpTrafficAsync(string tag, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var safeTag = SanitizeFileName(tag ?? "traffic");
            var filePath = Path.Combine(logDir, $"{safeTag}-http-{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);

            var reqMsg = request ?? response?.RequestMessage;
            if (reqMsg != null)
            {
                sb.AppendLine("RequestUri: " + (reqMsg.RequestUri?.ToString() ?? string.Empty));
                sb.AppendLine("RequestMethod: " + reqMsg.Method.Method);
                sb.AppendLine("Request Headers:");
                foreach (var h in reqMsg.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (reqMsg.Content != null)
                {
                    foreach (var h in reqMsg.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }
            }
            else
            {
                sb.AppendLine("Request: (null)");
            }

            if (response != null)
            {
                sb.AppendLine("Response Status: " + response.StatusCode);
                sb.AppendLine("Response Headers:");
                foreach (var h in response.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (response.Content != null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }

                
                if (response.Headers.TryGetValues("Set-Cookie", out var scs))
                {
                    sb.AppendLine("Set-Cookie:");
                    foreach (var s in scs) sb.AppendLine(s);
                }
            }

            
            try
            {
                var cookieContainerLocal = _cookieContainer;
                if (cookieContainerLocal != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                {
                    var uri = new Uri(_settings.BaseUrl);
                    var cookieCol = cookieContainerLocal.GetCookies(uri);
                    var list = new List<object>();
                    foreach (System.Net.Cookie ck in cookieCol)
                    {
                        list.Add(new
                        {
                            ck.Name,
                            ck.Value,
                            ck.Domain,
                            ck.Path,
                            Expires = ck.Expires == DateTime.MinValue ? (DateTime?)null : ck.Expires,
                            ck.Secure,
                            ck.HttpOnly
                        });
                    }
                    var cookieFile = Path.Combine(logDir, $"{safeTag}-cookies-{timestamp}.json");
                    await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("CookiesFile: " + cookieFile);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Cookie dump failed: " + ex.Message);
            }

            sb.AppendLine("----");

            try
            {
                await File.WriteAllTextAsync(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogWarning(ex, "Failed to write http traffic file '{FilePath}', falling back to safe filename.", filePath);
                }
                catch { }

                var fallback = Path.Combine(logDir, $"http-traffic-{timestamp}-{Guid.NewGuid().ToString("N").Substring(0,8)}.txt");
                await File.WriteAllTextAsync(fallback, sb.ToString());
                filePath = fallback;
            }

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, Path.GetFileName(filePath));
                    try
                    {
                        await File.WriteAllTextAsync(repoFile, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        try { _logger.LogWarning(ex, "Failed to write repo-copy of http traffic file '{RepoFile}'", repoFile); } catch { }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save HTTP traffic diagnostics");
        }
    }
    private async Task<string?> SaveHttpTrafficAndGetFilePathAsync(string tag, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var safeTag = SanitizeFileName(tag ?? "traffic");
            var filePath = Path.Combine(logDir, $"{safeTag}-http-{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("----");
            sb.AppendLine(DateTime.UtcNow.ToString("o") + " " + tag);

            var reqMsg = request ?? response?.RequestMessage;
            if (reqMsg != null)
            {
                sb.AppendLine("RequestUri: " + (reqMsg.RequestUri?.ToString() ?? string.Empty));
                sb.AppendLine("RequestMethod: " + reqMsg.Method.Method);
                sb.AppendLine("Request Headers:");
                foreach (var h in reqMsg.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (reqMsg.Content != null)
                {
                    foreach (var h in reqMsg.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }
            }
            else
            {
                sb.AppendLine("Request: (null)");
            }

            if (response != null)
            {
                sb.AppendLine("Response Status: " + response.StatusCode);
                sb.AppendLine("Response Headers:");
                foreach (var h in response.Headers)
                {
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                }
                if (response.Content != null)
                {
                    foreach (var h in response.Content.Headers)
                    {
                        sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    }
                }

                if (response.Headers.TryGetValues("Set-Cookie", out var scs))
                {
                    sb.AppendLine("Set-Cookie:");
                    foreach (var s in scs) sb.AppendLine(s);
                }
            }
            try
            {
                var cookieContainerLocal = _cookieContainer;
                if (cookieContainerLocal != null && !string.IsNullOrWhiteSpace(_settings.BaseUrl))
                {
                    var uri = new Uri(_settings.BaseUrl);
                    var cookieCol = cookieContainerLocal.GetCookies(uri);
                    var list = new List<object>();
                    foreach (System.Net.Cookie ck in cookieCol)
                    {
                        list.Add(new
                        {
                            ck.Name,
                            ck.Value,
                            ck.Domain,
                            ck.Path,
                            Expires = ck.Expires == DateTime.MinValue ? (DateTime?)null : ck.Expires,
                            ck.Secure,
                            ck.HttpOnly
                        });
                    }
                    var cookieFile = Path.Combine(logDir, $"{safeTag}-cookies-{timestamp}.json");
                    await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("CookiesFile: " + cookieFile);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Cookie dump failed: " + ex.Message);
            }

            sb.AppendLine("----");
            await File.WriteAllTextAsync(filePath, sb.ToString());

            try
            {
                var cwd = Directory.GetCurrentDirectory();
                var repoLogDir = Path.Combine(cwd, "logs");
                if (!string.Equals(repoLogDir, logDir, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(repoLogDir);
                    var repoFile = Path.Combine(repoLogDir, Path.GetFileName(filePath));
                    await File.WriteAllTextAsync(repoFile, sb.ToString());
                    return repoFile;
                }
            }
            catch (Exception)
            {
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save HTTP traffic diagnostics (and return file path)");
            return null;
        }
    }

    private async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim().ToLowerInvariant();
        var bytes = await response.Content.ReadAsByteArrayAsync();

        
        if (!string.IsNullOrWhiteSpace(charset))
        {
            if (charset.Contains("1254") || charset.Contains("iso-8859-9"))
            {
                try { return _encoding.GetString(bytes); } catch {  }
            }
            if (charset.Contains("utf-8"))
            {
                try { return Encoding.UTF8.GetString(bytes); } catch {  }
            }
        }

        
        try { return Encoding.UTF8.GetString(bytes); } catch {  }
        try { return _encoding.GetString(bytes); } catch {  }
        return string.Empty;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "file";
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        var s = sb.ToString();
        while (s.Contains("__")) s = s.Replace("__", "_");
        if (s.Length > 120) s = s.Substring(0, 120);
        s = s.TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(s)) return "file";
        return s;
    }
    private static long TryParseId(string responseContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Number && root.TryGetInt64(out var num))
            {
                return num;
            }
            string[] idKeys = { "id", "faturaId", "irsaliyeId", "ssIrsaliyeBaslikId", "ssSiparisBaslikId", "belgeId", "entityId" };
            foreach (var key in idKeys)
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var parsed))
                        return parsed;
                    if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsedStr))
                        return parsedStr;
                }
            }
        }
        catch
        {
            
        }
        return 0;
    }
    private List<T> DeserializeList<T>(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<T>>(element.GetRawText(), _jsonOptions) ?? new List<T>();
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(data.GetRawText(), _jsonOptions) ?? new List<T>();
            }
            if (element.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(list.GetRawText(), _jsonOptions) ?? new List<T>();
            }
        }
        return new List<T>();
    }

    /// <summary>
    /// Search for a stock card by SKU/KartKodu in Luca.
    /// Returns the skartId if found, null if not found.
    /// </summary>
    public async Task<long?> FindStockCardBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        // 🔥 CACHE KONTROLÜ: Aynı session'da tekrar sorgulamayı önle
        await _stockCardCacheLock.WaitAsync();
        try
        {
            if (_stockCardCache.TryGetValue(sku, out var cachedId))
            {
                _logger.LogDebug("🔄 Cache HIT: {SKU} → {Id}", sku, cachedId);
                return cachedId;
            }
        }
        finally
        {
            _stockCardCacheLock.Release();
        }

        try
        {
            _logger.LogDebug("🔍 Luca'da stok kartı aranıyor: {SKU}", sku);
            
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter
                {
                    KodBas = sku,
                    KodBit = sku,
                    KodOp = "between"
                }
            };

            var result = await ListStockCardsAsync(request);

            // 🔥 BOŞ/GEÇERSİZ RESPONSE KONTROLÜ
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("⚠️ Luca'dan geçersiz response geldi (Undefined/Null) - SKU: {SKU}", sku);
                return null;
            }

            // Boş array kontrolü
            if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() == 0)
            {
                _logger.LogInformation("ℹ️ Stok kartı bulunamadı (boş liste): {SKU}", sku);
                return null;
            }

            if (result.ValueKind == JsonValueKind.Object)
            {
                // Check for "list" array
                if (result.TryGetProperty("list", out var listProp) && listProp.ValueKind == JsonValueKind.Array)
                {
                    if (listProp.GetArrayLength() == 0)
                    {
                        _logger.LogInformation("ℹ️ Stok kartı bulunamadı (list boş): {SKU}", sku);
                        return null;
                    }

                    foreach (var item in listProp.EnumerateArray())
                    {
                        // KartKodu eşleşmesi kontrol et
                        var kartKodu = item.TryGetProperty("kod", out var kodProp) ? kodProp.GetString() :
                                       item.TryGetProperty("kartKodu", out var kartKoduProp) ? kartKoduProp.GetString() : null;

                        // SKU eşleşmesi kontrolü (case-insensitive)
                        if (!string.IsNullOrEmpty(kartKodu) && 
                            kartKodu.Trim().Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            if (item.TryGetProperty("skartId", out var skartIdProp))
                            {
                                long? skartId = null;
                                if (skartIdProp.ValueKind == JsonValueKind.Number)
                                    skartId = skartIdProp.GetInt64();
                                else if (skartIdProp.ValueKind == JsonValueKind.String && long.TryParse(skartIdProp.GetString(), out var parsed))
                                    skartId = parsed;

                                if (skartId.HasValue)
                                {
                                    _logger.LogInformation("✅ Stok kartı bulundu: {SKU} → skartId: {SkartId}", sku, skartId.Value);
                                    
                                    // ✅ Cache'e ekle
                                    await _stockCardCacheLock.WaitAsync();
                                    try
                                    {
                                        _stockCardCache[sku] = skartId;
                                    }
                                    finally
                                    {
                                        _stockCardCacheLock.Release();
                                    }
                                    
                                    return skartId;
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("ℹ️ Stok kartı bulunamadı: {SKU}", sku);
            
            // ✅ Bulunamayan kartları da cache'e ekle (tekrar sorgulamayı önle)
            await _stockCardCacheLock.WaitAsync();
            try
            {
                _stockCardCache[sku] = null;
            }
            finally
            {
                _stockCardCacheLock.Release();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ FindStockCardBySkuAsync error for '{SKU}': {Message}", sku, ex.Message);
            return null; // ✅ HATA DURUMUNDA NULL DÖN
        }
    }

    /// <summary>
    /// Luca'daki stok kartı detaylarını getir (karşılaştırma için)
    /// </summary>
    public async Task<LucaStockCardDetails?> GetStockCardDetailsBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        try
        {
            _logger.LogDebug("🔍 Luca'da stok kartı detayları getiriliyor: {SKU}", sku);
            
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            var request = new LucaListStockCardsRequest
            {
                StkSkart = new LucaStockCardCodeFilter
                {
                    KodBas = sku,
                    KodBit = sku,
                    KodOp = "between"
                }
            };

            var result = await ListStockCardsAsync(request);

            // 🔥 CRITICAL: Raw JSON'u logla (debugging için)
            var rawJson = result.GetRawText();
            _logger.LogInformation("📊 LUCA RAW RESPONSE for SKU '{SKU}': {RawJsonPreview}", 
                sku, rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson);

            // 🔥 BOŞ/GEÇERSİZ RESPONSE KONTROLÜ
            if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("⚠️ GetStockCardDetailsBySkuAsync: Geçersiz response (Undefined/Null) - SKU: {SKU}", sku);
                return null;
            }

            if (result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("list", out var listProp) && 
                listProp.ValueKind == JsonValueKind.Array)
            {
                if (listProp.GetArrayLength() == 0)
                {
                    _logger.LogInformation("ℹ️ Stok kartı detayları bulunamadı (list boş): {SKU}", sku);
                    return null;
                }

                foreach (var item in listProp.EnumerateArray())
                {
                    // Kod eşleşmesi kontrol et
                    var kartKodu = item.TryGetProperty("kod", out var kodProp) ? kodProp.GetString() : 
                                   item.TryGetProperty("kartKodu", out var kartKoduProp) ? kartKoduProp.GetString() : null;
                    
                    if (!string.Equals(kartKodu?.Trim(), sku.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 🔥 CRITICAL: Available fields'ı logla
                    var availableFields = string.Join(", ", item.EnumerateObject().Select(p => p.Name));
                    _logger.LogInformation("📦 Available fields for SKU '{SKU}': {Fields}", sku, availableFields);

                    // ✅ Çoklu field kontrolü - hangi field dolu ise onu kullan
                    var kartAdi = item.TryGetProperty("KartAdi", out var kartAdiProp) ? kartAdiProp.GetString() :
                                  item.TryGetProperty("kartAdi", out var kartAdi2Prop) ? kartAdi2Prop.GetString() :
                                  item.TryGetProperty("tanim", out var tanimProp) ? tanimProp.GetString() :
                                  item.TryGetProperty("stokKartAdi", out var stokAdiProp) ? stokAdiProp.GetString() :
                                  item.TryGetProperty("adi", out var adiProp) ? adiProp.GetString() :
                                  item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() :
                                  sku; // Son çare: SKU'yu kullan

                    _logger.LogInformation("✅ KartAdi extracted: '{KartAdi}' for SKU: {SKU}", kartAdi, sku);

                    var details = new LucaStockCardDetails
                    {
                        SkartId = item.TryGetProperty("skartId", out var idProp) && idProp.ValueKind == JsonValueKind.Number 
                            ? idProp.GetInt64() : 0,
                        KartKodu = kartKodu ?? sku,
                        KartAdi = kartAdi, // Artık asla null olmaz
                        KartTuru = item.TryGetProperty("kartTuru", out var turuProp) && turuProp.ValueKind == JsonValueKind.Number 
                            ? turuProp.GetInt32() : 1,
                        OlcumBirimiId = item.TryGetProperty("olcumBirimiId", out var obProp) && obProp.ValueKind == JsonValueKind.Number 
                            ? obProp.GetInt64() : 1,
                        KartAlisKdvOran = item.TryGetProperty("kartAlisKdvOran", out var akdvProp) && akdvProp.ValueKind == JsonValueKind.Number 
                            ? akdvProp.GetDouble() : 0,
                        KartSatisKdvOran = item.TryGetProperty("kartSatisKdvOran", out var skdvProp) && skdvProp.ValueKind == JsonValueKind.Number 
                            ? skdvProp.GetDouble() : 0,
                        KartTipi = item.TryGetProperty("kartTipi", out var tipiProp) && tipiProp.ValueKind == JsonValueKind.Number 
                            ? tipiProp.GetInt32() : 1,
                        KategoriAgacKod = item.TryGetProperty("kategoriAgacKod", out var katProp) ? katProp.GetString() : null,
                        Barkod = item.TryGetProperty("barkod", out var barkodProp) ? barkodProp.GetString() : null,
                        // Fiyat alanları - karşılaştırma için
                        SatisFiyat = TryGetDoubleProperty(item, "perakendeSatisBirimFiyat", "satisFiyat", "salesPrice", "fiyat"),
                        AlisFiyat = TryGetDoubleProperty(item, "perakendeAlisBirimFiyat", "alisFiyat", "purchasePrice")
                    };

                    _logger.LogInformation("✅ Stok kartı detayları bulundu: {SKU} → KartAdi: {KartAdi}, SkartId: {SkartId}", 
                        sku, details.KartAdi ?? "(boş)", details.SkartId);
                    return details;
                }
            }

            _logger.LogInformation("ℹ️ Stok kartı detayları bulunamadı: {SKU}", sku);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ GetStockCardDetailsBySkuAsync error for '{SKU}': {Message}", sku, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Yeni gelen stok kartını Luca'daki mevcut kartla karşılaştır
    /// Farklılık varsa true döner (yeni kart açılmalı)
    /// NOT: Luca API'si bazı alanları boş döndürüyor, bu yüzden sadece güvenilir alanları karşılaştırıyoruz
    /// </summary>
    public bool HasStockCardChanges(LucaCreateStokKartiRequest newCard, LucaStockCardDetails? existingCard)
    {
        // CRITICAL: NULL kontrolü - Luca'dan veri gelmezse yeni kayıt olarak işle
        if (existingCard == null)
        {
            _logger.LogWarning("Stok kartı bulunamadı: {KartKodu}, yeni kayıt olarak işlenecek", newCard.KartKodu);
            return true; // Yeni kayıt olarak oluştur
        }

        // Parse hatasını yakala - KartKodu boşsa veri güvenilir değil
        if (string.IsNullOrEmpty(existingCard.KartKodu))
        {
            _logger.LogError("❌ Luca'dan dönen data eksik (KartKodu boş): {KartKodu}", newCard.KartKodu);
            _logger.LogDebug("Existing data: KartKodu={ExistingKartKodu}, KartAdi={KartAdi}, SkartId={SkartId}", 
                existingCard.KartKodu ?? "(null)", existingCard.KartAdi ?? "(null)", existingCard.SkartId);
            return false; // Atlama yap, hata logla - güvenli taraf
        }

        // 🔥 KRİTİK: Luca'dan gelen data güvenilir mi kontrol et
        // KartAdi boşsa fallback kullan
        if (string.IsNullOrWhiteSpace(existingCard.KartAdi))
        {
            _logger.LogWarning("⚠️ Luca'dan KartAdi boş geldi, SKU fallback kullanılıyor: {KartKodu}", newCard.KartKodu);
            existingCard.KartAdi = existingCard.KartKodu ?? newCard.KartKodu; // SKU'yu kullan
            _logger.LogDebug("Fallback applied: KartAdi set to '{FallbackKartAdi}' for SKU: {SKU}", 
                existingCard.KartAdi, existingCard.KartKodu);
        }

        // 🔥 BOŞ OBJECT KONTROLÜ - HTML parse hatası sonucu boş object oluşmuş olabilir
        // Tüm önemli alanlar boşsa bu güvenilir değil
        if (existingCard.SkartId == 0 &&
            !existingCard.SatisFiyat.HasValue &&
            string.IsNullOrWhiteSpace(existingCard.KategoriAgacKod))
        {
            _logger.LogError("❌ Luca'dan dönen data boş object (HTML parse hatası olabilir): {KartKodu}. Güvenli taraf: değişiklik yok sayılıyor.", newCard.KartKodu);
            return false; // Güvenli taraf: Atlama yap
        }

        try
        {
            // Sadece güvenilir alanları karşılaştır
            // Luca API'si kartAdi, kdvOran gibi alanları bazen boş/0 döndürüyor
            bool hasChanges = false;
            var changeReasons = new List<string>();

            // 🔥 DEBUG: Karşılaştırma öncesi değerleri logla
            _logger.LogDebug("🔍 KARŞILAŞTIRMA BAŞLIYOR: {KartKodu}", newCard.KartKodu);
            _logger.LogDebug("   Katana KartAdi: '{KatanaAdi}'", newCard.KartAdi ?? "(null)");
            _logger.LogDebug("   Luca KartAdi: '{LucaAdi}'", existingCard.KartAdi ?? "(null)");
            _logger.LogDebug("   Katana Fiyat: {KatanaFiyat}", newCard.PerakendeSatisBirimFiyat);
            _logger.LogDebug("   Luca Fiyat: {LucaFiyat}", existingCard.SatisFiyat ?? 0);
            _logger.LogDebug("   Katana Kategori: '{KatanaKategori}'", newCard.KategoriAgacKod ?? "(null)");
            _logger.LogDebug("   Luca Kategori: '{LucaKategori}'", existingCard.KategoriAgacKod ?? "(null)");

            // KartAdi karşılaştırması - sadece her iki tarafta da doluysa
            // 🔥 Türkçe karakter toleranslı karşılaştırma (Luca ? karakteri sorunu)
            if (!string.IsNullOrWhiteSpace(newCard.KartAdi) && !string.IsNullOrWhiteSpace(existingCard.KartAdi))
            {
                var areNamesEqual = AreEqualIgnoringTurkishChars(newCard.KartAdi, existingCard.KartAdi);
                _logger.LogInformation("🔍 İSİM KARŞILAŞTIRMASI: Katana='{KatanaAdi}' (len={KatanaLen}) vs Luca='{LucaAdi}' (len={LucaLen}) => Eşit={AreEqual}",
                    newCard.KartAdi, newCard.KartAdi?.Length ?? 0, 
                    existingCard.KartAdi, existingCard.KartAdi?.Length ?? 0,
                    areNamesEqual);
                
                if (!areNamesEqual)
                {
                    hasChanges = true;
                    changeReasons.Add($"KartAdi: '{existingCard.KartAdi}' -> '{newCard.KartAdi}'");
                }
            }

            // Fiyat karşılaştırması - Luca'da fiyat bilgisi varsa karşılaştır
            // NOT: Luca stok kartlarında fiyat genellikle null/0 olarak gelir
            // Fiyat bilgisi ayrı bir yerde (cari hesap, fatura vb.) tutulur
            // Bu yüzden Luca'da fiyat 0 veya null ise karşılaştırmayı atlıyoruz
            var existingPrice = existingCard.SatisFiyat ?? 0;
            var newPrice = newCard.PerakendeSatisBirimFiyat;
            // Sadece Luca'da gerçek bir fiyat varsa (0'dan büyük) karşılaştır
            if (existingPrice > 0.01 && Math.Abs(newPrice - existingPrice) > 0.01)
            {
                hasChanges = true;
                changeReasons.Add($"Fiyat: {existingPrice:N2} -> {newPrice:N2}");
            }

            // Kategori karşılaştırması - Katana'da kategori varsa ve Luca'dakinden farklıysa
            if (!string.IsNullOrWhiteSpace(newCard.KategoriAgacKod))
            {
                var lucaKategori = existingCard.KategoriAgacKod?.Trim() ?? string.Empty;
                var katanaKategori = newCard.KategoriAgacKod.Trim();
                if (!string.Equals(katanaKategori, lucaKategori, StringComparison.OrdinalIgnoreCase))
                {
                    hasChanges = true;
                    changeReasons.Add($"Kategori: '{lucaKategori}' -> '{katanaKategori}'");
                }
            }

            // 🔥 MİKTAR DEĞİŞİKLİĞİ KONTROLÜ - Kullanıcı isteği üzerine eklendi
            // NOT: Stok kartı oluşturma sırasında miktar bilgisi genellikle gönderilmez
            // Miktar değişikliği stok hareketi (DSH) ile yapılır, stok kartı güncellemesi ile değil
            // Ancak kullanıcı miktar değişikliğini de algılamak istiyor
            // Bu durumda yeni versiyonlu stok kartı açılacak
            // Miktar bilgisi varsa karşılaştır
            if (existingCard.Miktar.HasValue && newCard.Miktar.HasValue)
            {
                if (Math.Abs(existingCard.Miktar.Value - newCard.Miktar.Value) > 0.001)
                {
                    hasChanges = true;
                    changeReasons.Add($"Miktar: {existingCard.Miktar.Value:N2} -> {newCard.Miktar.Value:N2}");
                }
            }

            if (hasChanges)
            {
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogWarning("🔄 ÜRÜN DEĞİŞİKLİĞİ TESPİT EDİLDİ: {KartKodu}", newCard.KartKodu);
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                
                foreach (var reason in changeReasons)
                {
                    _logger.LogWarning("   📝 {Reason}", reason);
                }
                
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogWarning("⚡ AKSIYON: Luca API güncelleme desteklemiyor");
                _logger.LogWarning("   → Yeni versiyonlu SKU ile stok kartı oluşturulacak");
                _logger.LogWarning("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                _logger.LogInformation("✅ Stok kartı '{KartKodu}' - Değişiklik yok, atlanıyor", newCard.KartKodu);
            }

            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HasStockCardChanges hatası: {KartKodu}", newCard.KartKodu);
            return false; // Güvenli taraf: Değişiklik yok say
        }
    }

    /// <summary>
    /// Stok kartı için versiyon numarası oluştur (ör: SKU-V2, SKU-V3)
    /// </summary>
    public async Task<string> GenerateVersionedSkuAsync(string baseSku)
    {
        _logger.LogInformation("🔢 Versiyonlu SKU oluşturuluyor: {BaseSku}", baseSku);
        
        // Önce base SKU ile başlayan tüm kartları bul
        var version = 2;
        var maxVersion = 10; // Makul bir üst limit

        while (version <= maxVersion)
        {
            var versionedSku = $"{baseSku}-V{version}";
            _logger.LogDebug("   Kontrol ediliyor: {VersionedSku}", versionedSku);
            
            var exists = await FindStockCardBySkuAsync(versionedSku);
            
            if (!exists.HasValue)
            {
                _logger.LogInformation("✅ Uygun versiyon bulundu: {VersionedSku}", versionedSku);
                return versionedSku;
            }
            
            _logger.LogDebug("   ❌ {VersionedSku} zaten mevcut, sonraki versiyon deneniyor...", versionedSku);
            version++;
        }

        // Fallback: timestamp ekle
        var timestampSku = $"{baseSku}-{DateTime.Now:yyyyMMddHHmm}";
        _logger.LogWarning("⚠️ Maksimum versiyon sayısına ulaşıldı (V{MaxVersion}), timestamp kullanılıyor: {Sku}", maxVersion, timestampSku);
        return timestampSku;
    }

    /// <summary>
    /// UPSERT: If stock card exists in Luca, mark as duplicate (API doesn't support update).
    /// If not exists, create new card.
    /// </summary>
    public async Task<SyncResultDto> UpsertStockCardAsync(LucaCreateStokKartiRequest stockCard)
    {
        var result = new SyncResultDto
        {
            SyncType = "STOCK_CARD_UPSERT",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            var sku = stockCard.KartKodu;
            
            // First, check if the card already exists
            var existingSkartId = await FindStockCardBySkuAsync(sku);
            
            if (existingSkartId.HasValue)
            {
                // Card already exists in Luca
                // NOTE: Luca Koza API does NOT support stock card updates!
                // The card already exists, so we mark it as "duplicate" (already synced)
                result.DuplicateRecords = 1;
                result.IsSuccess = true;
                result.Message = $"Stok kartı '{sku}' zaten Luca'da mevcut (skartId: {existingSkartId.Value}). Luca API stok kartı güncellemesini desteklemiyor.";
                _logger.LogInformation("Stock card {SKU} already exists in Luca with skartId {SkartId}. Luca API does not support updates.", sku, existingSkartId.Value);
                return result;
            }

            // Card doesn't exist, create new
            var sendResult = await SendStockCardsAsync(new List<LucaCreateStokKartiRequest> { stockCard });
            
            result.IsSuccess = sendResult.IsSuccess || sendResult.DuplicateRecords > 0;
            result.SuccessfulRecords = sendResult.SuccessfulRecords;
            result.FailedRecords = sendResult.FailedRecords;
            result.DuplicateRecords = sendResult.DuplicateRecords;
            result.Errors = sendResult.Errors;
            result.Message = sendResult.IsSuccess 
                ? $"Stok kartı '{sku}' Luca'ya başarıyla eklendi."
                : $"Stok kartı '{sku}' Luca'ya eklenemedi: {string.Join(", ", sendResult.Errors)}";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting stock card {SKU} to Luca", stockCard.KartKodu);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"{stockCard.KartKodu}: {ex.Message}");
            result.Message = $"Stok kartı işlenirken hata: {ex.Message}";
            return result;
        }
    }

    #region Cari Kart (Customer) Methods

    /// <summary>
    /// Luca'da cari kart arar (kartKodu bazlı)
    /// </summary>
    public async Task<long?> FindCariCardByCodeAsync(string kartKodu)
    {
        if (string.IsNullOrWhiteSpace(kartKodu))
            return null;

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // ListeleFinMusteri.do ile ara
            var request = new LucaListCariKartRequest
            {
                FinMusteri = new LucaCariKartListFilter
                {
                    GnlFinansalNesne = new LucaCariKartFilter
                    {
                        KodBas = kartKodu,
                        KodBit = kartKodu,
                        KodOp = "between"
                    }
                }
            };

            var result = await ListCustomersAsync(new LucaListCustomersRequest());
            
            if (result.ValueKind == JsonValueKind.Object)
            {
                if (result.TryGetProperty("list", out var listProp) && listProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in listProp.EnumerateArray())
                    {
                        // kartKodu kontrolü
                        if (item.TryGetProperty("kod", out var kodProp) && 
                            kodProp.ValueKind == JsonValueKind.String &&
                            string.Equals(kodProp.GetString(), kartKodu, StringComparison.OrdinalIgnoreCase))
                        {
                            // finansalNesneId al
                            if (item.TryGetProperty("finansalNesneId", out var idProp))
                            {
                                if (idProp.ValueKind == JsonValueKind.Number)
                                    return idProp.GetInt64();
                                if (idProp.ValueKind == JsonValueKind.String && long.TryParse(idProp.GetString(), out var parsed))
                                    return parsed;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Cari kart with code {KartKodu} not found in Luca", kartKodu);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for cari kart by code {KartKodu} in Luca", kartKodu);
            return null;
        }
    }

    /// <summary>
    /// Luca'da cari kart günceller
    /// NOT: Luca Koza API'de cari kart güncelleme endpoint'i sınırlı olabilir
    /// </summary>
    public async Task<SyncResultDto> UpdateCariCardAsync(LucaUpdateCustomerFullRequest request)
    {
        var result = new SyncResultDto
        {
            SyncType = "CARI_CARD_UPDATE",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // NOT: Luca Koza API'de GuncelleFinMusteriWS.do yoksa bu çalışmaz
            // Şu an için sadece log bırakıyoruz
            _logger.LogWarning("Cari kart güncelleme henüz desteklenmiyor. KartKod: {KartKod}", request.KartKod);
            
            result.IsSuccess = false;
            result.Message = "Luca API cari kart güncelleme desteklemiyor. Manuel güncelleme gerekli.";
            result.Errors.Add($"{request.KartKod}: API does not support customer updates");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cari kart {KartKod} in Luca", request.KartKod);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"{request.KartKod}: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// UPSERT: Cari kart varsa duplicate olarak işaretle (güncelleme yok), yoksa oluştur
    /// </summary>
    public async Task<SyncResultDto> UpsertCariCardAsync(Customer customer)
    {
        var result = new SyncResultDto
        {
            SyncType = "CARI_CARD_UPSERT",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            // ÖNEMLİ: Branch seçimi zorunlu (1003 hatası önleme)
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();
            
            var kartKodu = customer.LucaCode ?? customer.GenerateLucaCode();
            
            // Önce Luca'da ara
            var existingId = await FindCariCardByCodeAsync(kartKodu);
            
            if (existingId.HasValue)
            {
                // Zaten var - Luca API güncelleme desteklemediği için sadece log
                result.DuplicateRecords = 1;
                result.IsSuccess = true;
                result.Message = $"Cari kart '{kartKodu}' zaten Luca'da mevcut (finansalNesneId: {existingId.Value}). Luca API güncelleme desteklemiyor.";
                _logger.LogInformation("Cari kart {KartKodu} already exists in Luca with finansalNesneId {Id}. API does not support updates.", 
                    kartKodu, existingId.Value);
                return result;
            }

            // Yeni kart oluştur
            var createRequest = MappingHelper.MapToLucaCustomerCreate(customer);
            var createResult = await CreateCustomerAsync(createRequest);
            
            // Sonucu kontrol et
            if (createResult.ValueKind == JsonValueKind.Object)
            {
                if (createResult.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.True)
                {
                    var msg = createResult.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Unknown error";
                    
                    // Duplicate kontrolü
                    if (msg?.Contains("daha önce kullanılmış", StringComparison.OrdinalIgnoreCase) == true ||
                        msg?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.DuplicateRecords = 1;
                        result.IsSuccess = true;
                        result.Message = $"Cari kart '{kartKodu}' zaten Luca'da mevcut.";
                        return result;
                    }
                    
                    result.IsSuccess = false;
                    result.FailedRecords = 1;
                    result.Errors.Add($"{kartKodu}: {msg}");
                    return result;
                }
                
                // Başarılı - finansalNesneId al
                if (createResult.TryGetProperty("finansalNesneId", out var idProp))
                {
                    long newId = 0;
                    if (idProp.ValueKind == JsonValueKind.Number)
                        newId = idProp.GetInt64();
                    else if (idProp.ValueKind == JsonValueKind.String)
                        long.TryParse(idProp.GetString(), out newId);
                    
                    result.IsSuccess = true;
                    result.SuccessfulRecords = 1;
                    result.Message = $"Cari kart '{kartKodu}' Luca'ya başarıyla eklendi (finansalNesneId: {newId}).";
                    result.Details.Add($"finansalNesneId={newId}");
                    return result;
                }
            }
            
            result.IsSuccess = true;
            result.SuccessfulRecords = 1;
            result.Message = $"Cari kart '{kartKodu}' Luca'ya gönderildi.";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting cari kart for customer {CustomerId} to Luca", customer.Id);
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add($"Customer {customer.Id}: {ex.Message}");
            result.Message = $"Cari kart işlenirken hata: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Müşteri adresini Luca'ya gönderir
    /// </summary>
    public async Task<SyncResultDto> SendCustomerAddressAsync(long finansalNesneId, string address, string? city, string? district, bool isDefault = true)
    {
        var result = new SyncResultDto
        {
            SyncType = "CUSTOMER_ADDRESS",
            ProcessedRecords = 1,
            SyncTime = DateTime.UtcNow
        };

        try
        {
            await EnsureAuthenticatedAsync();
            await EnsureBranchSelectedAsync();

            // EkleWSGnlSsAdres.do endpoint'i
            var endpoint = "EkleWSGnlSsAdres.do";
            var client = _settings.UseTokenAuth ? _httpClient : _cookieHttpClient ?? _httpClient;

            var payload = new
            {
                finansalNesneId = finansalNesneId,
                adresTipId = 1, // 1=Fatura adresi
                ulke = "TURKIYE",
                il = city,
                ilce = district,
                adresSerbest = address,
                varsayilanFlag = isDefault ? 1 : 0
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            ApplyManualSessionCookie(request);

            var response = await client.SendAsync(request);
            var responseContent = await ReadResponseContentAsync(response);

            _logger.LogInformation("SendCustomerAddress response: {Response}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.SuccessfulRecords = 1;
                result.Message = "Adres başarıyla eklendi.";
            }
            else
            {
                result.IsSuccess = false;
                result.FailedRecords = 1;
                result.Errors.Add($"HTTP {response.StatusCode}: {responseContent}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending customer address to Luca");
            result.IsSuccess = false;
            result.FailedRecords = 1;
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    #endregion

    #region Turkish Character Normalization Helper

    /// <summary>
    /// Türkçe karakterleri normalize eder.
    /// Luca API'si Türkçe karakterleri bazen ? olarak döndürüyor.
    /// Örn: BÜKÜMLÜ -> B?K?ML? olarak geliyor, bu yüzden karşılaştırma yaparken
    /// Türkçe karakterleri ASCII eşdeğerlerine çeviriyoruz.
    /// </summary>
    private static string NormalizeTurkishCharsForComparison(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Türkçe karakterleri ASCII eşdeğerlerine çevir
        var result = input
            .Replace("Ü", "U").Replace("ü", "u")
            .Replace("Ö", "O").Replace("ö", "o")
            .Replace("Ş", "S").Replace("ş", "s")
            .Replace("Ç", "C").Replace("ç", "c")
            .Replace("Ğ", "G").Replace("ğ", "g")
            .Replace("İ", "I").Replace("ı", "i")
            .Replace("Ø", "O").Replace("ø", "o")  // Çap sembolü (diameter symbol)
            .Trim();

        return result;
    }

    /// <summary>
    /// İki string'i Türkçe karakter toleranslı karşılaştırır.
    /// Luca API'sinin Türkçe karakter encoding sorunu nedeniyle kullanılır.
    /// ? karakterleri wildcard olarak değerlendirilir (herhangi bir karakterle eşleşir).
    /// </summary>
    private static bool AreEqualIgnoringTurkishChars(string? str1, string? str2)
    {
        // Önce Türkçe karakterleri normalize et
        var normalized1 = NormalizeTurkishCharsForComparison(str1);
        var normalized2 = NormalizeTurkishCharsForComparison(str2);
        
        // Eğer uzunluklar farklıysa ve ? yoksa, eşit değildir
        if (!normalized1.Contains('?') && !normalized2.Contains('?'))
        {
            return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
        }
        
        // ? karakterli karşılaştırma (wildcard match)
        // Luca'dan gelen string genelde ? içerir
        var lucaStr = normalized1.Contains('?') ? normalized1 : normalized2;
        var katanaStr = normalized1.Contains('?') ? normalized2 : normalized1;
        
        // Uzunluklar aynı olmalı (? bir karakterin yerine geçiyor)
        if (lucaStr.Length != katanaStr.Length)
            return false;
        
        // Karakter karakter karşılaştır
        for (int i = 0; i < lucaStr.Length; i++)
        {
            char c1 = char.ToUpperInvariant(lucaStr[i]);
            char c2 = char.ToUpperInvariant(katanaStr[i]);
            
            // ? karakteri herhangi bir karakterle eşleşir
            if (c1 == '?' || c2 == '?')
                continue;
            
            if (c1 != c2)
                return false;
        }
        
        return true;
    }

    #endregion
}
