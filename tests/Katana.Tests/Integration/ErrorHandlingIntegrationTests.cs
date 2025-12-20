using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Katana.Core.Entities;
using Katana.Core.Enums;
using Katana.Data;
using Katana.Data.Context;
using Katana.Data.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Katana.Tests.Integration
{
    
    
    
    
    
    
    
    
    
    
    
    public class ErrorHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ErrorHandlingIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer TEST_TOKEN");
        }

        #region Test Setup Helpers

        private IntegrationDbContext GetDbContext()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        }

        private async Task<FailedSyncRecord> CreateTestFailedRecord(string recordType = "STOCK")
        {
            using var context = GetDbContext();
            
            
            var integrationLog = new IntegrationLog
            {
                SyncType = "KATANA_TO_LUCA",
                Status = SyncStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessage = "Test error"
            };
            context.IntegrationLogs.Add(integrationLog);
            await context.SaveChangesAsync();

            
            var failedRecord = new FailedSyncRecord
            {
                RecordType = recordType,
                RecordId = "TEST-001",
                OriginalData = JsonSerializer.Serialize(new 
                { 
                    sku = "TEST-SKU-001",
                    quantity = -1,  
                    productName = "Test Product"
                }),
                ErrorMessage = "Validation failed: Quantity cannot be negative",
                ErrorCode = "VAL-001",
                FailedAt = DateTime.UtcNow,
                RetryCount = 0,
                Status = "FAILED",
                IntegrationLogId = integrationLog.Id
            };

            context.FailedSyncRecords.Add(failedRecord);
            await context.SaveChangesAsync();

            return failedRecord;
        }

        private async Task<PendingStockAdjustment> CreateTestPendingAdjustment()
        {
            using var context = GetDbContext();

            
            var product = await context.Products.FirstOrDefaultAsync(p => p.SKU == "TEST-PENDING-001");
            if (product == null)
            {
                product = new Product
                {
                    SKU = "TEST-PENDING-001",
                    Name = "Test Pending Product",
                    Stock = 100,
                    IsActive = true
                };
                context.Products.Add(product);
                await context.SaveChangesAsync();
            }

            var pending = new PendingStockAdjustment
            {
                ExternalOrderId = $"TEST-ORDER-{Guid.NewGuid()}",
                ProductId = product.Id,
                Sku = product.SKU,
                Quantity = 150, 
                Status = "Pending",
                RequestedBy = "TestSystem",
                RequestedAt = DateTimeOffset.UtcNow,
                Notes = $"Old quantity: {product.Stock}, Product: {product.Name}"
            };

            context.PendingStockAdjustments.Add(pending);
            await context.SaveChangesAsync();

            return pending;
        }

        #endregion

        #region 1. FailedSyncRecord List Tests

        [Fact]
        public async Task GetFailedRecords_ReturnsListWithPagination()
        {
            
            var testRecord = await CreateTestFailedRecord();

            
            var response = await _client.GetAsync("/api/adminpanel/failed-records?page=1&pageSize=10");

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<FailedRecordsResponse>();
            Assert.NotNull(result);
            Assert.True(result.Total > 0);
            Assert.NotEmpty(result.Items);
            
            var createdRecord = result.Items.FirstOrDefault(r => r.Id == testRecord.Id);
            Assert.NotNull(createdRecord);
            Assert.Equal("STOCK", createdRecord.RecordType);
            Assert.Equal("FAILED", createdRecord.Status);
        }

        [Fact]
        public async Task GetFailedRecords_FilterByStatus_ReturnsFilteredResults()
        {
            
            await CreateTestFailedRecord("STOCK");
            
            
            var response = await _client.GetAsync("/api/adminpanel/failed-records?status=FAILED");

            
            var result = await response.Content.ReadFromJsonAsync<FailedRecordsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, item => Assert.Equal("FAILED", item.Status));
        }

        [Fact]
        public async Task GetFailedRecords_FilterByRecordType_ReturnsFilteredResults()
        {
            
            await CreateTestFailedRecord("ORDER");
            
            
            var response = await _client.GetAsync("/api/adminpanel/failed-records?recordType=ORDER");

            
            var result = await response.Content.ReadFromJsonAsync<FailedRecordsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, item => Assert.Equal("ORDER", item.RecordType));
        }

        #endregion

        #region 2. FailedSyncRecord Detail Tests

        [Fact]
        public async Task GetFailedRecord_ValidId_ReturnsFullDetails()
        {
            
            var testRecord = await CreateTestFailedRecord();

            
            var response = await _client.GetAsync($"/api/adminpanel/failed-records/{testRecord.Id}");

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<FailedRecordDetail>();
            Assert.NotNull(result);
            Assert.Equal(testRecord.Id, result.Id);
            Assert.NotNull(result.OriginalData);
            Assert.Contains("TEST-SKU-001", result.OriginalData);
            Assert.NotNull(result.IntegrationLog);
        }

        [Fact]
        public async Task GetFailedRecord_InvalidId_ReturnsNotFound()
        {
            
            var response = await _client.GetAsync("/api/adminpanel/failed-records/999999");

            
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region 3. Resolve Failed Record Tests

        [Fact]
        public async Task ResolveFailedRecord_ValidData_UpdatesStatusAndDatabase()
        {
            
            var testRecord = await CreateTestFailedRecord();
            var correctedData = JsonSerializer.Serialize(new 
            { 
                sku = "TEST-SKU-001",
                quantity = 10,  
                productName = "Test Product"
            });

            var resolveDto = new
            {
                Resolution = "Düzeltildi: Negatif miktar pozitif yapıldı",
                CorrectedData = correctedData,
                Resend = false
            };

            
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var updatedRecord = await context.FailedSyncRecords.FindAsync(testRecord.Id);
            Assert.NotNull(updatedRecord);
            Assert.Equal("RESOLVED", updatedRecord.Status);
            Assert.Equal(resolveDto.Resolution, updatedRecord.Resolution);
            Assert.NotNull(updatedRecord.ResolvedAt);
            Assert.NotNull(updatedRecord.ResolvedBy);
            Assert.Equal(correctedData, updatedRecord.OriginalData);
        }

        [Fact]
        public async Task ResolveFailedRecord_WithResendFlag_TriggersResendLogic()
        {
            
            var testRecord = await CreateTestFailedRecord();
            var correctedData = JsonSerializer.Serialize(new 
            { 
                sku = "TEST-SKU-001",
                quantity = 10,
                productName = "Test Product"
            });

            var resolveDto = new
            {
                Resolution = "Düzeltildi ve yeniden gönderildi",
                CorrectedData = correctedData,
                Resend = true  
            };

            
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var auditLogs = await context.AuditLogs
                .Where(a => a.EntityName == "FailedSyncRecord" && a.EntityId == testRecord.Id.ToString())
                .ToListAsync();
            
            Assert.NotEmpty(auditLogs);
            Assert.Contains(auditLogs, a => a.ActionType.Contains("RESEND"));
        }

        [Fact]
        public async Task ResolveFailedRecord_CreatesAuditLog()
        {
            
            var testRecord = await CreateTestFailedRecord();
            var resolveDto = new
            {
                Resolution = "Test resolution",
                CorrectedData = (string?)null,
                Resend = false
            };

            
            await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            
            using var context = GetDbContext();
            var auditLog = await context.AuditLogs
                .FirstOrDefaultAsync(a => 
                    a.EntityName == "FailedSyncRecord" && 
                    a.EntityId == testRecord.Id.ToString() &&
                    a.ActionType.Contains("Resolved"));

            Assert.NotNull(auditLog);
            Assert.NotNull(auditLog.PerformedBy);
            
        }

        #endregion

        #region 4. Ignore Failed Record Tests

        [Fact]
        public async Task IgnoreFailedRecord_ValidReason_UpdatesStatusToIgnored()
        {
            
            var testRecord = await CreateTestFailedRecord();
            var ignoreDto = new
            {
                Reason = "Artık gerekli değil, ürün katalogdan kaldırıldı"
            };

            
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/ignore", 
                ignoreDto
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var updatedRecord = await context.FailedSyncRecords.FindAsync(testRecord.Id);
            Assert.NotNull(updatedRecord);
            Assert.Equal("IGNORED", updatedRecord.Status);
            Assert.Equal(ignoreDto.Reason, updatedRecord.Resolution);
            Assert.NotNull(updatedRecord.ResolvedAt);
        }

        #endregion

        #region 5. Retry Failed Record Tests

        [Fact]
        public async Task RetryFailedRecord_IncrementsRetryCount()
        {
            
            var testRecord = await CreateTestFailedRecord();
            var initialRetryCount = testRecord.RetryCount;

            
            var response = await _client.PostAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/retry", 
                null
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var updatedRecord = await context.FailedSyncRecords.FindAsync(testRecord.Id);
            Assert.NotNull(updatedRecord);
            Assert.Equal(initialRetryCount + 1, updatedRecord.RetryCount);
            Assert.Equal("RETRYING", updatedRecord.Status);
            Assert.NotNull(updatedRecord.LastRetryAt);
            Assert.NotNull(updatedRecord.NextRetryAt);
        }

        [Fact]
        public async Task RetryFailedRecord_CalculatesExponentialBackoff()
        {
            
            var testRecord = await CreateTestFailedRecord();

            
            for (int i = 0; i < 3; i++)
            {
                await _client.PostAsync($"/api/adminpanel/failed-records/{testRecord.Id}/retry", null);
            }

            
            using var context = GetDbContext();
            var updatedRecord = await context.FailedSyncRecords.FindAsync(testRecord.Id);
            Assert.NotNull(updatedRecord);
            Assert.Equal(3, updatedRecord.RetryCount);
            
            
            var expectedNextRetry = DateTime.UtcNow.AddMinutes(8);
            Assert.True(updatedRecord.NextRetryAt.HasValue);
            Assert.True(Math.Abs((updatedRecord.NextRetryAt.Value - expectedNextRetry).TotalMinutes) < 1);
        }

        #endregion

        #region 6. PendingStockAdjustment Approval Tests

        [Fact]
        public async Task GetPendingAdjustments_ReturnsListOfPendingItems()
        {
            
            var pending = await CreateTestPendingAdjustment();

            
            var response = await _client.GetAsync("/api/adminpanel/pending-adjustments?status=Pending");

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<List<PendingAdjustmentDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Id == pending.Id);
        }

        [Fact]
        public async Task ApproveAdjustment_UpdatesStatusAndStock()
        {
            
            var pending = await CreateTestPendingAdjustment();
            
            
            using var contextBefore = GetDbContext();
            var productBefore = await contextBefore.Products.FindAsync(pending.ProductId);
            var initialStock = productBefore!.Stock;

            
            var response = await _client.PutAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/approve", 
                null
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var updatedPending = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(updatedPending);
            Assert.Equal("Approved", updatedPending.Status);
            Assert.NotNull(updatedPending.ApprovedAt);
            Assert.NotNull(updatedPending.ApprovedBy);

            
            var updatedProduct = await context.Products.FindAsync(pending.ProductId);
            Assert.NotNull(updatedProduct);
            Assert.Equal(pending.Quantity, updatedProduct.Stock);
        }

        [Fact]
        public async Task RejectAdjustment_UpdatesStatusWithReason()
        {
            
            var pending = await CreateTestPendingAdjustment();
            var rejectDto = new
            {
                Reason = "Stok miktarı yanlış, doğrulanması gerekiyor"
            };

            
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/reject", 
                rejectDto
            );

            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            
            using var context = GetDbContext();
            var updatedPending = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(updatedPending);
            Assert.Equal("Rejected", updatedPending.Status);
            Assert.Equal(rejectDto.Reason, updatedPending.RejectionReason);
            
            

            
            var product = await context.Products.FindAsync(pending.ProductId);
            Assert.NotNull(product);
            Assert.NotEqual(pending.Quantity, product.Stock);
        }

        #endregion

        #region 7. End-to-End Workflow Tests

        [Fact]
        public async Task CompleteErrorCorrectionWorkflow_EndToEnd()
        {
            

            
            var failedRecord = await CreateTestFailedRecord("STOCK");
            Assert.Equal("FAILED", failedRecord.Status);

            
            var listResponse = await _client.GetAsync("/api/adminpanel/failed-records?status=FAILED");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            
            var detailResponse = await _client.GetAsync($"/api/adminpanel/failed-records/{failedRecord.Id}");
            var detail = await detailResponse.Content.ReadFromJsonAsync<FailedRecordDetail>();
            Assert.NotNull(detail);
            Assert.Contains("-1", detail.OriginalData); 

            
            var correctedData = JsonSerializer.Serialize(new 
            { 
                sku = "TEST-SKU-001",
                quantity = 10,
                productName = "Test Product"
            });

            
            var resolveDto = new
            {
                Resolution = "Negatif miktar düzeltildi ve pozitif yapıldı",
                CorrectedData = correctedData,
                Resend = true
            };

            var resolveResponse = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{failedRecord.Id}/resolve", 
                resolveDto
            );
            Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

            
            using var context = GetDbContext();
            var resolved = await context.FailedSyncRecords.FindAsync(failedRecord.Id);
            Assert.NotNull(resolved);
            Assert.Equal("RESOLVED", resolved.Status);
            Assert.Equal(resolveDto.Resolution, resolved.Resolution);
            Assert.Contains("10", resolved.OriginalData); 

            
            var auditLogs = await context.AuditLogs
                .Where(a => a.EntityName == "FailedSyncRecord" && a.EntityId == failedRecord.Id.ToString())
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        }

        [Fact]
        public async Task CompleteApprovalWorkflow_EndToEnd()
        {
            

            
            var pending = await CreateTestPendingAdjustment();
            Assert.Equal("Pending", pending.Status);

            
            var listResponse = await _client.GetAsync("/api/adminpanel/pending-adjustments");
            var list = await listResponse.Content.ReadFromJsonAsync<List<PendingAdjustmentDto>>();
            Assert.NotNull(list);
            Assert.Contains(list, p => p.Id == pending.Id);

            
            var pendingItem = list.First(p => p.Id == pending.Id);
            Assert.Equal(100, pendingItem.OldQuantity);
            Assert.Equal(150, pendingItem.Quantity);

            
            var approveResponse = await _client.PutAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/approve", 
                null
            );
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

            
            using var context = GetDbContext();
            var approved = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(approved);
            Assert.Equal("Approved", approved.Status);
            Assert.NotNull(approved.ApprovedBy);

            
            var product = await context.Products.FindAsync(pending.ProductId);
            Assert.NotNull(product);
            Assert.Equal(150, product.Stock);
        }

        #endregion

        #region DTOs for Test Responses

        private class FailedRecordsResponse
        {
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public List<FailedRecordListItem> Items { get; set; } = new();
        }

        private class FailedRecordListItem
        {
            public int Id { get; set; }
            public string RecordType { get; set; } = string.Empty;
            public string? RecordId { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public string? ErrorCode { get; set; }
            public DateTime FailedAt { get; set; }
            public int RetryCount { get; set; }
            public DateTime? LastRetryAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? ResolvedAt { get; set; }
            public string? ResolvedBy { get; set; }
            public int IntegrationLogId { get; set; }
            public string SourceSystem { get; set; } = string.Empty;
        }

        private class FailedRecordDetail : FailedRecordListItem
        {
            public string OriginalData { get; set; } = string.Empty;
            public DateTime? NextRetryAt { get; set; }
            public string? Resolution { get; set; }
            public IntegrationLogDto? IntegrationLog { get; set; }
        }

        private class IntegrationLogDto
        {
            public int Id { get; set; }
            public string SyncType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
        }

        private class PendingAdjustmentDto
        {
            public int Id { get; set; }
            public string ExternalOrderId { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string Sku { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int OldQuantity { get; set; }
            public int Quantity { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string? ApprovedBy { get; set; }
            public DateTime? RejectedAt { get; set; }
            public string? RejectedBy { get; set; }
            public string? RejectionReason { get; set; }
        }

        #endregion
    }
}
