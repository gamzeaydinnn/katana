using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IIntegrationTestService
{
    Task<IntegrationTestResultDto> TestKatanaToLucaStockFlowAsync(int sampleSize = 10);
    Task<IntegrationTestResultDto> TestKatanaToLucaInvoiceFlowAsync(int sampleSize = 10);
    Task<IntegrationTestResultDto> TestMappingConsistencyAsync();
    Task<List<IntegrationTestResultDto>> RunFullUATSuiteAsync();
}
