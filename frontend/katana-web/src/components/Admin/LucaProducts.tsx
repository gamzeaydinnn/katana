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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import RefreshIcon from "@mui/icons-material/Refresh";
import InventoryIcon from "@mui/icons-material/Inventory";
import EditIcon from "@mui/icons-material/Edit";
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
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<LucaProduct | null>(
    null
  );
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
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
      setProducts([]);
      setFilteredProducts([]);
    } finally {
      setLoading(false);
    }
  };

  const handleEditProduct = (product: LucaProduct) => {
    setSelectedProduct(product);
    setEditModalOpen(true);
  };

  const handleCloseModal = () => {
    setEditModalOpen(false);
    setSelectedProduct(null);
  };

  const handleSaveProduct = async () => {
    if (!selectedProduct) return;
    setSaving(true);
    setError(null);

    try {
      const productId = selectedProduct.id;
      const updateDto = {
        Name: selectedProduct.productName || selectedProduct.ProductName || "",
        ProductCode:
          selectedProduct.productCode || selectedProduct.ProductCode || "",
        UnitPrice: selectedProduct.unitPrice ?? selectedProduct.UnitPrice ?? 0,
        Quantity: selectedProduct.quantity ?? selectedProduct.Quantity ?? 0,
      };

      await api.put(`/Products/luca/${productId}`, updateDto);
      setSuccessMessage("Luca ürünü güncellendi!");
      setTimeout(() => setSuccessMessage(null), 3000);
      handleCloseModal();
      fetchProducts();
    } catch (err: any) {
      setError(err.response?.data?.error || "Ürün güncellenemedi");
    } finally {
      setSaving(false);
    }
  };

  const handleProductChange = (field: keyof LucaProduct, value: any) => {
    if (!selectedProduct) return;
    setSelectedProduct({ ...selectedProduct, [field]: value });
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
                <TableCell align="center">
                  <strong>İşlemler</strong>
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
                      <TableCell align="center">
                        <Tooltip title="Düzenle">
                          <IconButton
                            size="small"
                            onClick={() => handleEditProduct(product)}
                            color="primary"
                          >
                            <EditIcon fontSize="small" />
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

      {/* Edit Dialog */}
      <Dialog
        open={editModalOpen}
        onClose={handleCloseModal}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Ürünü Düzenle</DialogTitle>
        <DialogContent sx={{ pt: 3 }}>
          {selectedProduct && (
            <Stack spacing={3}>
              <TextField
                fullWidth
                label="Ürün Kodu"
                value={
                  selectedProduct.productCode ||
                  selectedProduct.ProductCode ||
                  ""
                }
                disabled
                size="small"
              />
              <TextField
                fullWidth
                label="Ürün Adı"
                value={
                  selectedProduct.productName ||
                  selectedProduct.ProductName ||
                  ""
                }
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    productName: e.target.value,
                  })
                }
                size="small"
              />
              <TextField
                fullWidth
                label="Birim"
                value={selectedProduct.unit || selectedProduct.Unit || ""}
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    unit: e.target.value,
                  })
                }
                size="small"
              />
              <TextField
                fullWidth
                label="Miktar"
                type="number"
                value={
                  selectedProduct.quantity ?? selectedProduct.Quantity ?? 0
                }
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    quantity: parseInt(e.target.value),
                  })
                }
                size="small"
              />
              <TextField
                fullWidth
                label="Birim Fiyat (₺)"
                type="number"
                inputProps={{ step: "0.01" }}
                value={
                  selectedProduct.unitPrice ?? selectedProduct.UnitPrice ?? 0
                }
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    unitPrice: parseFloat(e.target.value),
                  })
                }
                size="small"
              />
              <TextField
                fullWidth
                label="KDV Oranı (%)"
                type="number"
                value={selectedProduct.vatRate ?? selectedProduct.VatRate ?? 0}
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    vatRate: parseInt(e.target.value),
                  })
                }
                size="small"
              />
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={handleCloseModal}
            variant="outlined"
            sx={{
              fontWeight: 600,
              borderColor: "#64748b",
              color: "#64748b",
              "&:hover": {
                borderColor: "#475569",
                backgroundColor: "rgba(100, 116, 139, 0.04)",
              },
            }}
          >
            İptal
          </Button>
          <Button
            onClick={handleSaveProduct}
            variant="contained"
            disabled={saving}
            sx={{
              fontWeight: 600,
              color: "white",
              backgroundColor: "#3b82f6",
              "&:hover": {
                backgroundColor: "#2563eb",
              },
            }}
          >
            {saving ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default LucaProducts;
