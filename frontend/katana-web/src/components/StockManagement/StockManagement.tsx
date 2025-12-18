import React, { useState, useEffect } from "react";
import {
  Container,
  Box,
  Paper,
  Typography,
  Button,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Alert,
  CircularProgress,
  InputAdornment,
  Stack,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { Search, Refresh, Inventory } from "@mui/icons-material";
import { stockAPI } from "../../services/api";

interface Product {
  id: string;
  sku: string;
  name: string;
  stock: number;
  isActive: boolean;
  createdAt: string;
}

const StockManagement: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");

  const loadProducts = async () => {
    try {
      setLoading(true);
      setError("");
      const res: any = await stockAPI.getKatanaProducts();
      const raw = res?.products ?? res?.data ?? res ?? [];
      const normalized = (Array.isArray(raw) ? raw : []) as any[];

      setProducts(
        normalized.map((p) => ({
          id: String(p.id ?? p.sku ?? p.SKU ?? ""),
          sku: String(p.sku ?? p.SKU ?? ""),
          name: String(p.name ?? p.Name ?? ""),
          stock: Number(p.stock ?? p.stockQuantity ?? p.quantity ?? 0),
          isActive: Boolean(p.isActive ?? p.IsActive ?? true),
          createdAt: String(
            p.createdAt ?? p.createdAt ?? new Date().toISOString()
          ),
        })) as Product[]
      );
    } catch (err: any) {
      setError(err.message || "Ürünler yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProducts();
  }, []);

  const filteredProducts = products.filter(
    (p) =>
      p.name?.toLowerCase().includes(search.toLowerCase()) ||
      p.sku?.toLowerCase().includes(search.toLowerCase())
  );

  const getStockStatus = (stock: number) => {
    if (stock === 0) return { label: "Stokta Yok", color: "error" as const };
    if (stock < 10) return { label: "Düşük", color: "warning" as const };
    return { label: "Normal", color: "success" as const };
  };

  return (
    <Container
      maxWidth="lg"
      sx={{
        mt: { xs: 2, sm: 3, md: 4 },
        mb: { xs: 2, sm: 3, md: 4 },
        px: { xs: 1, sm: 2, md: 3 },
      }}
    >
      <Stack
        direction={{ xs: "column", sm: "row" }}
        justifyContent="space-between"
        alignItems={{ xs: "flex-start", sm: "center" }}
        spacing={{ xs: 1.5, sm: 2 }}
        sx={{ mb: { xs: 2, sm: 3 } }}
      >
        <Stack direction="row" alignItems="center" spacing={{ xs: 1, sm: 2 }}>
          <Inventory
            sx={{ fontSize: { xs: 26, sm: 32 }, color: "primary.main" }}
          />
          <Typography
            variant="h4"
            sx={{
              fontWeight: 800,
              letterSpacing: "-0.02em",
              fontSize: { xs: "1.15rem", sm: "1.5rem", md: "2rem" },
              background: (t) =>
                `linear-gradient(90deg, ${t.palette.primary.main}, ${t.palette.secondary.main})`,
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            Stok Yönetimi
          </Typography>
        </Stack>
        <Button
          variant="contained"
          startIcon={<Refresh fontSize="small" />}
          onClick={loadProducts}
          disabled={loading}
          size="small"
          sx={{
            fontWeight: 600,
            alignSelf: { xs: "flex-end", sm: "center" },
            fontSize: { xs: "0.75rem", sm: "0.875rem" },
          }}
        >
          Yenile
        </Button>
      </Stack>

      {error && (
        <Alert
          severity="error"
          sx={{
            mb: { xs: 1.5, sm: 3 },
            fontSize: { xs: "0.7rem", sm: "0.875rem" },
          }}
        >
          {error}
        </Alert>
      )}

      <Paper
        elevation={0}
        sx={{
          p: { xs: 0.75, sm: 2, md: 3 },
          borderRadius: { xs: 2, md: 3 },
          overflow: "hidden",
        }}
      >
        <TextField
          fullWidth
          placeholder="Ürün ara..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          size="small"
          sx={{ mb: { xs: 1, sm: 2 } }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search fontSize="small" />
              </InputAdornment>
            ),
          }}
        />

        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
            <CircularProgress size={isMobile ? 32 : 40} />
          </Box>
        ) : (
          <>
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                mb: { xs: 1, sm: 1.5 },
                fontSize: { xs: "0.7rem", sm: "0.875rem" },
                px: { xs: 0.5, sm: 0 },
              }}
            >
              {filteredProducts.length} ürün
            </Typography>

            {isMobile ? (
              <Box sx={{ mx: -0.75 }}>
                <Stack spacing={0.5}>
                  {filteredProducts.length === 0 ? (
                    <Box py={2} textAlign="center">
                      <Typography color="text.secondary" fontSize="0.75rem">
                        Ürün bulunamadı
                      </Typography>
                    </Box>
                  ) : (
                    filteredProducts.map((product, idx) => {
                      const status = getStockStatus(product.stock);
                      return (
                        <Paper
                          key={
                            product.id && product.id !== ""
                              ? product.id
                              : product.sku && product.sku !== ""
                              ? product.sku
                              : `product-${idx}`
                          }
                          variant="outlined"
                          sx={{
                            p: 0.75,
                            borderRadius: 1,
                            mx: 0.75,
                          }}
                        >
                          <Stack spacing={0.5}>
                            <Stack
                              direction="row"
                              justifyContent="space-between"
                              alignItems="center"
                              spacing={0.5}
                            >
                              <Box sx={{ flex: 1, minWidth: 0, pr: 0.5 }}>
                                <Typography
                                  variant="body2"
                                  fontWeight="700"
                                  sx={{
                                    fontSize: "0.75rem",
                                    whiteSpace: "nowrap",
                                    overflow: "hidden",
                                    textOverflow: "ellipsis",
                                    lineHeight: 1.3,
                                  }}
                                >
                                  {product.sku}
                                </Typography>
                                <Typography
                                  variant="body2"
                                  sx={{
                                    fontSize: "0.65rem",
                                    color: "text.secondary",
                                    whiteSpace: "nowrap",
                                    overflow: "hidden",
                                    textOverflow: "ellipsis",
                                    lineHeight: 1.3,
                                    mt: 0.25,
                                  }}
                                >
                                  {product.name}
                                </Typography>
                              </Box>
                              <Chip
                                label={product.isActive ? "✓" : "✕"}
                                color={product.isActive ? "success" : "default"}
                                size="small"
                                variant="outlined"
                                sx={{
                                  fontSize: "0.65rem",
                                  height: 18,
                                  minWidth: 24,
                                  flexShrink: 0,
                                  "& .MuiChip-label": {
                                    px: 0.5,
                                  },
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
                                  sx={{
                                    fontSize: "0.6rem",
                                    lineHeight: 1.2,
                                  }}
                                >
                                  Stok
                                </Typography>
                                <Typography
                                  variant="h6"
                                  fontWeight="700"
                                  sx={{
                                    fontSize: "0.9rem",
                                    lineHeight: 1.2,
                                  }}
                                >
                                  {product.stock}
                                </Typography>
                              </Box>
                              <Chip
                                label={status.label}
                                color={status.color}
                                size="small"
                                sx={{
                                  fontSize: "0.6rem",
                                  height: 18,
                                  "& .MuiChip-label": {
                                    px: 0.75,
                                  },
                                }}
                              />
                            </Stack>
                          </Stack>
                        </Paper>
                      );
                    })
                  )}
                </Stack>
              </Box>
            ) : (
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 700 }}>SKU</TableCell>
                      <TableCell sx={{ fontWeight: 700 }}>Ürün Adı</TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700 }}>
                        Stok
                      </TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700 }}>
                        Durum
                      </TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700 }}>
                        Aktif
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {filteredProducts.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={5} align="center">
                          <Typography color="text.secondary">
                            Ürün bulunamadı
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      filteredProducts.map((product, idx) => {
                        const status = getStockStatus(product.stock);
                        return (
                          <TableRow
                            key={
                              product.id && product.id !== ""
                                ? product.id
                                : product.sku && product.sku !== ""
                                ? product.sku
                                : `product-${idx}`
                            }
                            hover
                            sx={{ transition: "background-color .15s ease" }}
                          >
                            <TableCell>{product.sku}</TableCell>
                            <TableCell>{product.name}</TableCell>
                            <TableCell align="center">
                              <strong>{product.stock}</strong>
                            </TableCell>
                            <TableCell align="center">
                              <Chip
                                label={status.label}
                                color={status.color}
                                size="small"
                              />
                            </TableCell>
                            <TableCell align="center">
                              <Chip
                                label={product.isActive ? "Aktif" : "Pasif"}
                                color={product.isActive ? "success" : "default"}
                                size="small"
                                variant="outlined"
                              />
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
      </Paper>
    </Container>
  );
};

export default StockManagement;
