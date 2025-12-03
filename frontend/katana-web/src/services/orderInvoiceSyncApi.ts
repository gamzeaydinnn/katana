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
const BASE_URL = "/api/OrderInvoiceSync";

/**
 * Tüm siparişleri ve senkronizasyon durumlarını getirir
 */
export const getOrders = async (
  status?: string,
  page: number = 1,
  pageSize: number = 50
): Promise<PaginatedResponse<OrderListItem>> => {
  const params = new URLSearchParams();
  if (status) params.append("status", status);
  params.append("page", page.toString());
  params.append("pageSize", pageSize.toString());

  const response = await api.get<PaginatedResponse<OrderListItem>>(
    `${BASE_URL}/orders?${params.toString()}`
  );
  return response.data;
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
  const response = await api.post<SyncResult>(`${BASE_URL}/sync/${orderId}`);
  return response.data;
};

/**
 * Birden fazla siparişi toplu olarak Luca'ya gönderir
 */
export const syncBatch = async (
  orderIds: number[]
): Promise<BatchSyncResult> => {
  const response = await api.post<BatchSyncResult>(`${BASE_URL}/sync/batch`, {
    orderIds,
  });
  return response.data;
};

/**
 * Tüm bekleyen siparişleri Luca'ya gönderir
 */
export const syncAllPending = async (): Promise<BatchSyncResult> => {
  const response = await api.post<BatchSyncResult>(
    `${BASE_URL}/sync/all-pending`
  );
  return response.data;
};

/**
 * Faturayı kapatır (ödeme işlemi)
 */
export const closeInvoice = async (
  orderId: number,
  amount: number
): Promise<SyncResult> => {
  const response = await api.post<SyncResult>(`${BASE_URL}/close/${orderId}`, {
    amount,
  });
  return response.data;
};

/**
 * Faturayı siler
 */
export const deleteInvoice = async (orderId: number): Promise<SyncResult> => {
  const response = await api.delete<SyncResult>(
    `${BASE_URL}/invoice/${orderId}`
  );
  return response.data;
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
