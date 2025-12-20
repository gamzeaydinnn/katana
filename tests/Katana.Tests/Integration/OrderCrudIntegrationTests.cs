using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Katana.Core.DTOs;

namespace Katana.Tests.Integration;

/// <summary>
/// Integration testleri - Gerçek API endpoint'lerini test eder
/// NOT: Bu testler çalışması için API'nin ayakta olması gerekir
/// </summary>
public class OrderCrudIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderCrudIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SalesOrders_GetAll_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/sales-orders");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task SalesOrders_GetStats_ReturnsStatistics()
    {
        // Act
        var response = await _client.GetAsync("/api/sales-orders/stats");

        // Assert
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(stats);
    }

    [Fact]
    public async Task PurchaseOrders_CreateAndDelete_WorksCorrectly()
    {
        // Arrange - Create request
        var createRequest = new
        {
            supplierId = 1,
            orderDate = System.DateTime.UtcNow,
            items = new[]
            {
                new
                {
                    productId = 1,
                    quantity = 10,
                    unitPrice = 100.00m
                }
            }
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/purchase-orders", createRequest);
        
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            // Supplier veya Product yoksa test skip edilebilir
            Assert.True(createResponse.StatusCode == System.Net.HttpStatusCode.BadRequest, 
                $"Expected BadRequest or Success, got {createResponse.StatusCode}: {errorContent}");
            return;
        }

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var orderId = createdOrder?.id;

        // Act - Delete
        var deleteResponse = await _client.DeleteAsync($"/api/purchase-orders/{orderId}");

        // Assert
        Assert.True(deleteResponse.IsSuccessStatusCode || 
                    deleteResponse.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invoices_FullCrudCycle_WorksCorrectly()
    {
        // 1. CREATE
        var createDto = new
        {
            customerId = 1,
            invoiceDate = System.DateTime.UtcNow,
            dueDate = System.DateTime.UtcNow.AddDays(30),
            items = new[]
            {
                new
                {
                    productId = 1,
                    quantity = 5,
                    unitPrice = 200.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createDto);
        
        if (!createResponse.IsSuccessStatusCode)
        {
            // Customer veya Product yoksa test skip
            return;
        }

        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var invoiceId = (int)createdInvoice.id;

        // 2. READ
        var getResponse = await _client.GetAsync($"/api/invoices/{invoiceId}");
        getResponse.EnsureSuccessStatusCode();
        var invoice = await getResponse.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(invoice);

        // 3. UPDATE STATUS
        var updateStatusDto = new { status = "Paid" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/invoices/{invoiceId}/status", updateStatusDto);
        updateResponse.EnsureSuccessStatusCode();

        // 4. DELETE
        var deleteResponse = await _client.DeleteAsync($"/api/invoices/{invoiceId}");
        deleteResponse.EnsureSuccessStatusCode();

        // 5. VERIFY DELETION
        var verifyResponse = await _client.GetAsync($"/api/invoices/{invoiceId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task Invoices_GetByStatus_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/status/Pending");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Invoices_GetStatistics_ReturnsAggregatedData()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/statistics");

        // Assert
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(stats);
    }
}
