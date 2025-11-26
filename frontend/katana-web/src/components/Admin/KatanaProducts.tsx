import CloseIcon from "@mui/icons-material/Close";
import EditIcon from "@mui/icons-material/Edit";
import InventoryIcon from "@mui/icons-material/Inventory";
import RefreshIcon from "@mui/icons-material/Refresh";
import SaveIcon from "@mui/icons-material/Save";
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
import api from "../../services/api";
import { showGlobalToast } from "../../providers/FeedbackProvider";
import { decodeJwtPayload, getJwtRoles } from "../../utils/jwt";

interface KatanaProduct {
  id: string;
  sku: string;
  name: string;
  category?: string;
  unit?: string;
  inStock?: number;
  committed?: number;
  available?: number;
  onHand?: number;
  salesPrice?: number;
  costPrice?: number;
  isActive?: boolean;
  
  SKU?: string;
  Name?: string;
  Category?: string;
  Unit?: string;
  InStock?: number;
  Committed?: number;
  Available?: number;
  OnHand?: number;
  SalesPrice?: number;
  CostPrice?: number;
  IsActive?: boolean;
}

const KatanaProducts: React.FC = () => {
  const [products, setProducts] = useState<KatanaProduct[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<KatanaProduct[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [searchTerm, setSearchTerm] = useState("");
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<KatanaProduct | null>(
    null
  );
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchProducts = async () => {
    setLoading(true);
    try {
      console.log("Fetching Katana products...");
      
      const response = await api.get("/Products/katana?sync=true");
      console.log("Response:", response.data);
      const responseData: any = response.data;
      const productData = responseData?.data || responseData || [];
      console.log("Product data:", productData.length, "items");
      setProducts(productData);
      setFilteredProducts(productData);

      
      if (responseData?.sync) {
        const { created, updated, skipped } = responseData.sync;
        if (created > 0 || updated > 0) {
          setSuccessMessage(
            `Senkronizasyon tamamlandı: ${created} yeni, ${updated} güncellendi, ${skipped} atlandı`
          );
          setTimeout(() => setSuccessMessage(null), 5000);
        }
      }
    } catch (err: any) {
      const errorMsg =
        err.response?.data?.error || err.message || "Ürünler yüklenemedi";
      try {
        showGlobalToast({
          message: errorMsg,
          severity: "error",
          durationMs: 4000,
        });
      } catch {
        console.error("Katana ürünleri yükleme hatası:", err);
      }
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
        const name = (p.name || p.Name || "").toLowerCase();
        const sku = (p.sku || p.SKU || "").toLowerCase();
        const category = (p.category || p.Category || "").toLowerCase();
        const term = searchTerm.toLowerCase();
        return (
          name.includes(term) || sku.includes(term) || category.includes(term)
        );
      });
      setFilteredProducts(filtered);
    }
  }, [searchTerm, products]);

  const getStockStatus = (onHand?: number) => {
    if (!onHand || onHand === 0)
      return { label: "Yok", color: "error" as const };
    if (onHand < 10) return { label: "Düşük", color: "warning" as const };
    return { label: "Normal", color: "success" as const };
  };

  const handleEditClick = (product: KatanaProduct) => {
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
      const productId = parseInt(selectedProduct.id);
      const updateDto = {
        Name: selectedProduct.name || selectedProduct.Name || "",
        SKU: selectedProduct.sku || selectedProduct.SKU || "",
        Price: selectedProduct.salesPrice || selectedProduct.SalesPrice || 0,
        Stock: selectedProduct.onHand || selectedProduct.OnHand || 0,
        CategoryId: 1001,
        IsActive: selectedProduct.isActive ?? selectedProduct.IsActive ?? true,
      };

      await api.put(`/Products/${productId}`, updateDto);

      setSuccessMessage("Ürün başarıyla güncellendi!");
      setTimeout(() => setSuccessMessage(null), 3000);

      handleCloseModal();
      fetchProducts();
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
          err.response?.data?.errors?.[0] ||
          "Ürün güncellenemedi"
      );
      console.error("Ürün güncelleme hatası:", err);
    } finally {
      setSaving(false);
    }
  };

  const handleProductChange = (field: keyof KatanaProduct, value: any) => {
    if (!selectedProduct) return;
    setSelectedProduct({ ...selectedProduct, [field]: value });
  };

  
  const _token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;
  const _roles = getJwtRoles(decodeJwtPayload(_token));
  const canEdit = _roles.includes("admin") || _roles.includes("stokyonetici");

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
              <InventoryIcon color="primary" />
              <Typography variant="h5">Katana Ürünleri</Typography>
            </Stack>
            <Tooltip title="Yenile">
              <IconButton onClick={fetchProducts} disabled={loading}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Stack>

          <TextField
            fullWidth
            placeholder="SKU, ürün adı veya kategori ara..."
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
            <Chip label={`Toplam: ${products.length}`} color="primary" />
            <Chip label={`Görüntülenen: ${filteredProducts.length}`} />
            <Chip
              label={`Stokta: ${
                filteredProducts.filter((p) => (p.onHand ?? p.OnHand ?? 0) > 0)
                  .length
              }`}
              color="success"
            />
          </Stack>
        </CardContent>
      </Card>

      {}

      {successMessage && (
        <Alert
          severity="success"
          sx={{ mb: 2 }}
          onClose={() => setSuccessMessage(null)}
        >
          {successMessage}
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
              {searchTerm ? "Arama sonucu bulunamadı" : "Ürün bulunamadı"}
            </Typography>
          )}
          {filteredProducts.map((product) => {
            const onHand = product.onHand ?? product.OnHand ?? 0;
            const available = product.available ?? product.Available ?? 0;
            const committed = product.committed ?? product.Committed ?? 0;
            const salesPrice = product.salesPrice ?? product.SalesPrice;
            const costPrice = product.costPrice ?? product.CostPrice;
            const sku = product.sku || product.SKU || "";
            const name = product.name || product.Name || "";
            const category = product.category || product.Category;
            const unit = product.unit || product.Unit || "";
            const stockStatus = getStockStatus(onHand);

            return (
              <Paper
                key={product.id}
                sx={{ p: 1.5, borderRadius: 2, border: "1px solid", borderColor: "divider" }}
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
                      {name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      SKU: <strong>{sku}</strong>
                    </Typography>
                    {category && (
                      <Chip
                        label={category}
                        size="small"
                        variant="outlined"
                        sx={{ mt: 0.5 }}
                      />
                    )}
                  </Box>
                  <Chip
                    label={stockStatus.label}
                    color={stockStatus.color}
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
                      Eldeki
                    </Typography>
                    <Typography fontWeight={700} color={stockStatus.color}>
                      {onHand} {unit}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Kullanılabilir
                    </Typography>
                    <Typography fontWeight={600}>
                      {available} {unit}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Taahhütlü
                    </Typography>
                    <Typography fontWeight={600}>
                      {committed} {unit}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Satış Fiyatı
                    </Typography>
                    <Typography fontWeight={600}>
                      {salesPrice ? `${salesPrice.toFixed(2)} ₺` : "-"}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Maliyet
                    </Typography>
                    <Typography fontWeight={600}>
                      {costPrice ? `${costPrice.toFixed(2)} ₺` : "-"}
                    </Typography>
                  </Box>
                </Box>
                {canEdit && (
                  <Box sx={{ display: "flex", justifyContent: "flex-end", mt: 1 }}>
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<EditIcon fontSize="small" />}
                      onClick={() => handleEditClick(product)}
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
                  <strong>SKU</strong>
                </TableCell>
                <TableCell>
                  <strong>Ürün Adı</strong>
                </TableCell>
                <TableCell>
                  <strong>Kategori</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Eldeki</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Kullanılabilir</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Taahhütlü</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Satış Fiyatı</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Maliyet</strong>
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
                  <TableCell colSpan={10} align="center">
                    <Typography color="textSecondary">
                      {searchTerm
                        ? "Arama sonucu bulunamadı"
                        : "Ürün bulunamadı"}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                filteredProducts.map((product) => {
                  const onHand = product.onHand ?? product.OnHand ?? 0;
                  const available = product.available ?? product.Available ?? 0;
                  const committed = product.committed ?? product.Committed ?? 0;
                  const salesPrice = product.salesPrice ?? product.SalesPrice;
                  const costPrice = product.costPrice ?? product.CostPrice;
                  const sku = product.sku || product.SKU || "";
                  const name = product.name || product.Name || "";
                  const category = product.category || product.Category;
                  const unit = product.unit || product.Unit || "";

                  const stockStatus = getStockStatus(onHand);
                  return (
                    <TableRow key={product.id} hover>
                      <TableCell>
                        <Typography variant="body2" fontWeight="bold">
                          {sku}
                        </Typography>
                      </TableCell>
                      <TableCell>{name}</TableCell>
                      <TableCell>
                        {category && (
                          <Chip
                            label={category}
                            size="small"
                            variant="outlined"
                          />
                        )}
                      </TableCell>
                      <TableCell align="right">
                        <Typography fontWeight="bold" color={stockStatus.color}>
                          {onHand} {unit}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {available} {unit}
                      </TableCell>
                      <TableCell align="right">
                        {committed} {unit}
                      </TableCell>
                      <TableCell align="right">
                        {salesPrice ? `${salesPrice.toFixed(2)} ₺` : "-"}
                      </TableCell>
                      <TableCell align="right">
                        {costPrice ? `${costPrice.toFixed(2)} ₺` : "-"}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={stockStatus.label}
                          color={stockStatus.color}
                          size="small"
                        />
                      </TableCell>
                      <TableCell align="center">
                        {canEdit ? (
                          <Tooltip title="Düzenle">
                            <IconButton
                              size="small"
                              color="primary"
                              onClick={() => handleEditClick(product)}
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

      {}
      <Dialog
        open={editModalOpen}
        onClose={handleCloseModal}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 3,
            boxShadow: "0 8px 32px rgba(0,0,0,0.1)",
          },
        }}
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <EditIcon color="primary" />
            <Typography variant="h5" fontWeight="600">
              Ürün Düzenle
            </Typography>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ pt: 3, pb: 3 }}>
          {selectedProduct && (
            <Box
              sx={{
                display: "grid",
                gap: 3,
                gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
              }}
            >
              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  SKU *
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  placeholder="SKU kodu"
                  value={selectedProduct.sku || selectedProduct.SKU || ""}
                  onChange={(e) => handleProductChange("sku", e.target.value)}
                  disabled
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                      bgcolor: "action.hover",
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Ürün Adı *
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  placeholder="Ürün adını girin"
                  value={selectedProduct.name || selectedProduct.Name || ""}
                  onChange={(e) => handleProductChange("name", e.target.value)}
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Kategori
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  placeholder="Kategori"
                  value={
                    selectedProduct.category || selectedProduct.Category || ""
                  }
                  onChange={(e) =>
                    handleProductChange("category", e.target.value)
                  }
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Birim
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  placeholder="Adet, kg, vb."
                  value={selectedProduct.unit || selectedProduct.Unit || ""}
                  onChange={(e) => handleProductChange("unit", e.target.value)}
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Eldeki Stok
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  type="number"
                  placeholder="0"
                  value={selectedProduct.onHand ?? selectedProduct.OnHand ?? 0}
                  onChange={(e) =>
                    handleProductChange("onHand", parseInt(e.target.value))
                  }
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Satış Fiyatı (₺)
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  type="number"
                  placeholder="0.00"
                  value={
                    selectedProduct.salesPrice ??
                    selectedProduct.SalesPrice ??
                    0
                  }
                  onChange={(e) =>
                    handleProductChange(
                      "salesPrice",
                      parseFloat(e.target.value)
                    )
                  }
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>

              <Box sx={{ gridColumn: { xs: "1", sm: "span 2" } }}>
                <Typography
                  variant="caption"
                  sx={{
                    display: "block",
                    mb: 0.5,
                    fontWeight: 500,
                    color: "text.secondary",
                  }}
                >
                  Maliyet Fiyatı (₺)
                </Typography>
                <TextField
                  fullWidth
                  size="small"
                  type="number"
                  placeholder="0.00"
                  value={
                    selectedProduct.costPrice ?? selectedProduct.CostPrice ?? 0
                  }
                  onChange={(e) =>
                    handleProductChange("costPrice", parseFloat(e.target.value))
                  }
                  sx={{
                    "& .MuiOutlinedInput-root": {
                      borderRadius: 2,
                    },
                  }}
                />
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3, pt: 2 }}>
          <Button
            onClick={handleCloseModal}
            variant="outlined"
            startIcon={<CloseIcon />}
            disabled={saving}
            sx={{
              borderRadius: 2,
              textTransform: "none",
              px: 3,
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
            startIcon={<SaveIcon />}
            disabled={saving}
            sx={{
              borderRadius: 2,
              textTransform: "none",
              px: 3,
              fontWeight: 600,
              color: "white",
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              boxShadow: "0 4px 15px rgba(102, 126, 234, 0.4)",
              "&:hover": {
                background: "linear-gradient(135deg, #5568d3 0%, #653a8e 100%)",
                boxShadow: "0 6px 20px rgba(102, 126, 234, 0.6)",
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

export default KatanaProducts;
