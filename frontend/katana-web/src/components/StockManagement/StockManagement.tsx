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
  useMediaQuery,
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
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const isMobile = useMediaQuery("(max-width:900px)");

  const loadProducts = async () => {
    try {
      setLoading(true);
      setError("");
      const res: any = await stockAPI.getKatanaProducts();
      // Normalize possible response shapes: { products: [...] } or { data: [...] } or plain array
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
      sx={{ mt: { xs: 7.5, md: 4 }, mb: { xs: 2.5, md: 4 }, px: { xs: 1.5, sm: 2, md: 0 } }}
    >
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", sm: "center" },
          flexDirection: { xs: "column", sm: "row" },
          gap: { xs: 1.5, sm: 0 },
          mb: 3,
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Inventory sx={{ fontSize: 32, color: "primary.main" }} />
          <Typography
            variant="h4"
            sx={{
              fontSize: { xs: "1.6rem", md: "2rem" },
              fontWeight: 800,
              letterSpacing: "-0.02em",
              background: (t) =>
                `linear-gradient(90deg, ${t.palette.primary.main}, ${t.palette.secondary.main})`,
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
            }}
          >
            Stok Yönetimi
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Refresh />}
          onClick={loadProducts}
          disabled={loading}
          sx={{ width: { xs: "100%", sm: "auto" } }}
        >
          Yenile
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Paper elevation={0} sx={{ p: { xs: 2, md: 3 } }}>
        <TextField
          fullWidth
          placeholder="Ürün ara (isim veya SKU)..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ mb: { xs: 2, md: 3 } }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search />
              </InputAdornment>
            ),
          }}
        />

        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Toplam {filteredProducts.length} ürün gösteriliyor
            </Typography>

            {isMobile ? (
              <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
                {filteredProducts.length === 0 ? (
                  <Paper
                    variant="outlined"
                    sx={{
                      p: 2,
                      borderRadius: 2,
                      textAlign: "center",
                      backgroundColor: "background.default",
                    }}
                  >
                    <Typography color="text.secondary">
                      Ürün bulunamadı
                    </Typography>
                  </Paper>
                ) : (
                  filteredProducts.map((product, idx) => {
                    const status = getStockStatus(product.stock);
                    const updatedAtText = product.createdAt
                      ? new Date(product.createdAt).toLocaleDateString("tr-TR")
                      : "-";
                    const key =
                      product.id && product.id !== ""
                        ? product.id
                        : product.sku && product.sku !== ""
                        ? product.sku
                        : `product-${idx}`;

                    return (
                      <Box
                        key={key}
                        sx={{
                          border: "1px solid",
                          borderColor: "divider",
                          borderRadius: 2,
                          p: 1.5,
                          backgroundColor: "background.paper",
                          boxShadow: (t) => t.shadows[1],
                        }}
                      >
                        <Typography
                          variant="subtitle1"
                          fontWeight={600}
                          sx={{
                            wordBreak: "break-word",
                            fontSize: "1rem",
                          }}
                        >
                          {product.name || "İsimsiz Ürün"}
                        </Typography>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{ mt: 0.25 }}
                        >
                          SKU: <strong>{product.sku || "-"}</strong>
                        </Typography>

                        <Box
                          sx={{
                            display: "flex",
                            flexWrap: "wrap",
                            gap: 1,
                            mt: 1,
                          }}
                        >
                          <Chip
                            label={status.label}
                            color={status.color}
                            size="small"
                            sx={{ fontSize: "0.75rem" }}
                          />
                          <Chip
                            label={product.isActive ? "Aktif" : "Pasif"}
                            color={product.isActive ? "success" : "default"}
                            size="small"
                            variant={product.isActive ? "filled" : "outlined"}
                          />
                        </Box>

                        <Box
                          sx={{
                            display: "grid",
                            gridTemplateColumns:
                              "repeat(auto-fit, minmax(140px, 1fr))",
                            rowGap: 1.25,
                            columnGap: 1,
                            mt: 1.5,
                          }}
                        >
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Stok
                            </Typography>
                            <Typography fontWeight={700}>
                              {product.stock}
                            </Typography>
                          </Box>
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Son Güncelleme
                            </Typography>
                            <Typography fontWeight={500}>
                              {updatedAtText}
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    );
                  })
                )}
              </Box>
            ) : (
              <TableContainer
                sx={{
                  overflowX: "auto",
                  borderRadius: 2,
                  "&::-webkit-scrollbar": { height: 6 },
                  "&::-webkit-scrollbar-thumb": {
                    backgroundColor: "#cbd5f5",
                    borderRadius: 3,
                  },
                }}
              >
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 700, fontSize: "0.9rem" }}>
                        SKU
                      </TableCell>
                      <TableCell sx={{ fontWeight: 700, fontSize: "0.9rem" }}>
                        Ürün Adı
                      </TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700, fontSize: "0.9rem" }}>
                        Stok
                      </TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700, fontSize: "0.9rem" }}>
                        Durum
                      </TableCell>
                      <TableCell align="center" sx={{ fontWeight: 700, fontSize: "0.9rem" }}>
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
