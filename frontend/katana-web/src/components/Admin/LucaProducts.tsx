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
  Snackbar,
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
  skartId?: number;
  productCode?: string;
  productName?: string;
  uzunAdi?: string;
  barcode?: string;
  category?: string;
  kategoriAgacKod?: string;
  measurementUnit?: string;
  unit?: string;
  quantity?: number;
  purchasePrice?: number;
  salesPrice?: number;
  unitPrice?: number;
  vatRate?: number;
  gtipCode?: string;
  lastUpdated?: string;
  isActive?: boolean;

  // Luca API field names (PascalCase)
  ProductCode?: string;
  ProductName?: string;
  UzunAdi?: string;
  Barkod?: string;
  KategoriAgacKod?: string;
  OlcumBirimi?: string;
  Unit?: string;
  Quantity?: number;
  PerakendeAlisBirimFiyat?: number;
  PerakendeSatisBirimFiyat?: number;
  UnitPrice?: number;
  VatRate?: number;
  GtipKodu?: string;
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
  const [productToDelete, setProductToDelete] = useState<LucaProduct | null>(
    null
  );
  const [searchTerm, setSearchTerm] = useState("");
  const [syncing, setSyncing] = useState(false);
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "warning" | "info";
  }>({ open: false, message: "", severity: "success" });
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      // Products endpoint'ini kullan (veritabanından çeker)
      const response = await api.get<any>("/Products?page=1&limit=10000");

      const rawData = response?.data?.data || response?.data || {};
      const productData =
        rawData?.items ||
        rawData?.products ||
        rawData?.data ||
        (Array.isArray(rawData) ? rawData : []);

      // Ürünleri frontend formatına dönüştür
      const mappedProducts = Array.isArray(productData)
        ? productData.map((p: any, index: number) => ({
            id: p.id || p.Id || index,
            skartId: p.lucaId || p.LucaId,
            productCode: p.sku || p.Sku || p.productCode || p.ProductCode || "",
            productName:
              p.name || p.Name || p.productName || p.ProductName || "",
            uzunAdi: p.uzunAdi || p.UzunAdi || p.description || "",
            barcode: p.barcode || p.Barcode || p.barkod || "",
            kategoriAgacKod: p.categoryCode || p.kategoriAgacKod || "",
            measurementUnit: p.uom || p.Uom || p.measurementUnit || "ADET",
            purchasePrice: p.purchasePrice || p.alisFiyat || 0,
            salesPrice: p.price || p.Price || p.satisFiyat || 0,
            gtipCode: p.gtipCode || p.gtipKodu || "",
            isActive: p.isActive ?? p.IsActive ?? true,
            _uniqueKey: `${p.id || p.Id || index}_${p.sku || p.Sku || index}`,
          }))
        : [];

      // Duplicate SKU'ları filtrele
      const seenSkus = new Set<string>();
      const uniqueProducts = mappedProducts.filter((p: any) => {
        const sku = p.productCode?.toLowerCase() || "";
        if (!sku || seenSkus.has(sku)) return false;
        seenSkus.add(sku);
        return true;
      });

      console.log(`[LucaProducts] ${uniqueProducts.length} ürün yüklendi`);
      setProducts(uniqueProducts);
      setFilteredProducts(uniqueProducts);
    } catch (err: any) {
      console.error("[LucaProducts] Ürünler yüklenemedi", err);
      const finalMessage =
        err?.response?.data?.error || err?.message || "Ürünler yüklenemedi.";
      setError(finalMessage);
      setProducts([]);
      setFilteredProducts([]);
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

      await api.post(
        `/adminpanel/test-delete-product?sku=${encodeURIComponent(sku)}`
      );
      setConfirmDeleteOpen(false);
      setProductToDelete(null);

      // Local state'den kaldır
      setProducts((prev) =>
        prev.filter((p) => (p.productCode || p.ProductCode) !== sku)
      );
      setFilteredProducts((prev) =>
        prev.filter((p) => (p.productCode || p.ProductCode) !== sku)
      );
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
      const productCode = selectedProduct.productCode || "";

      if (!productCode) {
        setError("Ürün kodu bulunamadı.");
        setSaving(false);
        return;
      }

      // 🔥 Luca'da güncellenebilir alanları gönder - kategoriAgacKod string olarak gönderilmeli
      const updateRequest = {
        name: selectedProduct.productName || "",
        uzunAdi: selectedProduct.uzunAdi || "",
        barcode: selectedProduct.barcode || "",
        kategoriAgacKod: String(selectedProduct.kategoriAgacKod || ""), // 🔥 String olarak gönder - baştaki sıfırları koru
        purchasePrice: selectedProduct.purchasePrice ?? 0,
        salesPrice: selectedProduct.salesPrice ?? 0,
        gtipCode: selectedProduct.gtipCode || "",
      };

      console.log("📤 Luca'ya gönderilen request:", updateRequest);

      // SKU ile güncelleme gönder (Local DB + Luca + Katana)
      const response = await api.put<{
        success?: boolean;
        localDbUpdated?: boolean;
        lucaUpdated?: boolean;
        katanaUpdated?: boolean;
        lucaError?: string;
        katanaError?: string;
        message?: string;
        updatedProduct?: {
          productCode?: string;
          productName?: string;
          uzunAdi?: string;
          barcode?: string;
          kategoriAgacKod?: string;
          purchasePrice?: number;
          salesPrice?: number;
          gtipCode?: string;
        };
      }>(
        `/products/by-sku/${encodeURIComponent(productCode)}/sync-to-luca`,
        updateRequest
      );

      if (response.data?.success) {
        // 🔥 KRİTİK: Local state'i güncelle - gönderilen request değerleriyle (backend'den dönen değil)
        // Products listesini güncelle
        setProducts((prev) =>
          prev.map((p) =>
            (p.productCode || p.ProductCode) === productCode
              ? {
                  ...p,
                  productName: updateRequest.name,
                  uzunAdi: updateRequest.uzunAdi,
                  barcode: updateRequest.barcode,
                  kategoriAgacKod: updateRequest.kategoriAgacKod,
                  purchasePrice: updateRequest.purchasePrice,
                  salesPrice: updateRequest.salesPrice,
                  gtipCode: updateRequest.gtipCode,
                }
              : p
          )
        );
        // Filtered products'ı da güncelle
        setFilteredProducts((prev) =>
          prev.map((p) =>
            (p.productCode || p.ProductCode) === productCode
              ? {
                  ...p,
                  productName: updateRequest.name,
                  uzunAdi: updateRequest.uzunAdi,
                  barcode: updateRequest.barcode,
                  kategoriAgacKod: updateRequest.kategoriAgacKod,
                  purchasePrice: updateRequest.purchasePrice,
                  salesPrice: updateRequest.salesPrice,
                  gtipCode: updateRequest.gtipCode,
                }
              : p
          )
        );

        handleCloseModal();

        // Sync durumlarını göster
        const lucaOk = response.data?.lucaUpdated;
        const katanaOk = response.data?.katanaUpdated;
        const localOk = response.data?.localDbUpdated;

        let statusParts: string[] = [];
        if (localOk) statusParts.push("Local DB ✓");
        if (lucaOk) statusParts.push("Luca ✓");
        if (katanaOk) statusParts.push("Katana ✓");

        const statusText =
          statusParts.length > 0
            ? statusParts.join(", ")
            : "Hiçbir sistem güncellenemedi";

        if (lucaOk && katanaOk) {
          setSnackbar({
            open: true,
            message: `✅ ${productCode} tüm sistemlerde güncellendi! (${statusText})`,
            severity: "success",
          });
        } else if (lucaOk || localOk) {
          setSnackbar({
            open: true,
            message: `⚠️ ${productCode} kısmen güncellendi. (${statusText})`,
            severity: "warning",
          });
        } else {
          setSnackbar({
            open: true,
            message: `❌ ${productCode} güncellenemedi!`,
            severity: "error",
          });
        }
      } else {
        setError(response.data?.message || "Ürün güncellenemedi");
        setSnackbar({
          open: true,
          message: response.data?.message || "Ürün güncellenemedi",
          severity: "error",
        });
      }
    } catch (err: any) {
      const errorMsg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        "Ürün güncellenemedi";
      setError(errorMsg);
      setSnackbar({
        open: true,
        message: errorMsg,
        severity: "error",
      });
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
                  <Stack
                    direction="row"
                    spacing={1}
                    justifyContent="flex-end"
                    mt={1}
                  >
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<EditIcon fontSize="small" />}
                      onClick={() => handleEditProduct(product)}
                    >
                      Düzenle
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      startIcon={<DeleteIcon fontSize="small" />}
                      onClick={() => handleDeleteClick(product)}
                    >
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
                  <strong>Uzun Adı</strong>
                </TableCell>
                <TableCell>
                  <strong>Barkod</strong>
                </TableCell>
                <TableCell>
                  <strong>Kategori Kodu</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Alış Fiyatı</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Satış Fiyatı</strong>
                </TableCell>
                <TableCell>
                  <strong>GTIP</strong>
                </TableCell>
                <TableCell align="center">
                  <strong>İşlemler</strong>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredProducts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9} align="center" sx={{ py: 4 }}>
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
                  const code = product.productCode || "";
                  const name = product.productName || "";
                  const uzunAdi = product.uzunAdi || "";
                  const barcode = product.barcode || "";
                  const kategoriKod = product.kategoriAgacKod || "";
                  const purchasePrice = product.purchasePrice ?? 0;
                  const salesPrice = product.salesPrice ?? 0;
                  const gtipCode = product.gtipCode || "";

                  return (
                    <TableRow key={`desktop-${product.id}-${_idx}`} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight="bold">
                          {code}
                        </Typography>
                      </TableCell>
                      <TableCell>{name}</TableCell>
                      <TableCell>{uzunAdi || "-"}</TableCell>
                      <TableCell>{barcode || "-"}</TableCell>
                      <TableCell>{kategoriKod || "-"}</TableCell>
                      <TableCell align="right">
                        {purchasePrice ? `${purchasePrice.toFixed(2)} ₺` : "-"}
                      </TableCell>
                      <TableCell align="right">
                        {salesPrice ? `${salesPrice.toFixed(2)} ₺` : "-"}
                      </TableCell>
                      <TableCell>{gtipCode || "-"}</TableCell>
                      <TableCell align="center">
                        {canEdit ? (
                          <Stack
                            direction="row"
                            spacing={0.5}
                            justifyContent="center"
                          >
                            <Tooltip title="Düzenle">
                              <IconButton
                                size="small"
                                onClick={() => handleEditProduct(product)}
                                color="primary"
                              >
                                <EditIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Sil">
                              <IconButton
                                size="small"
                                onClick={() => handleDeleteClick(product)}
                                color="error"
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </Stack>
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
              {/* ÜRÜN KODU - READ ONLY */}
              <TextField
                fullWidth
                label="Ürün Kodu (Değiştirilemez)"
                value={selectedProduct.productCode || ""}
                disabled
                size="small"
                sx={{ bgcolor: "grey.100" }}
              />

              {/* ÜRÜN ADI - kartAdi */}
              <TextField
                fullWidth
                label="Ürün Adı (kartAdi)"
                value={selectedProduct.productName || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, productName: e.target.value } : prev
                  )
                }
                size="small"
              />

              {/* UZUN ADI */}
              <TextField
                fullWidth
                label="Uzun Adı (uzunAdi)"
                value={selectedProduct.uzunAdi || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, uzunAdi: e.target.value } : prev
                  )
                }
                size="small"
                multiline
                rows={2}
              />

              {/* BARKOD */}
              <TextField
                fullWidth
                label="Barkod"
                value={selectedProduct.barcode || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, barcode: e.target.value } : prev
                  )
                }
                size="small"
              />

              {/* KATEGORİ AĞAÇ KOD */}
              <TextField
                fullWidth
                label="Kategori Ağaç Kodu (kategoriAgacKod)"
                value={selectedProduct.kategoriAgacKod || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, kategoriAgacKod: e.target.value } : prev
                  )
                }
                size="small"
                placeholder="Örn: 01"
              />

              {/* ALIŞ FİYATI */}
              <TextField
                fullWidth
                label="Alış Fiyatı (perakendeAlisBirimFiyat)"
                type="number"
                value={selectedProduct.purchasePrice ?? ""}
                onChange={(e) => {
                  const val =
                    e.target.value === ""
                      ? undefined
                      : parseFloat(e.target.value);
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, purchasePrice: val } : prev
                  );
                }}
                size="small"
                inputProps={{ min: 0, step: 0.01 }}
                placeholder="0.00"
              />

              {/* SATIŞ FİYATI */}
              <TextField
                fullWidth
                label="Satış Fiyatı (perakendeSatisBirimFiyat)"
                type="number"
                value={selectedProduct.salesPrice ?? ""}
                onChange={(e) => {
                  const val =
                    e.target.value === ""
                      ? undefined
                      : parseFloat(e.target.value);
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, salesPrice: val } : prev
                  );
                }}
                size="small"
                inputProps={{ min: 0, step: 0.01 }}
                placeholder="0.00"
              />

              {/* GTIP KODU */}
              <TextField
                fullWidth
                label="GTIP Kodu (gtipKodu)"
                value={selectedProduct.gtipCode || ""}
                onChange={(e) =>
                  setSelectedProduct((prev) =>
                    prev ? { ...prev, gtipCode: e.target.value } : prev
                  )
                }
                size="small"
              />

              <Alert severity="info" sx={{ mt: 1 }}>
                Bu alanlar Luca'da güncellenebilir alanlardır. Kaydet butonuna
                basınca Luca'ya gönderilir.
              </Alert>
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
      <Dialog
        open={confirmDeleteOpen}
        onClose={() => setConfirmDeleteOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Ürünü Sil</DialogTitle>
        <DialogContent>
          <Typography>
            <strong>
              {productToDelete?.productCode || productToDelete?.ProductCode}
            </strong>{" "}
            kodlu ürünü silmek istediğinize emin misiniz?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setConfirmDeleteOpen(false)}
            variant="outlined"
            disabled={deleting}
          >
            İptal
          </Button>
          <Button
            onClick={handleConfirmDelete}
            variant="contained"
            color="error"
            disabled={deleting}
          >
            {deleting ? "Siliniyor..." : "Sil"}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar - Güncelleme bildirimi */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={() => setSnackbar((prev) => ({ ...prev, open: false }))}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert
          onClose={() => setSnackbar((prev) => ({ ...prev, open: false }))}
          severity={snackbar.severity}
          sx={{ width: "100%" }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default LucaProducts;
