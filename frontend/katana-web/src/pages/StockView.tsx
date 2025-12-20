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
    Button,
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
import { useNavigate } from "react-router-dom";
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

interface CatalogResponse {
  data: Product[];
  total: number;
  hiddenCount?: number;
  filters?: {
    hideZeroStockProducts?: boolean;
    requirePublishedCategory?: boolean;
    requireActiveStatus?: boolean;
  };
}

const StockView: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
  const navigate = useNavigate();
  const [products, setProducts] = useState<Product[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<Product[]>([]);
  const [stats, setStats] = useState<StockStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [lowStockThreshold] = useState(10);
  const [hideZeroStockFlag, setHideZeroStockFlag] = useState(false);
  const [hiddenCount, setHiddenCount] = useState(0);
  const [hiddenReason, setHiddenReason] = useState<string | null>(null);
  const actionButtonSx = {
    width: { xs: "100%", md: 200 },
    height: 40,
    fontWeight: 600,
    borderRadius: 999,
  };

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [catalogRes, statsRes] = await Promise.all([
        api.get<CatalogResponse>("/Products/catalog"),
        api.get<StockStats>("/Products/statistics"),
      ]);

      const catalogData = catalogRes.data;
      const productData = Array.isArray(catalogData?.data)
        ? catalogData.data
        : [];
      const hideZero =
        catalogData?.filters?.hideZeroStockProducts ?? hideZeroStockFlag;

      setHideZeroStockFlag(hideZero);
      const hiddenFromResponse = catalogData?.hiddenCount ?? 0;
      setHiddenCount(hiddenFromResponse);
      if (hiddenFromResponse > 0) {
        setHiddenReason(
          hideZero
            ? "Sıfır stoklu ürünler gizlendi."
            : "Kategori veya ürün pasif olduğu için gizlenen kayıtlar var."
        );
      } else {
        setHiddenReason(null);
      }
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

  const handleAdminRedirect = (product: Product) => {
    if (product.stock === 0) return;
    navigate(`/admin/orders?sku=${encodeURIComponent(product.sku)}`);
  };

  return (
    <Box
      sx={{
        p: { xs: 1, sm: 1.5, md: 3 },
        maxWidth: 1440,
        mx: "auto",
        width: "100%",
        overflow: "hidden",
      }}
    >
      {}
      <Stack
        direction={{ xs: "column", sm: "row" }}
        justifyContent="space-between"
        alignItems={{ xs: "flex-start", sm: "center" }}
        gap={1.5}
        mb={2}
      >
        <Box sx={{ width: { xs: "100%", sm: "auto" } }}>
          <Typography
            variant="h4"
            sx={{
              fontWeight: 900,
              letterSpacing: "-0.02em",
              fontSize: { xs: "1.25rem", sm: "1.4rem", md: "2rem" },
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
            sx={{
              color: "text.secondary",
              mt: 0.5,
              fontSize: { xs: "0.75rem", sm: "0.8rem" },
            }}
          >
            En güncel stok rakamları ve canlı uyarılar
          </Typography>
        </Box>
        <Tooltip title="Otomatik yenileme: Her 30 saniye">
          <span>
            <IconButton
              onClick={fetchData}
              disabled={loading}
              color="primary"
              size={isMobile ? "small" : "medium"}
              sx={{
                alignSelf: { xs: "flex-end", sm: "center" },
                backgroundColor: "rgba(79,134,255,0.08)",
              }}
            >
              <RefreshIcon fontSize={isMobile ? "small" : "medium"} />
            </IconButton>
          </span>
        </Tooltip>
      </Stack>

      {hiddenCount > 0 && hiddenReason && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {hiddenCount} ürün gizlendi. {hiddenReason}
        </Alert>
      )}

      {}
      {!loading && stats && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "repeat(2, 1fr)",
              sm: "repeat(3, 1fr)",
              md: "repeat(5, 1fr)",
            },
            gap: { xs: 0.75, sm: 1, md: 2 },
            mb: 2,
          }}
        >
          {[
            {
              key: "total",
              title: "Toplam Ürün",
              value: stats.totalProducts || 0,
              icon: (
                <InventoryIcon
                  sx={{ fontSize: { xs: 20, sm: 24, md: 32 }, opacity: 0.8 }}
                />
              ),
              gradient: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
            },
            {
              key: "active",
              title: "Aktif Ürün",
              value: stats.activeProducts || 0,
              icon: (
                <TrendingUpIcon
                  sx={{ fontSize: { xs: 20, sm: 24, md: 32 }, opacity: 0.8 }}
                />
              ),
              gradient: "linear-gradient(135deg, #2ecc71 0%, #27ae60 100%)",
            },
            {
              key: "value",
              title: "Toplam Değer",
              value: `₺${(stats.totalInventoryValue || 0).toLocaleString(
                "tr-TR"
              )}`,
              icon: (
                <TrendingUpIcon
                  sx={{ fontSize: { xs: 20, sm: 24, md: 32 }, opacity: 0.8 }}
                />
              ),
              gradient: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
            },
            {
              key: "low",
              title: "Düşük Stok",
              value: stats.lowStockProducts || 0,
              icon: (
                <WarningIcon
                  sx={{ fontSize: { xs: 20, sm: 24, md: 32 }, opacity: 0.8 }}
                />
              ),
              gradient: "linear-gradient(135deg, #f39c12 0%, #e67e22 100%)",
            },
            {
              key: "out",
              title: "Stokta Yok",
              value: stats.outOfStockProducts || 0,
              icon: (
                <Badge badgeContent="!" color="error">
                  <TrendingDownIcon
                    sx={{ fontSize: { xs: 20, sm: 24, md: 32 }, opacity: 0.8 }}
                  />
                </Badge>
              ),
              gradient: "linear-gradient(135deg, #e74c3c 0%, #c0392b 100%)",
            },
          ].map((stat) => (
            <Card
              key={stat.key}
              sx={{
                color: "white",
                background: stat.gradient,
                minHeight: { xs: 70, sm: 80, md: "auto" },
              }}
            >
              <CardContent
                sx={{
                  py: { xs: 0.75, sm: 1, md: 1.5 },
                  px: { xs: 1, sm: 1.25, md: 2 },
                  "&:last-child": { pb: { xs: 0.75, sm: 1, md: 1.5 } },
                }}
              >
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                  spacing={0.5}
                >
                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Typography
                      variant="caption"
                      sx={{
                        opacity: 0.9,
                        fontSize: {
                          xs: "0.65rem",
                          sm: "0.7rem",
                          md: "0.75rem",
                        },
                        display: "block",
                        whiteSpace: "nowrap",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                      }}
                    >
                      {stat.title}
                    </Typography>
                    <Typography
                      variant="h6"
                      fontWeight="bold"
                      sx={{
                        fontSize: {
                          xs: "0.85rem",
                          sm: "0.95rem",
                          md: "1.2rem",
                        },
                        whiteSpace: "nowrap",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                      }}
                    >
                      {stat.value}
                    </Typography>
                  </Box>
                  <Box sx={{ flexShrink: 0 }}>{stat.icon}</Box>
                </Stack>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {}
      {criticalProducts.length > 0 && (
        <Alert
          severity="error"
          icon={
            <NotificationsActiveIcon fontSize={isMobile ? "small" : "medium"} />
          }
          sx={{
            mb: 1.5,
            borderRadius: 2,
            fontSize: { xs: "0.75rem", sm: "0.875rem" },
            "& .MuiAlert-message": {
              width: "100%",
            },
          }}
        >
          <strong>KRİTİK UYARI:</strong> {criticalProducts.length} ürün stokta
          yok!
        </Alert>
      )}

      {lowStockProducts.length > 0 && (
        <Alert
          severity="warning"
          icon={<WarningIcon fontSize={isMobile ? "small" : "medium"} />}
          sx={{
            mb: 1.5,
            borderRadius: 2,
            fontSize: { xs: "0.75rem", sm: "0.875rem" },
            "& .MuiAlert-message": {
              width: "100%",
            },
          }}
        >
          <strong>DİKKAT:</strong> {lowStockProducts.length} ürün düşük stokta.
        </Alert>
      )}

      {error && (
        <Alert
          severity="error"
          sx={{
            mb: 1.5,
            fontSize: { xs: "0.75rem", sm: "0.875rem" },
          }}
          onClose={() => setError(null)}
        >
          {error}
        </Alert>
      )}

      {}
      <Card
        sx={{
          borderRadius: { xs: 2, md: 3 },
          boxShadow: "0 12px 40px rgba(15,34,67,0.08)",
          overflow: "hidden",
        }}
      >
        <CardContent sx={{ p: { xs: 1.5, sm: 2, md: 3 } }}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            alignItems={{ xs: "flex-start", sm: "center" }}
            gap={1}
            mb={2}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <InventoryIcon
                color="primary"
                sx={{ fontSize: { xs: 24, md: 32 } }}
              />
              <Typography
                variant="h5"
                fontWeight="600"
                sx={{ fontSize: { xs: "0.95rem", sm: "1rem", md: "1.25rem" } }}
              >
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
            size={isMobile ? "small" : "medium"}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize={isMobile ? "small" : "medium"} />
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
            <>
              {isMobile ? (
                <Box>
                  {filteredProducts.length === 0 ? (
                    <Box py={2} textAlign="center">
                      <Typography color="textSecondary" fontSize="0.85rem">
                        Ürün bulunamadı
                      </Typography>
                    </Box>
                  ) : (
                    <Stack spacing={1}>
                      {filteredProducts.map((product) => {
                        const status = getStockStatus(product.stock);
                        const totalValue = product.stock * product.price;
                        const isOutOfStock = product.stock === 0;
                        const isLowStock =
                          product.stock > 0 &&
                          product.stock <= lowStockThreshold;
                        return (
                          <Paper
                            key={product.id}
                            variant="outlined"
                            sx={{
                              p: 1.5,
                              borderRadius: 2,
                              bgcolor:
                                isOutOfStock
                                  ? "rgba(244, 67, 54, 0.05)"
                                  : product.stock <= lowStockThreshold
                                  ? "rgba(255, 152, 0, 0.05)"
                                  : "inherit",
                            }}
                          >
                            <Stack spacing={1}>
                              <Stack
                                direction="row"
                                justifyContent="space-between"
                                alignItems="flex-start"
                              >
                                <Box sx={{ flex: 1, minWidth: 0, mr: 1 }}>
                                  <Typography
                                    variant="body2"
                                    fontWeight="700"
                                    sx={{
                                      fontSize: "0.9rem",
                                      wordBreak: "break-word",
                                    }}
                                  >
                                    {product.sku}
                                  </Typography>
                                  <Typography
                                    variant="body2"
                                    sx={{
                                      fontSize: "0.8rem",
                                      color: "text.secondary",
                                      wordBreak: "break-word",
                                    }}
                                  >
                                    {product.name}
                                  </Typography>
                                </Box>
                                <Chip
                                  label={status.label}
                                  color={status.color}
                                  size="small"
                                  sx={{
                                    fontSize: "0.7rem",
                                    height: 24,
                                    flexShrink: 0,
                                    fontWeight: 700,
                                    bgcolor: isOutOfStock
                                      ? "error.main"
                                      : undefined,
                                    color: isOutOfStock
                                      ? "common.white"
                                      : undefined,
                                  }}
                                />
                              </Stack>
                              <Stack
                                direction="row"
                                justifyContent="space-between"
                                alignItems="center"
                                sx={{
                                  pt: 0.5,
                                  borderTop: "1px solid",
                                  borderColor: "divider",
                                }}
                              >
                                <Box>
                                  <Typography
                                    variant="caption"
                                    color="text.secondary"
                                    sx={{ fontSize: "0.7rem" }}
                                  >
                                    Stok
                                  </Typography>
                                  <Typography
                                    variant="h6"
                                    fontWeight="700"
                                    sx={{
                                      fontSize: "1.1rem",
                                      color:
                                        product.stock === 0
                                          ? "error.main"
                                          : isLowStock
                                          ? "warning.main"
                                          : "success.main",
                                    }}
                                  >
                                    {product.stock}
                                  </Typography>
                                  {isOutOfStock && (
                                    <Chip
                                      label="Stokta Yok"
                                      color="error"
                                      size="small"
                                      variant="outlined"
                                      sx={{
                                        mt: 0.4,
                                        fontSize: "0.65rem",
                                        fontWeight: 700,
                                      }}
                                    />
                                  )}
                                </Box>
                                <Box sx={{ textAlign: "right" }}>
                                  <Typography
                                    variant="caption"
                                    color="text.secondary"
                                    sx={{ fontSize: "0.7rem" }}
                                  >
                                    Birim Fiyat
                                  </Typography>
                                  <Typography
                                    variant="body2"
                                    fontWeight="600"
                                    sx={{ fontSize: "0.85rem" }}
                                  >
                                    ₺{(product.price || 0).toFixed(2)}
                                  </Typography>
                                </Box>
                                <Box sx={{ textAlign: "right" }}>
                                  <Typography
                                    variant="caption"
                                    color="text.secondary"
                                    sx={{ fontSize: "0.7rem" }}
                                  >
                                    Toplam
                                  </Typography>
                                  <Typography
                                    variant="body2"
                                    fontWeight="700"
                                    sx={{
                                      fontSize: "0.85rem",
                                      color:
                                        isOutOfStock
                                          ? "text.disabled"
                                          : "primary.main",
                                    }}
                                  >
                                    ₺
                                    {totalValue.toLocaleString("tr-TR", {
                                      minimumFractionDigits: 2,
                                      maximumFractionDigits: 2,
                                    })}
                                  </Typography>
                                </Box>
                              </Stack>
                              <Button
                                variant="contained"
                                color="primary"
                                size="small"
                                disabled={isOutOfStock}
                                onClick={() => handleAdminRedirect(product)}
                                sx={{ ...actionButtonSx, mt: 1 }}
                              >
                                {isOutOfStock
                                  ? "Stokta olmadığı için pasif"
                                  : "Admin panelinden kontrol"}
                              </Button>
                            </Stack>
                          </Paper>
                        );
                      })}
                    </Stack>
                  )}
                </Box>
              ) : (
                <TableContainer
                  component={Paper}
                  sx={{
                    maxHeight: 600,
                    borderRadius: 2,
                    "&::-webkit-scrollbar": {
                      height: 6,
                      width: 6,
                    },
                    "&::-webkit-scrollbar-thumb": {
                      backgroundColor: "#cbd5f5",
                      borderRadius: 999,
                    },
                  }}
                >
                  <Table stickyHeader size="small">
                    <TableHead>
                      <TableRow>
                        {[
                          { label: "SKU", align: "left" },
                          { label: "Ürün Adı", align: "left" },
                          { label: "Mevcut Stok", align: "center" },
                          { label: "Birim Fiyat", align: "right" },
                          { label: "Durum", align: "center" },
                          { label: "Toplam Değer", align: "right" },
                          { label: "İşlem", align: "center" },
                        ].map((col) => (
                          <TableCell
                            key={col.label}
                            align={col.align as any}
                            sx={{
                              fontWeight: 700,
                              fontSize: "0.85rem",
                              px: 2,
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
                          <TableCell colSpan={7} align="center">
                            <Typography color="textSecondary">
                              Ürün bulunamadı
                            </Typography>
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredProducts.map((product) => {
                          const status = getStockStatus(product.stock);
                          const totalValue = product.stock * product.price;
                          const isOutOfStock = product.stock === 0;
                          const isLowStock =
                            product.stock > 0 &&
                            product.stock <= lowStockThreshold;

                          return (
                            <TableRow
                              key={product.id}
                              hover
                              sx={{
                                bgcolor:
                                  isOutOfStock
                                    ? "rgba(244, 67, 54, 0.05)"
                                    : isLowStock
                                    ? "rgba(255, 152, 0, 0.05)"
                                    : "inherit",
                              }}
                            >
                              <TableCell>
                                <Typography variant="body2" fontWeight="bold">
                                  {product.sku}
                                </Typography>
                              </TableCell>
                              <TableCell>
                                <Typography
                                  variant="body2"
                                  sx={{ fontSize: "0.9rem" }}
                                >
                                  {product.name}
                                </Typography>
                              </TableCell>
                              <TableCell align="center">
                                <Stack alignItems="center" spacing={0.5}>
                                  <Typography
                                    variant="h6"
                                    fontWeight="bold"
                                    color={
                                      isOutOfStock
                                        ? "error.main"
                                        : isLowStock
                                        ? "warning.main"
                                        : "success.main"
                                    }
                                  >
                                    {product.stock}
                                  </Typography>
                                  {isOutOfStock && (
                                    <Chip
                                      label="Stokta Yok"
                                      color="error"
                                      size="small"
                                      variant="outlined"
                                      sx={{
                                        fontSize: "0.65rem",
                                        fontWeight: 700,
                                      }}
                                    />
                                  )}
                                </Stack>
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
                                    isOutOfStock
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
                              <TableCell align="center">
                                <Button
                                  variant="contained"
                                  color="primary"
                                  size="small"
                                  disabled={isOutOfStock}
                                  sx={actionButtonSx}
                                  onClick={() => handleAdminRedirect(product)}
                                >
                                  {isOutOfStock
                                    ? "Stokta olmadığı için pasif"
                                    : "Admin panelinden kontrol"}
                                </Button>
                              </TableCell>
                            </TableRow>
                          );
                        })
                      )}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </>
          )}

          <Box
            sx={{
              mt: 2,
              p: { xs: 1.5, md: 2 },
              bgcolor: "action.hover",
              borderRadius: 2,
            }}
          >
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ fontSize: { xs: "0.7rem", sm: "0.75rem" } }}
            >
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
