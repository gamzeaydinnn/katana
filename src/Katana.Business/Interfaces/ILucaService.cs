using Katana.Business.DTOs;
using Katana.Business.Models.DTOs;
using Katana.Core.DTOs;
using System.Text.Json;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
    
    Task<bool> TestConnectionAsync();
    
    Task<SyncResultDto> SendInvoicesAsync(List<LucaCreateInvoiceHeaderRequest> invoices);
    Task<SyncResultDto> SendStockMovementsAsync(List<LucaStockDto> stockMovements);
    Task<SyncResultDto> SendCustomersAsync(List<LucaCreateCustomerRequest> customers);
    
    Task<SyncResultDto> SendProductsAsync(List<LucaProductUpdateDto> products);
    
    Task<SyncResultDto> SendStockCardsAsync(List<LucaCreateStokKartiRequest> stockCards);
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
    Task<List<LucaDepoDto>> GetDepoListAsync();
    Task<List<LucaVergiDairesiDto>> GetVergiDairesiListAsync();
    Task<List<LucaOlcumBirimiDto>> GetOlcumBirimiListAsync();
    
    Task<List<LucaInvoiceDto>> FetchInvoicesAsync(DateTime? fromDate = null);
    Task<List<LucaStockDto>> FetchStockMovementsAsync(DateTime? fromDate = null);
    Task<List<LucaCustomerDto>> FetchCustomersAsync(DateTime? fromDate = null);
    Task<List<LucaProductDto>> FetchProductsAsync(DateTime? fromDate = null);

    
    Task<System.Text.Json.JsonElement> ListStockCardsAsync(LucaListStockCardsRequest request);
    Task<List<LucaStockCardSummaryDto>> ListStockCardsAsync(System.Threading.CancellationToken cancellationToken = default);
    Task<System.Text.Json.JsonElement> ListStockCardPriceListsAsync(LucaListStockCardPriceListsRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltUnitsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardAltStocksAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCardCostsAsync(LucaStockCardByIdRequest request);
    Task<System.Text.Json.JsonElement> ListStockCategoriesAsync(LucaListStockCategoriesRequest request);

    
    Task<System.Text.Json.JsonElement> ListInvoicesAsync(LucaListInvoicesRequest request, bool detayliListe = false);
    Task<System.Text.Json.JsonElement> CreateInvoiceRawAsync(LucaCreateInvoiceHeaderRequest request);
    Task<System.Text.Json.JsonElement> CloseInvoiceAsync(LucaCloseInvoiceRequest request);
    Task<System.Text.Json.JsonElement> DeleteInvoiceAsync(LucaDeleteInvoiceRequest request);

    
    Task<System.Text.Json.JsonElement> ListCustomerAddressesAsync(LucaListCustomerAddressesRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerWorkingConditionsAsync(LucaGetCustomerWorkingConditionsRequest request);
    Task<System.Text.Json.JsonElement> ListCustomerAuthorizedPersonsAsync(LucaListCustomerAuthorizedPersonsRequest request);
    Task<System.Text.Json.JsonElement> GetCustomerRiskAsync(LucaGetCustomerRiskRequest request);
    Task<System.Text.Json.JsonElement> CreateCustomerTransactionAsync(LucaCreateCariHareketRequest request);

    Task<System.Text.Json.JsonElement> ListDeliveryNotesAsync(bool detayliListe = false);
    Task<System.Text.Json.JsonElement> CreateDeliveryNoteAsync(LucaCreateIrsaliyeBaslikRequest request);
    Task<System.Text.Json.JsonElement> DeleteDeliveryNoteAsync(LucaDeleteIrsaliyeRequest request);
    Task<List<LucaDespatchDto>> FetchDeliveryNotesAsync(DateTime? fromDate = null);

    Task<JsonElement> ListTaxOfficesAsync(LucaListTaxOfficesRequest? request = null);
    
    Task<JsonElement> ListMeasurementUnitsAsync(LucaListMeasurementUnitsRequest? request = null);
    
    Task<JsonElement> ListCustomersAsync(LucaListCustomersRequest? request = null);
    
    Task<JsonElement> ListSuppliersAsync(LucaListSuppliersRequest? request = null);
    
    Task<JsonElement> ListWarehousesAsync(LucaListWarehousesRequest? request = null);
    
    Task<JsonElement> CreateCustomerAsync(LucaCreateCustomerRequest request);
    
    Task<JsonElement> CreateSupplierAsync(LucaCreateSupplierRequest request);
    
    Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request);
    
    Task<JsonElement> CreateOtherStockMovementAsync(LucaCreateDshBaslikRequest request);
    
    Task<JsonElement> CreateSalesOrderAsync(LucaCreateSalesOrderRequest request);
    Task<JsonElement> CreateSalesOrderHeaderAsync(LucaCreateOrderHeaderRequest request);
    Task<JsonElement> DeleteSalesOrderAsync(LucaDeleteSalesOrderRequest request);
    Task<JsonElement> DeleteSalesOrderDetailAsync(LucaDeleteSalesOrderDetailRequest request);
    
    Task<JsonElement> CreatePurchaseOrderAsync(LucaCreatePurchaseOrderRequest request);
    Task<JsonElement> CreatePurchaseOrderHeaderAsync(LucaCreateOrderHeaderRequest request);
    Task<JsonElement> DeletePurchaseOrderAsync(LucaDeletePurchaseOrderRequest request);
    Task<JsonElement> DeletePurchaseOrderDetailAsync(LucaDeletePurchaseOrderDetailRequest request);
    
    Task<JsonElement> CreateWarehouseTransferAsync(LucaCreateWarehouseTransferRequest request);
    Task<JsonElement> CreateStockCountResultAsync(LucaCreateStockCountRequest request);
    Task<JsonElement> CreateWarehouseAsync(LucaCreateWarehouseRequest request);
    Task<JsonElement> CreateCreditCardEntryAsync(LucaCreateCreditCardEntryRequest request);
    Task<List<LucaBranchDto>> GetBranchesAsync();
    Task<List<LucaWarehouseDto>> GetWarehousesAsync();
    Task<List<LucaMeasurementUnitDto>> GetMeasurementUnitsAsync();
    Task<SyncResultDto> SendProductsFromExcelAsync(List<ExcelProductDto> products, System.Threading.CancellationToken cancellationToken = default);
    
    Task<JsonElement> ListStockCardSuppliersAsync(LucaStockCardByIdRequest request);
    
    Task<JsonElement> ListCustomerContactsAsync(LucaListCustomerContactsRequest request);
    
    Task<JsonElement> ListBanksAsync(LucaListBanksRequest? request = null);
    
    Task<JsonElement> GetWarehouseStockQuantityAsync(LucaGetWarehouseStockRequest request);
    
    Task<JsonElement> ListStockCardPurchaseTermsAsync(LucaStockCardByIdRequest request);
    
    Task<JsonElement> ListSalesOrdersAsync(LucaListSalesOrdersRequest? request = null, bool detayliListe = false);
}
