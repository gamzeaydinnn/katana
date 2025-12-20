using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Katana.Data.Configuration;
using Katana.Infrastructure.APIClients;
using Katana.Business.Interfaces;

namespace Katana.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }

  public class DelegatingHandlerStub : DelegatingHandler
  {
    private readonly System.Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public DelegatingHandlerStub(System.Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
      _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var resp = _responder(request);
      return Task.FromResult(resp);
    }
  }

    public class KatanaServiceMappingTests
    {
        private const string SampleProductsJson = @"{
  ""data"": [
    {
      ""id"": 15526484,
  ""name"": ""093-58064-083"",
  ""uom"": ""adet"",
  ""category_name"": ""1MAMUL"",
  ""created_at"": ""2025-09-30T09:48:47.891Z"",
  ""updated_at"": ""2025-10-02T12:03:38.589Z"",
      ""variants"": [
        {
          ""id"": 36840230,
          ""product_id"": 15526484,
          ""sku"": ""6250681"",
          ""sales_price"": 0
        }
      ]
    },
    {
  ""id"": 15526483,
  ""name"": ""83.9945.3A"",
      ""variants"": [
        {
          ""id"": 36840229,
          ""product_id"": 15526483,
          ""sku"": ""387758"",
          ""sales_price"": ""0""
        }
      ]
    }
  ]
}";

    [Fact]
    public async Task GetProductsAsync_Should_Map_Variant_Sku_And_Price()
    {
      
      var handler = new FakeHttpMessageHandler(SampleProductsJson, HttpStatusCode.OK);
      var httpClient = new HttpClient(handler)
      {
        BaseAddress = new System.Uri("https://api.katanamrp.com/v1/")
      };

      var katanaSettings = new KatanaApiSettings
      {
        BaseUrl = "https://api.katanamrp.com/v1/",
        ApiKey = "test",
        Endpoints = new KatanaApiEndpoints { Products = "products" }
      };

  var options = Options.Create(katanaSettings);
  var loggerMock = new Mock<ILogger<KatanaService>>();
  var loggingServiceMock = new Mock<ILoggingService>();
  var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

  var service = new KatanaService(httpClient, options, loggerMock.Object, loggingServiceMock.Object, memoryCache);

      
      var products = await service.GetProductsAsync();

      
      products.Should().NotBeNull();
      products.Count.Should().Be(2);
      products[0].SKU.Should().Be("6250681");
      products[0].Price.Should().Be(0m);
      products[0].Name.Should().Be("093-58064-083");

      products[1].SKU.Should().Be("387758");
      products[1].Price.Should().Be(0m);
    }

    [Fact]
    public async Task GetProductBySkuAsync_Should_Find_Variant_Then_Product()
    {
      
      var variantsJson = @"{ ""data"": [ { ""id"": 36840230, ""product_id"": 15526484, ""sku"": ""6250681"" } ] }";
      var productJson = @"{ ""id"": 15526484, ""name"": ""093-58064-083"", ""variants"": [ { ""id"": 36840230, ""sku"": ""6250681"", ""sales_price"": 0 } ] }";

      var routingHandler = new DelegatingHandlerStub((req) =>
      {
        var path = req.RequestUri?.AbsolutePath ?? string.Empty;
        if (path.Contains("/variants"))
          return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(variantsJson) };
        if (path.Contains("/products/15526484"))
          return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(productJson) };
        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
      });

      var httpClient = new HttpClient(routingHandler) { BaseAddress = new System.Uri("https://api.katanamrp.com/v1/") };

      var katanaSettings = new KatanaApiSettings { BaseUrl = "https://api.katanamrp.com/v1/", Endpoints = new KatanaApiEndpoints { Products = "products" } };
    var options = Options.Create(katanaSettings);
    var loggerMock = new Mock<ILogger<KatanaService>>();
    var loggingServiceMock = new Mock<ILoggingService>();
    var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

    var service = new KatanaService(httpClient, options, loggerMock.Object, loggingServiceMock.Object, memoryCache);

      
      var product = await service.GetProductBySkuAsync("6250681");

      
      product.Should().NotBeNull();
      product!.SKU.Should().Be("6250681");
      product.Name.Should().Be("093-58064-083");
    }

    [Fact]
    public async Task GetInvoicesAsync_Should_Map_SalesOrders_To_Invoices()
    {
      
      var invoicesJson = @"{ ""data"": [ { ""id"": 35450823, ""order_no"": ""SO-22"", ""order_created_date"": ""2025-10-28T14:36:54.561Z"", ""currency"": ""USD"", ""total"": 123.45, ""sales_order_rows"": [ { ""id"": 87053276, ""quantity"": 1, ""variant_id"": 36731430, ""price_per_unit"": ""123.45"", ""total"": 123.45 } ] } ] }";

      var handler = new FakeHttpMessageHandler(invoicesJson, HttpStatusCode.OK);
      var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("https://api.katanamrp.com/v1/") };

      var katanaSettings = new KatanaApiSettings { BaseUrl = "https://api.katanamrp.com/v1/", Endpoints = new KatanaApiEndpoints { Invoices = "sales_orders" } };
    var options = Options.Create(katanaSettings);
    var loggerMock = new Mock<ILogger<KatanaService>>();
    var loggingServiceMock = new Mock<ILoggingService>();
    var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

    var service = new KatanaService(httpClient, options, loggerMock.Object, loggingServiceMock.Object, memoryCache);

      
      var invs = await service.GetInvoicesAsync(System.DateTime.UtcNow.AddDays(-1), System.DateTime.UtcNow);

      
      invs.Should().NotBeNull();
      invs.Count.Should().Be(1);
      invs[0].InvoiceNo.Should().Be("SO-22");
      invs[0].Items.Should().NotBeNull();
      invs[0].Items.Count.Should().Be(1);
      invs[0].TotalAmount.Should().Be(123.45m);
    }
    }
}
