using Katana.Core.DTOs;
using Katana.Core.DTOs.Koza;
using Katana.Core.Entities;
using System.Text.Json;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
    Task<bool> WarmupCacheWithRetryAsync(int maxAttempts = 3, CancellationToken cancellationToken = default);
    Task<LucaCacheStatusDto> GetCacheStatusAsync();
    Task<bool> UpdateStockCardAsync(long stockCardId, Product product);
    Task<long?> CreateStockCardAsync(Product product);
    
    Task<bool> TestConnectionAsync();
    
    Task<SyncResultDto> SendInvoicesAsync(List<LucaCreateInvoiceHeaderRequest> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCreateCustomerRequest> customers);
    
    Task<SyncResultDto> SendProductsAsync(List<LucaProductUpdateDto> products);
    
    Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards);
    Task<SyncResultDto> SendStockCardsAsync(string sessionId, List<LucaCreateStokKartiRequest> stockCards);
    Task<SyncResultDto> SendStockCardAsync(LucaStockCardDto stockCard);
    Task<SyncResultDto> SendInvoiceAsync(LucaCreateInvoiceHeaderRequest invoice);
    Task<long> CreateIrsaliyeAsync(LucaIrsaliyeDto dto);
    Task DeleteIrsaliyeAsync(long irsaliyeId);
    Task<long> CreateSatinalmaSiparisAsync(LucaSatinalmaSiparisDto dto);
    Task DeleteSatinalmaSiparisAsync(long siparisId);
    Task<long> CreateDepoTransferAsync(LucaDepoTransferDto dto);
    Task<List<LucaTedarikciDto>> GetTedarikciListAsync();
    Task<long> CreateTedarikciAsync(LucaCreateSupplierRequest dto);
    Task<long> CreateCariHareketAsync(LucaCariHareketDto dto);
    Task<long> CreateFaturaKapamaAsync(LucaFaturaKapamaDto dto, long belgeTurDetayId);
    Task<List<LucaWarehouseDto>> GetDepoListAsync();
    Task<List<LucaVergiDairesiDto>> GetVergiDairesiListAsync();
    Task<List<LucaOlcumBirimiDto>> GetOlcumBirimiListAsync();
    
    Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null);
    Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null);
    Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null);
    Task<List<Katana.Core.DTOs.LucaProductDto>> FetchProductsAsync(DateTime? fromDate = null);
    Task<List<Katana.Core.DTOs.LucaProductDto>> FetchProductsAsync(System.Threading.CancellationToken cancellationToken = default);

    
    Task<System.Text.Json.JsonElement> ListStockCardsAsync(LucaListStockCardsRequest request, CancellationToken ct = default);
    Task<System.Text.Json.JsonElement> ListStockCardsAsync(string? kodBas = null, string? kodBit = null, string kodOp = "between", CancellationToken ct = default);
    Task<List<LucaStockCardSummaryDto>> ListStockCardsAsync(System.Threading.CancellationToken cancellationToken = default);
    Task<System.Text.Json.JsonElement> ListStockCardPriceListsAsync(LucaListStockCardPriceListsRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltUnitsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltStocksAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardCostsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCategoriesAsync(LucaListStockCategoriesRequest request);

    
    Task<System.Text.Json.JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false, CancellationToken ct = default);
    Task<System.Text.Json.JsonElement> ListInvoicesAsync(
        int? parUstHareketTuru = null,
        int? parAltHareketTuru = null,
        long? belgeNoBas = null,
        long? belgeNoBit = null,
        string? belgeTarihiBas = null,
        string? belgeTarihiBit = null,
        bool detayliListe = false,
        CancellationToken ct = default);
    Task<System.Text.Json.JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request);
    Task<System.Text.Json.JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request);
    Task<System.Text.Json.JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request);
    Task<System.Text.Json.JsonElement> GetInvoicePdfLinkAsync(LucaInvoicePdfLinkRequest request);
    Task<System.Text.Json.JsonElement> ListCurrencyInvoicesAsync(LucaListCurrencyInvoicesRequest request);

    
    Task<System.Text.Json.JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerWorkingConditionsAsync(LucaGetCustomerWorkingConditionsRequest request);
    Task<System.Text.Json.JsonElement> ListCustomerAuthorizedPersonsAsync(LucaListCustomerAuthorizedPersonsRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerRiskAsync(LucaGetCustomerRiskRequest request);
    Task<System.Text.Json.JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request);

    Task<System.Text.Json.JsonElement> ListDeliveryNotesAsync(LucaListIrsaliyeRequest? request = null, bool detayliListe = false);
    Task<System.Text.Json.JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request);
    Task<System.Text.Json.JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request);
    Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null);
    Task<string> GetEirsaliyeXmlAsync(LucaGetEirsaliyeXmlRequest request);

    Task<JsonElement> ListTaxOfficesAsync(LucaListTaxOfficesRequest? request = null);
    
    Task<JsonElement> ListMeasurementUnitsAsync(LucaListMeasurementUnitsRequest? request = null);
    
    Task<JsonElement> ListDocumentTypeDetailsAsync(LucaListDocumentTypeDetailsRequest? request = null);
    
    Task<JsonElement> ListDocumentSeriesAsync(LucaListDocumentSeriesRequest request);
    
    Task<JsonElement> ListBranchCurrenciesAsync(LucaListBranchCurrenciesRequest request);
    
    Task<JsonElement> GetDocumentSeriesMaxAsync(LucaGetDocumentSeriesMaxRequest request);
    
    Task<JsonElement> ListDynamicLovValuesAsync(LucaListDynamicLovValuesRequest request);
    
    Task<JsonElement> UpdateDynamicLovValueAsync(LucaUpdateDynamicLovValueRequest request);
    Task<JsonElement> CreateDynamicLovValueAsync(LucaCreateDynamicLovRequest request);
    Task<JsonElement> UpdateAttributeAsync(LucaUpdateAttributeRequest request);
    
    Task<JsonElement> ListCustomersAsync(LucaListCustomersRequest? request = null);
    
    Task<JsonElement> ListSuppliersAsync(LucaListSuppliersRequest? request = null);
    
    Task<JsonElement> ListWarehousesAsync(LucaListWarehousesRequest? request = null);
    
    Task<JsonElement> CreateCustomerAsync(LucaCreateCustomerRequest request);
    
    Task<JsonElement> ListCustomerTransactionsAsync(LucaListCariHareketBaslikRequest request, bool detayliListe = false);
    
    Task<JsonElement> ListSpecialCustomerTransactionsAsync(LucaListOzelCariHareketBaslikRequest? request = null);
    
    Task<JsonElement> CreateCustomerContractAsync(LucaCreateCustomerContractRequest request);
    
    Task<JsonElement> ListStockCardsAutoCompleteAsync(LucaStockCardAutoCompleteRequest request);
    
    Task<JsonElement> NotifyUtsAsync(LucaUtsTransmitRequest request);
    
    /// <summary>
    /// Luca'da cari kart arar (kartKodu/cariKodu bazlı)
    /// </summary>
    /// <param name="kartKodu">Cari kart kodu (örn: CK-102)</param>
    /// <returns>finansalNesneId bulunursa, bulunamazsa null</returns>
    Task<long?> FindCariCardByCodeAsync(string kartKodu);
    
    /// <summary>
    /// Luca'da cari kart günceller (GuncelleFinMusteriWS.do)
    /// </summary>
    Task<SyncResultDto> UpdateCariCardAsync(LucaUpdateCustomerFullRequest request);
    
    /// <summary>
    /// UPSERT: Cari kart varsa güncelle, yoksa oluştur (Customer için)
    /// </summary>
    Task<SyncResultDto> UpsertCariCardAsync(Customer customer);
    
    /// <summary>
    /// UPSERT: Cari kart varsa güncelle, yoksa oluştur (Supplier için)
    /// </summary>
    Task<SyncResultDto> UpsertCariCardAsync(Supplier supplier);
    
    /// <summary>
    /// Müşteri adreslerini Luca'ya gönderir
    /// </summary>
    Task<SyncResultDto> SendCustomerAddressAsync(long finansalNesneId, string address, string? city, string? district, bool isDefault = true);
    
    Task<JsonElement> CreateSupplierAsync(LucaCreateSupplierRequest request);
    
    Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request);
    
    /// <summary>
    /// Luca/Koza'ya stok kartı oluşturur - V2 (Yeni API formatı)
    /// </summary>
    Task<LucaCreateStockCardResponse> CreateStockCardV2Async(LucaCreateStockCardRequestV2 request, CancellationToken ct = default);
    
    /// <summary>
    /// Search for a stock card by SKU/KartKodu in Luca.
    /// Returns the skartId if found, null if not found.
    /// </summary>
    Task<long?> FindStockCardBySkuAsync(string sku);
    
    /// <summary>
    /// UPSERT: If stock card exists in Luca, skip (API doesn't support update).
    /// If not exists, create new card.
    /// Returns success with info about whether it was created or already existed.
    /// </summary>
    Task<SyncResultDto> UpsertStockCardAsync(LucaCreateStokKartiRequest stockCard);
    
    Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request);
    Task<JsonElement> ListStockCardPurchasePricesAsync(LucaStockCardByIdRequest request);
    Task<JsonElement> ListStockCardSalesPricesAsync(LucaStockCardByIdRequest request);
    
    Task<JsonElement> CreateSalesOrderAsync(LucaCreateSalesOrderRequest request);
    Task<JsonElement> CreateSalesOrderHeaderAsync(LucaCreateOrderHeaderRequest request);
    Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request);
    Task<JsonElement> DeleteSalesOrderDetailAsync(LucaDeleteSalesOrderDetailRequest request);
    
    Task<JsonElement> CreatePurchaseOrderAsync(LucaCreatePurchaseOrderRequest request);
    Task<JsonElement> CreatePurchaseOrderHeaderAsync(LucaCreateOrderHeaderRequest request);
    Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request);
    Task<JsonElement> DeletePurchaseOrderDetailAsync(LucaDeletePurchaseOrderDetailRequest request);
    
    Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request);
    
    /// <summary>
    /// Luca Depo Transferi (Wrapper) - LucaStockTransferRequest için
    /// </summary>
    Task<long> CreateWarehouseTransferAsync(LucaStockTransferRequest request);
    
    /// <summary>
    /// Luca DSH Stok Hareketi Fişi (Fire, Sarf, Sayım Fazlası vb.)
    /// EkleStkWsDshBaslik.do endpoint'i
    /// </summary>
    Task<long> CreateStockVoucherAsync(LucaStockVoucherRequest request);
    
    Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request);
    Task<JsonElement> CreateWarehouseAsync(LucaCreateWarehouseRequest request);
    Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request);
    Task<List<LucaBranchDto>> GetBranchesAsync();
    Task<List<LucaWarehouseDto>> GetWarehousesAsync();
    Task<List<LucaMeasurementUnitDto>> GetMeasurementUnitsAsync();
    Task<SyncResultDto> SendProductsFromExcelAsync(List<ExcelProductDto> products, System.Threading.CancellationToken cancellationToken = default);
    
    Task<JsonElement> ListStockCardSuppliersAsync(LucaStockCardByIdRequest request, CancellationToken ct = default);
    Task<JsonElement> ListStockCardSuppliersAsync(long skartId, CancellationToken ct = default);
    
    Task<JsonElement> ListCustomerContactsAsync(LucaListCustomerContactsRequest request);
    
    Task<JsonElement> ListBanksAsync(LucaListBanksRequest? request = null);
    Task<JsonElement> ListCashAccountsAsync(LucaListCashAccountsRequest? request = null);
    
    Task<JsonElement> GetWarehouseStockQuantityAsync(LucaGetWarehouseStockRequest request);
    
    Task<JsonElement> ListStockCardPurchaseTermsAsync(LucaStockCardByIdRequest request);
    
    Task<JsonElement> ListSalesOrdersAsync(LucaListSalesOrdersRequest? request = null, bool detayliListe = false);
    Task<JsonElement> ListPurchaseOrdersAsync(LucaListPurchaseOrdersRequest? request = null, bool detayliListe = false);
    Task<byte[]> GenerateStockServiceReportAsync(LucaDynamicStockServiceReportRequest request);
    
    // Koza Depo (Depot) işlemleri
    Task<IReadOnlyList<KozaDepoDto>> ListDepotsAsync(CancellationToken ct = default);
    Task<KozaResult> CreateDepotAsync(KozaCreateDepotRequest req, CancellationToken ct = default);
    
    // Koza Stok Kartı işlemleri
    Task<IReadOnlyList<KozaStokKartiDto>> ListStockCardsSimpleAsync(DateTime? eklemeBas = null, DateTime? eklemeBit = null, CancellationToken ct = default);
    Task<KozaResult> CreateStockCardSimpleAsync(KozaCreateStokKartiRequest req, CancellationToken ct = default);
    
    // Koza Cari (Müşteri/Tedarikçi) işlemleri
    Task<JsonElement> ListCariAddressesAsync(long finansalNesneId, CancellationToken ct = default);
    Task<JsonElement> GetCariCalismaKosulAsync(long calismaKosulId, CancellationToken ct = default);
    Task<JsonElement> ListCariYetkililerAsync(long finansalNesneId, CancellationToken ct = default);
    Task<KozaResult> CreateCariHareketAsync(KozaCariHareketRequest req, CancellationToken ct = default);
    
    // Koza Müşteri Listesi
    Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(CancellationToken ct = default);
    Task<IReadOnlyList<KozaCariDto>> ListMusteriCarilerAsync(string? kodBas, string? kodBit, string kodOp = "between", CancellationToken ct = default);
    Task<IReadOnlyList<KozaCustomerListItemDto>> ListMusteriCustomerItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<KozaCustomerListItemDto>> ListMusteriCustomerItemsAsync(string? kodBas, string? kodBit, string kodOp = "between", CancellationToken ct = default);
    
    // Koza Tedarikçi Listesi
    Task<IReadOnlyList<KozaCariDto>> ListTedarikciCarilerAsync(CancellationToken ct = default);
    Task<IReadOnlyList<KozaCariDto>> ListTedarikciCarilerAsync(string? kodBas, string? kodBit, string kodOp = "between", CancellationToken ct = default);
    Task<IReadOnlyList<KozaSupplierListItemDto>> ListTedarikciSupplierItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<KozaSupplierListItemDto>> ListTedarikciSupplierItemsAsync(string? kodBas, string? kodBit, string kodOp = "between", CancellationToken ct = default);
    Task<KozaResult> CreateMusteriCariAsync(KozaMusteriEkleRequest request, CancellationToken ct = default);
    Task<KozaResult> CreateTedarikciCariAsync(KozaTedarikciEkleRequest request, CancellationToken ct = default);
    
    // Katana Supplier → Koza Tedarikçi Cari Sync
    Task<KozaResult> EnsureSupplierCariAsync(KatanaSupplierToCariDto supplier, CancellationToken ct = default);
    
    // Katana Location → Koza Depo Sync
    Task<KozaResult> EnsureDepotAsync(KatanaLocationToDepoDto depot, CancellationToken ct = default);
    
    // Katana Customer → Luca Müşteri Cari Sync
    Task<KozaResult> EnsureCustomerCariAsync(KatanaCustomerToCariDto customer, CancellationToken ct = default);
}
