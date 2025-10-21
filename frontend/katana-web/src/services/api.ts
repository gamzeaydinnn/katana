import axios from "axios";

// Geliştirmede CRA proxy'sini kullanmak için varsayılanı '/api' yapıyoruz.
// Üretimde veya farklı bir porta yönlendirmek için REACT_APP_API_URL ayarla.
const API_BASE_URL = process.env.REACT_APP_API_URL || "/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("authToken");
  // Only attach Authorization if the token looks like a JWT (3 parts separated by dots)
  if (token && typeof token === "string") {
    const parts = token.split(".");
    if (parts.length === 3) {
      config.headers.Authorization = `Bearer ${token}`;
    } else {
      // Malformed or non-JWT token found in storage — don't send it to backend
      console.warn(
        "Auth token in storage does not look like a JWT, skipping Authorization header."
      );
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

export const authAPI = {
  login: (username: string, password: string) =>
    api
      .post("/Auth/login", { username, password })
      .then((res) => ({
        ...res.data,
        token: res.data?.token ?? res.data?.Token,
      })),
};

export default api;
