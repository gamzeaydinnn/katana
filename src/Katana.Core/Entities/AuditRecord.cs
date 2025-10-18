/*AuditLog doğrudan EF Core tarafından tabloya yazılırken, AuditRecord servis katmanında daha anlamlı verilerle (örneğin enum ActionType, JSON diff, kullanıcı kimliği) çalışmak içindir.*/
using Katana.Core.Enums;

namespace Katana.Core.Entities;

/// <summary>
/// Domain katmanında yapılan işlemlerin izlenmesi ve geçmişin tutulması için kullanılan kayıt.
/// Veritabanındaki AuditLog tablosunun business karşılığıdır.
/// </summary>
public class AuditRecord
{
    /// <summary>
    /// Log kaydının benzersiz kimliği.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// İşlemin türü (örnek: CREATE, UPDATE, DELETE).
    /// </summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>
    /// İşlemin yapıldığı varlık (örnek: Product, Invoice, Mapping).
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Etkilenen varlığın benzersiz kimliği (örnek: ProductId, InvoiceId).
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Değişiklik öncesi değerler (JSON formatında).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Değişiklik sonrası değerler (JSON formatında).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// İşlemi başlatan kullanıcı veya sistem (örnek: "System", "API", "AdminUser").
    /// </summary>
    public string PerformedBy { get; set; } = "System";

    /// <summary>
    /// İşleme ait açıklama veya özel not.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// İşlemin gerçekleştirildiği tarih ve saat.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// İşleme ait benzersiz işlem kimliği (örneğin toplu işlemler için TransactionId).
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
