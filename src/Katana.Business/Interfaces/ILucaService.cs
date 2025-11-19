using Katana.Business.DTOs;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface ILucaService
{
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
    
    Task<bool> TestConnectionAsync();
}


