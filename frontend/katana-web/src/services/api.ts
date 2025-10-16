import axios from "axios";

// Backend API URL - Katana.API projesi
const API_BASE_URL =
  process.env.REACT_APP_API_URL || "http://localhost:5000/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor for auth token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("authToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor for error handling
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
  // Dashboard
  getDashboardStats: (): Promise<DashboardStats> =>
    api.get("/dashboard/stats").then((res) => res.data),

  // Products from Katana API
  getProducts: (
    page?: number,
    limit?: number
  ): Promise<{ data: Product[]; count: number }> => {
    const params = new URLSearchParams();
    if (page) params.append("page", page.toString());
    if (limit) params.append("limit", limit.toString());
    return api.get(`/products?${params.toString()}`).then((res) => res.data);
  },

  getProductById: (id: string): Promise<Product> =>
    api.get(`/products/${id}`).then((res) => res.data),

  // Stock Status from Katana API
  getStockStatus: (
    page?: number,
    limit?: number
  ): Promise<{ data: StockItem[]; count: number }> => {
    const params = new URLSearchParams();
    if (page) params.append("page", page.toString());
    if (limit) params.append("limit", limit.toString());
    return api
      .get(`/stock/status?${params.toString()}`)
      .then((res) => res.data);
  },

  // Stock Movements from Katana API
  getStockMovements: (
    fromDate?: string,
    page?: number
  ): Promise<{ data: StockMovement[]; count: number }> => {
    const params = new URLSearchParams();
    if (fromDate) params.append("fromDate", fromDate);
    if (page) params.append("page", page.toString());
    return api
      .get(`/stock/movements?${params.toString()}`)
      .then((res) => res.data);
  },

  // Sync Management
  getSyncLogs: (
    page = 1,
    pageSize = 50
  ): Promise<{ logs: SyncLog[]; total: number }> =>
    api
      .get(`/sync/logs?page=${page}&pageSize=${pageSize}`)
      .then((res) => res.data),

  startSync: (
    syncType: string
  ): Promise<{ success: boolean; message: string }> =>
    api.post("/sync/start", { syncType }).then((res) => res.data),

  // Reports Controller - /api/Reports
  getStockReport: (): Promise<any[]> =>
    api.get("/Reports/stock").then((res) => res.data),

  getSyncReport: (): Promise<any[]> =>
    api.get("/Reports/sync").then((res) => res.data),

  // Health Controller - /api/Health
  getHealthStatus: (): Promise<HealthStatus> =>
    api.get("/Health").then((res) => res.data),

  // Admin Panel Controller - /api/AdminPanel
  getAdminStatistics: (): Promise<AdminStatistics> =>
    api.get("/adminpanel/statistics").then((res) => res.data),

  getAdminProducts: (
    page = 1,
    pageSize = 10
  ): Promise<{ products: AdminProduct[]; total: number }> =>
    api
      .get(`/adminpanel/products?page=${page}&pageSize=${pageSize}`)
      .then((res) => res.data),

  getAdminSyncLogs: (
    page = 1,
    pageSize = 10
  ): Promise<{ logs: AdminSyncLog[]; total: number }> =>
    api
      .get(`/adminpanel/sync-logs?page=${page}&pageSize=${pageSize}`)
      .then((res) => res.data),

  getKatanaHealth: (): Promise<{ isHealthy: boolean }> =>
    api.get("/adminpanel/katana-health").then((res) => res.data),
};

// Auth API - /api/Auth
export const authAPI = {
  login: (username: string, password: string): Promise<{ token: string }> =>
    api.post("/auth/login", { username, password }).then((res) => res.data),
};

export default api;
