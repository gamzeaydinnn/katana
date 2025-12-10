namespace Katana.Data.Configuration;

public class LucaApiSettings
{
    public const string SectionName = "LucaApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MemberNumber { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 180;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseTokenAuth { get; set; } = true;
    
    public bool UseCookieContainer { get; set; } = true;
    
    public string? ManualSessionCookie { get; set; } = string.Empty;
    // If true, use a headless browser (Playwright) to obtain a real JSESSIONID cookie
    // This is required when the Koza/Luca login flow requires a real browser session
    public bool UseHeadlessAuth { get; set; } = false;
    
    
    
    public string Encoding { get; set; } = "ISO-8859-9"; // ← Luca API'nin native Turkish encoding
    
    
    public long? DefaultBranchId { get; set; }
    public long? ForcedBranchId { get; set; } = null;
    public string DefaultBelgeSeri { get; set; } = "A";
    public double DefaultKdvOran { get; set; } = 0.20;
    public long DefaultOlcumBirimiId { get; set; } = 5;
    public long DefaultKartTipi { get; set; } = 4;
    public string DefaultKategoriKodu { get; set; } = "001";
    public bool UsePostmanCustomerFormat { get; set; } = false;
    
    // 🔥 Kategori Mapping: Katana category_name → Luca KategoriAgacKod
    // Örnek: "1MAMUL" → "001", "3YARI MAMUL" → "220"
    public Dictionary<string, string> CategoryMapping { get; set; } = new();
    
    // No-Paging header'ı kullanılsın mı? (Performans için kapatılabilir)
    public bool UseNoPagingHeader { get; set; } = false;

    
    public LucaEndpoints Endpoints { get; set; } = new();
}

    public class LucaEndpoints
    {
        public string Invoices { get; set; } = "api/documents/invoices";
        public string Stock { get; set; } = "api/inventory/movements";
        public string Customers { get; set; } = "api/customers";
    public string Auth { get; set; } = "api/auth/token";
    public string Health { get; set; } = "api/health";
    
    public string Products { get; set; } = "api/products";
    
        public string Branches { get; set; } = "YdlUserResponsibilityOrgSs.do";
        public string ChangeBranch { get; set; } = "GuncelleYtkSirketSubeDegistir.do";
        public string DocumentTypeDetails { get; set; } = "ListeleGnlBelgeTurDetay.do";
        public string DocumentSeries { get; set; } = "ListeleGnlOrgSsSeri.do";
        public string BranchCurrencies { get; set; } = "ListeleGnlOrgSsParaBirim.do";
        public string DocumentSeriesMax { get; set; } = "MaxSeriNoWSGnlOrgSsSeri.do";
        public string DynamicLovValueList { get; set; } = "ListeleYtkDynamicLovValue.do";
        public string DynamicLovValueUpdate { get; set; } = "GuncelleYtkDynamicLovValue.do";
        public string DynamicLovValueCreate { get; set; } = "EkleYtkWSDynamicLov.do";
        public string AttributeUpdate { get; set; } = "GuncelleYtkAttribute.do";

        
        public string TaxOffices { get; set; } = "ListeleGnlVergiDairesi.do";
        public string MeasurementUnits { get; set; } = "ListeleGnlOlcumBirimi.do";
        public string CustomerList { get; set; } = "ListeleFinMusteri.do";
    public string SupplierList { get; set; } = "ListeleFinTedarikci.do";
    public string Warehouses { get; set; } = "ListeleStkDepo.do";

    
    public string StockCards { get; set; } = "ListeleStkSkart.do";
    
    public string StockCardCreate { get; set; } = "EkleStkWsSkart.do";
    public string StockCardPriceLists { get; set; } = "ListeleStkSkartFiyatListeleri.do";
    public string StockCardAltUnits { get; set; } = "ListeleStkSkartAlternatifOb.do";
    public string StockCardAltStocks { get; set; } = "ListeleStkSkartAlternatif.do";
    public string StockCardCosts { get; set; } = "ListeleStkSkartMaliyet.do";
    public string StockCategories { get; set; } = "ListeleStkSkartKategoriAgac.do";
    public string StockCardSuppliers { get; set; } = "ListeleStkSkartTeminYeri.do";
    public string StockCardPurchaseTerms { get; set; } = "ListeleStkSkartAlimSart.do";
    public string StockCardPurchasePrices { get; set; } = "ListeleStkSkartAlisFiyat.do";
    public string StockCardSalesPrices { get; set; } = "ListeleStkSkartSatisFiyat.do";

    
    public string InvoiceList { get; set; } = "ListeleFtrSsFaturaBaslik.do";
    public string InvoiceCreate { get; set; } = "EkleFat.do";
    public string InvoiceClose { get; set; } = "EkleFtrWsFaturaKapama.do";
    public string InvoiceDelete { get; set; } = "SilFtrWsFaturaBaslik.do";
    public string InvoicePdfLink { get; set; } = "FaturaPDFLinkFtrWsFaturaBaslik.do";
    public string CurrencyInvoiceList { get; set; } = "ListeleDovizliFtrSsFaturaBaslik.do";

    
    public string CustomerAddresses { get; set; } = "ListeleWSGnlSsAdres.do";
    public string CustomerWorkingConditions { get; set; } = "GetirFinCalismaKosul.do";
    public string CustomerAuthorizedPersons { get; set; } = "ListeleFinFinansalNesneYetkili.do";
    public string CustomerRisk { get; set; } = "GetirFinRisk.do";
    public string CustomerTransaction { get; set; } = "EkleFinCariHareketBaslikWS.do";
    public string CustomerContacts { get; set; } = "ListeleWSGnlSsIletisim.do";
    public string BankList { get; set; } = "ListeleFinSsBanka.do";
    public string CashList { get; set; } = "ListeleFinSsKasa.do";
    public string StockServiceReport { get; set; } = "DinamikRaporRprStokHizmetEkstre.do";

    
        public string CustomerCreate { get; set; } = "EkleFinMusteriWS.do";
    public string SupplierCreate { get; set; } = "EkleFinTedarikciWS.do";
    public string CustomerTransactionList { get; set; } = "ListeleFinCariHareketBaslik.do";
    public string SpecialCustomerTransactionList { get; set; } = "ListeleFinOzelCariHrktBaslik.do";
    public string CustomerContractCreate { get; set; } = "EkleFinMusteriSozlesmeWS.do";
    public string StockCardAutoComplete { get; set; } = "SdlSkart.do";
    public string UtsTransmit { get; set; } = "UTSIletildiFtrWsUTSIletim.do";
        
        
        public string IrsaliyeList { get; set; } = "ListeleStkSsIrsaliyeBaslik.do";
    public string IrsaliyeCreate { get; set; } = "EkleStkWsIrsaliyeBaslik.do";
    public string IrsaliyeDelete { get; set; } = "SilStkWsIrsaliyeBaslik.do";

    
    public string OtherStockMovement { get; set; } = "EkleStkWsDshBaslik.do";

    
    public string WarehouseStockQuantity { get; set; } = "ListeleStkSsEldekiMiktar.do";
    public string SalesOrderList { get; set; } = "ListeleStsSsSiparisBaslik.do";
    public string SalesOrder { get; set; } = "EkleStsWsSiparisBaslik.do";
    public string SalesOrderDelete { get; set; } = "SilStsWsSiparisBaslik.do";
    public string SalesOrderDetailDelete { get; set; } = "SilDetayStsWsSiparisBaslik.do";
    public string PurchaseOrderList { get; set; } = "ListeleStnSsSiparisBaslik.do";
    public string PurchaseOrder { get; set; } = "EkleStnWsSiparisBaslik.do";
    public string PurchaseOrderDelete { get; set; } = "SilStnWsSiparisBaslik.do";
    public string PurchaseOrderDetailDelete { get; set; } = "SilDetayStnWsSiparisBaslik.do";
    public string WarehouseTransfer { get; set; } = "EkleStkWsDtransferBaslik.do";
    public string StockCountResult { get; set; } = "EkleStkWsSayimBaslik.do";
    public string Warehouse { get; set; } = "EkleStkWsDepo.do";
    public string CreditCardEntry { get; set; } = "EkleFinKrediKartiWS.do";
    public string EirsaliyeXml { get; set; } = "EirsaliyeXmlStkEirsaliye.do";
}
