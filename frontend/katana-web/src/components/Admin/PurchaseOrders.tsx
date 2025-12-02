import React, { useEffect, useState } from "react";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  Checkbox,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
  Alert,
  Snackbar,
  Card,
  CardContent,
  CardHeader,
  Divider,
} from "@mui/material";
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  HourglassEmpty as PendingIcon,
  Save as SaveIcon,
} from "@mui/icons-material";
import api from "../../services/api";

// Types
interface PurchaseOrder {
  id: number;
  orderNo: string;
  supplierId: number;
  supplierName?: string;
  supplierCode?: string;
  status: string;
  totalAmount: number;
  orderDate: string;
  expectedDate?: string;
  isSynced: boolean;
  createdAt: string;
  lucaOrderId?: number;
  belgeSeri?: string;
  belgeNo?: string;
  currency?: string;
  lastSyncAt?: string;
  lastSyncError?: string;
  items: PurchaseOrderItem[];
  lucaSyncStatus: "synced" | "error" | "not_synced";
}

interface PurchaseOrderItem {
  id: number;
  purchaseOrderId: number;
  productId: number;
  productName?: string;
  sku?: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  totalPrice: number;
}

interface Supplier {
  id: number;
  name: string;
  code?: string;
  taxNo?: string;
}

interface Product {
  id: number;
  name: string;
  sku: string;
  lucaCode?: string;
}

interface CreatePurchaseOrderForm {
  supplierId: number;
  orderDate: string;
  expectedDate: string;
  currency: string;
  belgeSeri: string;
  belgeTurDetayId: number;
  kdvFlag: boolean;
  items: CreatePurchaseOrderItem[];
}

interface CreatePurchaseOrderItem {
  productId: number;
  kartKodu: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  depoKodu: string;
}

// Status badge component
const LucaStatusBadge: React.FC<{
  status: "synced" | "error" | "not_synced";
  error?: string;
}> = ({ status, error }) => {
  if (status === "synced") {
    return (
      <Chip
        icon={<CheckCircleIcon />}
        label="Senkronize"
        color="success"
        size="small"
      />
    );
  }
  if (status === "error") {
    return (
      <Tooltip title={error || "Hata oluştu"}>
        <Chip icon={<ErrorIcon />} label="Hata" color="error" size="small" />
      </Tooltip>
    );
  }
  return (
    <Chip icon={<PendingIcon />} label="Bekliyor" color="default" size="small" />
  );
};

const gridStyles = {
  container: {
    display: "grid",
    gap: 2,
    gridTemplateColumns: { xs: "1fr", md: "repeat(2, 1fr)" },
  },
  threeCol: {
    display: "grid",
    gap: 2,
    gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" },
  },
};

const emptyItem: CreatePurchaseOrderItem = {
  productId: 0,
  kartKodu: "",
  quantity: 1,
  unitPrice: 0,
  taxRate: 20,
  depoKodu: "",
};

const PurchaseOrders: React.FC = () => {
  // List state
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [products, setProducts] = useState<Product[]>([]);

  // Dialog state
  const [openDialog, setOpenDialog] = useState(false);
  const [formData, setFormData] = useState<CreatePurchaseOrderForm>({
    supplierId: 0,
    orderDate: new Date().toISOString().split("T")[0],
    expectedDate: "",
    currency: "TRY",
    belgeSeri: "SAT",
    belgeTurDetayId: 18,
    kdvFlag: true,
    items: [{ ...emptyItem }],
  });
  const [saving, setSaving] = useState(false);

  // Delete state
  const [deleting, setDeleting] = useState<number | null>(null);

  // Snackbar
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "info";
  }>({
    open: false,
    message: "",
    severity: "success",
  });

  // Fetch orders
  const fetchOrders = async () => {
    try {
      setLoading(true);
      const response = await api.get<PurchaseOrder[]>("/purchase-orders");
      setOrders(response.data);
    } catch (err) {
      console.error("Failed to fetch purchase orders:", err);
      setSnackbar({
        open: true,
        message: "Satın alma siparişleri yüklenemedi",
        severity: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  // Fetch suppliers
  const fetchSuppliers = async () => {
    try {
      const response = await api.get<Supplier[]>("/suppliers");
      setSuppliers(response.data);
    } catch (err) {
      console.error("Failed to fetch suppliers:", err);
    }
  };

  // Fetch products
  const fetchProducts = async () => {
    try {
      const response = await api.get<Product[]>("/products");
      setProducts(response.data);
    } catch (err) {
      console.error("Failed to fetch products:", err);
    }
  };

  useEffect(() => {
    fetchOrders();
    fetchSuppliers();
    fetchProducts();
  }, []);

  // Add line item
  const addLineItem = () => {
    setFormData({
      ...formData,
      items: [...formData.items, { ...emptyItem }],
    });
  };

  // Remove line item
  const removeLineItem = (index: number) => {
    const newItems = formData.items.filter((_, i) => i !== index);
    setFormData({ ...formData, items: newItems.length > 0 ? newItems : [{ ...emptyItem }] });
  };

  // Update line item
  const updateLineItem = (index: number, field: keyof CreatePurchaseOrderItem, value: string | number) => {
    const newItems = [...formData.items];
    newItems[index] = { ...newItems[index], [field]: value };
    
    // Auto-fill kartKodu when product is selected
    if (field === "productId") {
      const product = products.find((p) => p.id === value);
      if (product) {
        newItems[index].kartKodu = product.lucaCode || product.sku;
      }
    }
    
    setFormData({ ...formData, items: newItems });
  };

  // Calculate total
  const calculateTotal = () => {
    return formData.items.reduce((sum, item) => {
      return sum + item.quantity * item.unitPrice;
    }, 0);
  };

  // Create order
  const handleCreate = async () => {
    if (formData.supplierId === 0) {
      setSnackbar({
        open: true,
        message: "Lütfen tedarikçi seçin",
        severity: "error",
      });
      return;
    }

    if (formData.items.every((i) => i.productId === 0)) {
      setSnackbar({
        open: true,
        message: "En az bir ürün ekleyin",
        severity: "error",
      });
      return;
    }

    try {
      setSaving(true);
      await api.post("/purchase-orders", formData);
      setSnackbar({
        open: true,
        message: "Satın alma siparişi oluşturuldu",
        severity: "success",
      });
      setOpenDialog(false);
      resetForm();
      await fetchOrders();
    } catch (err) {
      console.error("Failed to create purchase order:", err);
      setSnackbar({
        open: true,
        message: "Sipariş oluşturulamadı",
        severity: "error",
      });
    } finally {
      setSaving(false);
    }
  };

  // Delete order
  const handleDelete = async (id: number) => {
    if (!window.confirm("Bu siparişi silmek istediğinize emin misiniz?")) return;

    try {
      setDeleting(id);
      await api.delete(`/purchase-orders/${id}`);
      setSnackbar({
        open: true,
        message: "Sipariş silindi",
        severity: "success",
      });
      await fetchOrders();
    } catch (err) {
      console.error("Failed to delete purchase order:", err);
      setSnackbar({
        open: true,
        message: "Sipariş silinemedi",
        severity: "error",
      });
    } finally {
      setDeleting(null);
    }
  };

  // Reset form
  const resetForm = () => {
    setFormData({
      supplierId: 0,
      orderDate: new Date().toISOString().split("T")[0],
      expectedDate: "",
      currency: "TRY",
      belgeSeri: "SAT",
      belgeTurDetayId: 18,
      kdvFlag: true,
      items: [{ ...emptyItem }],
    });
  };

  // Format
  const formatDate = (dateStr?: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleDateString("tr-TR");
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(amount);
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3 }}>
        <Typography variant="h5">Satın Alma Siparişleri</Typography>
        <Box sx={{ display: "flex", gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={fetchOrders}
            disabled={loading}
          >
            Yenile
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setOpenDialog(true)}
          >
            Yeni Sipariş
          </Button>
        </Box>
      </Box>

      {/* Table */}
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Belge No</TableCell>
              <TableCell>Tedarikçi</TableCell>
              <TableCell>Tarih</TableCell>
              <TableCell>Beklenen Tarih</TableCell>
              <TableCell align="right">Toplam</TableCell>
              <TableCell>Luca</TableCell>
              <TableCell align="center">İşlemler</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : orders.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <Typography color="textSecondary">Sipariş bulunamadı</Typography>
                </TableCell>
              </TableRow>
            ) : (
              orders.map((order) => (
                <TableRow key={order.id} hover>
                  <TableCell>
                    <Typography fontWeight="medium">
                      {order.belgeSeri}-{order.belgeNo || order.orderNo}
                    </Typography>
                  </TableCell>
                  <TableCell>{order.supplierName || "-"}</TableCell>
                  <TableCell>{formatDate(order.orderDate)}</TableCell>
                  <TableCell>{formatDate(order.expectedDate)}</TableCell>
                  <TableCell align="right">
                    {formatCurrency(order.totalAmount)}
                  </TableCell>
                  <TableCell>
                    <LucaStatusBadge
                      status={order.lucaSyncStatus}
                      error={order.lastSyncError}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Sil">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(order.id)}
                        disabled={deleting === order.id}
                      >
                        {deleting === order.id ? (
                          <CircularProgress size={16} />
                        ) : (
                          <DeleteIcon fontSize="small" />
                        )}
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Create Dialog */}
      <Dialog
        open={openDialog}
        onClose={() => setOpenDialog(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Yeni Satın Alma Siparişi</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            {/* Header Fields */}
            <Card sx={{ mb: 3 }}>
              <CardHeader title="Sipariş Bilgileri" />
              <Divider />
              <CardContent>
                <Box sx={gridStyles.container}>
                  <FormControl size="small" fullWidth>
                    <InputLabel>Tedarikçi *</InputLabel>
                    <Select
                      value={formData.supplierId}
                      label="Tedarikçi *"
                      onChange={(e) =>
                        setFormData({ ...formData, supplierId: e.target.value as number })
                      }
                    >
                      <MenuItem value={0}>Seçiniz</MenuItem>
                      {suppliers.map((s) => (
                        <MenuItem key={s.id} value={s.id}>
                          {s.name} {s.code ? `(${s.code})` : ""}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  <TextField
                    label="Belge Tarihi"
                    type="date"
                    value={formData.orderDate}
                    onChange={(e) =>
                      setFormData({ ...formData, orderDate: e.target.value })
                    }
                    size="small"
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                  />

                  <TextField
                    label="Teslim Tarihi"
                    type="date"
                    value={formData.expectedDate}
                    onChange={(e) =>
                      setFormData({ ...formData, expectedDate: e.target.value })
                    }
                    size="small"
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                  />

                  <TextField
                    label="Belge Seri"
                    value={formData.belgeSeri}
                    onChange={(e) =>
                      setFormData({ ...formData, belgeSeri: e.target.value })
                    }
                    size="small"
                    fullWidth
                  />

                  <FormControl size="small" fullWidth>
                    <InputLabel>Para Birimi</InputLabel>
                    <Select
                      value={formData.currency}
                      label="Para Birimi"
                      onChange={(e) =>
                        setFormData({ ...formData, currency: e.target.value })
                      }
                    >
                      <MenuItem value="TRY">TRY</MenuItem>
                      <MenuItem value="USD">USD</MenuItem>
                      <MenuItem value="EUR">EUR</MenuItem>
                    </Select>
                  </FormControl>

                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={formData.kdvFlag}
                        onChange={(e) =>
                          setFormData({ ...formData, kdvFlag: e.target.checked })
                        }
                      />
                    }
                    label="KDV Dahil"
                  />
                </Box>
              </CardContent>
            </Card>

            {/* Line Items */}
            <Card>
              <CardHeader
                title="Sipariş Kalemleri"
                action={
                  <Button
                    size="small"
                    startIcon={<AddIcon />}
                    onClick={addLineItem}
                  >
                    Satır Ekle
                  </Button>
                }
              />
              <Divider />
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Ürün</TableCell>
                      <TableCell>Kart Kodu</TableCell>
                      <TableCell align="right">Miktar</TableCell>
                      <TableCell align="right">Birim Fiyat</TableCell>
                      <TableCell align="right">KDV %</TableCell>
                      <TableCell align="right">Toplam</TableCell>
                      <TableCell></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {formData.items.map((item, index) => (
                      <TableRow key={index}>
                        <TableCell>
                          <FormControl size="small" fullWidth sx={{ minWidth: 150 }}>
                            <Select
                              value={item.productId}
                              onChange={(e) =>
                                updateLineItem(index, "productId", e.target.value as number)
                              }
                              displayEmpty
                            >
                              <MenuItem value={0}>Seçiniz</MenuItem>
                              {products.map((p) => (
                                <MenuItem key={p.id} value={p.id}>
                                  {p.sku} - {p.name}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </TableCell>
                        <TableCell>
                          <TextField
                            value={item.kartKodu}
                            onChange={(e) =>
                              updateLineItem(index, "kartKodu", e.target.value)
                            }
                            size="small"
                            sx={{ width: 100 }}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <TextField
                            type="number"
                            value={item.quantity}
                            onChange={(e) =>
                              updateLineItem(index, "quantity", parseFloat(e.target.value) || 0)
                            }
                            size="small"
                            sx={{ width: 80 }}
                            inputProps={{ min: 0, step: 1 }}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <TextField
                            type="number"
                            value={item.unitPrice}
                            onChange={(e) =>
                              updateLineItem(index, "unitPrice", parseFloat(e.target.value) || 0)
                            }
                            size="small"
                            sx={{ width: 100 }}
                            inputProps={{ min: 0, step: 0.01 }}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <TextField
                            type="number"
                            value={item.taxRate}
                            onChange={(e) =>
                              updateLineItem(index, "taxRate", parseFloat(e.target.value) || 0)
                            }
                            size="small"
                            sx={{ width: 60 }}
                            inputProps={{ min: 0, max: 100 }}
                          />
                        </TableCell>
                        <TableCell align="right">
                          {formatCurrency(item.quantity * item.unitPrice)}
                        </TableCell>
                        <TableCell>
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => removeLineItem(index)}
                            disabled={formData.items.length === 1}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                    <TableRow>
                      <TableCell colSpan={5} align="right">
                        <Typography fontWeight="bold">Genel Toplam:</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography fontWeight="bold">
                          {formatCurrency(calculateTotal())}
                        </Typography>
                      </TableCell>
                      <TableCell></TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>İptal</Button>
          <Button
            variant="contained"
            startIcon={saving ? <CircularProgress size={20} /> : <SaveIcon />}
            onClick={handleCreate}
            disabled={saving}
          >
            Kaydet ve Luca'ya Gönder
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          onClose={() => setSnackbar({ ...snackbar, open: false })}
          severity={snackbar.severity}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default PurchaseOrders;
