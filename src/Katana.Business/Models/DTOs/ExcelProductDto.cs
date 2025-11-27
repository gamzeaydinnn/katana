using System;
using System.ComponentModel.DataAnnotations;

namespace Katana.Business.Models.DTOs
{
    /// <summary>
    /// Excel'den okunan ürün satırını temsil eden DTO
    /// Mapping: KatanaToLucaMapper.MapFromExcelRow tarafından Luca DTO'suna çevrilir
    /// </summary>
    public class ExcelProductDto
    {
        /// <summary>
        /// Stok Kodu (Zorunlu)
        /// Örnek: "PRD001", "AUTO-0001"
        /// </summary>
        [Required(ErrorMessage = "SKU kolonu zorunludur")]
        [MaxLength(50, ErrorMessage = "SKU maksimum 50 karakter olabilir")]
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Ürün Adı (Zorunlu)
        /// Örnek: "Bilgisayar Masası", "Kurumsal Danışmanlık Hizmeti"
        /// </summary>
        [Required(ErrorMessage = "Name kolonu zorunludur")]
        [MaxLength(200, ErrorMessage = "Name maksimum 200 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// KDV Oranı (Opsiyonel - Boşsa appsettings'ten default çekilir)
        /// Örnek: 20, 10, 1, 0
        /// </summary>
        public decimal? VatRate { get; set; }

        /// <summary>
        /// Ölçü Birimi (Opsiyonel - Boşsa appsettings'ten default çekilir)
        /// Örnek: "Adet", "Kg", "Saat", "Lt"
        /// </summary>
        [MaxLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// Başlangıç Tarihi (Zorunlu)
        /// Excel formatı: 01/01/2025
        /// Koza formatı: dd/MM/yyyy
        /// </summary>
        [Required(ErrorMessage = "StartDate kolonu zorunludur")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Aktif mi? (Opsiyonel - Default: true)
        /// Excel: 1/0 veya TRUE/FALSE
        /// Koza: 1 (aktif) / 0 (pasif)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Hizmet mi? (Opsiyonel - Default: false)
        /// Excel: 1/0 veya TRUE/FALSE
        /// Koza: 1 (hizmet) / 0 (ürün)
        /// </summary>
        public bool IsService { get; set; } = false;

        /// <summary>
        /// Stokta takip edilsin mi? (Opsiyonel - Default: true)
        /// Excel: 1/0 veya TRUE/FALSE
        /// Koza: 1 (evet) / 0 (hayır)
        /// </summary>
        public bool TrackStock { get; set; } = true;

        /// <summary>
        /// Alışta maliyet hesaplansın mı? (Opsiyonel - Default: true)
        /// Excel: 1/0 veya TRUE/FALSE
        /// Koza: 1 (evet) / 0 (hayır)
        /// </summary>
        public bool CalculateCostOnPurchase { get; set; } = true;

        /// <summary>
        /// Ürün Açıklaması (Opsiyonel)
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Barkod (Opsiyonel)
        /// </summary>
        [MaxLength(50)]
        public string? Barcode { get; set; }

        /// <summary>
        /// Kategori Kodu (Opsiyonel)
        /// Koza'daki mevcut kategori kodu
        /// </summary>
        [MaxLength(50)]
        public string? CategoryCode { get; set; }

        /// <summary>
        /// Alış KDV Oranı (Opsiyonel - Boşsa VatRate kullanılır)
        /// </summary>
        public decimal? PurchaseVatRate { get; set; }

        /// <summary>
        /// Satış KDV Oranı (Opsiyonel - Boşsa VatRate kullanılır)
        /// </summary>
        public decimal? SalesVatRate { get; set; }

        /// <summary>
        /// Excel satır numarası (hata ayıklama için)
        /// Script tarafından otomatik doldurulur
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Validasyon kontrolü
        /// </summary>
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

        /// <summary>
        /// Excel satırını string'e çevirir (log için)
        /// </summary>
        public override string ToString()
        {
            return $"SKU: {SKU}, Name: {Name}, VatRate: {VatRate?.ToString() ?? "N/A"}, " +
                   $"Unit: {Unit ?? "N/A"}, StartDate: {StartDate:dd/MM/yyyy}, " +
                   $"IsActive: {IsActive}, IsService: {IsService}";
        }
    }
}
