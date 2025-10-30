using System.Collections.Generic;
using System.Threading.Tasks;
using Katana.Data.Models;

namespace Katana.Business.Interfaces
{
    public interface IPendingStockAdjustmentService
    {
        Task<PendingStockAdjustment> CreateAsync(PendingStockAdjustment creation);
        Task<IEnumerable<PendingStockAdjustment>> GetAllAsync();
        Task<PendingStockAdjustment?> GetByIdAsync(long id);
        Task<bool> ApproveAsync(long id, string approvedBy);
        Task<bool> RejectAsync(long id, string rejectedBy, string? reason = null);
    }
}
