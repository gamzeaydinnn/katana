namespace Katana.Core.DTOs;
public class AccountingRecordDto
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNo { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime TransactionDate { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAccountingRecordDto
{
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public int? InvoiceId { get; set; }
    public int? CustomerId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class UpdateAccountingRecordDto
{
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public int? InvoiceId { get; set; }
    public int? CustomerId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class AccountingRecordSummaryDto
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class AccountingStatisticsDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalTransactions { get; set; }
    public int IncomeTransactions { get; set; }
    public int ExpenseTransactions { get; set; }
    public Dictionary<string, decimal> IncomeByCategory { get; set; } = new();
    public Dictionary<string, decimal> ExpenseByCategory { get; set; } = new();
}

public class FinancialReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<CategoryBreakdownDto> IncomeBreakdown { get; set; } = new();
    public List<CategoryBreakdownDto> ExpenseBreakdown { get; set; } = new();
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int TransactionCount { get; set; }
}
