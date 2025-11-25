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
    // Optional: pre-baked session cookie for Koza (e.g., "JSESSIONID=...") to bypass CAPTCHA
    public string? ManualSessionCookie { get; set; } = string.Empty;
    // Optional: force a specific branch id for Koza (orgSirketSubeId).
    // When set, the client will use this branch id during cookie-based auth
    // instead of attempting to auto-select from the branches response.
    public long? ForcedBranchId { get; set; } = null;
    public string DefaultBelgeSeri { get; set; } = "A";
    public double DefaultKdvOran { get; set; } = 0.20;
    public long DefaultOlcumBirimiId { get; set; } = 5;
    public long DefaultKartTipi { get; set; } = 4;
    public string DefaultKategoriKodu { get; set; } = "001";

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

    // Koza listeleme endpoint'leri
    public string TaxOffices { get; set; } = "ListeleGnlVergiDairesi.do";
    public string MeasurementUnits { get; set; } = "ListeleGnlOlcumBirimi.do";
    public string CustomerList { get; set; } = "ListeleFinMusteri.do";
    public string SupplierList { get; set; } = "ListeleFinTedarikci.do";
    public string Warehouses { get; set; } = "ListeleStkDepo.do";

    // Koza stok kartı listeleme endpoint'leri
    public string StockCards { get; set; } = "ListeleStkSkart.do";
    // Koza stok kartı ekleme endpoint'i (EkleStkWsSkart.do)
    public string StockCardCreate { get; set; } = "EkleStkWsSkart.do";
    public string StockCardPriceLists { get; set; } = "ListeleStkSkartFiyatListeleri.do";
    public string StockCardAltUnits { get; set; } = "ListeleStkSkartAlternatifOb.do";
    public string StockCardAltStocks { get; set; } = "ListeleStkSkartAlternatif.do";
    public string StockCardCosts { get; set; } = "ListeleStkSkartMaliyet.do";
    public string StockCategories { get; set; } = "ListeleStkSkartKategoriAgac.do";
    public string StockCardSuppliers { get; set; } = "ListeleStkSkartTeminYeri.do";
    public string StockCardPurchaseTerms { get; set; } = "ListeleStkSkartAlimSart.do";

    // Koza fatura endpoint'leri
    public string InvoiceList { get; set; } = "ListeleFtrSsFaturaBaslik.do";
    public string InvoiceCreate { get; set; } = "EkleFat.do";
    public string InvoiceClose { get; set; } = "EkleFtrWsFaturaKapama.do";
    public string InvoiceDelete { get; set; } = "SilFtrWsFaturaBaslik.do";

    // Koza cari / finansal nesne endpoint'leri
    public string CustomerAddresses { get; set; } = "ListeleWSGnlSsAdres.do";
    public string CustomerWorkingConditions { get; set; } = "GetirFinCalismaKosul.do";
    public string CustomerAuthorizedPersons { get; set; } = "ListeleFinFinansalNesneYetkili.do";
    public string CustomerRisk { get; set; } = "GetirFinRisk.do";
    public string CustomerTransaction { get; set; } = "EkleFinCariHareketBaslikWS.do";
    public string CustomerContacts { get; set; } = "ListeleWSGnlSsIletisim.do";
    public string BankList { get; set; } = "ListeleFinSsBanka.do";

    // Koza cari/tedarikçi ekleme endpoint'leri
    public string CustomerCreate { get; set; } = "EkleFinMusteriWS.do";
    public string SupplierCreate { get; set; } = "EkleFinTedarikciWS.do";

    // Koza irsaliye endpoint'leri
    public string IrsaliyeList { get; set; } = "ListeleStkSsIrsaliyeBaslik.do";
    public string IrsaliyeCreate { get; set; } = "EkleStkWsIrsaliyeBaslik.do";
    public string IrsaliyeDelete { get; set; } = "SilStkWsIrsaliyeBaslik.do";

    // Koza stok hareket endpoint'i (Diğer stok hareketi)
    public string OtherStockMovement { get; set; } = "EkleStkWsDshBaslik.do";

    // Stok / sipariş / depo ek uçlar
    public string WarehouseStockQuantity { get; set; } = "ListeleStkSsEldekiMiktar.do";
    public string SalesOrderList { get; set; } = "ListeleStsSsSiparisBaslik.do";
    public string SalesOrder { get; set; } = "EkleStsWsSiparisBaslik.do";
    public string SalesOrderDelete { get; set; } = "SilStsWsSiparisBaslik.do";
    public string SalesOrderDetailDelete { get; set; } = "SilDetayStsWsSiparisBaslik.do";
    public string PurchaseOrder { get; set; } = "EkleStnWsSiparisBaslik.do";
    public string PurchaseOrderDelete { get; set; } = "SilStnWsSiparisBaslik.do";
    public string PurchaseOrderDetailDelete { get; set; } = "SilDetayStnWsSiparisBaslik.do";
    public string WarehouseTransfer { get; set; } = "EkleStkWsDtransferBaslik.do";
    public string StockCountResult { get; set; } = "EkleStkWsSayimBaslik.do";
    public string Warehouse { get; set; } = "EkleStkWsDepo.do";
    public string CreditCardEntry { get; set; } = "EkleFinKrediKartiWS.do";
}
