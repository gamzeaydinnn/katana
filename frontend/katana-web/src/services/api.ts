import axios from "axios";
import { decodeJwtPayload, isJwtExpired } from "../utils/jwt";

// Development: prefer an explicit env var REACT_APP_API_URL. If not set, use a relative
// '/api' so the CRA dev server can proxy requests to the backend (see package.json proxy).
const API_BASE_URL = process.env.REACT_APP_API_URL || "/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// On module load: validate any stored auth token and remove it if malformed.
// This ensures we don't accidentally send non-JWT strings as Bearer tokens
// which were triggering IDX14100 logs on the backend.
try {
  if (typeof window !== "undefined") {
    const stored = window.localStorage.getItem("authToken");
    const payload = decodeJwtPayload(stored);
    if (!payload) {
      console.warn(
        "Stored authToken is malformed; removing from localStorage."
      );
      window.localStorage.removeItem("authToken");
    } else if (isJwtExpired(payload)) {
      console.warn("Stored authToken has expired; removing from localStorage.");
      window.localStorage.removeItem("authToken");
    }
  }
} catch {
  // localStorage may be unavailable in some environments; ignore failures
}

api.interceptors.request.use((config) => {
  const token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;
  // Ensure headers object exists to avoid runtime errors
  if (!config.headers) config.headers = {} as any;

  // Only attach Authorization if the token looks like a JWT (3 parts separated by dots)
  if (token && typeof token === "string") {
    const payload = decodeJwtPayload(token);
    if (payload && !isJwtExpired(payload)) {
      (config.headers as any).Authorization = `Bearer ${token}`;
    } else if (typeof window !== "undefined") {
      console.warn(
        "Auth token in storage is invalid or expired, skipping Authorization header."
      );
      window.localStorage.removeItem("authToken");
    }
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("authToken");
      window.location.href = "/login";
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

export const stockAPI = {
  // Dashboard - GET /api/Dashboard
  getDashboardStats: () => api.get("/Dashboard").then((res) => res.data),

  // Katana Products - GET /api/adminpanel/products
  getKatanaProducts: () =>
    api.get("/adminpanel/products?page=1&pageSize=100").then((res) => res.data),

  // Stock Status - GET /api/Stock
  getStockStatus: () => api.get("/Stock").then((res) => res.data),

  // Stock Movements - GET /api/Stock/movements
  getStockMovements: (fromDate?: string, toDate?: string) => {
    const params = new URLSearchParams();
    if (fromDate) params.append("fromDate", fromDate);
    if (toDate) params.append("toDate", toDate);
    return api
      .get(`/Stock/movements?${params.toString()}`)
      .then((res) => res.data);
  },

  // Sync - GET /api/Sync/history
  getSyncHistory: () => api.get("/Sync/history").then((res) => res.data),

  // Sync - POST /api/Sync/start
  startSync: (syncType: string) =>
    api.post("/Sync/start", { syncType }).then((res) => res.data),

  // Reports - GET /api/Reports/stock
  getStockReport: () => api.get("/Reports/stock").then((res) => res.data),

  // Reports - GET /api/Reports/sync
  getSyncReport: () => api.get("/Reports/sync").then((res) => res.data),

  // Health - GET /api/Health
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

export default api;
