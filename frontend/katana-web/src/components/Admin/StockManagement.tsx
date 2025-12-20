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
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Badge,
  useMediaQuery,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import RefreshIcon from "@mui/icons-material/Refresh";
import InventoryIcon from "@mui/icons-material/Inventory";
import AddShoppingCartIcon from "@mui/icons-material/AddShoppingCart";
import WarningIcon from "@mui/icons-material/Warning";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import api from "../../services/api";

interface Product {
  id: number;
  sku: string;
  name: string;
  stock: number;
  price: number;
  isActive: boolean;
  categoryId?: number;
}

interface StockStats {
  totalProducts: number;
  lowStockCount: number;
  outOfStockCount: number;
  totalValue: number;
}

const StockManagement: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<Product[]>([]);
  const [stats, setStats] = useState<StockStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [purchaseModalOpen, setPurchaseModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [purchaseQty, setPurchaseQty] = useState(0);
  const [lowStockThreshold] = useState(10);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const isMobile = useMediaQuery("(max-width:900px)");

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
      return { label: "Yok", color: "error" as const, severity: "critical" };
    if (stock <= lowStockThreshold)
      return { label: "Düşük", color: "warning" as const, severity: "warning" };
    return { label: "Normal", color: "success" as const, severity: "normal" };
  };

  const handlePurchaseClick = (product: Product) => {
    setSelectedProduct(product);
    setPurchaseQty(10);
    setPurchaseModalOpen(true);
  };

  const handlePurchase = async () => {
    if (!selectedProduct || purchaseQty <= 0) return;

    try {
      const newStock = selectedProduct.stock + purchaseQty;
      await api.patch(`/Products/${selectedProduct.id}/stock`, newStock, {
        headers: { "Content-Type": "application/json" },
      });

      setSuccessMessage(
        `${selectedProduct.name} için ${purchaseQty} adet satın alındı!`
      );
      setTimeout(() => setSuccessMessage(null), 3000);

      setPurchaseModalOpen(false);
      fetchData();
    } catch (err: any) {
      setError(err.response?.data?.error || "Satın alma başarısız");
    }
  };

  const lowStockProducts = products.filter(
    (p) => p.stock > 0 && p.stock <= lowStockThreshold
  );
  const criticalProducts = products.filter((p) => p.stock === 0);

  return (
    <Box>
      {}
      {!loading && stats && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, 1fr)",
              md: "repeat(4, 1fr)",
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
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
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
              background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
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
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Düşük Stok
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.lowStockCount || 0}
                  </Typography>
                </Box>
                <Badge badgeContent={stats.lowStockCount || 0} color="error">
                  <WarningIcon sx={{ fontSize: 40, opacity: 0.8 }} />
                </Badge>
              </Stack>
            </CardContent>
          </Card>

          <Card
            sx={{
              background: "linear-gradient(135deg, #fa709a 0%, #fee140 100%)",
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
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Stokta Yok
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    {stats.outOfStockCount || 0}
                  </Typography>
                </Box>
                <Badge badgeContent={stats.outOfStockCount || 0} color="error">
                  <NotificationsActiveIcon
                    sx={{ fontSize: 40, opacity: 0.8 }}
                  />
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
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Toplam Değer
                  </Typography>
                  <Typography variant="h4" fontWeight="bold">
                    ₺{(stats.totalValue || 0).toLocaleString("tr-TR")}
                  </Typography>
                </Box>
                <TrendingUpIcon sx={{ fontSize: 40, opacity: 0.8 }} />
              </Stack>
            </CardContent>
          </Card>
        </Box>
      )}

      {}
      {criticalProducts.length > 0 && (
        <Alert
          severity="error"
          icon={<NotificationsActiveIcon />}
          sx={{ mb: 2 }}
        >
          <strong>KRİTİK:</strong> {criticalProducts.length} ürün stokta yok!
          Acil satın alma gerekli.
        </Alert>
      )}

      {lowStockProducts.length > 0 && (
        <Alert severity="warning" icon={<WarningIcon />} sx={{ mb: 2 }}>
          <strong>UYARI:</strong> {lowStockProducts.length} ürün düşük stokta.
          Satın alma planlaması yapılmalı.
        </Alert>
      )}

      {successMessage && (
        <Alert
          severity="success"
          sx={{ mb: 2 }}
          onClose={() => setSuccessMessage(null)}
        >
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {}
      <Card>
        <CardContent>
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            mb={2}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <InventoryIcon color="primary" />
              <Typography variant="h5" fontWeight="600">
                Stok Yönetimi
              </Typography>
            </Stack>
            <Tooltip title="Yenile">
              <IconButton onClick={fetchData} disabled={loading}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
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
          ) : isMobile ? (
            <Stack spacing={1.5}>
              {filteredProducts.length === 0 && (
                <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
                  Ürün bulunamadı
                </Typography>
              )}
              {filteredProducts.map((product) => {
                const status = getStockStatus(product.stock);
                return (
                  <Paper
                    key={product.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: "1px solid",
                      borderColor: "divider",
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
                          {product.name}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          SKU: <strong>{product.sku}</strong>
                        </Typography>
                      </Box>
                      <Chip
                        label={status.label}
                        color={status.color}
                        size="small"
                      />
                    </Box>
                    <Box
                      sx={{
                        display: "grid",
                        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                        columnGap: 1,
                        rowGap: 1,
                        mt: 1.25,
                      }}
                    >
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Stok
                        </Typography>
                        <Typography
                          fontWeight={700}
                          color={
                            status.severity === "critical"
                              ? "error.main"
                              : status.severity === "warning"
                              ? "warning.main"
                              : "success.main"
                          }
                        >
                          {product.stock}
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Fiyat
                        </Typography>
                        <Typography fontWeight={600}>
                          ₺{(product.price || 0).toFixed(2)}
                        </Typography>
                      </Box>
                    </Box>
                    <Button
                      fullWidth
                      size="small"
                      variant="outlined"
                      startIcon={<AddShoppingCartIcon />}
                      sx={{ mt: 1.25 }}
                      onClick={() => handlePurchaseClick(product)}
                    >
                      Satın Al
                    </Button>
                  </Paper>
                );
              })}
            </Stack>
          ) : (
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>SKU</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Ürün Adı</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Stok</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Fiyat</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>İşlem</strong>
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
                      return (
                        <TableRow key={product.id} hover>
                          <TableCell>
                            <Typography variant="body2" fontWeight="bold">
                              {product.sku}
                            </Typography>
                          </TableCell>
                          <TableCell>{product.name}</TableCell>
                          <TableCell align="right">
                            <Typography
                              fontWeight="bold"
                              color={
                                status.severity === "critical"
                                  ? "error.main"
                                  : status.severity === "warning"
                                  ? "warning.main"
                                  : "success.main"
                              }
                            >
                              {product.stock}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            ₺{(product.price || 0).toFixed(2)}
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={status.label}
                              color={status.color}
                              size="small"
                              icon={
                                status.severity === "critical" ? (
                                  <NotificationsActiveIcon />
                                ) : status.severity === "warning" ? (
                                  <WarningIcon />
                                ) : undefined
                              }
                            />
                          </TableCell>
                          <TableCell align="center">
                            <Tooltip title="Satın Al">
                              <IconButton
                                size="small"
                                color="primary"
                                onClick={() => handlePurchaseClick(product)}
                              >
                                <AddShoppingCartIcon />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        </TableRow>
                      );
                    })
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {}
      {criticalProducts.length > 0 && (
        <Card sx={{ mt: 3 }}>
          <CardContent>
            <Stack direction="row" alignItems="center" spacing={1} mb={2}>
              <NotificationsActiveIcon color="error" />
              <Typography variant="h5" fontWeight="600" color="error.main">
                Stokta Olmayan Ürünler ({criticalProducts.length})
              </Typography>
            </Stack>
            {isMobile ? (
              <Stack spacing={1.5}>
                {criticalProducts.map((product) => (
                  <Paper
                    key={product.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: "1px solid",
                      borderColor: "error.light",
                      backgroundColor: "error.lighter",
                    }}
                  >
                    <Typography variant="subtitle1" fontWeight={600}>
                      {product.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      SKU: <strong>{product.sku}</strong>
                    </Typography>
                    <Typography fontWeight={600} sx={{ mt: 1 }}>
                      ₺{(product.price || 0).toFixed(2)}
                    </Typography>
                    <Button
                      fullWidth
                      variant="contained"
                      color="error"
                      startIcon={<AddShoppingCartIcon />}
                      sx={{ mt: 1 }}
                      onClick={() => handlePurchaseClick(product)}
                    >
                      Satın Al
                    </Button>
                  </Paper>
                ))}
              </Stack>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>
                        <strong>SKU</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ürün Adı</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Fiyat</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>İşlem</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {criticalProducts.map((product) => (
                      <TableRow
                        key={product.id}
                        hover
                        sx={{ bgcolor: "error.lighter" }}
                      >
                        <TableCell>
                          <Typography variant="body2" fontWeight="bold">
                            {product.sku}
                          </Typography>
                        </TableCell>
                        <TableCell>{product.name}</TableCell>
                        <TableCell align="right">
                          ₺{(product.price || 0).toFixed(2)}
                        </TableCell>
                        <TableCell align="center">
                          <Tooltip title="Satın Al">
                            <IconButton
                              size="small"
                              color="error"
                              onClick={() => handlePurchaseClick(product)}
                            >
                              <AddShoppingCartIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      )}

      {}
      <Dialog
        open={purchaseModalOpen}
        onClose={() => setPurchaseModalOpen(false)}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: { borderRadius: 3 },
        }}
      >
        <DialogTitle>
          <Stack direction="row" alignItems="center" spacing={1}>
            <AddShoppingCartIcon color="primary" />
            <Typography variant="h5" fontWeight="600">
              Satın Alma
            </Typography>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ pt: 3 }}>
          {selectedProduct && (
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Ürün: <strong>{selectedProduct.name}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Mevcut Stok: <strong>{selectedProduct.stock}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary" mb={2}>
                Birim Fiyat:{" "}
                <strong>₺{(selectedProduct.price || 0).toFixed(2)}</strong>
              </Typography>

              <Box sx={{ mt: 3 }}>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Satın Alınacak Miktar
                </Typography>
                <TextField
                  fullWidth
                  type="number"
                  value={purchaseQty}
                  onChange={(e) =>
                    setPurchaseQty(Math.max(1, parseInt(e.target.value) || 0))
                  }
                  inputProps={{ min: 1 }}
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box
                sx={{
                  mt: 3,
                  p: 2,
                  bgcolor: "action.hover",
                  borderRadius: 2,
                }}
              >
                <Typography variant="body2" color="text.secondary">
                  Toplam Tutar
                </Typography>
                <Typography variant="h5" fontWeight="bold" color="primary">
                  ₺
                  {(
                    (purchaseQty || 0) * (selectedProduct.price || 0)
                  ).toLocaleString("tr-TR")}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Yeni Stok: {(selectedProduct.stock || 0) + (purchaseQty || 0)}
                </Typography>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={() => setPurchaseModalOpen(false)}
            sx={{ borderRadius: 2 }}
          >
            İptal
          </Button>
          <Button
            onClick={handlePurchase}
            variant="contained"
            startIcon={<AddShoppingCartIcon />}
            disabled={purchaseQty <= 0}
            sx={{
              borderRadius: 2,
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
            }}
          >
            Satın Al
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default StockManagement;
