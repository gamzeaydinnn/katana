using System;
using System.ComponentModel.DataAnnotations;

namespace Katana.Business.Models.DTOs
{
    public class ExcelProductDto
    {
        [Required(ErrorMessage = "SKU kolonu zorunludur")]
        [MaxLength(50, ErrorMessage = "SKU maksimum 50 karakter olabilir")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name kolonu zorunludur")]
        [MaxLength(200, ErrorMessage = "Name maksimum 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;
        public decimal? VatRate { get; set; }
        [MaxLength(20)]
        public string? Unit { get; set; }

        [Required(ErrorMessage = "StartDate kolonu zorunludur")]
        public DateTime StartDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsService { get; set; } = false;
        public bool TrackStock { get; set; } = true;
        public bool CalculateCostOnPurchase { get; set; } = true;
        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Barcode { get; set; }

        [MaxLength(50)]
        public string? CategoryCode { get; set; }

        public decimal? PurchaseVatRate { get; set; }
        public decimal? SalesVatRate { get; set; }

        public int RowNumber { get; set; }
        public bool IsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SKU))
            {
                errorMessage = "SKU boş olamaz";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                errorMessage = "Name boş olamaz";
                return false;
            }

            if (StartDate == default)
            {
                errorMessage = "StartDate geçerli bir tarih olmalıdır";
                return false;
            }

            if (VatRate.HasValue && (VatRate < 0 || VatRate > 100))
            {
                errorMessage = "VatRate 0-100 arasında olmalıdır";
                return false;
            }

            return true;
        }
        public override string ToString()
        {
            return $"SKU: {SKU}, Name: {Name}, VatRate: {VatRate?.ToString() ?? "N/A"}, " +
                   $"Unit: {Unit ?? "N/A"}, StartDate: {StartDate:dd/MM/yyyy}, " +
                   $"IsActive: {IsActive}, IsService: {IsService}";
        }
    }
}
