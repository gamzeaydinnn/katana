import InventoryIcon from "@mui/icons-material/Inventory";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import RefreshIcon from "@mui/icons-material/Refresh";
import SearchIcon from "@mui/icons-material/Search";
import TrendingDownIcon from "@mui/icons-material/TrendingDown";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import WarningIcon from "@mui/icons-material/Warning";
import {
  Alert,
  Badge,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api from "../services/api";

interface Product {
  id: number;
  sku: string;
  name: string;
  stock: number;
  price: number;
  isActive: boolean;
}

interface StockStats {
  totalProducts: number;
  activeProducts: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  totalInventoryValue: number;
}

const StockView: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
  const [products, setProducts] = useState<Product[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<Product[]>([]);
  const [stats, setStats] = useState<StockStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [lowStockThreshold] = useState(10);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [productsRes, statsRes] = await Promise.all([
        api.get<any>("/Products"),
        api.get<StockStats>("/Products/statistics"),
      ]);

      const productData = Array.isArray(productsRes.data)
        ? productsRes.data
        : productsRes.data?.data || [];

      setProducts(productData);
      setFilteredProducts(productData);
      setStats(statsRes.data || null);
    } catch (err: any) {
      setError(err.response?.data?.error || "Veri yüklenemedi");
      console.error("Stok yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchData, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredProducts(products);
    } else {
      const filtered = products.filter(
        (p) =>
          p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          p.sku.toLowerCase().includes(searchTerm.toLowerCase())
      );
      setFilteredProducts(filtered);
    }
  }, [searchTerm, products]);

  const getStockStatus = (stock: number) => {
    if (stock === 0)
      return { label: "Stokta Yok", color: "error" as const, hasIcon: true };
    if (stock <= lowStockThreshold)
      return { label: "Düşük Stok", color: "warning" as const, hasIcon: true };
    return { label: "Normal", color: "success" as const, hasIcon: false };
  };

  const lowStockProducts = products.filter(
    (p) => p.stock > 0 && p.stock <= lowStockThreshold
  );
  const criticalProducts = products.filter((p) => p.stock === 0);

  return (
    <Box
      sx={{
        p: { xs: 1.5, md: 3 },
        maxWidth: 1440,
        mx: "auto",
      }}
    >
      {/* Header */}
      <Stack
        direction={{ xs: "column", sm: "row" }}
        justifyContent="space-between"
        alignItems={{ xs: "flex-start", sm: "center" }}
        gap={1.5}
        mb={3}
      >
        <Box>
          <Typography
            variant="h4"
            sx={{
              fontWeight: 900,
              letterSpacing: "-0.02em",
              fontSize: { xs: "1.55rem", md: "2rem" },
              background: "linear-gradient(135deg, #4f46e5 0%, #0891b2 100%)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            Canlı Stok
          </Typography>
          <Typography
            variant="body2"
            sx={{ color: "text.secondary", mt: 0.5 }}
          >
            En güncel stok rakamları ve canlı uyarılar
          </Typography>
        </Box>
        <Tooltip title="Otomatik yenileme: Her 30 saniye">
          <IconButton
            onClick={fetchData}
            disabled={loading}
            color="primary"
            sx={{
              alignSelf: { xs: "flex-end", sm: "center" },
              backgroundColor: "rgba(79,134,255,0.08)",
            }}
          >
            <RefreshIcon />
          </IconButton>
        </Tooltip>
      </Stack>

      {/* Statistics Cards */}
      {!loading && stats && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "repeat(auto-fit, minmax(115px, 1fr))",
              sm: "repeat(2, minmax(160px, 1fr))",
              md: "repeat(5, minmax(200px, 1fr))",
            },
            gap: { xs: 0.75, md: 2 },
            mb: 3,
          }}
        >
          {[
            {
              key: "total",
              title: "Toplam Ürün",
              value: stats.totalProducts || 0,
              icon: <InventoryIcon sx={{ fontSize: { xs: 26, md: 32 }, opacity: 0.8 }} />,
              gradient: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              mobileOrder: 1,
            },
            {
              key: "active",
              title: "Aktif Ürün",
              value: stats.activeProducts || 0,
              icon: <TrendingUpIcon sx={{ fontSize: { xs: 26, md: 32 }, opacity: 0.8 }} />,
              gradient: "linear-gradient(135deg, #2ecc71 0%, #27ae60 100%)",
              mobileOrder: 2,
            },
            {
              key: "value",
              title: "Toplam Değer",
              value: `₺${(stats.totalInventoryValue || 0).toLocaleString(
                "tr-TR"
              )}`,
              icon: <TrendingUpIcon sx={{ fontSize: { xs: 26, md: 32 }, opacity: 0.8 }} />,
              gradient: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
              mobileOrder: 3,
            },
            {
              key: "low",
              title: "Düşük Stok",
              value: stats.lowStockProducts || 0,
              icon: <WarningIcon sx={{ fontSize: { xs: 26, md: 32 }, opacity: 0.8 }} />,
              gradient: "linear-gradient(135deg, #f39c12 0%, #e67e22 100%)",
              mobileOrder: 4,
            },
            {
              key: "out",
              title: "Stokta Yok",
              value: stats.outOfStockProducts || 0,
              icon: (
                <Badge badgeContent="!" color="error">
                  <TrendingDownIcon sx={{ fontSize: { xs: 26, md: 32 }, opacity: 0.8 }} />
                </Badge>
              ),
              gradient: "linear-gradient(135deg, #e74c3c 0%, #c0392b 100%)",
              mobileOrder: 5,
            },
          ].map((stat) => (
            <Card
              key={stat.key}
              sx={{
                color: "white",
                background: stat.gradient,
                order: { xs: stat.mobileOrder, md: "initial" },
                minHeight: { xs: 120, md: "auto" },
              }}
            >
              <CardContent sx={{ py: 1.5, px: 2 }}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                >
                  <Box>
                    <Typography variant="caption" sx={{ opacity: 0.9 }}>
                      {stat.title}
                    </Typography>
                    <Typography
                      variant="h5"
                      fontWeight="bold"
                      sx={{ fontSize: { xs: "1.1rem", md: "1.65rem" } }}
                    >
                      {stat.value}
                    </Typography>
                  </Box>
                  {stat.icon}
                </Stack>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {/* Alerts */}
      {criticalProducts.length > 0 && (
        <Alert
          severity="error"
          icon={<NotificationsActiveIcon />}
          sx={{ mb: 2, borderRadius: 3 }}
        >
          <strong>KRİTİK UYARI:</strong> {criticalProducts.length} ürün stokta
          yok! Lütfen yöneticiye bildiriniz.
        </Alert>
      )}

      {lowStockProducts.length > 0 && (
        <Alert
          severity="warning"
          icon={<WarningIcon />}
          sx={{ mb: 2, borderRadius: 3 }}
        >
          <strong>DİKKAT:</strong> {lowStockProducts.length} ürün düşük stokta.
          Yakında tükenebilit.
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Main Card */}
      <Card
        sx={{
          borderRadius: 3,
          boxShadow: "0 12px 40px rgba(15,34,67,0.08)",
        }}
      >
        <CardContent sx={{ p: { xs: 2, md: 3 } }}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            alignItems={{ xs: "flex-start", sm: "center" }}
            gap={1.5}
            mb={2}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <InventoryIcon color="primary" fontSize="large" />
              <Typography variant="h5" fontWeight="600">
                Anlık Stok Durumu
              </Typography>
            </Stack>
            <Chip
              label="Canlı Takip"
              color="success"
              size="small"
              icon={<CircularProgress size={12} color="inherit" />}
              sx={{ alignSelf: { xs: "flex-start", sm: "center" } }}
            />
          </Stack>

          <TextField
            fullWidth
            placeholder="Ürün adı veya SKU ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ mb: 2 }}
          />

          {loading ? (
            <Box display="flex" justifyContent="center" p={4}>
              <CircularProgress />
            </Box>
          ) : (
            <TableContainer
              component={Paper}
              sx={{
                maxHeight: 600,
                overflowX: "auto",
                borderRadius: 2,
                "&::-webkit-scrollbar": { height: 6 },
                "&::-webkit-scrollbar-thumb": {
                  backgroundColor: "#cbd5f5",
                  borderRadius: 999,
                },
              }}
            >
              <Table
                stickyHeader
                size="small"
                sx={{
                  minWidth: isMobile ? 560 : "auto",
                  tableLayout: "fixed",
                }}
              >
                <TableHead>
                  <TableRow>
                    {[
                      { label: "SKU", align: "left" },
                      { label: "Ürün Adı", align: "left" },
                      { label: "Mevcut Stok", align: "center" },
                      { label: "Birim Fiyat", align: "right" },
                      { label: "Durum", align: "center" },
                      { label: "Toplam Değer", align: "right" },
                    ].map((col) => (
                      <TableCell
                        key={col.label}
                        align={col.align as any}
                        sx={{
                          fontWeight: 700,
                          fontSize: { xs: "0.75rem", md: "0.85rem" },
                          px: { xs: 1, md: 2 },
                        }}
                      >
                        {col.label}
                      </TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredProducts.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography color="textSecondary">
                          Ürün bulunamadı
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredProducts.map((product) => {
                      const status = getStockStatus(product.stock);
                      const totalValue = product.stock * product.price;

                      return (
                        <TableRow
                          key={product.id}
                          hover
                          sx={{
                            bgcolor:
                              product.stock === 0
                                ? "error.lighter"
                                : product.stock <= lowStockThreshold
                                ? "warning.lighter"
                                : "inherit",
                          }}
                        >
                          <TableCell>
                            <Typography variant="body2" fontWeight="bold">
                              {product.sku}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2">
                              {product.name}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <Typography
                              variant="h6"
                              fontWeight="bold"
                              color={
                                product.stock === 0
                                  ? "error.main"
                                  : product.stock <= lowStockThreshold
                                  ? "warning.main"
                                  : "success.main"
                              }
                            >
                              {product.stock}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Typography variant="body2">
                              ₺{(product.price || 0).toFixed(2)}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <Chip
                              label={status.label}
                              color={status.color}
                              size="small"
                              icon={
                                status.hasIcon && product.stock === 0 ? (
                                  <NotificationsActiveIcon fontSize="small" />
                                ) : status.hasIcon ? (
                                  <WarningIcon fontSize="small" />
                                ) : undefined
                              }
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Typography
                              variant="body2"
                              fontWeight="bold"
                              color={
                                product.stock === 0
                                  ? "text.disabled"
                                  : "text.primary"
                              }
                            >
                              ₺
                              {totalValue.toLocaleString("tr-TR", {
                                minimumFractionDigits: 2,
                                maximumFractionDigits: 2,
                              })}
                            </Typography>
                          </TableCell>
                        </TableRow>
                      );
                    })
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          <Box sx={{ mt: 2, p: 2, bgcolor: "action.hover", borderRadius: 2 }}>
            <Typography variant="caption" color="text.secondary">
              <strong>Not:</strong> Stok bilgileri 30 saniyede bir otomatik
              güncellenir. Stok düzenlemeleri için admin panelini kullanın.
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default StockView;
