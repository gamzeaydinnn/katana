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
  const params = new URLSearchParams();
  if (filter?.movementType) params.append("movementType", filter.movementType);
  if (filter?.syncStatus) params.append("syncStatus", filter.syncStatus);
  if (filter?.startDate) params.append("startDate", filter.startDate);
  if (filter?.endDate) params.append("endDate", filter.endDate);

  const response = await api.get<StockMovementSyncDto[]>(
    `/movements?${params.toString()}`
  );
  return response.data;
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
  const response = await api.post<{
    success: boolean;
    message: string;
    lucaId?: number;
  }>(`/sync-movement/${type}/${id}`);
  return response.data;
};

/**
 * Toplu senkronizasyon
 */
export const syncBatch = async (
  transferIds: number[],
  adjustmentIds: number[]
): Promise<MovementBatchSyncResultDto> => {
  const response = await api.post<MovementBatchSyncResultDto>("/sync/batch", {
    transferIds,
    adjustmentIds,
  });
  return response.data;
};

/**
 * Bekleyen tüm hareketleri senkronize eder
 */
export const syncAllPending = async (): Promise<MovementBatchSyncResultDto> => {
  const response = await api.post<MovementBatchSyncResultDto>(
    "/sync/all-pending"
  );
  return response.data;
};

/**
 * Dashboard istatistiklerini getirir
 */
export const getDashboardStats =
  async (): Promise<MovementDashboardStatsDto> => {
    const response = await api.get<MovementDashboardStatsDto>("/dashboard");
    return response.data;
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
