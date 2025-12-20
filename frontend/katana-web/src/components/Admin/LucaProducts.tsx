import DeleteIcon from "@mui/icons-material/Delete";
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
import React, { useEffect, useState, useCallback, useMemo } from "react";
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
  const [deleting, setDeleting] = useState(false);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
  const [productToDelete, setProductToDelete] = useState<LucaProduct | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [syncing, setSyncing] = useState(false);
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      // Products endpoint'ini kullan (veritabanından direkt çeker)
      // Limit yok - tüm ürünleri çek
      const response = await api.get<any>("/Products?page=1&limit=10000");

      // API yanıtı: {items: Array, total: number} formatında
      const rawData = response?.data?.data || response?.data || {};
      const productData =
        rawData?.items ||
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

  const handleDeleteClick = (product: LucaProduct) => {
    setProductToDelete(product);
    setConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!productToDelete) return;
    setDeleting(true);
    setError(null);

    try {
      const sku = productToDelete.productCode || productToDelete.ProductCode;
      if (!sku) {
        setError("Silinecek ürün kodu bulunamadı.");
        setDeleting(false);
        return;
      }

      await api.post(`/adminpanel/test-delete-product?sku=${encodeURIComponent(sku)}`);
      setConfirmDeleteOpen(false);
      setProductToDelete(null);
      
      // Local state'den kaldır
      setProducts(prev => prev.filter(p => (p.productCode || p.ProductCode) !== sku));
      setFilteredProducts(prev => prev.filter(p => (p.productCode || p.ProductCode) !== sku));
    } catch (err: any) {
      setError(err?.response?.data?.message || "Ürün silinemedi");
    } finally {
      setDeleting(false);
    }
  };

  const handleSaveProduct = async () => {
    if (!selectedProduct) return;
    setSaving(true);
    setError(null);

    try {
      const productCode = selectedProduct.productCode || selectedProduct.ProductCode || "";
      if (!productCode) {
        setError("Ürün kodu bulunamadı.");
        setSaving(false);
        return;
      }

      const updateRequest = {
        kartKodu: productCode,
        kartAdi: selectedProduct.productName || selectedProduct.ProductName || "",
        kdvOrani: Number(selectedProduct.vatRate ?? selectedProduct.VatRate ?? 20),
      };

      await api.post("/adminpanel/test-update-product", updateRequest);
      handleCloseModal();
      
      // Local state güncelle
      const updated = { ...selectedProduct };
      setProducts(prev => prev.map(p => 
        (p.productCode || p.ProductCode) === productCode ? updated : p
      ));
      setFilteredProducts(prev => prev.map(p => 
        (p.productCode || p.ProductCode) === productCode ? updated : p
      ));
    } catch (err: any) {
      setError(err?.response?.data?.message || "Ürün güncellenemedi");
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
        <Box
          display="flex"
          flexDirection="column"
          alignItems="center"
          justifyContent="center"
          p={4}
          minHeight="300px"
        >
          <CircularProgress size={48} />
          <Typography variant="body1" color="text.secondary" sx={{ mt: 2 }}>
            Luca ürünleri yükleniyor...
          </Typography>
          <Typography variant="caption" color="text.disabled" sx={{ mt: 1 }}>
            Bu işlem birkaç saniye sürebilir
          </Typography>
        </Box>
      ) : isMobile ? (
        <Stack spacing={1.5}>
          {filteredProducts.length === 0 && (
            <Box textAlign="center" sx={{ py: 4 }}>
              <Typography color="text.secondary" gutterBottom>
                {searchTerm
                  ? "Arama sonucu bulunamadı"
                  : "Henüz ürün bulunamadı"}
              </Typography>
              {!searchTerm && (
                <Typography variant="caption" color="text.disabled">
                  Luca'dan ürün çekmek için "Koza'dan Çek" butonunu
                  kullanabilirsiniz
                </Typography>
              )}
            </Box>
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
                  <Stack direction="row" spacing={1} justifyContent="flex-end" mt={1}>
                    <Button size="small" variant="outlined" startIcon={<EditIcon fontSize="small" />} onClick={() => handleEditProduct(product)}>
                      Düzenle
                    </Button>
                    <Button size="small" variant="outlined" color="error" startIcon={<DeleteIcon fontSize="small" />} onClick={() => handleDeleteClick(product)}>
                      Sil
                    </Button>
                  </Stack>
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
                  <TableCell colSpan={11} align="center" sx={{ py: 4 }}>
                    <Typography color="textSecondary" gutterBottom>
                      {searchTerm
                        ? "Arama sonucu bulunamadı"
                        : "Henüz ürün bulunamadı"}
                    </Typography>
                    {!searchTerm && (
                      <Typography variant="caption" color="text.disabled">
                        Luca'dan ürün çekmek için "Koza'dan Çek" butonunu
                        kullanabilirsiniz
                      </Typography>
                    )}
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
                          <Stack direction="row" spacing={0.5} justifyContent="center">
                            <Tooltip title="Düzenle">
                              <IconButton size="small" onClick={() => handleEditProduct(product)} color="primary">
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Sil">
                              <IconButton size="small" onClick={() => handleDeleteClick(product)} color="error">
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </Stack>
                        ) : (
                          <Typography variant="body2" color="text.secondary">-</Typography>
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
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, productCode: e.target.value } as LucaProduct) : prev
                  )
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
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, productName: e.target.value } as LucaProduct) : prev
                  )
                }
                size="small"
              />
              <TextField
                fullWidth
                label="Birim"
                value={selectedProduct.unit || selectedProduct.Unit || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, unit: e.target.value } as LucaProduct) : prev
                  )
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
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, quantity: parseInt(e.target.value, 10) } as LucaProduct) : prev
                  )
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
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, unitPrice: parseFloat(e.target.value) } as LucaProduct) : prev
                  )
                }
                size="small"
              />
              <TextField
                fullWidth
                label="KDV Oranı (%)"
                type="number"
                value={selectedProduct.vatRate ?? selectedProduct.VatRate ?? 0}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? ({ ...prev, vatRate: parseInt(e.target.value, 10) } as LucaProduct) : prev
                  )
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

      {/* Silme Onay Dialog */}
      <Dialog open={confirmDeleteOpen} onClose={() => setConfirmDeleteOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Ürünü Sil</DialogTitle>
        <DialogContent>
          <Typography>
            <strong>{productToDelete?.productCode || productToDelete?.ProductCode}</strong> kodlu ürünü silmek istediğinize emin misiniz?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDeleteOpen(false)} variant="outlined" disabled={deleting}>İptal</Button>
          <Button onClick={handleConfirmDelete} variant="contained" color="error" disabled={deleting}>
            {deleting ? "Siliniyor..." : "Sil"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default LucaProducts;
