import {
  CheckCircle,
  CompareArrows as CompareArrowsIcon,
  Error as ErrorIcon,
  Inventory,
  Article as LogsIcon,
  Receipt,
  Refresh,
  ReportProblem,
  Settings as SettingsIcon,
  ShoppingCart,
  TrendingUp,
  Group as UsersIcon,
  Warehouse,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Tabs,
  Typography,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../services/api";
import DataCorrectionPanel from "../Admin/DataCorrectionPanel";
import FailedRecords from "../Admin/FailedRecords";
import KatanaProducts from "../Admin/KatanaProducts";
import LucaProducts from "../Admin/LucaProducts";
import Orders from "../Admin/Orders";
import PendingAdjustments from "../Admin/PendingAdjustments";
import StockManagement from "../Admin/StockManagement";
import Settings from "../Settings/Settings";
import LogsViewer from "./LogsViewer";
import UsersManagement from "./UsersManagement";

interface Statistics {
  totalProducts: number;
  totalStock: number;
  successfulSyncs: number;
  failedSyncs: number;
}

interface AdminProduct {
  id: string;
  sku: string;
  name: string;
  stock: number;
  isActive: boolean;
}

interface AdminSyncLog {
  id: number;
  integrationName: string;
  createdAt: string;
  isSuccess: boolean;
}

const AdminPanel: React.FC = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState(0);
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [syncLogs, setSyncLogs] = useState<AdminSyncLog[]>([]);
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalSyncLogs, setTotalSyncLogs] = useState(0);
  // null = unknown / not loaded yet, true/false = explicit health state
  const [katanaHealth, setKatanaHealth] = useState<boolean | null>(null);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      // Backend'ten gerçek verileri çek
      const [statsRes, productsRes, logsRes, healthRes] = await Promise.all([
        api.get("/adminpanel/statistics"),
        api.get(
          `/adminpanel/products?page=${page + 1}&pageSize=${rowsPerPage}`
        ),
        api.get(
          `/adminpanel/sync-logs?page=${page + 1}&pageSize=${rowsPerPage}`
        ),
        api.get("/adminpanel/katana-health"),
      ]);

      // İstatistikler
      setStatistics(statsRes.data as Statistics);

      // Ürünler - normalize farklı response şekillerine toleranslı olarak
      const productsData = ((productsRes as any).data?.data ??
        (productsRes as any).data?.products ??
        (productsRes as any).data ??
        []) as any[];
      setProducts(
        (Array.isArray(productsData) ? productsData : []).map((p) => ({
          id: String(p.id ?? p.sku ?? p.SKU ?? ""),
          sku: String(p.sku ?? p.SKU ?? ""),
          name: String(p.name ?? p.Name ?? ""),
          stock: Number(p.stock ?? p.stockQuantity ?? p.quantity ?? 0),
          isActive: Boolean(p.isActive ?? p.IsActive ?? true),
          // createdAt is optional for the small table; provide a fallback
          createdAt: String(
            p.createdAt ?? p.createdAt ?? new Date().toISOString()
          ),
        }))
      );

      // Senkronizasyon logları - normalize ve boolean/string durumlarını destekle
      const rawLogs = ((logsRes as any).data?.data ??
        (logsRes as any).data?.logs ??
        (logsRes as any).data ??
        []) as any[];
      setSyncLogs(
        (Array.isArray(rawLogs) ? rawLogs : []).map((l) => {
          const createdAt = String(
            l.createdAt ?? l.endTime ?? l.startTime ?? new Date().toISOString()
          );
          let success = false;
          if (typeof l.isSuccess === "boolean") success = l.isSuccess;
          else if (typeof l.status === "boolean") success = l.status;
          else {
            const s = String(l.isSuccess ?? l.status ?? "").toUpperCase();
            success = s.includes("SUCCESS") || s === "OK" || s === "TRUE";
          }

          return {
            id: Number(l.id ?? 0),
            integrationName: String(l.integrationName ?? l.syncType ?? ""),
            createdAt,
            isSuccess: Boolean(success),
          } as AdminSyncLog;
        })
      );
      setTotalSyncLogs(
        Number(
          (logsRes as any).data?.total ??
            (Array.isArray(rawLogs) ? rawLogs.length : 0) ??
            0
        )
      );

      // Katana API health (harici Katana servisi)
      // If the endpoint returned no data, keep it as `null` so the UI can hide the chip
      if (healthRes && typeof (healthRes as any).data !== "undefined") {
        setKatanaHealth(Boolean((healthRes as any).data?.isHealthy ?? false));
      } else {
        setKatanaHealth(null);
      }
    } catch (err: any) {
      console.error("Failed to load admin data:", err);
      setError(
        `Hata: ${err.message || "Admin verileri yüklenirken hata oluştu"}`
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, rowsPerPage]);

  const StatCard: React.FC<{
    title: string;
    value: string | number;
    icon: React.ReactNode;
    color: string;
  }> = ({ title, value, icon, color }) => (
    <Card
      sx={{
        borderRadius: 2,
        boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
        transition: "transform 0.2s ease, box-shadow 0.2s ease",
        "&:hover": {
          transform: "translateY(-4px)",
          boxShadow: "0 4px 16px rgba(0,0,0,0.12)",
        },
      }}
    >
      <CardContent sx={{ p: 3 }}>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography
              color="textSecondary"
              gutterBottom
              variant="body2"
              sx={{
                fontWeight: 500,
                fontSize: "0.875rem",
                letterSpacing: "0.2px",
                mb: 1,
              }}
            >
              {title}
            </Typography>
            <Typography
              variant="h4"
              sx={{
                fontWeight: 700,
                fontFamily: '"Poppins", "Inter", sans-serif',
                letterSpacing: "-0.5px",
              }}
            >
              {value}
            </Typography>
          </Box>
          <Box
            sx={{
              color,
              fontSize: 48,
              opacity: 0.9,
              display: "flex",
              alignItems: "center",
            }}
          >
            {icon}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );

  if (loading && !statistics) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="400px"
      >
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={4}
        mt={2}
      >
        <Typography
          variant="h4"
          fontWeight={700}
          sx={{
            fontFamily: '"Poppins", "Inter", sans-serif',
            letterSpacing: "-0.5px",
            color: "text.primary",
          }}
        >
          Admin Paneli
        </Typography>
        <Box display="flex" gap={2} alignItems="center">
          <IconButton
            onClick={() => navigate("/admin/logs")}
            color="primary"
            title="System Logs"
          >
            <LogsIcon />
          </IconButton>
          {katanaHealth !== null && (
            <Chip
              icon={katanaHealth ? <CheckCircle /> : <ErrorIcon />}
              label={
                katanaHealth ? "Katana API Bağlı" : "Katana API Bağlı Değil"
              }
              color={katanaHealth ? "success" : "error"}
            />
          )}
          <IconButton onClick={loadData} color="primary">
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {/* Tabs */}
      <Paper sx={{ mb: 4, borderRadius: 2 }}>
        <Tabs
          value={activeTab}
          onChange={(_, v) => setActiveTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{
            "& .MuiTab-root": {
              display: "flex",
              alignItems: "center",
              flexDirection: "row",
              textTransform: "none",
              fontSize: "0.9375rem",
              fontWeight: 500,
              minWidth: "auto",
              minHeight: 56,
              whiteSpace: "nowrap",
              px: 2.5,
              py: 1.5,
              fontFamily: '"Inter", "Poppins", sans-serif',
              letterSpacing: "-0.2px",
            },
            "& .MuiTab-iconWrapper": {
              marginRight: 1,
              marginBottom: "0 !important",
              display: "flex",
              alignItems: "center",
            },
            "& .MuiTabs-indicator": {
              height: 3,
              borderRadius: "3px 3px 0 0",
            },
          }}
        >
          <Tab icon={<TrendingUp />} label="Genel Bakış" iconPosition="start" />
          <Tab icon={<Receipt />} label="Siparişler" iconPosition="start" />
          <Tab
            icon={<ShoppingCart />}
            label="Katana Ürünleri"
            iconPosition="start"
          />
          <Tab
            icon={<Inventory />}
            label="Luca Ürünleri"
            iconPosition="start"
          />
          <Tab
            icon={<Warehouse />}
            label="Stok Yönetimi"
            iconPosition="start"
          />
          <Tab
            icon={<ReportProblem />}
            label="Hatalı Kayıtlar"
            iconPosition="start"
          />
          <Tab
            icon={<CompareArrowsIcon />}
            label="Veri Düzeltme"
            iconPosition="start"
          />
          <Tab icon={<UsersIcon />} label="Kullanıcılar" iconPosition="start" />
          <Tab icon={<LogsIcon />} label="Loglar" iconPosition="start" />
          <Tab icon={<SettingsIcon />} label="Ayarlar" iconPosition="start" />
        </Tabs>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Tab 0: Genel Bakış */}
      {activeTab === 0 && (
        <Box>
          {/* Pending adjustments - put high so admin can approve quickly */}
          <Box sx={{ mb: 4 }}>
            <PendingAdjustments />
          </Box>

          {/* Statistics Cards */}
          {statistics && (
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: {
                  xs: "1fr",
                  sm: "repeat(2, 1fr)",
                  md: "repeat(4, 1fr)",
                },
                gap: 3,
                mb: 4,
              }}
            >
              <StatCard
                title="Toplam Ürün"
                value={statistics.totalProducts}
                icon={<Inventory />}
                color="#1976d2"
              />
              <StatCard
                title="Toplam Stok"
                value={statistics.totalStock.toLocaleString("tr-TR")}
                icon={<TrendingUp />}
                color="#2e7d32"
              />
              <StatCard
                title="Başarılı Sync"
                value={statistics.successfulSyncs}
                icon={<CheckCircle />}
                color="#388e3c"
              />
              <StatCard
                title="Başarısız Sync"
                value={statistics.failedSyncs}
                icon={<ErrorIcon />}
                color="#d32f2f"
              />
            </Box>
          )}

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "repeat(2, 1fr)" },
              gap: 3,
            }}
          >
            {/* Recent Products */}
            <Paper sx={{ p: 3, borderRadius: 2 }}>
              <Typography
                variant="h6"
                gutterBottom
                sx={{
                  fontWeight: 600,
                  fontFamily: '"Poppins", "Inter", sans-serif',
                  letterSpacing: "-0.3px",
                  mb: 2,
                }}
              >
                Son Eklenen Ürünler
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>SKU</TableCell>
                      <TableCell>Ürün Adı</TableCell>
                      <TableCell align="right">Stok</TableCell>
                      <TableCell>Durum</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {products.map((product, idx) => (
                      <TableRow
                        key={
                          product.id && product.id !== ""
                            ? product.id
                            : product.sku && product.sku !== ""
                            ? product.sku
                            : `product-${idx}`
                        }
                      >
                        <TableCell>{product.sku}</TableCell>
                        <TableCell>{product.name}</TableCell>
                        <TableCell align="right">{product.stock}</TableCell>
                        <TableCell>
                          <Chip
                            label={product.isActive ? "Aktif" : "Pasif"}
                            color={product.isActive ? "success" : "default"}
                            size="small"
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>

            {/* Sync Logs */}
            <Paper sx={{ p: 3, borderRadius: 2 }}>
              <Typography
                variant="h6"
                gutterBottom
                sx={{
                  fontWeight: 600,
                  fontFamily: '"Poppins", "Inter", sans-serif',
                  letterSpacing: "-0.3px",
                  mb: 2,
                }}
              >
                Son Sync Logları
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Entegrasyon</TableCell>
                      <TableCell>Tarih</TableCell>
                      <TableCell>Durum</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {syncLogs.map((log, idx) => (
                      <TableRow
                        key={
                          log.id && String(log.id) !== "0"
                            ? log.id
                            : `log-${idx}`
                        }
                      >
                        <TableCell>{log.integrationName}</TableCell>
                        <TableCell>
                          {new Date(log.createdAt).toLocaleString("tr-TR")}
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={log.isSuccess ? "Başarılı" : "Başarısız"}
                            color={log.isSuccess ? "success" : "error"}
                            size="small"
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              <TablePagination
                component="div"
                count={totalSyncLogs}
                page={page}
                onPageChange={(_, newPage) => setPage(newPage)}
                rowsPerPage={rowsPerPage}
                onRowsPerPageChange={(e) => {
                  setRowsPerPage(parseInt(e.target.value, 10));
                  setPage(0);
                }}
                rowsPerPageOptions={[5, 10, 25]}
                labelRowsPerPage="Sayfa başına:"
              />
            </Paper>
          </Box>
        </Box>
      )}

      {/* Tab 1: Siparişler */}
      {activeTab === 1 && <Orders />}

      {/* Tab 2: Katana Ürünleri */}
      {activeTab === 2 && <KatanaProducts />}

      {/* Tab 3: Luca Ürünleri */}
      {activeTab === 3 && <LucaProducts />}

      {/* Tab 4: Stok Yönetimi */}
      {activeTab === 4 && <StockManagement />}

      {/* Tab 5: Hatalı Kayıtlar */}
      {activeTab === 5 && <FailedRecords />}

      {/* Tab 6: Veri Düzeltme */}
      {activeTab === 6 && <DataCorrectionPanel />}

  {/* Tab 7: Kullanıcılar */}
  {activeTab === 7 && <UsersManagement />}

  {/* Tab 8: Loglar */}
  {activeTab === 8 && <LogsViewer />}

  {/* Tab 9: Ayarlar */}
  {activeTab === 9 && <Settings />}
    </Box>
  );
};

export default AdminPanel;
