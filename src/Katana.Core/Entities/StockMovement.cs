/*Id, ProductId, MovementType 
(enum: Purchase, Sale, Return, Transfer), Quantity, Date, SourceDocument

StockMovement
Özellikler: Id, ProductSku, ChangeQuantity, MovementType (enum), SourceDocument, Timestamp

*/
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Katana.Core.Enums;

namespace Katana.Core.Entities;

/// <summary>
/// Stok hareketi (giriş, çıkış, iade, transfer vb.) bilgisini temsil eder.
/// Bu tablo, Katana'dan gelen hareketlerin izlenmesi ve Luca'ya aktarımı için kullanılır.
/// </summary>
public class StockMovement
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// İlgili ürünün veritabanı kimliği
    /// </summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// Ürünün stok kodu (SKU)
    /// </summary>
    [MaxLength(50)]
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Stok değişim miktarı (+ giriş, - çıkış)
    /// </summary>
    public int ChangeQuantity { get; set; }

    /// <summary>
    /// Hareket tipi (Purchase, Sale, Return, Transfer vb.)
    /// </summary>
    [Required]
    public MovementType MovementType { get; set; }

    /// <summary>
    /// Kaynak belge numarası (örnek: Fatura No, Sipariş No, Transfer No)
    /// </summary>
    [MaxLength(100)]
    public string SourceDocument { get; set; } = string.Empty;

    /// <summary>
    /// Hareketin oluştuğu tarih/zaman
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hareketin hangi lokasyonda/depo’da gerçekleştiği
    /// </summary>
    [MaxLength(100)]
    public string WarehouseCode { get; set; } = "MAIN";

    /// <summary>
    /// Luca’ya aktarım durumu
    /// </summary>
    public bool IsSynced { get; set; } = false;

    /// <summary>
    /// Senkronizasyon tarihi
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    /// <summary>
    /// İlişkili ürün nesnesi (EF Core navigation)
    /// Non-nullable: StockMovement her zaman bir Product ile ilişkili olmalıdır.
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}
