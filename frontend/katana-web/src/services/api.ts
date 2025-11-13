import axios from "axios";
import { showGlobalToast } from "../providers/FeedbackProvider";
import {
  decodeJwtPayload,
  isJwtExpired,
  isJwtTokenExpired,
} from "../utils/jwt";

// Development: prefer an explicit env var REACT_APP_API_URL. If not set, choose a smart default.
// In production when the static site is served on :3000 and backend runs on :5055, we want
// runtime to point requests to :5055 even if build-time env is missing.
const getDefaultApiBase = () => {
  try {
    if (typeof window !== "undefined") {
      const { protocol, hostname, port } = window.location;
      // Heuristic: if UI is on port 3000 (static serve), backend is on 5055
      if (port === "3000") {
        return `${protocol}//${hostname}:5055/api`;
      }
    }
  } catch {
    // ignore
  }
  // Fallback to relative '/api' for CRA dev proxy or same-origin deployments
  return "/api";
};

const API_BASE_URL = process.env.REACT_APP_API_URL || getDefaultApiBase();

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor: token'ı her istekte kontrol et
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
        // Authentication problem / token expired
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
        } catch {
          // ignore toast errors
        }

        if (!token || tokenExpiredOrInvalid) {
          try {
            if (typeof window !== "undefined") {
              window.localStorage.removeItem("authToken");
              window.location.href = "/login";
            }
          } catch {
            // ignore
          }
        }
      } else if (status === 403) {
        // Forbidden - user is authenticated but lacks permission
        try {
          showGlobalToast({
            message: "Bu işlem için yetkiniz yok.",
            severity: "warning",
            durationMs: 3500,
          });
        } catch {
          // ignore
        }
      } else if (status === 400) {
        // Bad request - try to display helpful validation message
        try {
          const data = error?.response?.data;
          if (data) {
            // ASP.NET ValidationProblemDetails shape: { title, status, errors: { field: [..] }}
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
        } catch {
          // ignore toast errors
        }
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

// Users
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
  role: string; // e.g. "Staff" | "Manager" | "Admin"
  email?: string;
}

export interface UpdateUserRequest {
  username: string;
  password?: string;
  role: string;
  isActive: boolean;
  email?: string;
}

export const stockAPI = {
  // Dashboard - GET /api/Dashboard
  getDashboardStats: () => api.get("/Dashboard").then((res) => res.data),

  // Katana Products - GET /api/adminpanel/products
  getKatanaProducts: () =>
    api.get("/adminpanel/products?page=1&pageSize=100").then((res) => res.data),

  // Stock Status - GET /api/Stock/status
  getStockStatus: () => api.get("/Stock/status").then((res) => res.data),

  // Local Stock Summary - GET /api/Stock/local/summary
  getLocalStockSummary: () =>
    api.get("/Stock/local/summary").then((res) => res.data),

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
  getStockReport: (queryString?: string) =>
    api
      .get(`/Reports/stock${queryString ? `?${queryString}` : ""}`)
      .then((res) => res.data),

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

export default api;
