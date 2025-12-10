using System.Text.Json;
using FluentAssertions;
using Katana.Core.Entities;
using Katana.Core.DTOs.Koza;
using Katana.Data.Configuration;
using Katana.Infrastructure.APIClients;
using Xunit;

namespace Katana.Tests.Services;

public class KozaCustomerRequestFactoryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Build_WithNameAndCode_ShouldMatchPostmanShape()
    {
        var customer = new Customer
        {
            LucaCode = "0087",
            Title = "TY Demir Cargo",
            Type = 1
        };
        var settings = new LucaApiSettings
        {
            DefaultKategoriKodu = "120.06"
        };

        var dto = KozaCustomerRequestFactory.Build(customer, settings);
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"kartKod\":\"0087\"");
        json.Should().Contain("\"tanim\":\"TY Demir Cargo\"");
        json.Should().Contain("\"kisaAd\":\"TY Demir Cargo\"");
        json.Should().Contain("\"yasalUnvan\":\"TY Demir Cargo\"");
        json.Should().Contain("\"paraBirimKod\":\"TRY\"");
        json.Should().NotContain("adresSerbest");
        json.Should().Contain("\"kategoriKod\":\"120.06\"");
    }

    [Fact]
    public void Build_WithEmailAndAddress_ShouldEmitMatchingCamelCaseFields()
    {
        var customer = new Customer
        {
            LucaCode = "CUST-001",
            Title = "Acme Corp",
            Address = "Ankara Çankaya",
            City = "ANKARA",
            District = "MERKEZ",
            Email = "test@example.com",
            Type = 1
        };

        var settings = new LucaApiSettings
        {
            DefaultKategoriKodu = "120.06"
        };

        var dto = KozaCustomerRequestFactory.Build(customer, settings);
        var json = JsonSerializer.Serialize(dto, JsonOptions);

        json.Should().Contain("\"kartKod\":\"CUST-001\"");
        json.Should().Contain("\"adresSerbest\":\"Ankara Çankaya\"");
        json.Should().Contain("\"il\":\"ANKARA\"");
        json.Should().Contain("\"ilce\":\"MERKEZ\"");
        json.Should().Contain("\"iletisimTanim\":\"test@example.com\"");
        json.Should().Contain("\"iletisimTipId\":5");
    }
}

