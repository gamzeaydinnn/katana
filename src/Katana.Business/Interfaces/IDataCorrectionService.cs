using Katana.Core.DTOs;
using Katana.Core.Entities;

namespace Katana.Business.Interfaces;

public interface IDataCorrectionService
{
  
    Task<List<ComparisonProductDto>> CompareKatanaAndLucaProductsAsync();
  
    Task<List<DataCorrectionDto>> GetPendingCorrectionsAsync();
    Task<DataCorrectionDto> CreateCorrectionAsync(CreateCorrectionDto dto, string userId);
    Task<bool> ApproveCorrectionAsync(int correctionId, string userId);
    Task<bool> RejectCorrectionAsync(int correctionId, string userId);
    Task<bool> ApplyCorrectionToLucaAsync(int correctionId);
    Task<bool> ApplyCorrectionToKatanaAsync(int correctionId);
}
