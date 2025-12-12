import {
  Add as AddIcon,
  ArrowBack as BackIcon,
  CheckCircle as CheckCircleIcon,
  CloudUpload as CloudUploadIcon,
  Delete as DeleteIcon,
  Error as ErrorIcon,
  HourglassEmpty as PendingIcon,
  Refresh as RefreshIcon,
  Save as SaveIcon,
  Visibility as ViewIcon,
  Warning as WarningIcon,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Checkbox,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  FormControlLabel,
  FormHelperText,
  Grid,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useCallback, useEffect, useState } from "react";
import api from "../../services/api";

// ===== TYPES =====

type LucaSyncStatus = "synced" | "error" | "not_synced";

interface PurchaseOrderListItem {
  id: number;
  orderNo: string;
  supplierName?: string;
  orderDate: string;
  expectedDate?: string;
  status: string;
  totalAmount: number;
  lucaPurchaseOrderId?: number;
  lucaDocumentNo?: string;
  isSyncedToLuca: boolean;
  lastSyncError?: string;
  lastSyncAt?: string;
}

interface PurchaseOrderDetail {
  id: number;
  orderNo: string;
  supplierId: number;
  supplierCode?: string;
  supplierName?: string;
  status: string;
  totalAmount: number;
  orderDate: string;
  expectedDate?: string;
  createdAt: string;
  updatedAt?: string;
  // Luca alanları
  lucaPurchaseOrderId?: number;
  lucaDocumentNo?: string;
  documentSeries?: string;
  documentTypeDetailId: number;
  vatIncluded: boolean;
  referenceCode?: string;
  projectCode?: string;
  description?: string;
  isSyncedToLuca: boolean;
  lastSyncAt?: string;
  lastSyncError?: string;
  syncRetryCount: number;
  items: PurchaseOrderItemDetail[];
}

interface PurchaseOrderItemDetail {
  id: number;
  productId: number;
  productName?: string;
  productSku?: string;
  quantity: number;
  unitPrice: number;
  lucaStockCode?: string;
  warehouseCode?: string;
  vatRate: number;
  unitCode?: string;
  discountAmount: number;
  lucaDetailId?: number;
}

interface Supplier {
  id: number;
  name: string;
  code?: string;
  taxNo?: string;
  lucaCode?: string;
  isActive: boolean;
}

interface Product {
  id: number;
  name: string;
  sku: string;
  lucaCode?: string;
  lucaStockCode?: string;
  isActive: boolean;
}

interface CreatePurchaseOrderForm {
  supplierId: number;
  orderDate: string;
  expectedDate: string;
  documentSeries: string;
  documentTypeDetailId: number;
  vatIncluded: boolean;
  projectCode: string;
  description: string;
  items: CreatePurchaseOrderItemForm[];
}

interface CreatePurchaseOrderItemForm {
  productId: number;
  lucaStockCode: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  warehouseCode: string;
  unitCode: string;
  discountAmount: number;
}

interface SyncResult {
  success: boolean;
  lucaPurchaseOrderId?: number;
  lucaDocumentNo?: string;
  message?: string;
}

interface OrderStats {
  total: number;
  synced: number;
  notSynced: number;
  withErrors: number;
  pending: number;
  approved: number;
  received: number;
  cancelled: number;
}

// ===== CONSTANTS =====

const DOCUMENT_TYPES = [
  { id: 2, label: "Tedarik Siparişi" },
  { id: 18, label: "Tedarik Siparişi (Standart)" },
];

const VAT_RATES = [0, 1, 10, 18, 20];

const WAREHOUSES = [
  { code: "01", label: "Ana Depo" },
  { code: "02", label: "Şube Depo" },
  { code: "03", label: "Depo 3" },
];

const UNIT_CODES = [
  { code: "AD", label: "Adet" },
  { code: "KG", label: "Kilogram" },
  { code: "MT", label: "Metre" },
  { code: "LT", label: "Litre" },
  { code: "PK", label: "Paket" },
];

// ===== HELPER COMPONENTS =====

const LucaStatusBadge: React.FC<{
  status: LucaSyncStatus;
  error?: string;
  lucaId?: number;
}> = ({ status, error, lucaId }) => {
  if (status === "synced") {
    return (
      <Tooltip title={`Koza ID: ${lucaId || "-"}`}>
        <Chip
          icon={<CheckCircleIcon />}
          label="Senkronize"
          color="success"
          size="small"
        />
      </Tooltip>
    );
  }
  if (status === "error") {
    return (
      <Tooltip title={error || "Bilinmeyen hata"}>
        <Chip icon={<ErrorIcon />} label="Hata" color="error" size="small" />
      </Tooltip>
    );
  }
  return (
    <Chip
      icon={<PendingIcon />}
      label="Gönderilmedi"
      color="default"
      size="small"
    />
  );
};

const getSyncStatus = (
  order: PurchaseOrderListItem | PurchaseOrderDetail
): LucaSyncStatus => {
  if (order.isSyncedToLuca && !order.lastSyncError) return "synced";
  if (order.lastSyncError) return "error";
  return "not_synced";
};

// ===== EMPTY ITEM TEMPLATE =====

const emptyItem: CreatePurchaseOrderItemForm = {
  productId: 0,
  lucaStockCode: "",
  quantity: 1,
  unitPrice: 0,
  vatRate: 20,
  warehouseCode: "01",
  unitCode: "AD",
  discountAmount: 0,
};

// ===== MAIN COMPONENT =====

const PurchaseOrders: React.FC = () => {
  // View state
  const [view, setView] = useState<"list" | "detail" | "create">("list");
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);

  // List state
  const [orders, setOrders] = useState<PurchaseOrderListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState<OrderStats | null>(null);

  // Detail state
  const [orderDetail, setOrderDetail] = useState<PurchaseOrderDetail | null>(
    null
  );
  const [detailLoading, setDetailLoading] = useState(false);

  // Create/Edit state
  const [formData, setFormData] = useState<CreatePurchaseOrderForm>({
    supplierId: 0,
    orderDate: new Date().toISOString().split("T")[0],
    expectedDate: "",
    documentSeries: "A",
    documentTypeDetailId: 2,
    vatIncluded: true,
    projectCode: "",
    description: "",
    items: [{ ...emptyItem }],
  });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  // Reference data
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [products, setProducts] = useState<Product[]>([]);

  // Sync state
  const [syncing, setSyncing] = useState(false);

  // Filter state
  const [filterSyncStatus, setFilterSyncStatus] = useState<string>("all");
  const [filterSupplierId, setFilterSupplierId] = useState<number>(0);

  // Delete state
  const [deleting, setDeleting] = useState<number | null>(null);

  // Snackbar
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "info" | "warning";
  }>({
    open: false,
    message: "",
    severity: "success",
  });

  // ===== DATA FETCHING =====

  const fetchOrders = useCallback(async () => {
    try {
      setLoading(true);
      const params: Record<string, string> = {};
      if (filterSyncStatus !== "all") {
        params.syncStatus = filterSyncStatus;
      }
      if (filterSupplierId > 0) {
        params.supplierId = filterSupplierId.toString();
      }

      const response = await api.get<{
        items: PurchaseOrderListItem[];
        pagination: {
          currentPage: number;
          pageSize: number;
          totalCount: number;
          totalPages: number;
        };
        warnings?: string[];
      }>("/purchase-orders", { params });
      setOrders(response.data.items || []);
    } catch (err) {
      console.error("Failed to fetch purchase orders:", err);
      setSnackbar({
        open: true,
        message: "Tedarik siparişleri yüklenemedi",
        severity: "error",
      });
    } finally {
      setLoading(false);
    }
  }, [filterSyncStatus, filterSupplierId]);

  const fetchStats = async () => {
    try {
      const response = await api.get<OrderStats>("/purchase-orders/stats");
      setStats(response.data);
    } catch (err) {
      console.error("Failed to fetch stats:", err);
    }
  };

  const fetchOrderDetail = async (id: number) => {
    try {
      setDetailLoading(true);
      const response = await api.get<PurchaseOrderDetail>(
        `/purchase-orders/${id}`
      );
      setOrderDetail(response.data);
    } catch (err) {
      console.error("Failed to fetch order detail:", err);
      setSnackbar({
        open: true,
        message: "Sipariş detayı yüklenemedi",
        severity: "error",
      });
    } finally {
      setDetailLoading(false);
    }
  };

  const fetchSuppliers = async () => {
    try {
      const response = await api.get<Supplier[]>("/suppliers");
      setSuppliers(response.data);
    } catch (err) {
      console.error("Failed to fetch suppliers:", err);
    }
  };

  const fetchProducts = async () => {
    try {
      const response = await api.get<Product[]>("/products");
      setProducts(response.data);
    } catch (err) {
      console.error("Failed to fetch products:", err);
    }
  };

  useEffect(() => {
    // Sayfa açıldığında önce Katana'dan sync yap, sonra listeyi yükle
    const initializeData = async () => {
      await handleSyncFromKatana();
      await fetchSuppliers();
      await fetchProducts();
      await fetchStats();
    };
    
    initializeData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (selectedOrderId && view === "detail") {
      fetchOrderDetail(selectedOrderId);
    }
  }, [selectedOrderId, view]);

  // ===== FORM HANDLERS =====

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (formData.supplierId === 0) {
      errors.supplierId = "Tedarikçi seçilmeli";
    } else {
      const supplier = suppliers.find((s) => s.id === formData.supplierId);
      if (supplier && !supplier.code) {
        errors.supplierId = "Seçili tedarikçinin Koza kodu yok";
      }
    }

    if (!formData.orderDate) {
      errors.orderDate = "Sipariş tarihi gerekli";
    }

    if (
      formData.items.length === 0 ||
      formData.items.every((i) => i.productId === 0)
    ) {
      errors.items = "En az bir ürün eklenmelidir";
    }

    formData.items.forEach((item, index) => {
      if (item.productId > 0) {
        const product = products.find((p) => p.id === item.productId);
        if (product && !product.sku && !item.lucaStockCode) {
          errors[`item_${index}_product`] = "Bu ürünün Koza stok kodu yok";
        }
        if (item.quantity <= 0) {
          errors[`item_${index}_quantity`] = "Miktar 0'dan büyük olmalı";
        }
        if (item.unitPrice < 0) {
          errors[`item_${index}_price`] = "Fiyat negatif olamaz";
        }
      }
    });

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async () => {
    if (!validateForm()) {
      setSnackbar({
        open: true,
        message: "Lütfen form hatalarını düzeltin",
        severity: "error",
      });
      return;
    }

    try {
      setSaving(true);
      const payload = {
        supplierId: formData.supplierId,
        orderDate: formData.orderDate,
        expectedDate: formData.expectedDate || null,
        documentSeries: formData.documentSeries,
        documentTypeDetailId: formData.documentTypeDetailId,
        vatIncluded: formData.vatIncluded,
        projectCode: formData.projectCode || null,
        description: formData.description || null,
        items: formData.items
          .filter((i) => i.productId > 0)
          .map((i) => ({
            productId: i.productId,
            quantity: i.quantity,
            unitPrice: i.unitPrice,
            lucaStockCode: i.lucaStockCode || null,
            warehouseCode: i.warehouseCode,
            vatRate: i.vatRate,
            unitCode: i.unitCode,
            discountAmount: i.discountAmount,
          })),
      };

      await api.post("/purchase-orders", payload);
      setSnackbar({
        open: true,
        message: "Tedarik siparişi oluşturuldu",
        severity: "success",
      });
      resetForm();
      setView("list");
      await fetchOrders();
      await fetchStats();
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

  const resetForm = () => {
    setFormData({
      supplierId: 0,
      orderDate: new Date().toISOString().split("T")[0],
      expectedDate: "",
      documentSeries: "A",
      documentTypeDetailId: 2,
      vatIncluded: true,
      projectCode: "",
      description: "",
      items: [{ ...emptyItem }],
    });
    setFormErrors({});
  };

  // ===== LINE ITEM HANDLERS =====

  const addLineItem = () => {
    setFormData({
      ...formData,
      items: [...formData.items, { ...emptyItem }],
    });
  };

  const removeLineItem = (index: number) => {
    const newItems = formData.items.filter((_, i) => i !== index);
    setFormData({
      ...formData,
      items: newItems.length > 0 ? newItems : [{ ...emptyItem }],
    });
  };

  const updateLineItem = (
    index: number,
    field: keyof CreatePurchaseOrderItemForm,
    value: string | number
  ) => {
    const newItems = [...formData.items];
    newItems[index] = { ...newItems[index], [field]: value };

    // Auto-fill lucaStockCode when product is selected
    if (field === "productId") {
      const product = products.find((p) => p.id === value);
      if (product) {
        newItems[index].lucaStockCode =
          product.lucaStockCode || product.sku || "";
      }
    }

    setFormData({ ...formData, items: newItems });
  };

  // ===== SYNC HANDLERS =====

  const handleSyncFromKatana = async () => {
    try {
      setSyncing(true);
      setSnackbar({
        open: true,
        message: "Katana'dan siparişler çekiliyor...",
        severity: "info",
      });

      const response = await api.post<{
        message: string;
        imported: number;
        updated: number;
        skipped: number;
        total: number;
        suppliersSynced: number;
      }>("/purchase-orders/sync-from-katana");

      setSnackbar({
        open: true,
        message: `✅ ${response.data.total} sipariş senkronize edildi (${response.data.imported} yeni, ${response.data.updated} güncellendi)`,
        severity: "success",
      });
      
      await fetchOrders();
      await fetchStats();
    } catch (err) {
      console.error("Katana sync failed:", err);
      setSnackbar({
        open: true,
        message: "Katana senkronizasyonu başarısız",
        severity: "error",
      });
    } finally {
      setSyncing(false);
    }
  };

  const handleSyncOrder = async (id: number) => {
    try {
      setSyncing(true);
      setSnackbar({
        open: true,
        message: "Koza'ya gönderiliyor...",
        severity: "info",
      });

      const response = await api.post<SyncResult>(
        `/purchase-orders/${id}/sync`
      );

      if (response.data.success) {
        setSnackbar({
          open: true,
          message: `Başarıyla senkronize edildi. Koza ID: ${response.data.lucaPurchaseOrderId}`,
          severity: "success",
        });
        // Refresh detail if viewing
        if (selectedOrderId === id) {
          await fetchOrderDetail(id);
        }
        await fetchOrders();
        await fetchStats();
      } else {
        setSnackbar({
          open: true,
          message: response.data.message || "Senkronizasyon başarısız",
          severity: "error",
        });
        if (selectedOrderId === id) {
          await fetchOrderDetail(id);
        }
      }
    } catch (err) {
      console.error("Sync failed:", err);
      setSnackbar({
        open: true,
        message: "Senkronizasyon hatası",
        severity: "error",
      });
    } finally {
      setSyncing(false);
    }
  };

  // ===== DELETE HANDLER =====

  const handleDelete = async (id: number) => {
    if (!window.confirm("Bu siparişi silmek istediğinize emin misiniz?"))
      return;

    try {
      setDeleting(id);
      await api.delete(`/purchase-orders/${id}`);
      setSnackbar({
        open: true,
        message: "Sipariş silindi",
        severity: "success",
      });
      if (view === "detail") {
        setView("list");
      }
      await fetchOrders();
      await fetchStats();
    } catch (err: unknown) {
      console.error("Failed to delete:", err);
      const errorMessage =
        err instanceof Error && "response" in err
          ? (err as { response?: { data?: { message?: string } } }).response
              ?.data?.message
          : "Sipariş silinemedi";
      setSnackbar({
        open: true,
        message: errorMessage || "Sipariş silinemedi",
        severity: "error",
      });
    } finally {
      setDeleting(null);
    }
  };

  // ===== FORMAT HELPERS =====

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleDateString("tr-TR");
  };

  const formatDateTime = (dateStr?: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleString("tr-TR");
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: "TRY",
    }).format(amount);
  };

  const calculateLineTotal = (item: CreatePurchaseOrderItemForm) => {
    return item.quantity * item.unitPrice - item.discountAmount;
  };

  const calculateFormTotal = () => {
    return formData.items.reduce(
      (sum, item) => sum + calculateLineTotal(item),
      0
    );
  };

  // ===== RENDER: STATS BAR =====

  const renderStats = () => {
    if (!stats) return null;

    return (
      <Box sx={{ display: "flex", gap: 2, mb: 3, flexWrap: "wrap" }}>
        <Chip
          label={`Toplam: ${stats.total}`}
          color="default"
          variant="outlined"
        />
        <Chip
          icon={<CheckCircleIcon />}
          label={`Senkronize: ${stats.synced}`}
          color="success"
          variant="outlined"
        />
        <Chip
          icon={<PendingIcon />}
          label={`Bekliyor: ${stats.notSynced}`}
          color="default"
          variant="outlined"
        />
        <Chip
          icon={<ErrorIcon />}
          label={`Hatalı: ${stats.withErrors}`}
          color="error"
          variant="outlined"
        />
      </Box>
    );
  };

  // ===== RENDER: LIST VIEW =====

  const renderListView = () => (
    <Box
      sx={{
        px: { xs: 1, sm: 0 },
        mx: { xs: 0, sm: 0 },
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "flex-end",
          alignItems: "center",
          flexWrap: "wrap",
          gap: 1,
          mb: 2,
          px: { xs: 1, sm: 0 },
        }}
      >
        <Typography
          variant="h6"
          sx={{ fontSize: { xs: "1rem", sm: "1.25rem" } }}
        >
          Satınalma Siparişleri
        </Typography>
        <Box sx={{ display: "flex", gap: 0.5 }}>
          <Tooltip title="Katana'dan Siparişleri Çek">
            <Button
              size="small"
              variant="outlined"
              startIcon={<CloudUploadIcon />}
              onClick={handleSyncFromKatana}
              disabled={syncing || loading}
              sx={{ fontSize: "0.75rem", px: 1.5, py: 0.5, minWidth: "auto" }}
            >
              {syncing ? "Çekiliyor..." : "Katana'dan Çek"}
            </Button>
          </Tooltip>
          <IconButton
            size="small"
            color="primary"
            onClick={() => {
              fetchOrders();
              fetchStats();
            }}
            disabled={loading}
          >
            <RefreshIcon fontSize="small" />
          </IconButton>
          <Button
            size="small"
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              resetForm();
              setView("create");
            }}
            sx={{ fontSize: "0.75rem", px: 1.5, py: 0.5, minWidth: "auto" }}
          >
            Yeni Tedarik Siparişi
          </Button>
        </Box>
      </Box>

      {/* Stats */}
      {renderStats()}

      {/* Filters */}
      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", sm: "row" },
          gap: 1,
          mb: 2,
          px: { xs: 1, sm: 0 },
          flexWrap: "wrap",
        }}
      >
        <FormControl
          size="small"
          sx={{ minWidth: 120, flex: { xs: 1, sm: "none" } }}
        >
          <InputLabel sx={{ fontSize: "0.8rem" }}>Sync Durumu</InputLabel>
          <Select
            value={filterSyncStatus}
            label="Sync Durumu"
            onChange={(e) => setFilterSyncStatus(e.target.value)}
            sx={{ fontSize: "0.8rem" }}
          >
            <MenuItem value="all">Tümü</MenuItem>
            <MenuItem value="synced">Senkronize</MenuItem>
            <MenuItem value="not_synced">Gönderilmedi</MenuItem>
            <MenuItem value="error">Hatalı</MenuItem>
          </Select>
        </FormControl>
        <FormControl
          size="small"
          sx={{ minWidth: 120, flex: { xs: 1, sm: "none" } }}
        >
          <InputLabel sx={{ fontSize: "0.8rem" }}>Tedarikçi</InputLabel>
          <Select
            value={filterSupplierId}
            label="Tedarikçi"
            onChange={(e) => setFilterSupplierId(e.target.value as number)}
            sx={{ fontSize: "0.8rem" }}
          >
            <MenuItem value={0}>Tümü</MenuItem>
            {suppliers.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {/* Table */}
      <TableContainer
        component={Paper}
        sx={{
          width: "100%",
          overflowX: "auto",
          mx: { xs: 0, sm: 0 },
        }}
      >
        <Table
          size="small"
          sx={{
            minWidth: { xs: 0, sm: 650 },
            tableLayout: { xs: "fixed", sm: "auto" },
            "& .MuiTableCell-root": {
              px: { xs: 0.75, sm: 2 },
              py: { xs: 0.75, sm: 1 },
              fontSize: { xs: "0.7rem", sm: "0.875rem" },
            },
          }}
        >
          <TableHead>
            <TableRow>
              <TableCell sx={{ whiteSpace: "nowrap" }}>Sipariş No</TableCell>
              <TableCell sx={{ whiteSpace: "nowrap" }}>Tedarikçi</TableCell>
              <TableCell
                sx={{
                  whiteSpace: "nowrap",
                  display: { xs: "none", sm: "table-cell" },
                }}
              >
                Tarih
              </TableCell>
              <TableCell
                sx={{
                  whiteSpace: "nowrap",
                  display: { xs: "none", sm: "table-cell" },
                }}
              >
                Teslim Tarihi
              </TableCell>
              <TableCell align="right" sx={{ whiteSpace: "nowrap" }}>
                Toplam
              </TableCell>
              <TableCell sx={{ whiteSpace: "nowrap" }}>Luca Durumu</TableCell>
              <TableCell align="center" sx={{ whiteSpace: "nowrap" }}>
                İşlemler
              </TableCell>
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
                  <Typography color="textSecondary">
                    Sipariş bulunamadı
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              orders.map((order) => (
                <TableRow key={order.id} hover>
                  <TableCell>
                    <Typography fontWeight="medium">{order.orderNo}</Typography>
                    {order.lucaDocumentNo && (
                      <Typography variant="caption" color="textSecondary">
                        Koza: {order.lucaDocumentNo}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{order.supplierName || "-"}</TableCell>
                  <TableCell
                    sx={{
                      display: { xs: "none", sm: "table-cell" },
                    }}
                  >
                    {formatDate(order.orderDate)}
                  </TableCell>
                  <TableCell
                    sx={{
                      display: { xs: "none", sm: "table-cell" },
                    }}
                  >
                    {formatDate(order.expectedDate)}
                  </TableCell>
                  <TableCell align="right">
                    {formatCurrency(order.totalAmount)}
                  </TableCell>
                  <TableCell>
                    <LucaStatusBadge
                      status={getSyncStatus(order)}
                      error={order.lastSyncError}
                      lucaId={order.lucaPurchaseOrderId}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Görüntüle">
                      <IconButton
                        size="small"
                        onClick={() => {
                          setSelectedOrderId(order.id);
                          setView("detail");
                        }}
                      >
                        <ViewIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {!order.isSyncedToLuca && (
                      <Tooltip title="Koza'ya Gönder">
                        <span>
                          <IconButton
                            size="small"
                            color="primary"
                            onClick={() => handleSyncOrder(order.id)}
                            disabled={syncing}
                          >
                            <CloudUploadIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    )}
                    <Tooltip title="Sil">
                      <span>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleDelete(order.id)}
                          disabled={deleting === order.id || order.isSyncedToLuca}
                        >
                          {deleting === order.id ? (
                            <CircularProgress size={16} />
                          ) : (
                            <DeleteIcon fontSize="small" />
                          )}
                        </IconButton>
                      </span>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );

  // ===== RENDER: DETAIL VIEW =====

  const renderDetailView = () => {
    if (detailLoading) {
      return (
        <Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
          <CircularProgress />
        </Box>
      );
    }

    if (!orderDetail) {
      return <Alert severity="error">Sipariş bulunamadı</Alert>;
    }

    const syncStatus = getSyncStatus(orderDetail);

    return (
      <Box>
        {/* Header */}
        <Box sx={{ display: "flex", alignItems: "center", mb: 3, gap: 2 }}>
          <IconButton onClick={() => setView("list")}>
            <BackIcon />
          </IconButton>
          <Typography variant="h5" sx={{ flex: 1 }}>
            Tedarik Siparişi: {orderDetail.orderNo}
          </Typography>
          <LucaStatusBadge
            status={syncStatus}
            error={orderDetail.lastSyncError}
            lucaId={orderDetail.lucaPurchaseOrderId}
          />
        </Box>

        <Grid container spacing={3}>
          {/* A) Temel Bilgiler */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardHeader title="Tedarik Siparişi Bilgileri" />
              <Divider />
              <CardContent>
                <Box
                  sx={{
                    display: "grid",
                    gap: 2,
                    gridTemplateColumns: "1fr 1fr",
                  }}
                >
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Sipariş No
                    </Typography>
                    <Typography>{orderDetail.orderNo}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Durum
                    </Typography>
                    <Typography>{orderDetail.status}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Tedarikçi
                    </Typography>
                    <Typography>{orderDetail.supplierName}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Tedarikçi Kodu
                    </Typography>
                    <Typography>{orderDetail.supplierCode || "-"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Sipariş Tarihi
                    </Typography>
                    <Typography>{formatDate(orderDetail.orderDate)}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Teslim Tarihi
                    </Typography>
                    <Typography>
                      {formatDate(orderDetail.expectedDate)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Toplam
                    </Typography>
                    <Typography fontWeight="bold">
                      {formatCurrency(orderDetail.totalAmount)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      KDV Dahil
                    </Typography>
                    <Typography>
                      {orderDetail.vatIncluded ? "Evet" : "Hayır"}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Oluşturulma
                    </Typography>
                    <Typography>
                      {formatDateTime(orderDetail.createdAt)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Güncelleme
                    </Typography>
                    <Typography>
                      {formatDateTime(orderDetail.updatedAt)}
                    </Typography>
                  </Box>
                </Box>
                {orderDetail.description && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="caption" color="textSecondary">
                      Açıklama
                    </Typography>
                    <Typography>{orderDetail.description}</Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* B) Luca Sync Panel */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Card
              sx={{
                borderColor:
                  syncStatus === "synced"
                    ? "success.main"
                    : syncStatus === "error"
                    ? "error.main"
                    : "grey.300",
                borderWidth: 2,
                borderStyle: "solid",
              }}
            >
              <CardHeader
                title="Koza Senkronizasyon Durumu"
                avatar={
                  syncStatus === "synced" ? (
                    <CheckCircleIcon color="success" />
                  ) : syncStatus === "error" ? (
                    <ErrorIcon color="error" />
                  ) : (
                    <PendingIcon color="disabled" />
                  )
                }
              />
              <Divider />
              <CardContent>
                <Box
                  sx={{
                    display: "grid",
                    gap: 2,
                    gridTemplateColumns: "1fr 1fr",
                  }}
                >
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Durum
                    </Typography>
                    <Box sx={{ mt: 0.5 }}>
                      <LucaStatusBadge
                        status={syncStatus}
                        error={orderDetail.lastSyncError}
                        lucaId={orderDetail.lucaPurchaseOrderId}
                      />
                    </Box>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Koza ID
                    </Typography>
                    <Typography fontWeight="bold">
                      {orderDetail.lucaPurchaseOrderId || "-"}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Koza Belge No
                    </Typography>
                    <Typography>{orderDetail.lucaDocumentNo || "-"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Son Sync
                    </Typography>
                    <Typography>
                      {formatDateTime(orderDetail.lastSyncAt)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Belge Seri
                    </Typography>
                    <Typography>{orderDetail.documentSeries || "-"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Deneme Sayısı
                    </Typography>
                    <Typography>{orderDetail.syncRetryCount}</Typography>
                  </Box>
                </Box>

                {orderDetail.lastSyncError && (
                  <Alert severity="error" sx={{ mt: 2 }}>
                    <Typography variant="subtitle2">Hata Detayı:</Typography>
                    <Typography variant="body2">
                      {orderDetail.lastSyncError}
                    </Typography>
                  </Alert>
                )}

                <Box sx={{ mt: 3, display: "flex", gap: 1 }}>
                  {!orderDetail.isSyncedToLuca && (
                    <Button
                      variant="contained"
                      color="primary"
                      startIcon={
                        syncing ? (
                          <CircularProgress size={16} color="inherit" />
                        ) : (
                          <CloudUploadIcon />
                        )
                      }
                      onClick={() => handleSyncOrder(orderDetail.id)}
                      disabled={syncing}
                    >
                      {orderDetail.lastSyncError
                        ? "Tekrar Dene"
                        : "Koza'ya Gönder"}
                    </Button>
                  )}
                  {orderDetail.isSyncedToLuca && (
                    <Chip
                      icon={<CheckCircleIcon />}
                      label="Senkronizasyon Tamamlandı"
                      color="success"
                    />
                  )}
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* C) Sipariş Satırları */}
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardHeader title="Sipariş Satırları" />
              <Divider />
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>#</TableCell>
                      <TableCell>Ürün</TableCell>
                      <TableCell>SKU</TableCell>
                      <TableCell>Luca Stok Kodu</TableCell>
                      <TableCell>Depo</TableCell>
                      <TableCell align="right">Miktar</TableCell>
                      <TableCell align="right">Birim Fiyat</TableCell>
                      <TableCell align="right">KDV %</TableCell>
                      <TableCell align="right">İndirim</TableCell>
                      <TableCell align="right">Toplam</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {orderDetail.items.map((item, index) => (
                      <TableRow key={item.id}>
                        <TableCell>{index + 1}</TableCell>
                        <TableCell>{item.productName || "-"}</TableCell>
                        <TableCell>{item.productSku || "-"}</TableCell>
                        <TableCell>
                          {item.lucaStockCode ? (
                            <Chip
                              label={item.lucaStockCode}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          ) : (
                            <Chip
                              label="Yok"
                              size="small"
                              color="warning"
                              icon={<WarningIcon />}
                            />
                          )}
                        </TableCell>
                        <TableCell>{item.warehouseCode}</TableCell>
                        <TableCell align="right">{item.quantity}</TableCell>
                        <TableCell align="right">
                          {formatCurrency(item.unitPrice)}
                        </TableCell>
                        <TableCell align="right">%{item.vatRate}</TableCell>
                        <TableCell align="right">
                          {formatCurrency(item.discountAmount)}
                        </TableCell>
                        <TableCell align="right" sx={{ fontWeight: "bold" }}>
                          {formatCurrency(
                            item.quantity * item.unitPrice - item.discountAmount
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          </Grid>
        </Grid>

        {/* Actions */}
        <Box sx={{ mt: 3, display: "flex", gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<BackIcon />}
            onClick={() => setView("list")}
          >
            Listeye Dön
          </Button>
          {!orderDetail.isSyncedToLuca && (
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteIcon />}
              onClick={() => handleDelete(orderDetail.id)}
              disabled={deleting === orderDetail.id}
            >
              Sil
            </Button>
          )}
        </Box>
      </Box>
    );
  };

  // ===== RENDER: CREATE VIEW =====

  const renderCreateView = () => {
    // Filter suppliers that have code (for Luca compatibility)
    const validSuppliers = suppliers.filter((s) => s.isActive !== false);

    return (
      <Box>
        {/* Header */}
        <Box sx={{ display: "flex", alignItems: "center", mb: 3, gap: 2 }}>
          <IconButton onClick={() => setView("list")}>
            <BackIcon />
          </IconButton>
          <Typography variant="h5">Yeni Tedarik Siparişi</Typography>
        </Box>

        {/* Form */}
        <Grid container spacing={3}>
          {/* A) Sipariş Bilgileri */}
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardHeader title="Tedarik Siparişi Bilgileri" />
              <Divider />
              <CardContent>
                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, md: 6 }}>
                    <FormControl
                      fullWidth
                      size="small"
                      error={!!formErrors.supplierId}
                    >
                      <InputLabel>Tedarikçi *</InputLabel>
                      <Select
                        value={formData.supplierId}
                        label="Tedarikçi *"
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            supplierId: e.target.value as number,
                          })
                        }
                      >
                        <MenuItem value={0}>Seçiniz</MenuItem>
                        {validSuppliers.map((s) => (
                          <MenuItem key={s.id} value={s.id}>
                            <Box
                              sx={{
                                display: "flex",
                                alignItems: "center",
                                gap: 1,
                              }}
                            >
                              {s.name}
                              {s.code && (
                                <Chip
                                  label={s.code}
                                  size="small"
                                  color="primary"
                                  variant="outlined"
                                />
                              )}
                              {!s.code && (
                                <Tooltip title="Bu tedarikçinin Koza kodu yok">
                                  <WarningIcon
                                    fontSize="small"
                                    color="warning"
                                  />
                                </Tooltip>
                              )}
                            </Box>
                          </MenuItem>
                        ))}
                      </Select>
                      {formErrors.supplierId && (
                        <FormHelperText>{formErrors.supplierId}</FormHelperText>
                      )}
                    </FormControl>
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <TextField
                      label="Sipariş Tarihi *"
                      type="date"
                      value={formData.orderDate}
                      onChange={(e) =>
                        setFormData({ ...formData, orderDate: e.target.value })
                      }
                      size="small"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                      error={!!formErrors.orderDate}
                      helperText={formErrors.orderDate}
                    />
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <TextField
                      label="Teslim Tarihi"
                      type="date"
                      value={formData.expectedDate}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          expectedDate: e.target.value,
                        })
                      }
                      size="small"
                      fullWidth
                      slotProps={{ inputLabel: { shrink: true } }}
                    />
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <TextField
                      label="Belge Serisi"
                      value={formData.documentSeries}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          documentSeries: e.target.value,
                        })
                      }
                      size="small"
                      fullWidth
                    />
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Belge Türü</InputLabel>
                      <Select
                        value={formData.documentTypeDetailId}
                        label="Belge Türü"
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            documentTypeDetailId: e.target.value as number,
                          })
                        }
                      >
                        {DOCUMENT_TYPES.map((dt) => (
                          <MenuItem key={dt.id} value={dt.id}>
                            {dt.label}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <TextField
                      label="Proje Kodu"
                      value={formData.projectCode}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          projectCode: e.target.value,
                        })
                      }
                      size="small"
                      fullWidth
                    />
                  </Grid>

                  <Grid size={{ xs: 12, md: 3 }}>
                    <FormControlLabel
                      control={
                        <Checkbox
                          checked={formData.vatIncluded}
                          onChange={(e) =>
                            setFormData({
                              ...formData,
                              vatIncluded: e.target.checked,
                            })
                          }
                        />
                      }
                      label="KDV Dahil"
                    />
                  </Grid>

                  <Grid size={{ xs: 12 }}>
                    <TextField
                      label="Açıklama"
                      value={formData.description}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          description: e.target.value,
                        })
                      }
                      size="small"
                      fullWidth
                      multiline
                      rows={2}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          {/* B) Sipariş Satırları */}
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardHeader
                title="Sipariş Satırları"
                action={
                  <Button
                    variant="contained"
                    color="primary"
                    startIcon={<AddIcon />}
                    onClick={addLineItem}
                    size="small"
                  >
                    Satır Ekle
                  </Button>
                }
              />
              <Divider />
              {formErrors.items && (
                <Alert severity="error" sx={{ m: 2 }}>
                  {formErrors.items}
                </Alert>
              )}
              <TableContainer sx={{ overflowX: 'auto' }}>
                <Table size="small" sx={{ minWidth: 900 }}>
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ minWidth: 180 }}>Ürün *</TableCell>
                      <TableCell sx={{ minWidth: 100 }}>Luca Stok Kodu</TableCell>
                      <TableCell sx={{ minWidth: 70 }}>Depo</TableCell>
                      <TableCell sx={{ minWidth: 70 }}>Miktar *</TableCell>
                      <TableCell sx={{ minWidth: 90 }}>Birim Fiyat *</TableCell>
                      <TableCell sx={{ minWidth: 70 }}>KDV %</TableCell>
                      <TableCell sx={{ minWidth: 70 }}>Birim</TableCell>
                      <TableCell sx={{ minWidth: 80 }}>İndirim</TableCell>
                      <TableCell sx={{ minWidth: 90 }} align="right">
                        Toplam
                      </TableCell>
                      <TableCell sx={{ minWidth: 40 }}></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {formData.items.map((item, index) => {
                      const product = products.find(
                        (p) => p.id === item.productId
                      );
                      const hasLucaCode =
                        !!item.lucaStockCode || !!product?.sku;

                      return (
                        <TableRow key={`item-${item.productId}-${index}`}>
                          <TableCell>
                            <FormControl
                              fullWidth
                              size="small"
                              error={!!formErrors[`item_${index}_product`]}
                            >
                              <Select
                                value={item.productId}
                                onChange={(e) =>
                                  updateLineItem(
                                    index,
                                    "productId",
                                    e.target.value as number
                                  )
                                }
                                displayEmpty
                              >
                                <MenuItem value={0}>Ürün Seçin</MenuItem>
                                {products
                                  .filter((p) => p.isActive !== false)
                                  .map((p) => (
                                    <MenuItem key={p.id} value={p.id}>
                                      <Box
                                        sx={{
                                          display: "flex",
                                          alignItems: "center",
                                          gap: 1,
                                        }}
                                      >
                                        {p.name}
                                        {!p.sku && !p.lucaStockCode && (
                                          <Tooltip title="Koza stok kodu yok">
                                            <WarningIcon
                                              fontSize="small"
                                              color="warning"
                                            />
                                          </Tooltip>
                                        )}
                                      </Box>
                                    </MenuItem>
                                  ))}
                              </Select>
                            </FormControl>
                          </TableCell>
                          <TableCell>
                            <TextField
                              size="small"
                              value={item.lucaStockCode}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "lucaStockCode",
                                  e.target.value
                                )
                              }
                              fullWidth
                              placeholder={product?.sku || "Stok kodu girin"}
                              error={!hasLucaCode && item.productId > 0}
                            />
                          </TableCell>
                          <TableCell>
                            <Select
                              size="small"
                              value={item.warehouseCode}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "warehouseCode",
                                  e.target.value
                                )
                              }
                              fullWidth
                            >
                              {WAREHOUSES.map((w) => (
                                <MenuItem key={w.code} value={w.code}>
                                  {w.code}
                                </MenuItem>
                              ))}
                            </Select>
                          </TableCell>
                          <TableCell>
                            <TextField
                              size="small"
                              type="number"
                              value={item.quantity}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "quantity",
                                  parseInt(e.target.value) || 0
                                )
                              }
                              fullWidth
                              inputProps={{ min: 1, style: { textAlign: 'center' } }}
                              error={!!formErrors[`item_${index}_quantity`]}
                            />
                          </TableCell>
                          <TableCell>
                            <TextField
                              size="small"
                              type="number"
                              value={item.unitPrice}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "unitPrice",
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              fullWidth
                              inputProps={{ min: 0, step: 0.01, style: { textAlign: 'right' } }}
                              error={!!formErrors[`item_${index}_price`]}
                            />
                          </TableCell>
                          <TableCell>
                            <Select
                              size="small"
                              value={item.vatRate}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "vatRate",
                                  e.target.value as number
                                )
                              }
                              fullWidth
                            >
                              {VAT_RATES.map((rate) => (
                                <MenuItem key={rate} value={rate}>
                                  %{rate}
                                </MenuItem>
                              ))}
                            </Select>
                          </TableCell>
                          <TableCell>
                            <Select
                              size="small"
                              value={item.unitCode}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "unitCode",
                                  e.target.value
                                )
                              }
                              fullWidth
                            >
                              {UNIT_CODES.map((u) => (
                                <MenuItem key={u.code} value={u.code}>
                                  {u.code}
                                </MenuItem>
                              ))}
                            </Select>
                          </TableCell>
                          <TableCell>
                            <TextField
                              size="small"
                              type="number"
                              value={item.discountAmount}
                              onChange={(e) =>
                                updateLineItem(
                                  index,
                                  "discountAmount",
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              fullWidth
                              inputProps={{ min: 0, step: 0.01, style: { textAlign: 'right' } }}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Typography fontWeight="bold">
                              {formatCurrency(calculateLineTotal(item))}
                            </Typography>
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
                      );
                    })}
                    {/* Total row */}
                    <TableRow>
                      <TableCell colSpan={8} align="right">
                        <Typography fontWeight="bold">Genel Toplam:</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography fontWeight="bold" fontSize="1.1rem">
                          {formatCurrency(calculateFormTotal())}
                        </Typography>
                      </TableCell>
                      <TableCell />
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          </Grid>

          {/* C) Kaydet Butonları - Card içinde */}
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardContent>
                <Box
                  sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}
                >
                  <Button 
                    variant="outlined" 
                    onClick={() => setView("list")}
                    size="large"
                  >
                    İptal
                  </Button>
                  <Button
                    variant="contained"
                    color="primary"
                    size="large"
                    startIcon={
                      saving ? (
                        <CircularProgress size={20} color="inherit" />
                      ) : (
                        <SaveIcon />
                      )
                    }
                    onClick={handleCreate}
                    disabled={saving}
                  >
                    Siparişi Kaydet
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
    );
  };

  // ===== MAIN RENDER =====

  return (
    <Box>
      {view === "list" && renderListView()}
      {view === "detail" && renderDetailView()}
      {view === "create" && renderCreateView()}

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          onClose={() => setSnackbar({ ...snackbar, open: false })}
          severity={snackbar.severity}
          variant="filled"
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default PurchaseOrders;
