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
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", mb: 3 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Inventory sx={{ fontSize: 32, color: "primary.main" }} />
          <Typography variant="h4" fontWeight="bold">
            Stok Yönetimi
          </Typography>
        </Box>
        <Button
          variant="outlined"
          startIcon={<Refresh />}
          onClick={loadProducts}
          disabled={loading}
        >
          Yenile
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <TextField
          fullWidth
          placeholder="Ürün ara (isim veya SKU)..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ mb: 3 }}
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

            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>SKU</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Ürün Adı</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Stok</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Aktif</strong>
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
          </>
        )}
      </Paper>
    </Container>
  );
};

export default StockManagement;
