namespace Katana.Core.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string TaxNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Luca integration fields
    public int Type { get; set; } // 1=Şirket, 2=Şahıs
    public string? TaxOffice { get; set; }
    public string? District { get; set; }
    public string? LucaCode { get; set; }
    public long? LucaFinansalNesneId { get; set; }
    public string? LastSyncError { get; set; }
    public string? GroupCode { get; set; }
    
    // Computed properties for UI
    public string LucaSyncStatus => LucaFinansalNesneId.HasValue && string.IsNullOrEmpty(LastSyncError) 
        ? "success" 
        : (!string.IsNullOrEmpty(LastSyncError) ? "error" : "pending");
    public bool IsLucaSynced => LucaFinansalNesneId.HasValue && string.IsNullOrEmpty(LastSyncError);
}

public class CreateCustomerDto
{
    public string TaxNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public decimal? CreditLimit { get; set; }
    
    // Luca integration fields
    public int Type { get; set; } = 1; // 1=Şirket (default), 2=Şahıs
    public string? TaxOffice { get; set; }
    public string? District { get; set; }
    public string? GroupCode { get; set; }
}

public class UpdateCustomerDto
{
    public string TaxNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public decimal? CreditLimit { get; set; }
    public bool IsActive { get; set; }
    
    // Luca integration fields
    public int Type { get; set; } = 1;
    public string? TaxOffice { get; set; }
    public string? District { get; set; }
    public string? GroupCode { get; set; }
}

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string TaxNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
    
    // Luca status for list view
    public string? LucaCode { get; set; }
    public long? LucaFinansalNesneId { get; set; }
    public string? LastSyncError { get; set; }
    public string LucaSyncStatus => LucaFinansalNesneId.HasValue && string.IsNullOrEmpty(LastSyncError) 
        ? "success" 
        : (!string.IsNullOrEmpty(LastSyncError) ? "error" : "pending");
}

public class CustomerStatisticsDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int InactiveCustomers { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalCreditLimit { get; set; }
}