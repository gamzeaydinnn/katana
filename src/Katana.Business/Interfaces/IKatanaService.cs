using Katana.Business.Interfaces;
using Katana.Business.DTOs;
using System;
using Katana.Core.DTOs;

namespace Katana.Business.Interfaces;

public interface IKatanaService
{
    Task<List<KatanaStockDto>> GetStockChangesAsync(DateTime fromDate, DateTime toDate);
    Task<List<KatanaProductDto>> GetProductsAsync();
    Task<List<KatanaInvoiceDto>> GetInvoicesAsync(DateTime fromDate, DateTime toDate);
    Task<KatanaProductDto?> GetProductBySkuAsync(string sku);
    Task<bool> TestConnectionAsync();
}

