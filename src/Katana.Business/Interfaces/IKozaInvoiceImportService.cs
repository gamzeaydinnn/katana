using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKozaInvoiceImportService
{
    
    
    
    
    
    
    
    
    Task<IntegrationTestResultDto> ImportInvoicesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? ustHareketTuru = null,
        int? altHareketTuru = null);
}
