import axios from "axios";
import { showGlobalToast } from "../providers/FeedbackProvider";
import {
    decodeJwtPayload,
    isJwtExpired,
    isJwtTokenExpired,
} from "../utils/jwt";

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
  baseURL: API_BASE_URL,
  // Increase default timeout to accommodate long-running sync operations
  // (sync can take >30s when creating many stock cards one-by-one).
  timeout: 120000,
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use((config) => {
  const token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;

  if (!config.headers) config.headers = {} as any;

  if (token && typeof token === "string") {
    const payload = decodeJwtPayload(token);
    if (payload && !isJwtExpired(payload)) {
      (config.headers as any).Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    try {
      const status = error?.response?.status;
      const path =
        typeof window !== "undefined" ? window.location.pathname : "";
      const token =
        typeof window !== "undefined"
          ? window.localStorage.getItem("authToken")
          : null;
      const tokenExpiredOrInvalid = isJwtTokenExpired(token);

      if (status === 401) {
        try {
          if (path.startsWith("/admin")) {
            showGlobalToast({
              message:
                "Oturum süresi doldu veya kimlik doğrulama başarısız. Lütfen giriş yapın.",
              severity: "warning",
              durationMs: 3500,
            });
          } else {
            showGlobalToast({
              message: "Oturum süresi doldu. Lütfen tekrar giriş yapın.",
              severity: "warning",
              durationMs: 3000,
            });
          }
        } catch {}

        if (!token || tokenExpiredOrInvalid) {
          try {
            if (typeof window !== "undefined") {
              window.localStorage.removeItem("authToken");
              window.location.href = "/login";
            }
          } catch {}
        }
      } else if (status === 403) {
        try {
          showGlobalToast({
            message: "Bu işlem için yetkiniz yok.",
            severity: "warning",
            durationMs: 3500,
          });
        } catch {}
      } else if (status === 400) {
        try {
          const data = error?.response?.data;
          if (data) {
            if (data.errors && typeof data.errors === "object") {
              const firstKey = Object.keys(data.errors)[0];
              const firstMsg = firstKey
                ? (data.errors[firstKey] || [])[0]
                : null;
              showGlobalToast({
                message: firstMsg || data.title || "İstem hatası (400).",
                severity: "error",
                durationMs: 5000,
              });
            } else if (typeof data === "string") {
              showGlobalToast({
                message: data,
                severity: "error",
                durationMs: 5000,
              });
            } else if (data.error) {
              showGlobalToast({
                message: data.error.toString(),
                severity: "error",
                durationMs: 5000,
              });
            } else if (data.title) {
              showGlobalToast({
                message: data.title,
                severity: "error",
                durationMs: 5000,
              });
            } else {
              showGlobalToast({
                message: "İstem hatası (400).",
                severity: "error",
                durationMs: 4000,
              });
            }
          }
        } catch {}
      }
    } catch (handlerError) {
      console.error("[Axios] Response interceptor error:", handlerError);
    }
    return Promise.reject(error);
  }
);

export interface StockItem {
  id: string;
  name: string;
  sku: string;
  quantity: number;
  unit: string;
  minStock?: number;
  maxStock?: number;
  status: "Normal" | "Low" | "High" | "Out";
  lastUpdated: string;
  category?: string;
}

export interface Product {
  id: string;
  name: string;
  sku: string;
  stockQuantity: number;
  unit: string;
  minStockLevel?: number;
  maxStockLevel?: number;
  category?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  movementType: string;
  quantity: number;
  date: string;
  notes?: string;
}

export interface DashboardStats {
  totalProducts: number;
  totalStock: number;
  pendingSync: number;
  lastSyncDate: string;
}

export interface SyncLog {
  id: number;
  syncType: string;
  status: string;
  startTime: string;
  endTime?: string;
  processedRecords: number;
  successfulRecords: number;
  failedRecords: number;
  errorMessage?: string;
}

export interface HealthStatus {
  status: string;
  timestamp: string;
  version?: string;
}

export interface AdminStatistics {
  totalProducts: number;
  totalStock: number;
  successfulSyncs: number;
  failedSyncs: number;
}

export interface AdminProduct {
  id: string;
  sku: string;
  name: string;
  stock: number;
  isActive: boolean;
  createdAt: string;
}

export interface AdminSyncLog {
  id: number;
  integrationName: string;
  createdAt: string;
  isSuccess: boolean;
  errorMessage?: string;
}

export interface UserDto {
  id: number;
  username: string;
  role: string;
  email: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  role: string;
  email?: string;
}

export interface UpdateUserRequest {
  username: string;
  password?: string;
  role: string;
  isActive: boolean;
  email?: string;
}

export interface SyncOptions {
  syncType?: string;
  dryRun?: boolean;
  preferBarcodeMatch?: boolean;
  forceSendDuplicates?: boolean;
  limit?: number;
}

export interface SyncResult {
  syncType?: string;
  processedRecords?: number;
  successfulRecords?: number;
  failedRecords?: number;
  totalChecked?: number;
  alreadyExists?: number;
  newCreated?: number;
  failed?: number;
  message?: string;
  errors?: string[];
  details?: string[];
  duplicateRecords?: number;
  sentRecords?: number;
  isDryRun?: boolean;
}

export const stockAPI = {
  getDashboardStats: () => api.get("/Dashboard").then((res) => res.data),

  getKatanaProducts: () => api.get("/Products/katana").then((res) => res.data),

  getLucaStockCards: () =>
    api.get("/Luca/koza-stock-cards").then((res) => res.data),

  getComparison: () => api.get("/Sync/comparison").then((res) => res.data),

  getStockStatus: () => api.get("/Stock/status").then((res) => res.data),

  getLocalStockSummary: () =>
    api.get("/Stock/local/summary").then((res) => res.data),

  getStockMovements: (fromDate?: string, toDate?: string) => {
    const params = new URLSearchParams();
    if (fromDate) params.append("fromDate", fromDate);
    if (toDate) params.append("toDate", toDate);
    return api
      .get(`/Stock/movements?${params.toString()}`)
      .then((res) => res.data);
  },

  getSyncHistory: () => api.get("/Sync/history").then((res) => res.data),

  startSync: (options?: SyncOptions) => {
    // Route sync request based on syncType
    const syncType = options?.syncType?.toUpperCase() || "STOCK_CARD";
    
    const endpointMap: Record<string, string> = {
      STOCK: "/Sync/stock",
      INVOICE: "/Sync/invoices",
      CUSTOMER: "/Sync/customers",
      DESPATCH: "/Sync/from-luca/despatch",
      ALL: "/Sync/run",
      STOCK_CARD: "/Sync/to-luca/stock-cards",
      PRODUCT: "/Sync/to-luca/stock-cards",
    };
    
    const endpoint = endpointMap[syncType] || "/Sync/to-luca/stock-cards";
    
    return api
      .post(endpoint, options || {}, { timeout: 120000 })
      .then((res) => res.data as SyncResult);
  },

  getStockReport: (queryString?: string) =>
    api
      .get(`/Reports/stock${queryString ? `?${queryString}` : ""}`)
      .then((res) => res.data),

  getSyncReport: () => api.get("/Reports/sync").then((res) => res.data),

  getHealthStatus: () => api.get("/Health").then((res) => res.data),
};

export const pendingAdjustmentsAPI = {
  list: () =>
    api.get("/adminpanel/pending-adjustments").then((res) => res.data),
  approve: (id: number, approvedBy = "admin") =>
    api
      .post(
        `/adminpanel/pending-adjustments/${id}/approve?approvedBy=${encodeURIComponent(
          approvedBy
        )}`
      )
      .then((res) => res.data),
  reject: (id: number, rejectedBy = "admin", reason?: string) =>
    api
      .post(`/adminpanel/pending-adjustments/${id}/reject`, {
        rejectedBy,
        reason,
      })
      .then((res) => res.data),
};

export const notificationsAPI = {
  list: (unread?: boolean) =>
    api
      .get(
        `/notifications${
          typeof unread === "boolean" ? `?unread=${unread}` : ""
        }`
      )
      .then((res) => res.data),
  get: (id: number) => api.get(`/notifications/${id}`).then((res) => res.data),
  markRead: (id: number) =>
    api.post(`/notifications/${id}/mark-read`).then((res) => res.data),
  delete: (id: number) =>
    api.delete(`/notifications/${id}`).then((res) => res.data),
};

export const authAPI = {
  login: (username: string, password: string) =>
    api.post("/Auth/login", { username, password }).then((res) => {
      const data = (res && (res as any).data) || {};
      return {
        ...data,
        token: (data.token ?? data.Token) || null,
      } as any;
    }),
};

export const usersAPI = {
  list: () => api.get<UserDto[]>("/Users").then((res) => res.data),
  create: (payload: CreateUserRequest) =>
    api.post<UserDto>("/Users", payload).then((res) => res.data),
  update: (id: number, payload: UpdateUserRequest) =>
    api.put<UserDto>(`/Users/${id}`, payload).then((res) => res.data),
  updateRole: (id: number, role: string) =>
    api
      .put<void>(`/Users/${id}/role`, role, {
        headers: { "Content-Type": "application/json" },
      })
      .then((res) => res.data),
  delete: (id: number) =>
    api.delete<void>(`/Users/${id}`).then((res) => res.data),
};

/**
 * Koza Entegrasyon API'leri
 * Backend üzerinden Koza API'ye güvenli erişim sağlar
 * GÜVENLIK: Frontend asla direkt Koza'ya bağlanmaz, backend proxy kullanır
 */
export const kozaAPI = {
  // Depo Kartı İşlemleri
  depots: {
    list: () => api.get("/admin/koza/depots").then((res) => res.data),
    create: (payload: any) =>
      api.post("/admin/koza/depots/create", payload).then((res) => res.data),
  },

  // Stok Kartı İşlemleri
  stockCards: {
    list: () => api.get("/admin/koza/stocks").then((res) => res.data),
    create: (payload: any) =>
      api.post("/admin/koza/stocks/create", payload).then((res) => res.data),
  },

  // Legacy endpoint - geriye dönük uyumluluk için
  getLucaStockCards: () =>
    api.get("/Luca/koza-stock-cards").then((res) => res.data),
};

export default api;
