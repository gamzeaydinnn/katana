/**
 * Luca/Koza Entegrasyon API Servisi
 * 
 * Backend LucaProxyController üzerinden tüm Luca API endpoint'lerine erişim sağlar.
 * Frontend asla direkt Luca API'ye bağlanmaz, her zaman backend proxy kullanır.
 * 
 * Kategoriler:
 * - Giriş (Login, Şube Seçimi)
 * - Genel (Ölçü Birimi, Vergi Dairesi, Belge Türleri, Para Birimi)
 * - Cari (Müşteri, Tedarikçi, Adres, Risk)
 * - Stok (Stok Kartları, Depo, İrsaliye, Kategori)
 * - Sipariş (Satış, Satınalma)
 * - Fatura (Fatura İşlemleri)
 * - Finans (Cari Hareket, Banka, Kasa, Kredi Kartı)
 * - Rapor (Stok-Hizmet Ekstre)
 */

import axios from "axios";

// API Base URL
const getApiBaseUrl = () => {
  if (typeof window !== "undefined") {
    const { protocol, hostname, port } = window.location;
    if (port === "3000" || port === "3001") {
      return `${protocol}//${hostname}:5055/api`;
    }
  }
  return process.env.REACT_APP_API_URL || "/api";
};

const API_BASE_URL = getApiBaseUrl();

// Luca Proxy API Client
const lucaProxyClient = axios.create({
  baseURL: `${API_BASE_URL}/luca-proxy`,
  withCredentials: true,
  timeout: 60000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Session ID yönetimi
let currentSessionId: string | null = null;

const getSessionId = (): string | null => {
  if (currentSessionId) return currentSessionId;
  if (typeof window !== "undefined") {
    return localStorage.getItem("lucaSessionId");
  }
  return null;
};

const setSessionId = (sessionId: string) => {
  currentSessionId = sessionId;
  if (typeof window !== "undefined") {
    localStorage.setItem("lucaSessionId", sessionId);
  }
};

// Request Interceptor - Session ID ekleme
lucaProxyClient.interceptors.request.use((config: any) => {
  const sessionId = getSessionId();
  if (sessionId && config.headers) {
    config.headers["X-Luca-Session"] = sessionId;
  }
  
  // Auth token ekle
  const token = typeof window !== "undefined" 
    ? window.localStorage.getItem("authToken") 
    : null;
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  
  return config;
});

// Response Interceptor - Hata yönetimi
lucaProxyClient.interceptors.response.use(
  (response: any) => {
    // Response'dan session ID'yi çıkar
    const sessionId = response.headers["x-luca-proxy-session"];
    if (sessionId && typeof sessionId === "string") {
      setSessionId(sessionId);
    }
    return response;
  },
  (error: any) => {
    if (error.code === "ERR_BLOCKED_BY_CLIENT") {
      console.error("🚫 AdBlock/Extension engellemesi tespit edildi.");
    }
    return Promise.reject(error);
  }
);

// ============================================================================
// TİP TANIMLARI
// ============================================================================

export interface LucaLoginRequest {
  orgCode?: string;
  userName?: string;
  userPassword?: string;
}

export interface LucaLoginResponse {
  sessionId?: string;
  raw?: any;
  message?: string;
}

export interface LucaBranchResponse {
  branches?: any[];
  raw?: any;
}

export interface LucaSelectBranchRequest {
  orgSirketSubeId: number;
}

// Genel Tipler
export interface LucaMeasurementUnit {
  olcumBirimiId?: number;
  aciklama?: string;
}

export interface LucaTaxOffice {
  vergiDairesiId?: number;
  aciklama?: string;
  ilId?: number;
}

export interface LucaDocumentTypeDetail {
  belgeTurDetayId?: number;
  aciklama?: string;
}

export interface LucaCurrency {
  paraBirimId?: number;
  paraBirimKod?: string;
  aciklama?: string;
}

// Cari Tipler
export interface LucaCustomer {
  finansalNesneId?: number;
  kartKod?: string;
  tanim?: string;
  kisaAd?: string;
  yasalUnvan?: string;
  vergiNo?: string;
}

export interface LucaSupplier {
  finansalNesneId?: number;
  kartKod?: string;
  tanim?: string;
  kisaAd?: string;
  yasalUnvan?: string;
  vergiNo?: string;
}

export interface LucaCreateCustomerRequest {
  tip: number;
  cariTipId: number;
  kartKod: string;
  tanim: string;
  paraBirimKod: string;
  kisaAd?: string;
  yasalUnvan?: string;
  adresSerbest?: string;
  il?: string;
  ilce?: string;
  vergiNo?: string;
  vergiDairesi?: string;
}

export interface LucaCreateSupplierRequest {
  tip: number;
  cariTipId: number;
  kartKod: string;
  tanim: string;
  paraBirimKod: string;
  kisaAd?: string;
  yasalUnvan?: string;
  adresSerbest?: string;
  il?: string;
  ilce?: string;
  vergiNo?: string;
}

// Stok Tipler
export interface LucaStockCard {
  skartId?: number;
  kartKodu?: string;
  kartAdi?: string;
  olcumBirimiId?: number;
  kartTuru?: number;
  kartAlisKdvOran?: number;
  kategoriAgacKod?: string;
}

export interface LucaCreateStockCardRequest {
  kartAdi: string;
  kartKodu: string;
  kartTipi?: number;
  kartAlisKdvOran?: number;
  olcumBirimiId?: number;
  baslangicTarihi: string;
  kartTuru: number;
  kategoriAgacKod?: string;
  barkod?: string;
  satilabilirFlag?: number;
  satinAlinabilirFlag?: number;
}

export interface LucaWarehouse {
  depoId?: number;
  depoKodu?: string;
  depoAdi?: string;
}

export interface LucaDeliveryNote {
  ssIrsaliyeBaslikId?: number;
  belgeSeri?: string;
  belgeNo?: number;
  belgeTarihi?: string;
}

export interface LucaCreateDeliveryNoteRequest {
  bayiNo?: string;
  eirsaliyeTuru?: number;
  belgeSeri: string;
  belgeNo?: number;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeTakipNo?: string;
  belgeAciklama?: string;
  belgeTurDetayId: number;
  paraBirimKod: string;
  kurBedeli?: number;
  yuklemeTarihi?: string;
  kdvFlag?: boolean;
  musteriTedarikci: number;
  cariKodu: string;
  detayList: any[];
}

// Sipariş Tipler
export interface LucaSalesOrder {
  ssSiparisBaslikId?: number;
  belgeSeri?: string;
  belgeNo?: number;
  belgeTarihi?: string;
}

export interface LucaCreateSalesOrderRequest {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeAciklama?: string;
  teklifSiparisTur: number;
  paraBirimKod: string;
  cariKodu: string;
  kdvFlag?: boolean;
  islemTuru?: number;
  detayList: any[];
}

export interface LucaCreatePurchaseOrderRequest {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeAciklama?: string;
  teklifSiparisTur: number;
  paraBirimKod: string;
  cariKodu: string;
  kdvFlag?: boolean;
  detayList: any[];
}

// Fatura Tipler
export interface LucaInvoice {
  ssFaturaBaslikId?: number;
  belgeSeri?: string;
  belgeNo?: number;
  belgeTarihi?: string;
}

export interface LucaCreateInvoiceRequest {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeAciklama?: string;
  belgeTurDetayId: number;
  faturaTur: number;
  paraBirimKod: string;
  kdvFlag?: boolean;
  musteriTedarikci: number;
  cariKodu: string;
  detayList: any[];
}

// Finans Tipler
export interface LucaCreateCreditCardEntryRequest {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeAciklama?: string;
  cariKodu: string;
  detayList: any[];
}

export interface LucaCreateCariMovementRequest {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeTurDetayId: number;
  belgeTakipNo?: string;
  belgeAciklama?: string;
  cariTuru: number;
  paraBirimKod: string;
  cariKodu: string;
  kurBedeli?: number;
  detayList: any[];
}

// ============================================================================
// GİRİŞ FONKSİYONLARI
// ============================================================================

/**
 * Luca'ya giriş yapar
 */
export const login = async (
  credentials?: LucaLoginRequest
): Promise<LucaLoginResponse> => {
  const response = await lucaProxyClient.post<LucaLoginResponse>(
    "/login",
    credentials || {}
  );
  
  const sessionId = response.data?.sessionId;
  if (sessionId) {
    setSessionId(sessionId);
  }
  
  return response.data;
};

/**
 * Şube listesini getirir
 */
export const getBranches = async (): Promise<LucaBranchResponse> => {
  const response = await lucaProxyClient.post<LucaBranchResponse>("/branches", {});
  return response.data;
};

/**
 * Şube seçer
 */
export const selectBranch = async (
  request: LucaSelectBranchRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/select-branch", request);
  return response.data;
};

// ============================================================================
// GENEL FONKSİYONLAR
// ============================================================================

/**
 * Ölçü birimi listesini getirir
 */
export const listMeasurementUnits = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/measurement-units/list", {});
  return response.data;
};

/**
 * Vergi dairesi listesini getirir
 */
export const listTaxOffices = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/tax-offices/list", {});
  return response.data;
};

/**
 * Belge türü detay listesini getirir
 */
export const listDocumentTypeDetails = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/document-type-details", {});
  return response.data;
};

/**
 * Seri listesini getirir
 */
export const listDocumentSeries = async (params?: {
  gnlBelgeTurDetay?: { belgeTurDetayId?: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/document-series", params || {});
  return response.data;
};

/**
 * Para birimi listesini getirir
 */
export const listBranchCurrencies = async (params?: {
  gnlOrgSirketSube?: { orgSirketSubeId?: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/branch-currencies", params || {});
  return response.data;
};

/**
 * Seri son numarasını getirir
 */
export const getDocumentSeriesMax = async (params: {
  seriNoWs: string;
  belgeTurDetayIdWs: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/document-series/max", params);
  return response.data;
};

/**
 * Dinamik LOV değerlerini listeler
 */
export const listDynamicLovValues = async (params?: {
  ytkDynamicLov?: { dynamicLovId?: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/dynamic-lov-values", params || {});
  return response.data;
};

/**
 * Dinamik LOV değeri günceller
 */
export const updateDynamicLovValue = async (params: any): Promise<any> => {
  const response = await lucaProxyClient.post("/dynamic-lov-values/update", params);
  return response.data;
};

/**
 * Dinamik LOV değeri oluşturur
 */
export const createDynamicLovValue = async (params: any): Promise<any> => {
  const response = await lucaProxyClient.post("/dynamic-lov-values/create", params);
  return response.data;
};

// ============================================================================
// CARİ FONKSİYONLARI
// ============================================================================

/**
 * Müşteri listesini getirir
 */
export const listCustomers = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/customers/list", params || {});
  return response.data;
};

/**
 * Tedarikçi listesini getirir
 */
export const listSuppliers = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/suppliers/list", params || {});
  return response.data;
};

/**
 * Müşteri ekler
 */
export const createCustomer = async (
  request: LucaCreateCustomerRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/customers/create", request);
  return response.data;
};

/**
 * Tedarikçi ekler
 */
export const createSupplier = async (
  request: LucaCreateSupplierRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/suppliers/create", request);
  return response.data;
};

/**
 * Cari adres listesini getirir
 */
export const listCustomerAddresses = async (params: {
  finansalNesneId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-addresses", params);
  return response.data;
};

/**
 * Cari çalışma koşullarını getirir
 */
export const getCustomerWorkingConditions = async (params: {
  calismaKosulId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-working-conditions", params);
  return response.data;
};

/**
 * Cari yetkili kişileri getirir
 */
export const listCustomerAuthorizedPersons = async (params: {
  gnlFinansalNesne: { finansalNesneId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-authorized-persons", params);
  return response.data;
};

/**
 * Cari risk bilgilerini getirir
 */
export const getCustomerRisk = async (params: {
  gnlFinansalNesne: { finansalNesneId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-risk", params);
  return response.data;
};

/**
 * Cari hareket oluşturur
 */
export const createCustomerTransaction = async (
  request: LucaCreateCariMovementRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/finance/cari-movements/create", request);
  return response.data;
};

/**
 * Cari hareket listesini getirir
 */
export const listCustomerTransactions = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/finance/cari-movements/list", params || {});
  return response.data;
};

/**
 * Özel cari hareket listesini getirir
 */
export const listSpecialCustomerTransactions = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-transactions/special-list", {});
  return response.data;
};

/**
 * Sözleşme ekler
 */
export const createCustomerContract = async (params: any): Promise<any> => {
  const response = await lucaProxyClient.post("/customer-contracts/create", params);
  return response.data;
};

// ============================================================================
// STOK FONKSİYONLARI
// ============================================================================

/**
 * Stok kartı listesini getirir
 */
export const listStockCards = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/list", params || {});
  return response.data;
};

/**
 * Stok kartı oluşturur
 */
export const createStockCard = async (
  request: LucaCreateStockCardRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/create", request);
  return response.data;
};

/**
 * Stok kategorilerini listeler
 */
export const listStockCategories = async (params?: {
  kartTuru?: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-categories/list", params || {});
  return response.data;
};

/**
 * Stok kartı alternatif ölçü birimlerini getirir
 */
export const listStockCardAltUnits = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/alt-units", params);
  return response.data;
};

/**
 * Stok kartı alternatif stokları getirir
 */
export const listStockCardAltStocks = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/alt-stocks", params);
  return response.data;
};

/**
 * Stok kartı alış fiyatlarını getirir
 */
export const listStockCardPurchasePrices = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/purchase-prices", params);
  return response.data;
};

/**
 * Stok kartı satış fiyatlarını getirir
 */
export const listStockCardSalesPrices = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/sales-prices", params);
  return response.data;
};

/**
 * Stok kartı maliyet bilgilerini getirir
 */
export const listStockCardCosts = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/costs", params);
  return response.data;
};

/**
 * Stok kartı alım şartlarını getirir
 */
export const listStockCardPurchaseTerms = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/purchase-terms", params);
  return response.data;
};

/**
 * Stok kartı tedarikçilerini getirir
 */
export const listStockCardSuppliers = async (params: {
  stkSkart: { skartId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/suppliers", params);
  return response.data;
};

/**
 * Stok kartı autocomplete
 */
export const autocompleteStockCards = async (params: {
  kartTuru: number;
  q: string;
  pageNo?: number;
  autoComplete?: number;
  pageSize?: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-cards/autocomplete", params);
  return response.data;
};

/**
 * Depo listesini getirir
 */
export const listWarehouses = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/warehouses/list", {});
  return response.data;
};

/**
 * Depo eldeki miktar listesini getirir
 */
export const getWarehouseStockQuantity = async (params: {
  cagirilanKart: string;
  stkDepo: { depoId: number };
}): Promise<any> => {
  const response = await lucaProxyClient.post("/warehouses/stock-quantity", params);
  return response.data;
};

/**
 * İrsaliye listesini getirir
 */
export const listDeliveryNotes = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/delivery-notes/list", params || {});
  return response.data;
};

/**
 * İrsaliye oluşturur
 */
export const createDeliveryNote = async (
  request: LucaCreateDeliveryNoteRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/delivery-notes/create", request);
  return response.data;
};

/**
 * İrsaliye siler
 */
export const deleteDeliveryNote = async (params: {
  ssIrsaliyeBaslikId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/delivery-notes/delete", params);
  return response.data;
};

/**
 * E-irsaliye XML getirir
 */
export const getEirsaliyeXml = async (params: {
  eirsaliyeId: number;
}): Promise<string> => {
  const response = await lucaProxyClient.post("/delivery-notes/eirsaliye/xml", params);
  return response.data as string;
};

/**
 * Depo transferi oluşturur
 */
export const createWarehouseTransfer = async (params: {
  belgeSeri: string;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeTakipNo?: string;
  belgeAciklama?: string;
  girisDepoKodu: string;
  cikisDepoKodu: string;
  detayList: any[];
}): Promise<any> => {
  const response = await lucaProxyClient.post("/warehouse-transfers/create", params);
  return response.data;
};

/**
 * Stok sayımı oluşturur
 */
export const createStockCount = async (params: {
  belgeSeri: string;
  belgeTarihi: string;
  belgeAciklama?: string;
  belgeTakipNo?: string;
  depoKodu: string;
  detayList: any[];
  kapamaBelgeOlustur?: boolean;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/stock-count/create", params);
  return response.data;
};

/**
 * DSH başlık ekler (Fire, Sarf, Sayım Fazlası vb.)
 */
export const createDshHeader = async (params: {
  belgeSeri: string;
  belgeTurDetayId: number;
  belgeTarihi: string;
  duzenlemeSaati: string;
  vadeTarihi: string;
  belgeAciklama?: string;
  paraBirimKod: string;
  depoKodu: string;
  detayList: any[];
}): Promise<any> => {
  const response = await lucaProxyClient.post("/other-stock-movements/create", params);
  return response.data;
};

/**
 * UTS iletimi yapar
 */
export const notifyUts = async (params: {
  ssFaturaDetayId: number;
  iletimTarihi: string;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/uts/transmit", params);
  return response.data;
};

// ============================================================================
// SİPARİŞ FONKSİYONLARI
// ============================================================================

/**
 * Satış sipariş listesini getirir
 */
export const listSalesOrders = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/sales-orders/list", params || {});
  return response.data;
};

/**
 * Satış siparişi oluşturur
 */
export const createSalesOrder = async (
  request: LucaCreateSalesOrderRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/sales-orders/create", request);
  return response.data;
};

/**
 * Satış siparişi siler
 */
export const deleteSalesOrder = async (params: {
  ssSiparisBaslikId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/sales-orders/delete", params);
  return response.data;
};

/**
 * Satınalma sipariş listesini getirir
 */
export const listPurchaseOrders = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/purchase-orders/list", params || {});
  return response.data;
};

/**
 * Satınalma siparişi oluşturur
 */
export const createPurchaseOrder = async (
  request: LucaCreatePurchaseOrderRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/purchase-orders/create", request);
  return response.data;
};

/**
 * Satınalma siparişi siler
 */
export const deletePurchaseOrder = async (params: {
  ssSiparisBaslikId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/purchase-orders/delete", params);
  return response.data;
};

// ============================================================================
// FATURA FONKSİYONLARI
// ============================================================================

/**
 * Fatura listesini getirir
 */
export const listInvoices = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/list", params || {});
  return response.data;
};

/**
 * Fatura oluşturur
 */
export const createInvoice = async (
  request: LucaCreateInvoiceRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/create", request);
  return response.data;
};

/**
 * Fatura PDF linkini getirir
 */
export const getInvoicePdfLink = async (params: {
  ssFaturaBaslikId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/pdf-link", params);
  return response.data;
};

/**
 * Fatura kapatır
 */
export const closeInvoice = async (params: {
  belgeTurDetayId: number;
  faturaId: number;
  belgeSeri: string;
  belgeTarih: string;
  vadeTarih: string;
  tutar: number;
  cariKod: string;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/close", params);
  return response.data;
};

/**
 * Fatura siler
 */
export const deleteInvoice = async (params: {
  ssFaturaBaslikId: number;
}): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/delete", params);
  return response.data;
};

/**
 * Dövizli fatura listesini getirir
 */
export const listCurrencyInvoices = async (params?: any): Promise<any> => {
  const response = await lucaProxyClient.post("/invoices/currency", params || {});
  return response.data;
};

// ============================================================================
// FİNANS FONKSİYONLARI
// ============================================================================

/**
 * Kredi kartı girişi oluşturur
 */
export const createCreditCardEntry = async (
  request: LucaCreateCreditCardEntryRequest
): Promise<any> => {
  const response = await lucaProxyClient.post("/finance/credit-card-entry/create", request);
  return response.data;
};

/**
 * Banka kartları listesini getirir
 */
export const listBanks = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/finance/banks/list", {});
  return response.data;
};

/**
 * Kasa kartları listesini getirir
 */
export const listCashAccounts = async (): Promise<any> => {
  const response = await lucaProxyClient.post("/finance/cash/list", {});
  return response.data;
};

// ============================================================================
// RAPOR FONKSİYONLARI
// ============================================================================

/**
 * Stok-Hizmet Ekstre Raporu oluşturur
 */
export const generateStockServiceReport = async (params?: any): Promise<Blob> => {
  const response = await lucaProxyClient.post("/reports/stock-service", params || {}, {
    responseType: "blob",
  });
  return response.data as Blob;
};

// ============================================================================
// EXPORT
// ============================================================================

const lucaService = {
  // Giriş
  login,
  getBranches,
  selectBranch,

  // Genel
  listMeasurementUnits,
  listTaxOffices,
  listDocumentTypeDetails,
  listDocumentSeries,
  listBranchCurrencies,
  getDocumentSeriesMax,
  listDynamicLovValues,
  updateDynamicLovValue,
  createDynamicLovValue,

  // Cari
  listCustomers,
  listSuppliers,
  createCustomer,
  createSupplier,
  listCustomerAddresses,
  getCustomerWorkingConditions,
  listCustomerAuthorizedPersons,
  getCustomerRisk,
  createCustomerTransaction,
  listCustomerTransactions,
  listSpecialCustomerTransactions,
  createCustomerContract,

  // Stok
  listStockCards,
  createStockCard,
  listStockCategories,
  listStockCardAltUnits,
  listStockCardAltStocks,
  listStockCardPurchasePrices,
  listStockCardSalesPrices,
  listStockCardCosts,
  listStockCardPurchaseTerms,
  listStockCardSuppliers,
  autocompleteStockCards,
  listWarehouses,
  getWarehouseStockQuantity,
  listDeliveryNotes,
  createDeliveryNote,
  deleteDeliveryNote,
  getEirsaliyeXml,
  createWarehouseTransfer,
  createStockCount,
  createDshHeader,
  notifyUts,

  // Sipariş
  listSalesOrders,
  createSalesOrder,
  deleteSalesOrder,
  listPurchaseOrders,
  createPurchaseOrder,
  deletePurchaseOrder,

  // Fatura
  listInvoices,
  createInvoice,
  getInvoicePdfLink,
  closeInvoice,
  deleteInvoice,
  listCurrencyInvoices,

  // Finans
  createCreditCardEntry,
  listBanks,
  listCashAccounts,

  // Rapor
  generateStockServiceReport,
};

export default lucaService;
