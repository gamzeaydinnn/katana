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
}

public class CustomerStatisticsDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int InactiveCustomers { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalCreditLimit { get; set; }
}