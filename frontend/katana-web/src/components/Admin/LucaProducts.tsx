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
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import RefreshIcon from "@mui/icons-material/Refresh";
import InventoryIcon from "@mui/icons-material/Inventory";
import api from "../../services/api";

interface LucaProduct {
  id: string;
  productCode: string;
  productName: string;
  unit?: string;
  quantity?: number;
  unitPrice?: number;
  vatRate?: number;
  isActive?: boolean;
  // Backend PascalCase alternatives
  ProductCode?: string;
  ProductName?: string;
  Unit?: string;
  Quantity?: number;
  UnitPrice?: number;
  VatRate?: number;
  IsActive?: boolean;
}

const LucaProducts: React.FC = () => {
  const [products, setProducts] = useState<LucaProduct[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<LucaProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      // Luca ürünleri endpoint'i eklendiğinde kullanılacak
      const response = await api.get("/Products/luca");
      const responseData: any = response.data;
      const productData = responseData?.data || responseData || [];
      setProducts(productData);
      setFilteredProducts(productData);
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
          "Luca ürünleri yüklenemedi. Endpoint henüz aktif olmayabilir."
      );
      console.error("Luca ürünleri yükleme hatası:", err);
      // Demo data için fallback
      setProducts([]);
      setFilteredProducts([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredProducts(products);
    } else {
      const filtered = products.filter((p) => {
        const name = (p.productName || p.ProductName || "").toLowerCase();
        const code = (p.productCode || p.ProductCode || "").toLowerCase();
        const term = searchTerm.toLowerCase();
        return name.includes(term) || code.includes(term);
      });
      setFilteredProducts(filtered);
    }
  }, [searchTerm, products]);

  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
            mb={2}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <InventoryIcon color="secondary" />
              <Typography variant="h5">Luca Ürünleri</Typography>
            </Stack>
            <Tooltip title="Yenile">
              <IconButton onClick={fetchProducts} disabled={loading}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Stack>

          <TextField
            fullWidth
            placeholder="Ürün kodu veya adı ara..."
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

          <Stack direction="row" spacing={2}>
            <Chip label={`Toplam: ${products.length}`} color="secondary" />
            <Chip label={`Görüntülenen: ${filteredProducts.length}`} />
          </Stack>
        </CardContent>
      </Card>

      {error && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {loading ? (
        <Box display="flex" justifyContent="center" p={4}>
          <CircularProgress />
        </Box>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>
                  <strong>Ürün Kodu</strong>
                </TableCell>
                <TableCell>
                  <strong>Ürün Adı</strong>
                </TableCell>
                <TableCell>
                  <strong>Birim</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Miktar</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Birim Fiyat</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>KDV Oranı</strong>
                </TableCell>
                <TableCell>
                  <strong>Durum</strong>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredProducts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    <Typography color="textSecondary">
                      {searchTerm
                        ? "Arama sonucu bulunamadı"
                        : "Luca ürün verisi yok veya endpoint aktif değil"}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                filteredProducts.map((product) => {
                  const code = product.productCode || product.ProductCode || "";
                  const name = product.productName || product.ProductName || "";
                  const unit = product.unit || product.Unit || "";
                  const quantity = product.quantity ?? product.Quantity ?? 0;
                  const unitPrice = product.unitPrice ?? product.UnitPrice ?? 0;
                  const vatRate = product.vatRate ?? product.VatRate ?? 0;
                  const isActive = product.isActive ?? product.IsActive ?? true;

                  return (
                    <TableRow key={product.id} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight="bold">
                          {code}
                        </Typography>
                      </TableCell>
                      <TableCell>{name}</TableCell>
                      <TableCell>{unit}</TableCell>
                      <TableCell align="right">{quantity}</TableCell>
                      <TableCell align="right">
                        {unitPrice ? `${unitPrice.toFixed(2)} ₺` : "-"}
                      </TableCell>
                      <TableCell align="right">%{vatRate}</TableCell>
                      <TableCell>
                        <Chip
                          label={isActive ? "Aktif" : "Pasif"}
                          color={isActive ? "success" : "default"}
                          size="small"
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
    </Box>
  );
};

export default LucaProducts;
