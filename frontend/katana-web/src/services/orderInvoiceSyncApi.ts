import api from "./api";

// Types
export interface OrderListItem {
  id: number;
  orderNo: string;
  customer: string;
  customerId: number;
  date: string;
  total: number;
  currency: string;
  status: "SYNCED" | "PENDING" | "ERROR" | "CANCELLED";
  orderStatus: string;
  lucaId?: number;
  errorMessage?: string;
  itemCount: number;
}

export interface OrderDetail {
  id: number;
  orderNo: string;
  customer: {
    id: number;
    name: string;
    taxNo: string;
    email?: string;
  };
  date: string;
  total: number;
  currency: string;
  status: string;
  isSynced: boolean;
  items: {
    productId: number;
    productName: string;
    sku: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
  }[];
}

export interface SyncResult {
  success: boolean;
  lucaId?: number;
  message: string;
}

export interface BatchSyncResult {
  success: boolean;
  message: string;
  totalCount: number;
  successCount: number;
  failCount: number;
  failedOrderIds: number[];
}

export interface DashboardStats {
  totalOrders: number;
  syncedOrders: number;
  pendingOrders: number;
  cancelledOrders: number;
  todayOrders: number;
  weekOrders: number;
  syncPercentage: number;
}

export interface PaginatedResponse<T> {
  success: boolean;
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

// API Functions
const BASE_URL = "/OrderInvoiceSync";

/**
 * Tüm siparişleri ve senkronizasyon durumlarını getirir
 */
export const getOrders = async (
  status?: string,
  page: number = 1,
  pageSize: number = 50
): Promise<PaginatedResponse<OrderListItem>> => {
  console.log('[OrderInvoiceSync] getOrders başladı:', { status, page, pageSize, timestamp: new Date().toISOString() });
  try {
    const params = new URLSearchParams();
    if (status) params.append("status", status);
    params.append("page", page.toString());
    params.append("pageSize", pageSize.toString());

    const response = await api.get<PaginatedResponse<OrderListItem>>(
      `${BASE_URL}/orders?${params.toString()}`
    );
    console.log('[OrderInvoiceSync] getOrders başarılı:', {
      status,
      page,
      pageSize,
      resultCount: response.data.data.length,
      totalCount: response.data.pagination.totalCount,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] getOrders HATA:', {
      statusFilter: status,
      page,
      pageSize,
      error: error.message,
      response: error.response?.data,
      httpStatus: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Sipariş detaylarını getirir
 */
export const getOrderDetail = async (orderId: number): Promise<OrderDetail> => {
  const response = await api.get<{ success: boolean; data: OrderDetail }>(
    `${BASE_URL}/orders/${orderId}`
  );
  return response.data.data;
};

/**
 * Tek bir siparişi Luca'ya gönderir
 */
export const syncOrder = async (orderId: number): Promise<SyncResult> => {
  console.log('[OrderInvoiceSync] syncOrder başladı:', { orderId, timestamp: new Date().toISOString() });
  try {
    const response = await api.post<SyncResult>(`${BASE_URL}/sync/${orderId}`);
    console.log('[OrderInvoiceSync] syncOrder başarılı:', { orderId, response: response.data, timestamp: new Date().toISOString() });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] syncOrder HATA:', {
      orderId,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Birden fazla siparişi toplu olarak Luca'ya gönderir
 */
export const syncBatch = async (
  orderIds: number[]
): Promise<BatchSyncResult> => {
  console.log('[OrderInvoiceSync] syncBatch başladı:', { orderIds, count: orderIds.length, timestamp: new Date().toISOString() });
  try {
    const response = await api.post<BatchSyncResult>(`${BASE_URL}/sync/batch`, {
      orderIds,
    });
    console.log('[OrderInvoiceSync] syncBatch tamamlandı:', {
      orderIds,
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] syncBatch HATA:', {
      orderIds,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Tüm bekleyen siparişleri Luca'ya gönderir
 */
export const syncAllPending = async (): Promise<BatchSyncResult> => {
  console.log('[OrderInvoiceSync] syncAllPending başladı:', { timestamp: new Date().toISOString() });
  try {
    const response = await api.post<BatchSyncResult>(
      `${BASE_URL}/sync/all-pending`
    );
    console.log('[OrderInvoiceSync] syncAllPending tamamlandı:', {
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] syncAllPending HATA:', {
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Faturayı kapatır (ödeme işlemi)
 */
export const closeInvoice = async (
  orderId: number,
  amount: number
): Promise<SyncResult> => {
  console.log('[OrderInvoiceSync] closeInvoice başladı:', { orderId, amount, timestamp: new Date().toISOString() });
  try {
    const response = await api.post<SyncResult>(`${BASE_URL}/close/${orderId}`, {
      amount,
    });
    console.log('[OrderInvoiceSync] closeInvoice başarılı:', {
      orderId,
      amount,
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] closeInvoice HATA:', {
      orderId,
      amount,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Faturayı siler
 */
export const deleteInvoice = async (orderId: number): Promise<SyncResult> => {
  console.log('[OrderInvoiceSync] deleteInvoice başladı:', { orderId, timestamp: new Date().toISOString() });
  try {
    const response = await api.delete<SyncResult>(
      `${BASE_URL}/invoice/${orderId}`
    );
    console.log('[OrderInvoiceSync] deleteInvoice başarılı:', {
      orderId,
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[OrderInvoiceSync] deleteInvoice HATA:', {
      orderId,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Dashboard istatistiklerini getirir
 */
export const getDashboardStats = async (): Promise<DashboardStats> => {
  const response = await api.get<{ success: boolean; data: DashboardStats }>(
    `${BASE_URL}/dashboard`
  );
  return response.data.data;
};
