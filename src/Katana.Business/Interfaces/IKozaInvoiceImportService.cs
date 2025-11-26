using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKozaInvoiceImportService
{
    /// <summary>
    /// Koza (Luca) tarafındaki faturaları listeleyip Invoice entity'lerine map eder..
    /// ve IntegrationDbContext üzerinden veritabanına yazdı
    /// </summary>
    /// <param name="fromDate">Opsiyonel başlangıç tarihi (belgeTarihiBas)</param>
    /// <param name="toDate">Opsiyonel bitiş tarihi (belgeTarihiBit)</param>
    /// <param name="ustHareketTuru">16/17/18/19 gibi üst hareket türü</param>
    /// <param name="altHareketTuru">Alt hareket türü ID'si</param>
    Task<IntegrationTestResultDto> ImportInvoicesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? ustHareketTuru = null,
        int? altHareketTuru = null);
}
