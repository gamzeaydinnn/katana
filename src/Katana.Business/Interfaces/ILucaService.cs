using Katana.Business.DTOs;
using Katana.Core.DTOs;
using System.Text.Json;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
    // Authentication
    Task<bool> TestConnectionAsync();

    // Katana → Luca (Push)
    Task<SyncResultDto> SendInvoicesAsync(List<LucaInvoiceDto> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCustomerDto> customers);
    // Katana -> Luca: send product records (create/update)
    Task<SyncResultDto> SendProductsAsync(List<LucaProductUpdateDto> products);
    // Katana -> Luca: Koza stok kartı oluşturma
    Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards);
    
    // Luca → Katana (Pull)
    Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null);
    Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null);
    Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null);
    Task<List<LucaProductDto>> FetchProductsAsync(DateTime? fromDate = null);

    // Luca → Katana (Pull) - Koza stok kartı ve ilgili listeler (ham JSON döner)
    Task<System.Text.Json.JsonElement> ListStockCardsAsync(LucaListStockCardsRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardPriceListsAsync(LucaListStockCardPriceListsRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltUnitsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltStocksAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardCostsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCategoriesAsync(LucaListStockCategoriesRequest request);

    // Luca → Katana (Pull) - Koza fatura listesi ve başlık işlemleri
    Task<System.Text.Json.JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false);
    Task<System.Text.Json.JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request);
    Task<System.Text.Json.JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request);
    Task<System.Text.Json.JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request);

    // Koza cari / finansal nesne işlemleri
    Task<System.Text.Json.JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerWorkingConditionsAsync(LucaGetCustomerWorkingConditionsRequest request);
    Task<System.Text.Json.JsonElement> ListCustomerAuthorizedPersonsAsync(LucaListCustomerAuthorizedPersonsRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerRiskAsync(long finansalNesneId);
    Task<System.Text.Json.JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request);

    // Koza irsaliye işlemleri
    Task<System.Text.Json.JsonElement> ListDeliveryNotesAsync(bool detayliListe = false);
    Task<System.Text.Json.JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request);
    Task<System.Text.Json.JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request);
    Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null);

    // NEW: Additional Methods from API Documentation
    // 3.2.1 Vergi Dairesi Listesi
    Task<JsonElement> ListTaxOfficesAsync(LucaListTaxOfficesRequest? request = null);
    // 3.2.2 Ölçü Birimi Listesi
    Task<JsonElement> ListMeasurementUnitsAsync(LucaListMeasurementUnitsRequest? request = null);
    // 3.2.3 Müşteri Listesi
    Task<JsonElement> ListCustomersAsync(LucaListCustomersRequest? request = null);
    // 3.2.4 Tedarikçi Listesi
    Task<JsonElement> ListSuppliersAsync(LucaListSuppliersRequest? request = null);
    // 3.2.16 Depo Listesi
    Task<JsonElement> ListWarehousesAsync(LucaListWarehousesRequest? request = null);
    // 3.2.29 Cari Kart Ekle
    Task<JsonElement> CreateCustomerAsync(LucaCreateCustomerRequest request);
    // 3.2.30 Tedarikçi Kart Ekle
    Task<JsonElement> CreateSupplierAsync(LucaCreateSupplierRequest request);
    // 3.2.33 Stok Kartı Ekle (birincil ürün oluşturma endpoint'i)
    Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request);
    // 3.2.40 Diğer Stok Hareketi (stok düzeltmeleri için)
    Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request);
    // Satış siparişi işlemleri
    Task<JsonElement> CreateSalesOrderAsync(LucaCreateSalesOrderRequest request);
    Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request);
    Task<JsonElement> DeleteSalesOrderDetailAsync(LucaDeleteSalesOrderDetailRequest request);
    // Satın alma siparişi işlemleri
    Task<JsonElement> CreatePurchaseOrderAsync(LucaCreatePurchaseOrderRequest request);
    Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request);
    Task<JsonElement> DeletePurchaseOrderDetailAsync(LucaDeletePurchaseOrderDetailRequest request);
    // Depo ve diğer stok operasyonları
    Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request);
    Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request);
    Task<JsonElement> CreateWarehouseAsync(LucaCreateWarehouseRequest request);
    Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request);

    // --- Eksik kalan listeleme/çekme uçları ---
    // 3.2.7 Stok Kartı Temin Yerleri
    Task<JsonElement> ListStockCardSuppliersAsync(LucaStockCardByIdRequest request);
    // 3.2.8 Cari İletişim Listesi
    Task<JsonElement> ListCustomerContactsAsync(LucaListCustomerContactsRequest request);
    // 3.2.12 Banka Kartları Listesi
    Task<JsonElement> ListBanksAsync(LucaListBanksRequest? request = null);
    // 3.2.17 Depo Eldeki Miktar
    Task<JsonElement> GetWarehouseStockQuantityAsync(long depoId);
    // 3.2.20 Stok Kartı Alım Şartları
    Task<JsonElement> ListStockCardPurchaseTermsAsync(LucaStockCardByIdRequest request);
    // 3.2.22 Satış Sipariş Listesi
    Task<JsonElement> ListSalesOrdersAsync(bool detayliListe = false);
}


