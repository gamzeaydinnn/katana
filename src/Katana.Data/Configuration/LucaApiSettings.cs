namespace Katana.Data.Configuration;

public class LucaApiSettings
{
    public const string SectionName = "LucaApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MemberNumber { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseTokenAuth { get; set; } = true;

    // API Endpoints
    public LucaEndpoints Endpoints { get; set; } = new();
}

public class LucaEndpoints
{
    public string Invoices { get; set; } = "api/documents/invoices";
    public string Stock { get; set; } = "api/inventory/movements";
    public string Customers { get; set; } = "api/customers";
    public string Auth { get; set; } = "api/auth/token";
    public string Health { get; set; } = "api/health";
    // Optional products endpoint (some Luca installations expose product API)
    public string Products { get; set; } = "api/products";
    // Koza-specific endpoints
    public string Branches { get; set; } = "YdlUserResponsibilityOrgSs.do";
    public string ChangeBranch { get; set; } = "GuncelleYtkSirketSubeDegistir.do";

    // Koza stok kartı listeleme endpoint'leri
    public string StockCards { get; set; } = "ListeleStkSkart.do";
    public string StockCardPriceLists { get; set; } = "ListeleStkSkartFiyatListeleri.do";
    public string StockCardAltUnits { get; set; } = "ListeleStkSkartAlternatifOb.do";
    public string StockCardAltStocks { get; set; } = "ListeleStkSkartAlternatif.do";
    public string StockCardCosts { get; set; } = "ListeleStkSkartMaliyet.do";
    public string StockCategories { get; set; } = "ListeleStkSkartKategoriAgac.do";

    // Koza fatura endpoint'leri
    public string InvoiceList { get; set; } = "ListeleFtrSsFaturaBaslik.do";
    public string InvoiceClose { get; set; } = "EkleFtrWsFaturaKapama.do";
    public string InvoiceDelete { get; set; } = "SilFtrWsFaturaBaslik.do";

    // Koza cari / finansal nesne endpoint'leri
    public string CustomerAddresses { get; set; } = "ListeleWSGnlSsAdres.do";
    public string CustomerWorkingConditions { get; set; } = "GetirFinCalismaKosul.do";
    public string CustomerAuthorizedPersons { get; set; } = "ListeleFinFinansalNesneYetkili.do";
    public string CustomerRisk { get; set; } = "GetirFinRisk.do";
    public string CustomerTransaction { get; set; } = "EkleFinCariHareketBaslikWS.do";

    // Koza irsaliye endpoint'leri
    public string IrsaliyeList { get; set; } = "ListeleStkSsIrsaliyeBaslik.do";
    public string IrsaliyeCreate { get; set; } = "EkleStkWsIrsaliyeBaslik.do";
    public string IrsaliyeDelete { get; set; } = "SilStkWsIrsaliyeBaslik.do";
}
