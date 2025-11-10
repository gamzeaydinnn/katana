import React, { useState, useEffect } from "react";
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  CircularProgress,
  Alert,
  TextField,
  InputAdornment,
  IconButton,
  Tooltip,
  Stack,
  Badge,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import RefreshIcon from "@mui/icons-material/Refresh";
import InventoryIcon from "@mui/icons-material/Inventory";
import WarningIcon from "@mui/icons-material/Warning";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import TrendingDownIcon from "@mui/icons-material/TrendingDown";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
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
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        mb={3}
      >
        <Typography variant="h4" fontWeight="bold">
          Stok Görünümü
        </Typography>
        <Tooltip title="Otomatik yenileme: Her 30 saniye">
          <IconButton onClick={fetchData} disabled={loading} color="primary">
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
              xs: "1fr",
              sm: "repeat(2, 1fr)",
              md: "repeat(5, 1fr)",
            },
            gap: 2,
            mb: 3,
          }}
        >
          <Card
            sx={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              color: "white",
            }}
          >
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.9 }}>
                    Toplam Ürün
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.totalProducts || 0}
                  </Typography>
                </Box>
                <InventoryIcon sx={{ fontSize: 40, opacity: 0.8 }} />
              </Stack>
            </CardContent>
          </Card>

          <Card
            sx={{
              background: "linear-gradient(135deg, #2ecc71 0%, #27ae60 100%)",
              color: "white",
            }}
          >
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.9 }}>
                    Aktif Ürün
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.activeProducts || 0}
                  </Typography>
                </Box>
                <TrendingUpIcon sx={{ fontSize: 40, opacity: 0.8 }} />
              </Stack>
            </CardContent>
          </Card>

          <Card
            sx={{
              background: "linear-gradient(135deg, #f39c12 0%, #e67e22 100%)",
              color: "white",
            }}
          >
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.9 }}>
                    Düşük Stok
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.lowStockProducts || 0}
                  </Typography>
                </Box>
                <Badge badgeContent={stats.lowStockProducts || 0} color="error">
                  <WarningIcon sx={{ fontSize: 40, opacity: 0.8 }} />
                </Badge>
              </Stack>
            </CardContent>
          </Card>

          <Card
            sx={{
              background: "linear-gradient(135deg, #e74c3c 0%, #c0392b 100%)",
              color: "white",
            }}
          >
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.9 }}>
                    Stokta Yok
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.outOfStockProducts || 0}
                  </Typography>
                </Box>
                <Badge badgeContent="!" color="error">
                  <TrendingDownIcon sx={{ fontSize: 40, opacity: 0.8 }} />
                </Badge>
              </Stack>
            </CardContent>
          </Card>

          <Card
            sx={{
              background: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
              color: "white",
            }}
          >
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Box>
                  <Typography variant="caption" sx={{ opacity: 0.9 }}>
                    Toplam Değer
                  </Typography>
                  <Typography variant="h5" fontWeight="bold">
                    ₺{(stats.totalInventoryValue || 0).toLocaleString("tr-TR")}
                  </Typography>
                </Box>
                <TrendingUpIcon sx={{ fontSize: 40, opacity: 0.8 }} />
              </Stack>
            </CardContent>
          </Card>
        </Box>
      )}

      {/* Alerts */}
      {criticalProducts.length > 0 && (
        <Alert
          severity="error"
          icon={<NotificationsActiveIcon />}
          sx={{ mb: 2 }}
        >
          <strong>KRİTİK UYARI:</strong> {criticalProducts.length} ürün stokta
          yok! Lütfen yöneticiye bildiriniz.
        </Alert>
      )}

      {lowStockProducts.length > 0 && (
        <Alert severity="warning" icon={<WarningIcon />} sx={{ mb: 2 }}>
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
      <Card>
        <CardContent>
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
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
            <TableContainer component={Paper} sx={{ maxHeight: 600 }}>
              <Table stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>SKU</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Ürün Adı</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Mevcut Stok</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Birim Fiyat</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Toplam Değer</strong>
                    </TableCell>
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
