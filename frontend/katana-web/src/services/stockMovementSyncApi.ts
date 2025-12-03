import axios from "axios";

const getDefaultApiBase = () => {
  try {
    if (typeof window !== "undefined") {
      const { protocol, hostname, port } = window.location;
      if (port === "3000" || port === "3001") {
        return `${protocol}//${hostname}:5055/api`;
      }
    }
  } catch {}
  return "/api";
};

const API_BASE_URL = process.env.REACT_APP_API_URL || getDefaultApiBase();

const api = axios.create({
  baseURL: `${API_BASE_URL}/StockMovementSync`,
  timeout: 120000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Token interceptor
api.interceptors.request.use((config) => {
  const token =
    localStorage.getItem("token") || localStorage.getItem("authToken");
  if (token) {
    config.headers = config.headers || {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Types
export interface StockMovementRowDto {
  id: number;
  productCode: string;
  productName: string;
  quantity: number;
  unitCost?: number;
}

export interface StockMovementSyncDto {
  id: number;
  documentNo: string;
  movementType: "TRANSFER" | "ADJUSTMENT";
  locationInfo: string;
  movementDate: string;
  totalQuantity: number;
  syncStatus: "PENDING" | "SYNCED" | "ERROR";
  lucaDocumentId?: number;
  errorMessage?: string;
  syncedAt?: string;
  adjustmentReason?: string;
  rows: StockMovementRowDto[];
}

export interface MovementSyncResultDto {
  success: boolean;
  movementId: number;
  movementType: string;
  lucaDocumentId?: number;
  errorMessage?: string;
  syncedAt: string;
}

export interface MovementBatchSyncResultDto {
  totalCount: number;
  successCount: number;
  failedCount: number;
  results: MovementSyncResultDto[];
}

export interface MovementDashboardStatsDto {
  totalTransfers: number;
  pendingTransfers: number;
  syncedTransfers: number;
  failedTransfers: number;
  totalAdjustments: number;
  pendingAdjustments: number;
  syncedAdjustments: number;
  failedAdjustments: number;
  lastSyncDate?: string;
}

export interface StockMovementFilterDto {
  movementType?: string;
  syncStatus?: string;
  startDate?: string;
  endDate?: string;
}

// API Functions

/**
 * Tüm stok hareketlerini listeler (Transfer + Adjustment)
 */
export const getAllMovements = async (
  filter?: StockMovementFilterDto
): Promise<StockMovementSyncDto[]> => {
  console.log('[StockMovementSync] getAllMovements başladı:', { filter, timestamp: new Date().toISOString() });
  try {
    const params = new URLSearchParams();
    if (filter?.movementType) params.append("movementType", filter.movementType);
    if (filter?.syncStatus) params.append("syncStatus", filter.syncStatus);
    if (filter?.startDate) params.append("startDate", filter.startDate);
    if (filter?.endDate) params.append("endDate", filter.endDate);

    const response = await api.get<StockMovementSyncDto[]>(
      `/movements?${params.toString()}`
    );
    console.log('[StockMovementSync] getAllMovements başarılı:', {
      filter,
      resultCount: response.data.length,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[StockMovementSync] getAllMovements HATA:', {
      filter,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Bekleyen transferleri listeler
 */
export const getPendingTransfers = async (): Promise<
  StockMovementSyncDto[]
> => {
  const response = await api.get<StockMovementSyncDto[]>("/transfers/pending");
  return response.data;
};

/**
 * Bekleyen adjustment'ları listeler
 */
export const getPendingAdjustments = async (): Promise<
  StockMovementSyncDto[]
> => {
  const response = await api.get<StockMovementSyncDto[]>(
    "/adjustments/pending"
  );
  return response.data;
};

/**
 * Tek bir transferi senkronize eder
 */
export const syncTransfer = async (
  transferId: number
): Promise<MovementSyncResultDto> => {
  const response = await api.post<MovementSyncResultDto>(
    `/sync/transfer/${transferId}`
  );
  return response.data;
};

/**
 * Tek bir adjustment'ı senkronize eder
 */
export const syncAdjustment = async (
  adjustmentId: number
): Promise<MovementSyncResultDto> => {
  const response = await api.post<MovementSyncResultDto>(
    `/sync/adjustment/${adjustmentId}`
  );
  return response.data;
};

/**
 * Genel senkronizasyon - type ve id ile
 */
export const syncMovement = async (
  type: string,
  id: number
): Promise<{ success: boolean; message: string; lucaId?: number }> => {
  console.log('[StockMovementSync] syncMovement başladı:', { type, id, timestamp: new Date().toISOString() });
  try {
    const response = await api.post<{
      success: boolean;
      message: string;
      lucaId?: number;
    }>(`/sync-movement/${type}/${id}`);
    console.log('[StockMovementSync] syncMovement başarılı:', {
      type,
      id,
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[StockMovementSync] syncMovement HATA:', {
      type,
      id,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Toplu senkronizasyon
 */
export const syncBatch = async (
  transferIds: number[],
  adjustmentIds: number[]
): Promise<MovementBatchSyncResultDto> => {
  console.log('[StockMovementSync] syncBatch başladı:', {
    transferIds,
    adjustmentIds,
    totalCount: transferIds.length + adjustmentIds.length,
    timestamp: new Date().toISOString()
  });
  try {
    const response = await api.post<MovementBatchSyncResultDto>("/sync/batch", {
      transferIds,
      adjustmentIds,
    });
    console.log('[StockMovementSync] syncBatch tamamlandı:', {
      transferIds,
      adjustmentIds,
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[StockMovementSync] syncBatch HATA:', {
      transferIds,
      adjustmentIds,
      error: error.message,
      response: error.response?.data,
      status: error.response?.status,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

/**
 * Bekleyen tüm hareketleri senkronize eder
 */
export const syncAllPending = async (): Promise<MovementBatchSyncResultDto> => {
  console.log('[StockMovementSync] syncAllPending başladı:', { timestamp: new Date().toISOString() });
  try {
    const response = await api.post<MovementBatchSyncResultDto>(
      "/sync/all-pending"
    );
    console.log('[StockMovementSync] syncAllPending tamamlandı:', {
      result: response.data,
      timestamp: new Date().toISOString()
    });
    return response.data;
  } catch (error: any) {
    console.error('[StockMovementSync] syncAllPending HATA:', {
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
export const getDashboardStats =
  async (): Promise<MovementDashboardStatsDto> => {
    console.log('[StockMovementSync] getDashboardStats başladı:', { timestamp: new Date().toISOString() });
    try {
      const response = await api.get<MovementDashboardStatsDto>("/dashboard");
      console.log('[StockMovementSync] getDashboardStats başarılı:', {
        stats: response.data,
        timestamp: new Date().toISOString()
      });
      return response.data;
    } catch (error: any) {
      console.error('[StockMovementSync] getDashboardStats HATA:', {
        error: error.message,
        response: error.response?.data,
        status: error.response?.status,
        timestamp: new Date().toISOString()
      });
      throw error;
    }
  };

export default {
  getAllMovements,
  getPendingTransfers,
  getPendingAdjustments,
  syncTransfer,
  syncAdjustment,
  syncMovement,
  syncBatch,
  syncAllPending,
  getDashboardStats,
};
