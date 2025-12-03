import {
    CheckCircle,
    CompareArrows as CompareArrowsIcon,
    Error as ErrorIcon,
    Inventory,
    IntegrationInstructions as KozaIcon,
    Article as LogsIcon,
    MoreVert as MoreVertIcon,
    Receipt,
    Refresh,
    ReportProblem,
    Settings as SettingsIcon,
    ShoppingCart,
    SwapHoriz as SwapHorizIcon,
    TrendingUp,
    Group as UsersIcon,
    Warehouse
} from "@mui/icons-material";
import {
    Alert,
    Box,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Container,
    Divider,
    IconButton,
    Menu,
    MenuItem,
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
    useMediaQuery,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api from "../../services/api";
import DataCorrectionPanel from "../Admin/DataCorrectionPanel";
import FailedRecords from "../Admin/FailedRecords";
import KatanaProducts from "../Admin/KatanaProducts";
import KozaIntegration from "../Admin/KozaIntegration";
import LucaProducts from "../Admin/LucaProducts";
import OrderIntegrationPage from "../Admin/OrderIntegrationPage";
import Orders from "../Admin/Orders";
import PendingAdjustments from "../Admin/PendingAdjustments";
import StockManagement from "../Admin/StockManagement";
import StockMovements from "../Admin/StockMovements";
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
  const [activeTab, setActiveTab] = useState(0);
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [syncLogs, setSyncLogs] = useState<AdminSyncLog[]>([]);
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalSyncLogs, setTotalSyncLogs] = useState(0);

  const [katanaHealth, setKatanaHealth] = useState<boolean | null>(null);
  const [moreMenuAnchor, setMoreMenuAnchor] = useState<null | HTMLElement>(
    null
  );
  const isMobile = useMediaQuery("(max-width:900px)");
  const moreTabIndex = isMobile ? 2 : 5;
  const visibleTabThreshold = moreTabIndex - 1;
  const overflowTabActive = activeTab > visibleTabThreshold;
  const tabBarValue = overflowTabActive ? moreTabIndex : activeTab;

  const tabLabel = (text: string) => (
    <Box component="span" translate="no" sx={{ display: "inline-flex" }}>
      {text}
    </Box>
  );

  const overflowMenuItemSx = {
    py: 1.2,
    px: 2,
    gap: 1.2,
    alignItems: "center",
    borderRadius: 1.5,
    "&:hover": { backgroundColor: "rgba(102, 126, 234, 0.12)" },
  };

  const overflowMenuTextSx = {
    fontWeight: 600,
    letterSpacing: "0.01em",
    color: "text.primary",
  };

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

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

      setStatistics(statsRes.data as Statistics);

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

          createdAt: String(
            p.createdAt ?? p.createdAt ?? new Date().toISOString()
          ),
        }))
      );

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
    <Container
      maxWidth="xl"
      disableGutters
      sx={{
        py: { xs: 2, md: 4 },
        px: { xs: 1.5, sm: 2, md: 3 },
      }}
    >
      {}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", md: "center" },
          flexDirection: { xs: "column", md: "row" },
          gap: { xs: 1.5, md: 2 },
          mb: { xs: 3, md: 4 },
          mt: { xs: 1, md: 2 },
        }}
      >
        <Typography
          variant="h4"
          fontWeight={700}
          translate="no"
          sx={{
            fontFamily: '"Poppins", "Inter", sans-serif',
            letterSpacing: "-0.5px",
            color: "text.primary",
            background: (theme) =>
              `linear-gradient(120deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
            display: "inline-block",
            flexShrink: 0,
          }}
        >
          Admin Paneli
        </Typography>
        <Box
          display="flex"
          gap={1}
          alignItems="center"
          sx={{ flexShrink: 0, width: { xs: "100%", md: "auto" } }}
        >
          <IconButton
            onClick={() => setActiveTab(9)}
            color="primary"
            title="Logları Aç"
            sx={{
              bgcolor: (theme) =>
                theme.palette.mode === "dark"
                  ? "rgba(129, 140, 248, 0.16)"
                  : "rgba(129, 140, 248, 0.14)",
              color: (theme) => theme.palette.primary.main,
              boxShadow: (theme) =>
                theme.palette.mode === "dark"
                  ? "0 0 0 1px rgba(129,140,248,0.4)"
                  : "0 0 0 1px rgba(129,140,248,0.3)",
              "&:hover": {
                bgcolor: (theme) =>
                  theme.palette.mode === "dark"
                    ? "rgba(129, 140, 248, 0.28)"
                    : "rgba(129, 140, 248, 0.24)",
              },
            }}
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
          <IconButton
            onClick={loadData}
            title="Admin verilerini yenile"
            sx={{
              ml: "auto",
              bgcolor: (theme) =>
                theme.palette.mode === "dark"
                  ? "rgba(15, 23, 42, 0.9)"
                  : "#ffffff",
              color: (theme) =>
                theme.palette.mode === "dark"
                  ? theme.palette.primary.light
                  : theme.palette.primary.main,
              boxShadow: (theme) =>
                theme.palette.mode === "dark"
                  ? "0 0 0 1px rgba(148,163,184,0.5)"
                  : "0 0 0 1px rgba(148,163,184,0.4)",
              "&:hover": {
                bgcolor: (theme) =>
                  theme.palette.mode === "dark"
                    ? "rgba(15, 23, 42, 1)"
                    : "rgba(248, 250, 252, 1)",
                boxShadow: (theme) =>
                  theme.palette.mode === "dark"
                    ? "0 0 0 1px rgba(165,180,252,0.9)"
                    : "0 0 0 1px rgba(129,140,248,0.8)",
              },
            }}
          >
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {}
      <Paper
        sx={{
          mb: 4,
          borderRadius: 2,
          overflow: "hidden",
        }}
      >
        <Tabs
          value={tabBarValue}
          onChange={(_, v) => {
            if (v !== moreTabIndex) {
              setActiveTab(v);
              setMoreMenuAnchor(null);
            }
          }}
          variant="scrollable"
          scrollButtons="auto"
          sx={{
            "& .MuiTab-root": {
              display: "flex",
              alignItems: "center",
              flexDirection: "row",
              textTransform: "none",
              fontSize: { xs: "0.85rem", sm: "0.9375rem" },
              fontWeight: 500,
              minWidth: "auto",
              minHeight: { xs: 48, sm: 56 },
              whiteSpace: "nowrap",
              px: { xs: 1.5, sm: 2.5 },
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
              backgroundColor: "#667eea",
            },
          }}
        >
          <Tab
            icon={<TrendingUp />}
            label={tabLabel("Genel Bakış")}
            iconPosition="start"
          />
          <Tab
            icon={<Receipt />}
            label={tabLabel("Siparişler")}
            iconPosition="start"
          />
          {!isMobile && [
            <Tab
              key="katana-products"
              icon={<ShoppingCart />}
              label={tabLabel("Katana Ürünleri")}
              iconPosition="start"
            />,
            <Tab
              key="luca-products"
              icon={<Inventory />}
              label={tabLabel("Luca Ürünleri")}
              iconPosition="start"
            />,
            <Tab
              key="stock-management"
              icon={<Warehouse />}
              label={tabLabel("Stok Yönetimi")}
              iconPosition="start"
            />,
          ]}
          <Tab
            icon={<MoreVertIcon />}
            label={tabLabel("Diğer")}
            iconPosition="start"
            onClick={(e: React.MouseEvent<HTMLDivElement>) => {
              e.stopPropagation();
              setMoreMenuAnchor(e.currentTarget);
            }}
            sx={{
              backgroundColor: overflowTabActive
                ? "rgba(102, 126, 234, 0.08)"
                : "transparent",
              "&:hover": {
                backgroundColor: "rgba(102, 126, 234, 0.12)",
              },
            }}
          />
        </Tabs>

        {}
        <Menu
          anchorEl={moreMenuAnchor}
          open={Boolean(moreMenuAnchor)}
          onClose={() => setMoreMenuAnchor(null)}
          anchorOrigin={{
            vertical: "bottom",
            horizontal: "left",
          }}
          transformOrigin={{
            vertical: "top",
            horizontal: "left",
          }}
          PaperProps={{
            sx: {
              mt: 1,
              minWidth: 200,
              boxShadow: "0 4px 20px rgba(0,0,0,0.1)",
              borderRadius: 2,
              backgroundColor: "rgba(255,255,255,0.98)",
              backdropFilter: "blur(10px)",
            },
          }}
        >
          {isMobile && [
            <MenuItem
              key="menu-katana"
              onClick={() => {
                setActiveTab(2);
                setMoreMenuAnchor(null);
              }}
              sx={overflowMenuItemSx}
            >
              <ShoppingCart sx={{ mr: 1.5, fontSize: 20, color: "#667eea" }} />
              <Typography
                variant="body2"
                translate="no"
                sx={overflowMenuTextSx}
              >
                Katana Ürünleri
              </Typography>
            </MenuItem>,
            <MenuItem
              key="menu-luca"
              onClick={() => {
                setActiveTab(3);
                setMoreMenuAnchor(null);
              }}
              sx={overflowMenuItemSx}
            >
              <Inventory sx={{ mr: 1.5, fontSize: 20, color: "#667eea" }} />
              <Typography
                variant="body2"
                translate="no"
                sx={overflowMenuTextSx}
              >
                Luca Ürünleri
              </Typography>
            </MenuItem>,
            <MenuItem
              key="menu-stock"
              onClick={() => {
                setActiveTab(4);
                setMoreMenuAnchor(null);
              }}
              sx={overflowMenuItemSx}
            >
              <Warehouse sx={{ mr: 1.5, fontSize: 20, color: "#667eea" }} />
              <Typography
                variant="body2"
                translate="no"
                sx={overflowMenuTextSx}
              >
                Stok Yönetimi
              </Typography>
            </MenuItem>,
            <Divider key="menu-divider" sx={{ my: 1 }} />,
          ]}
          <MenuItem
            onClick={() => {
              setActiveTab(5);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <SwapHorizIcon sx={{ mr: 1.5, fontSize: 20, color: "#8b5cf6" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Stok Hareketleri
            </Typography>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setActiveTab(6);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <ReportProblem sx={{ mr: 1.5, fontSize: 20, color: "#f59e0b" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Hatalı Kayıtlar
            </Typography>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setActiveTab(7);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <CompareArrowsIcon
              sx={{ mr: 1.5, fontSize: 20, color: "#10b981" }}
            />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Veri Düzeltme
            </Typography>
          </MenuItem>
          <Divider sx={{ my: 1 }} />
          <MenuItem
            onClick={() => {
              setActiveTab(8);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <UsersIcon sx={{ mr: 1.5, fontSize: 20, color: "#3b82f6" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Kullanıcılar
            </Typography>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setActiveTab(9);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <LogsIcon sx={{ mr: 1.5, fontSize: 20, color: "#64748b" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Loglar
            </Typography>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setActiveTab(10);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <SettingsIcon sx={{ mr: 1.5, fontSize: 20, color: "#0ea5e9" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Ayarlar
            </Typography>
          </MenuItem>
          <Divider sx={{ my: 1 }} />
          <MenuItem
            onClick={() => {
              setActiveTab(11);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <KozaIcon sx={{ mr: 1.5, fontSize: 20, color: "#667eea" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Koza Entegrasyon
            </Typography>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setActiveTab(12);
              setMoreMenuAnchor(null);
            }}
            sx={overflowMenuItemSx}
          >
            <ShoppingCart sx={{ mr: 1.5, fontSize: 20, color: "#22c55e" }} />
            <Typography variant="body2" translate="no" sx={overflowMenuTextSx}>
              Sipariş Entegrasyonu
            </Typography>
          </MenuItem>
        </Menu>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {}
      {activeTab === 0 && (
        <Box>
          {}
          <Box sx={{ mb: 4 }}>
            <PendingAdjustments />
          </Box>

          {}
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
            {}
            <Paper sx={{ p: { xs: 1.5, md: 3 }, borderRadius: 2 }}>
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
              {isMobile ? (
                <Box
                  sx={{
                    display: "flex",
                    flexDirection: "column",
                    gap: 1.5,
                  }}
                >
                  {products.length === 0 ? (
                    <Typography
                      color="text.secondary"
                      align="center"
                      sx={{ py: 2 }}
                    >
                      Ürün bulunamadı
                    </Typography>
                  ) : (
                    products.map((product, idx) => (
                      <Paper
                        key={
                          product.id && product.id !== ""
                            ? product.id
                            : product.sku && product.sku !== ""
                            ? product.sku
                            : `product-${idx}`
                        }
                        sx={{
                          border: "1px solid",
                          borderColor: "divider",
                          borderRadius: 2,
                          p: 1.5,
                        }}
                      >
                        <Typography variant="subtitle1" fontWeight={600}>
                          {product.name}
                        </Typography>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{ mt: 0.25 }}
                        >
                          SKU: <strong>{product.sku}</strong>
                        </Typography>
                        <Box
                          sx={{
                            display: "grid",
                            gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                            columnGap: 1,
                            rowGap: 1,
                            mt: 1,
                          }}
                        >
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Stok
                            </Typography>
                            <Typography fontWeight={600}>
                              {product.stock}
                            </Typography>
                          </Box>
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Durum
                            </Typography>
                            <Chip
                              label={product.isActive ? "Aktif" : "Pasif"}
                              color={product.isActive ? "success" : "default"}
                              size="small"
                              sx={{ mt: 0.25 }}
                            />
                          </Box>
                        </Box>
                      </Paper>
                    ))
                  )}
                </Box>
              ) : (
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
              )}
            </Paper>

            {}
            <Paper sx={{ p: { xs: 1.5, md: 3 }, borderRadius: 2 }}>
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
              {isMobile ? (
                <Box
                  sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}
                >
                  {syncLogs.length === 0 ? (
                    <Typography
                      color="text.secondary"
                      align="center"
                      sx={{ py: 2 }}
                    >
                      Kayıt bulunamadı
                    </Typography>
                  ) : (
                    syncLogs.map((log, idx) => (
                      <Paper
                        key={
                          log.id && String(log.id) !== "0"
                            ? log.id
                            : `log-${idx}`
                        }
                        sx={{
                          border: "1px solid",
                          borderColor: "divider",
                          borderRadius: 2,
                          p: 1.5,
                        }}
                      >
                        <Box
                          sx={{
                            display: "flex",
                            justifyContent: "space-between",
                            gap: 1,
                            alignItems: "flex-start",
                          }}
                        >
                          <Box>
                            <Typography variant="subtitle1" fontWeight={600}>
                              {log.integrationName}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              {new Date(log.createdAt).toLocaleString("tr-TR")}
                            </Typography>
                          </Box>
                          <Chip
                            label={log.isSuccess ? "Başarılı" : "Başarısız"}
                            color={log.isSuccess ? "success" : "error"}
                            size="small"
                          />
                        </Box>
                      </Paper>
                    ))
                  )}
                </Box>
              ) : (
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
              )}
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

      {}
      {activeTab === 1 && <Orders />}

      {}
      {activeTab === 2 && <KatanaProducts />}

      {}
      {activeTab === 3 && <LucaProducts />}

      {}
      {activeTab === 4 && <StockManagement />}

      {/* Stok Hareketleri Tab */}
      {activeTab === 5 && <StockMovements />}

      {}
      {activeTab === 6 && <FailedRecords />}

      {}
      {activeTab === 7 && <DataCorrectionPanel />}

      {}
      {activeTab === 8 && <UsersManagement />}

      {}
      {activeTab === 9 && <LogsViewer />}

      {}
      {activeTab === 10 && <Settings />}

      {/* Koza Entegrasyon Tab */}
      {activeTab === 11 && <KozaIntegration />}

      {/* Sipariş Entegrasyonu (Sales + Purchase Orders) */}
      {activeTab === 12 && <OrderIntegrationPage />}
    </Container>
  );
};

export default AdminPanel;
