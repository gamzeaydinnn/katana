using Katana.Core.DTOs;
using Katana.Core.Entities;
using Katana.Core.Interfaces;
using Katana.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Katana.Business.Services;

public class AccountingService : IAccountingService
{
    private readonly IntegrationDbContext _context;

    public AccountingService(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetAllRecordsAsync()
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<AccountingRecordDto?> GetRecordByIdAsync(int id)
    {
        var record = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);

        return record == null ? null : MapToDto(record);
    }

    public async Task<AccountingRecordDto?> GetRecordByTransactionNoAsync(string transactionNo)
    {
        var record = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.TransactionNo == transactionNo);

        return record == null ? null : MapToDto(record);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetRecordsByTypeAsync(string type)
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => r.Type.ToUpper() == type.ToUpper())
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetRecordsByCategoryAsync(string category)
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => r.Category.ToUpper() == category.ToUpper())
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetRecordsByCustomerAsync(int customerId)
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetRecordsByInvoiceAsync(int invoiceId)
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => r.InvoiceId == invoiceId)
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => r.TransactionDate >= startDate && r.TransactionDate <= endDate)
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AccountingRecordDto>> GetUnsyncedRecordsAsync()
    {
        var records = await _context.AccountingRecords
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Where(r => !r.IsSynced)
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        return records.Select(MapToDto);
    }

    public async Task<AccountingRecordDto> CreateRecordAsync(CreateAccountingRecordDto dto)
    {
        var transactionNo = await GenerateTransactionNoAsync(dto.Type);

        var record = new AccountingRecord
        {
            TransactionNo = transactionNo,
            Type = dto.Type.ToUpper(),
            Category = dto.Category,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Description = dto.Description,
            InvoiceId = dto.InvoiceId,
            CustomerId = dto.CustomerId,
            PaymentMethod = dto.PaymentMethod,
            TransactionDate = dto.TransactionDate,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccountingRecords.Add(record);
        await _context.SaveChangesAsync();

        return (await GetRecordByIdAsync(record.Id))!;
    }

    public async Task<AccountingRecordDto> UpdateRecordAsync(int id, UpdateAccountingRecordDto dto)
    {
        var record = await _context.AccountingRecords.FindAsync(id);
        if (record == null)
            throw new KeyNotFoundException($"Muhasebe kaydı bulunamadı: {id}");

        record.Type = dto.Type.ToUpper();
        record.Category = dto.Category;
        record.Amount = dto.Amount;
        record.Currency = dto.Currency;
        record.Description = dto.Description;
        record.InvoiceId = dto.InvoiceId;
        record.CustomerId = dto.CustomerId;
        record.PaymentMethod = dto.PaymentMethod;
        record.TransactionDate = dto.TransactionDate;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetRecordByIdAsync(id))!;
    }

    public async Task<bool> DeleteRecordAsync(int id)
    {
        var record = await _context.AccountingRecords.FindAsync(id);
        if (record == null)
            return false;

        _context.AccountingRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsSyncedAsync(int id)
    {
        var record = await _context.AccountingRecords.FindAsync(id);
        if (record == null)
            return false;

        record.IsSynced = true;
        record.SyncedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AccountingStatisticsDto> GetStatisticsAsync()
    {
        var allRecords = await _context.AccountingRecords.ToListAsync();
        return CalculateStatistics(allRecords);
    }

    public async Task<AccountingStatisticsDto> GetStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var records = await _context.AccountingRecords
            .Where(r => r.TransactionDate >= startDate && r.TransactionDate <= endDate)
            .ToListAsync();

        return CalculateStatistics(records);
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync(DateTime startDate, DateTime endDate)
    {
        var records = await _context.AccountingRecords
            .Where(r => r.TransactionDate >= startDate && r.TransactionDate <= endDate)
            .ToListAsync();

        var incomeRecords = records.Where(r => r.Type == "INCOME").ToList();
        var expenseRecords = records.Where(r => r.Type == "EXPENSE").ToList();

        var totalIncome = incomeRecords.Sum(r => r.Amount);
        var totalExpense = expenseRecords.Sum(r => r.Amount);
        var netProfit = totalIncome - totalExpense;

        var report = new FinancialReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetProfit = netProfit,
            ProfitMargin = totalIncome > 0 ? (netProfit / totalIncome) * 100 : 0,
            IncomeBreakdown = CalculateCategoryBreakdown(incomeRecords, totalIncome),
            ExpenseBreakdown = CalculateCategoryBreakdown(expenseRecords, totalExpense)
        };

        return report;
    }

    private async Task<string> GenerateTransactionNoAsync(string type)
    {
        var prefix = type.ToUpper() == "INCOME" ? "INC" : "EXP";
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        
        var lastRecord = await _context.AccountingRecords
            .Where(r => r.TransactionNo.StartsWith($"{prefix}-{date}"))
            .OrderByDescending(r => r.TransactionNo)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastRecord != null)
        {
            var lastSequence = lastRecord.TransactionNo.Split('-').Last();
            if (int.TryParse(lastSequence, out int num))
                sequence = num + 1;
        }

        return $"{prefix}-{date}-{sequence:D4}";
    }

    private AccountingStatisticsDto CalculateStatistics(List<AccountingRecord> records)
    {
        var incomeRecords = records.Where(r => r.Type == "INCOME").ToList();
        var expenseRecords = records.Where(r => r.Type == "EXPENSE").ToList();

        var stats = new AccountingStatisticsDto
        {
            TotalIncome = incomeRecords.Sum(r => r.Amount),
            TotalExpense = expenseRecords.Sum(r => r.Amount),
            TotalTransactions = records.Count,
            IncomeTransactions = incomeRecords.Count,
            ExpenseTransactions = expenseRecords.Count,
            IncomeByCategory = incomeRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)),
            ExpenseByCategory = expenseRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount))
        };

        stats.NetProfit = stats.TotalIncome - stats.TotalExpense;
        return stats;
    }

    private List<CategoryBreakdownDto> CalculateCategoryBreakdown(List<AccountingRecord> records, decimal total)
    {
        return records
            .GroupBy(r => r.Category)
            .Select(g => new CategoryBreakdownDto
            {
                Category = g.Key,
                Amount = g.Sum(r => r.Amount),
                Percentage = total > 0 ? (g.Sum(r => r.Amount) / total) * 100 : 0,
                TransactionCount = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .ToList();
    }

    private AccountingRecordDto MapToDto(AccountingRecord record)
    {
        return new AccountingRecordDto
        {
            Id = record.Id,
            TransactionNo = record.TransactionNo,
            Type = record.Type,
            Category = record.Category,
            Amount = record.Amount,
            Currency = record.Currency,
            Description = record.Description,
            InvoiceId = record.InvoiceId,
            InvoiceNo = record.Invoice?.InvoiceNo,
            CustomerId = record.CustomerId,
            CustomerName = record.Customer?.Title,
            PaymentMethod = record.PaymentMethod,
            TransactionDate = record.TransactionDate,
            IsSynced = record.IsSynced,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}
