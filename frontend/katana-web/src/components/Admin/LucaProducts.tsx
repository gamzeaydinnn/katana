import EditIcon from "@mui/icons-material/Edit";
import InventoryIcon from "@mui/icons-material/Inventory";
import RefreshIcon from "@mui/icons-material/Refresh";
import SearchIcon from "@mui/icons-material/Search";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api, { stockAPI } from "../../services/api";
import { decodeJwtPayload, getJwtRoles } from "../../utils/jwt";

interface LucaProduct {
  id: string | number | null;
  productCode?: string;
  productName?: string;
  barcode?: string;
  category?: string;
  measurementUnit?: string;
  unit?: string;
  quantity?: number;
  unitPrice?: number;
  vatRate?: number;
  lastUpdated?: string;
  isActive?: boolean;

  ProductCode?: string;
  ProductName?: string;
  Barkod?: string;
  KategoriAgacKod?: string;
  OlcumBirimi?: string;
  Unit?: string;
  Quantity?: number;
  UnitPrice?: number;
  VatRate?: number;
  LastUpdated?: string;
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
  const [searchTerm, setSearchTerm] = useState("");
  const [syncing, setSyncing] = useState(false);
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      // AdminPanel products endpoint'ini kullan
      const response = await api.get<any>(
        "/adminpanel/products?page=1&pageSize=2000"
      );

      // API yanıtı: {products: Array, total: number} formatında
      const rawData = response?.data?.data || response?.data || {};
      const productData =
        rawData?.products ||
        rawData?.data ||
        (Array.isArray(rawData) ? rawData : []);

      // Katana ürünlerini Luca formatına dönüştür
      const mappedProducts = Array.isArray(productData)
        ? productData.map((p: any, index: number) => ({
            id: p.id || p.Id || index,
            productCode: p.sku || p.Sku || p.productCode || p.ProductCode || "",
            productName:
              p.name || p.Name || p.productName || p.ProductName || "",
            category: p.categoryName || p.CategoryName || p.category || "",
            measurementUnit: p.uom || p.Uom || p.measurementUnit || "adet",
            isActive: p.isActive ?? p.IsActive ?? true,
            // React key için unique identifier
            _uniqueKey: `${p.id || p.Id || index}_${p.sku || p.Sku || index}`,
          }))
        : [];

      // Duplicate SKU'ları filtrele - sadece ilkini tut
      const seenSkus = new Set<string>();
      const uniqueProducts = mappedProducts.filter((p: any) => {
        const sku = p.productCode?.toLowerCase() || "";
        if (!sku || seenSkus.has(sku)) {
          return false;
        }
        seenSkus.add(sku);
        return true;
      });

      console.log(
        `[LucaProducts] ${uniqueProducts.length} unique ürün yüklendi (toplam: ${mappedProducts.length})`
      );
      setProducts(uniqueProducts);
      setFilteredProducts(uniqueProducts);
    } catch (err: any) {
      console.error("[LucaProducts] İstek başarısız", err);

      // Fallback: stockAPI.getLucaStockCards dene
      try {
        const data: any = await stockAPI.getLucaStockCards();
        const productData = data?.data || data || [];
        setProducts(Array.isArray(productData) ? productData : []);
        setFilteredProducts(Array.isArray(productData) ? productData : []);
      } catch (fallbackErr: any) {
        const finalMessage =
          fallbackErr?.response?.data?.error ||
          err?.response?.data?.error ||
          err?.message ||
          "Ürünler yüklenemedi.";
        setError(finalMessage);
        setProducts([]);
        setFilteredProducts([]);
      }
    } finally {
      setLoading(false);
    }
  };

  const syncFromKoza = async () => {
    setSyncing(true);
    setError(null);
    try {
      await stockAPI.startSync();
      await fetchProducts();
    } catch (err: any) {
      const finalMessage =
        err?.response?.data?.error || err?.message || "Sync failed";
      setError(finalMessage);
      console.error("[LucaProducts] Sync failed", err);
    } finally {
      setSyncing(false);
    }
  };

  const handleEditProduct = (product: LucaProduct) => {
    setSelectedProduct(product);
    setEditModalOpen(true);
  };

  const _token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;
  const _roles = getJwtRoles(decodeJwtPayload(_token));
  const canEdit = _roles.includes("admin") || _roles.includes("stokyonetici");

  const handleCloseModal = () => {
    setEditModalOpen(false);
    setSelectedProduct(null);
  };

  const handleSaveProduct = async () => {
    if (!selectedProduct) return;
    setSaving(true);
    setError(null);

    try {
      const productCode =
        selectedProduct.productCode || selectedProduct.ProductCode || "";

      if (!productCode) {
        setError("Ürün kodu bulunamadı.");
        setSaving(false);
        return;
      }

      // Önce ürünün local DB'deki ID'sini bul
      let productId = selectedProduct.id;

      // Eğer ID sayısal değilse veya yoksa, SKU ile ara
      if (!productId || typeof productId !== "number") {
        try {
          const localResp: any = await api.get(
            `/Products/by-sku/${encodeURIComponent(productCode)}`
          );
          productId = localResp?.data?.id || localResp?.data?.Id;
        } catch (resolveErr) {
          console.warn("Local product lookup by SKU failed", resolveErr);
        }
      }

      if (!productId) {
        setError(
          "Güncellenecek yerel ürün ID'si bulunamadı. Bu ürün henüz veritabanında olmayabilir."
        );
        setSaving(false);
        return;
      }

      const updateDto = {
        productCode: productCode,
        productName:
          selectedProduct.productName || selectedProduct.ProductName || "",
        unit:
          selectedProduct.unit ||
          selectedProduct.Unit ||
          selectedProduct.measurementUnit ||
          "Adet",
        quantity: Number(
          selectedProduct.quantity ?? selectedProduct.Quantity ?? 0
        ),
        unitPrice: Number(
          selectedProduct.unitPrice ?? selectedProduct.UnitPrice ?? 0
        ),
        vatRate: Number(
          selectedProduct.vatRate ?? selectedProduct.VatRate ?? 20
        ),
      };

      await api.put(`/Products/luca/${productId}`, updateDto);
      handleCloseModal();
      fetchProducts();
    } catch (err: any) {
      let errorMsg = "Ürün güncellenemedi";
      const data = err?.response?.data;
      if (data) {
        if (typeof data === "string") errorMsg = data;
        else if (data.error) errorMsg = data.error;
        else if (data.title) errorMsg = data.title;
        else if (Array.isArray(data.errors)) errorMsg = data.errors.join(", ");
        else if (typeof data.errors === "object") {
          const msgs = Object.values(data.errors).flat();
          errorMsg = msgs.join(", ");
        }
      } else {
        errorMsg = err?.message || errorMsg;
      }
      setError(errorMsg);
      console.error("Ürün güncelleme hatası:", err?.response?.data || err);
    } finally {
      setSaving(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredProducts(products);
    } else {
      const term = searchTerm.toLowerCase();
      const filtered = products.filter((p) => {
        const name = (p.productName || p.ProductName || "").toLowerCase();
        const code = (p.productCode || p.ProductCode || "").toLowerCase();
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
              <span>
                <IconButton onClick={fetchProducts} disabled={loading}>
                  <RefreshIcon />
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip title="Koza ile Senkronize Et">
              <span>
                <Button
                  variant="contained"
                  size="small"
                  onClick={syncFromKoza}
                  disabled={syncing || loading}
                  startIcon={
                    syncing ? (
                      <CircularProgress size={16} color="inherit" />
                    ) : (
                      <RefreshIcon />
                    )
                  }
                >
                  {syncing ? "Senkronize ediliyor..." : "Koza'dan Yenile"}
                </Button>
              </span>
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
      ) : isMobile ? (
        <Stack spacing={1.5}>
          {filteredProducts.length === 0 && (
            <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
              {searchTerm
                ? "Arama sonucu bulunamadı"
                : "Luca ürün verisi yok veya endpoint aktif değil"}
            </Typography>
          )}
          {filteredProducts.map((product, _idx) => {
            const code = product.productCode || product.ProductCode || "";
            const name = product.productName || product.ProductName || "";
            const unit =
              product.unit ||
              product.Unit ||
              product.measurementUnit ||
              product.OlcumBirimi ||
              "";
            const barcode = product.barcode || product.Barkod || "";
            const category = product.category || product.KategoriAgacKod || "";
            const lastUpdated =
              product.lastUpdated || product.LastUpdated || "";
            const quantity = product.quantity ?? product.Quantity ?? 0;
            const unitPrice = product.unitPrice ?? product.UnitPrice ?? 0;
            const vatRate = product.vatRate ?? product.VatRate ?? 0;
            const isActive = product.isActive ?? product.IsActive ?? true;

            return (
              <Paper
                key={`mobile-${product.id}-${_idx}`}
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  border: "1px solid",
                  borderColor: "divider",
                  mx: 1,
                  boxSizing: "border-box",
                }}
              >
                <Box
                  sx={{
                    display: "flex",
                    justifyContent: "space-between",
                    gap: 1,
                  }}
                >
                  <Box>
                    <Typography variant="subtitle1" fontWeight={600}>
                      {name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Kod: <strong>{code}</strong>
                    </Typography>
                    {barcode && (
                      <Typography variant="body2" color="text.secondary">
                        Barkod: {barcode}
                      </Typography>
                    )}
                    {category && (
                      <Chip
                        label={category}
                        size="small"
                        variant="outlined"
                        sx={{ mt: 0.5 }}
                      />
                    )}
                    {lastUpdated && (
                      <Typography variant="caption" color="text.secondary">
                        Güncelleme: {lastUpdated}
                      </Typography>
                    )}
                  </Box>
                  <Chip
                    label={isActive ? "Aktif" : "Pasif"}
                    color={isActive ? "success" : "default"}
                    size="small"
                  />
                </Box>
                <Box
                  sx={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(140px, 1fr))",
                    columnGap: 1,
                    rowGap: 1,
                    mt: 1.25,
                  }}
                >
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Birim
                    </Typography>
                    <Typography fontWeight={600}>{unit || "-"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Miktar
                    </Typography>
                    <Typography fontWeight={600}>{quantity}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Birim Fiyat
                    </Typography>
                    <Typography fontWeight={600}>
                      {unitPrice ? `${unitPrice.toFixed(2)} ₺` : "-"}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      KDV
                    </Typography>
                    <Typography fontWeight={600}>%{vatRate}</Typography>
                  </Box>
                </Box>
                {canEdit && (
                  <Box
                    sx={{ display: "flex", justifyContent: "flex-end", mt: 1 }}
                  >
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<EditIcon fontSize="small" />}
                      onClick={() => handleEditProduct(product)}
                    >
                      Düzenle
                    </Button>
                  </Box>
                )}
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
                  <strong>Ürün Kodu</strong>
                </TableCell>
                <TableCell>
                  <strong>Ürün Adı</strong>
                </TableCell>
                <TableCell>
                  <strong>Barkod</strong>
                </TableCell>
                <TableCell>
                  <strong>Kategori</strong>
                </TableCell>
                <TableCell>
                  <strong>Ölçü Birimi</strong>
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
                <TableCell>
                  <strong>Son Güncelleme</strong>
                </TableCell>
                <TableCell align="center">
                  <strong>İşlemler</strong>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredProducts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={11} align="center">
                    <Typography color="textSecondary">
                      {searchTerm
                        ? "Arama sonucu bulunamadı"
                        : "Luca ürün verisi yok veya endpoint aktif değil"}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                filteredProducts.map((product, _idx) => {
                  const code = product.productCode || product.ProductCode || "";
                  const name = product.productName || product.ProductName || "";
                  const unit =
                    product.unit ||
                    product.Unit ||
                    product.measurementUnit ||
                    product.OlcumBirimi ||
                    "";
                  const barcode = product.barcode || product.Barkod || "";
                  const category =
                    product.category || product.KategoriAgacKod || "";
                  const lastUpdated =
                    product.lastUpdated || product.LastUpdated || "";
                  const quantity = product.quantity ?? product.Quantity ?? 0;
                  const unitPrice = product.unitPrice ?? product.UnitPrice ?? 0;
                  const vatRate = product.vatRate ?? product.VatRate ?? 0;
                  const isActive = product.isActive ?? product.IsActive ?? true;

                  return (
                    <TableRow key={`desktop-${product.id}-${_idx}`} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight="bold">
                          {code}
                        </Typography>
                      </TableCell>
                      <TableCell>{name}</TableCell>
                      <TableCell>{barcode || "-"}</TableCell>
                      <TableCell>{category || "-"}</TableCell>
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
                      <TableCell>{lastUpdated || "-"}</TableCell>
                      <TableCell align="center">
                        {canEdit ? (
                          <Tooltip title="Düzenle">
                            <IconButton
                              size="small"
                              onClick={() => handleEditProduct(product)}
                              color="primary"
                            >
                              <EditIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog
        open={editModalOpen}
        onClose={handleCloseModal}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle sx={{ pb: 2 }}>Ürünü Düzenle</DialogTitle>
        <DialogContent dividers sx={{ pt: 2 }}>
          {selectedProduct && (
            <Stack spacing={2.5}>
              <TextField
                fullWidth
                label="Ürün Kodu"
                value={
                  selectedProduct.productCode ||
                  selectedProduct.ProductCode ||
                  ""
                }
                onChange={(e) =>
                  setSelectedProduct({
                    ...selectedProduct,
                    productCode: e.target.value,
                  })
                }
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
                    quantity: parseInt(e.target.value, 10),
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
                    vatRate: parseInt(e.target.value, 10),
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
