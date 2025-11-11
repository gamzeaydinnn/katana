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
    /// <summary>
    /// Hata Yönetimi ve Onay Mekanizması Entegrasyon Testleri
    /// 
    /// Test Kapsamı:
    /// 1. FailedSyncRecord CRUD işlemleri
    /// 2. Admin hata düzeltme workflow
    /// 3. Veritabanı kayıt kontrolü
    /// 4. Audit log oluşturma
    /// 5. Resend tetikleme
    /// 6. PendingStockAdjustment onay workflow
    /// </summary>
    public class ErrorHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ErrorHandlingIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            // Setup test user authentication (simulated JWT)
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
            
            // Create integration log first
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

            // Create failed record
            var failedRecord = new FailedSyncRecord
            {
                RecordType = recordType,
                RecordId = "TEST-001",
                OriginalData = JsonSerializer.Serialize(new 
                { 
                    sku = "TEST-SKU-001",
                    quantity = -1,  // Invalid quantity
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

            // Find or create test product
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
                Quantity = 150, // New quantity
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
            // Arrange
            var testRecord = await CreateTestFailedRecord();

            // Act
            var response = await _client.GetAsync("/api/adminpanel/failed-records?page=1&pageSize=10");

            // Assert
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
            // Arrange
            await CreateTestFailedRecord("STOCK");
            
            // Act
            var response = await _client.GetAsync("/api/adminpanel/failed-records?status=FAILED");

            // Assert
            var result = await response.Content.ReadFromJsonAsync<FailedRecordsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, item => Assert.Equal("FAILED", item.Status));
        }

        [Fact]
        public async Task GetFailedRecords_FilterByRecordType_ReturnsFilteredResults()
        {
            // Arrange
            await CreateTestFailedRecord("ORDER");
            
            // Act
            var response = await _client.GetAsync("/api/adminpanel/failed-records?recordType=ORDER");

            // Assert
            var result = await response.Content.ReadFromJsonAsync<FailedRecordsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, item => Assert.Equal("ORDER", item.RecordType));
        }

        #endregion

        #region 2. FailedSyncRecord Detail Tests

        [Fact]
        public async Task GetFailedRecord_ValidId_ReturnsFullDetails()
        {
            // Arrange
            var testRecord = await CreateTestFailedRecord();

            // Act
            var response = await _client.GetAsync($"/api/adminpanel/failed-records/{testRecord.Id}");

            // Assert
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
            // Act
            var response = await _client.GetAsync("/api/adminpanel/failed-records/999999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region 3. Resolve Failed Record Tests

        [Fact]
        public async Task ResolveFailedRecord_ValidData_UpdatesStatusAndDatabase()
        {
            // Arrange
            var testRecord = await CreateTestFailedRecord();
            var correctedData = JsonSerializer.Serialize(new 
            { 
                sku = "TEST-SKU-001",
                quantity = 10,  // Fixed: positive quantity
                productName = "Test Product"
            });

            var resolveDto = new
            {
                Resolution = "Düzeltildi: Negatif miktar pozitif yapıldı",
                CorrectedData = correctedData,
                Resend = false
            };

            // Act
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify database update
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
            // Arrange
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
                Resend = true  // Should trigger resend logic
            };

            // Act
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify audit log created
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
            // Arrange
            var testRecord = await CreateTestFailedRecord();
            var resolveDto = new
            {
                Resolution = "Test resolution",
                CorrectedData = (string?)null,
                Resend = false
            };

            // Act
            await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/resolve", 
                resolveDto
            );

            // Assert - Verify audit log
            using var context = GetDbContext();
            var auditLog = await context.AuditLogs
                .FirstOrDefaultAsync(a => 
                    a.EntityName == "FailedSyncRecord" && 
                    a.EntityId == testRecord.Id.ToString() &&
                    a.ActionType.Contains("Resolved"));

            Assert.NotNull(auditLog);
            Assert.NotNull(auditLog.PerformedBy);
            // Timestamp is DateTime (value type), no need to check for null
        }

        #endregion

        #region 4. Ignore Failed Record Tests

        [Fact]
        public async Task IgnoreFailedRecord_ValidReason_UpdatesStatusToIgnored()
        {
            // Arrange
            var testRecord = await CreateTestFailedRecord();
            var ignoreDto = new
            {
                Reason = "Artık gerekli değil, ürün katalogdan kaldırıldı"
            };

            // Act
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/ignore", 
                ignoreDto
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify database
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
            // Arrange
            var testRecord = await CreateTestFailedRecord();
            var initialRetryCount = testRecord.RetryCount;

            // Act
            var response = await _client.PostAsync(
                $"/api/adminpanel/failed-records/{testRecord.Id}/retry", 
                null
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify retry count increment
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
            // Arrange
            var testRecord = await CreateTestFailedRecord();

            // Act - Retry 3 times
            for (int i = 0; i < 3; i++)
            {
                await _client.PostAsync($"/api/adminpanel/failed-records/{testRecord.Id}/retry", null);
            }

            // Assert - Verify exponential backoff
            using var context = GetDbContext();
            var updatedRecord = await context.FailedSyncRecords.FindAsync(testRecord.Id);
            Assert.NotNull(updatedRecord);
            Assert.Equal(3, updatedRecord.RetryCount);
            
            // Next retry should be approximately 2^3 = 8 minutes from now
            var expectedNextRetry = DateTime.UtcNow.AddMinutes(8);
            Assert.True(updatedRecord.NextRetryAt.HasValue);
            Assert.True(Math.Abs((updatedRecord.NextRetryAt.Value - expectedNextRetry).TotalMinutes) < 1);
        }

        #endregion

        #region 6. PendingStockAdjustment Approval Tests

        [Fact]
        public async Task GetPendingAdjustments_ReturnsListOfPendingItems()
        {
            // Arrange
            var pending = await CreateTestPendingAdjustment();

            // Act
            var response = await _client.GetAsync("/api/adminpanel/pending-adjustments?status=Pending");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<List<PendingAdjustmentDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Id == pending.Id);
        }

        [Fact]
        public async Task ApproveAdjustment_UpdatesStatusAndStock()
        {
            // Arrange
            var pending = await CreateTestPendingAdjustment();
            
            // Get initial product stock
            using var contextBefore = GetDbContext();
            var productBefore = await contextBefore.Products.FindAsync(pending.ProductId);
            var initialStock = productBefore!.Stock;

            // Act
            var response = await _client.PutAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/approve", 
                null
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify pending adjustment updated
            using var context = GetDbContext();
            var updatedPending = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(updatedPending);
            Assert.Equal("Approved", updatedPending.Status);
            Assert.NotNull(updatedPending.ApprovedAt);
            Assert.NotNull(updatedPending.ApprovedBy);

            // Verify product stock updated
            var updatedProduct = await context.Products.FindAsync(pending.ProductId);
            Assert.NotNull(updatedProduct);
            Assert.Equal(pending.Quantity, updatedProduct.Stock);
        }

        [Fact]
        public async Task RejectAdjustment_UpdatesStatusWithReason()
        {
            // Arrange
            var pending = await CreateTestPendingAdjustment();
            var rejectDto = new
            {
                Reason = "Stok miktarı yanlış, doğrulanması gerekiyor"
            };

            // Act
            var response = await _client.PutAsJsonAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/reject", 
                rejectDto
            );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify database
            using var context = GetDbContext();
            var updatedPending = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(updatedPending);
            Assert.Equal("Rejected", updatedPending.Status);
            Assert.Equal(rejectDto.Reason, updatedPending.RejectionReason);
            // Note: PendingStockAdjustment model doesn't have RejectedAt/RejectedBy fields
            // Status change to "Rejected" is sufficient for test validation

            // Verify product stock NOT updated
            var product = await context.Products.FindAsync(pending.ProductId);
            Assert.NotNull(product);
            Assert.NotEqual(pending.Quantity, product.Stock);
        }

        #endregion

        #region 7. End-to-End Workflow Tests

        [Fact]
        public async Task CompleteErrorCorrectionWorkflow_EndToEnd()
        {
            // Scenario: Hatalı stok verisi geldi, admin düzeltip yeniden gönderiyor

            // Step 1: Hatalı kayıt oluştur
            var failedRecord = await CreateTestFailedRecord("STOCK");
            Assert.Equal("FAILED", failedRecord.Status);

            // Step 2: Admin hatayı listeden görüntüler
            var listResponse = await _client.GetAsync("/api/adminpanel/failed-records?status=FAILED");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            // Step 3: Admin detayları görüntüler
            var detailResponse = await _client.GetAsync($"/api/adminpanel/failed-records/{failedRecord.Id}");
            var detail = await detailResponse.Content.ReadFromJsonAsync<FailedRecordDetail>();
            Assert.NotNull(detail);
            Assert.Contains("-1", detail.OriginalData); // Hatalı miktar

            // Step 4: Admin veriyi düzeltir
            var correctedData = JsonSerializer.Serialize(new 
            { 
                sku = "TEST-SKU-001",
                quantity = 10,
                productName = "Test Product"
            });

            // Step 5: Admin çözüm kaydeder ve yeniden gönderir
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

            // Step 6: Doğrulama - Kayıt RESOLVED olmalı
            using var context = GetDbContext();
            var resolved = await context.FailedSyncRecords.FindAsync(failedRecord.Id);
            Assert.NotNull(resolved);
            Assert.Equal("RESOLVED", resolved.Status);
            Assert.Equal(resolveDto.Resolution, resolved.Resolution);
            Assert.Contains("10", resolved.OriginalData); // Düzeltilmiş miktar

            // Step 7: Audit log oluşturulmalı
            var auditLogs = await context.AuditLogs
                .Where(a => a.EntityName == "FailedSyncRecord" && a.EntityId == failedRecord.Id.ToString())
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        }

        [Fact]
        public async Task CompleteApprovalWorkflow_EndToEnd()
        {
            // Scenario: Stok güncelleme onay bekliyor, admin onaylıyor

            // Step 1: Onay bekleyen kayıt oluştur
            var pending = await CreateTestPendingAdjustment();
            Assert.Equal("Pending", pending.Status);

            // Step 2: Admin bekleyen işlemleri görüntüler
            var listResponse = await _client.GetAsync("/api/adminpanel/pending-adjustments");
            var list = await listResponse.Content.ReadFromJsonAsync<List<PendingAdjustmentDto>>();
            Assert.NotNull(list);
            Assert.Contains(list, p => p.Id == pending.Id);

            // Step 3: Admin detayları kontrol eder (old vs new quantity)
            var pendingItem = list.First(p => p.Id == pending.Id);
            Assert.Equal(100, pendingItem.OldQuantity);
            Assert.Equal(150, pendingItem.Quantity);

            // Step 4: Admin onaylar
            var approveResponse = await _client.PutAsync(
                $"/api/adminpanel/pending-adjustments/{pending.Id}/approve", 
                null
            );
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

            // Step 5: Doğrulama - Status Approved olmalı
            using var context = GetDbContext();
            var approved = await context.PendingStockAdjustments.FindAsync(pending.Id);
            Assert.NotNull(approved);
            Assert.Equal("Approved", approved.Status);
            Assert.NotNull(approved.ApprovedBy);

            // Step 6: Ürün stoğu güncellenmiş olmalı
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
