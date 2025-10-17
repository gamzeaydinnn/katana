using Katana.Core.DTOs;

namespace Katana.Core.Interfaces;

public interface IAccountingService
{
    Task<IEnumerable<AccountingRecordDto>> GetAllRecordsAsync();
    Task<AccountingRecordDto?> GetRecordByIdAsync(int id);
    Task<AccountingRecordDto?> GetRecordByTransactionNoAsync(string transactionNo);
    Task<IEnumerable<AccountingRecordDto>> GetRecordsByTypeAsync(string type);
    Task<IEnumerable<AccountingRecordDto>> GetRecordsByCategoryAsync(string category);
    Task<IEnumerable<AccountingRecordDto>> GetRecordsByCustomerAsync(int customerId);
    Task<IEnumerable<AccountingRecordDto>> GetRecordsByInvoiceAsync(int invoiceId);
    Task<IEnumerable<AccountingRecordDto>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AccountingRecordDto>> GetUnsyncedRecordsAsync();
    Task<AccountingRecordDto> CreateRecordAsync(CreateAccountingRecordDto dto);
    Task<AccountingRecordDto> UpdateRecordAsync(int id, UpdateAccountingRecordDto dto);
    Task<bool> DeleteRecordAsync(int id);
    Task<bool> MarkAsSyncedAsync(int id);
    Task<AccountingStatisticsDto> GetStatisticsAsync();
    Task<AccountingStatisticsDto> GetStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<FinancialReportDto> GetFinancialReportAsync(DateTime startDate, DateTime endDate);
}
