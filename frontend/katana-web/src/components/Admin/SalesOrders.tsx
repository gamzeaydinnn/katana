import {
  ArrowBack as ArrowBackIcon,
  CheckCircle as CheckCircleIcon,
  CloudUpload as CloudUploadIcon,
  Error as ErrorIcon,
  HourglassEmpty as PendingIcon,
  Refresh as RefreshIcon,
  Sync as SyncIcon,
  Visibility as ViewIcon,
  ThumbUp as ApproveIcon,
  Inventory as StockIcon,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  FormControl,
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
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api from "../../services/api";

// Types
interface SalesOrderSummary {
  id: number;
  orderNo: string;
  customerName?: string;
  orderCreatedDate?: string;
  status: string;
  currency?: string;
  total?: number;
  lucaOrderId?: number;
  isSyncedToLuca: boolean;
  lastSyncError?: string;
  lastSyncAt?: string;
  lucaSyncStatus: "synced" | "error" | "not_synced";
}

interface SalesOrderLine {
  id: number;
  salesOrderId: number;
  katanaRowId: number;
  variantId: number;
  sku: string;
  productName?: string;
  quantity: number;
  pricePerUnit?: number;
  total?: number;
  taxRate?: number;
  locationId?: number;
  lucaDetayId?: number;
  lucaStokId?: number;
  lucaDepoId?: number;
}

interface SalesOrderDetail {
  id: number;
  katanaOrderId: number;
  orderNo: string;
  customerId: number;
  customerName?: string;
  orderCreatedDate?: string;
  deliveryDate?: string;
  currency?: string;
  status: string;
  total?: number;
  totalInBaseCurrency?: number;
  additionalInfo?: string;
  customerRef?: string;
  source?: string;
  locationId?: number;
  // Luca fields
  lucaOrderId?: number;
  belgeSeri?: string;
  belgeNo?: string;
  duzenlemeSaati?: string;
  belgeTurDetayId?: number;
  nakliyeBedeliTuru?: number;
  teklifSiparisTur?: number;
  onayFlag: boolean;
  lastSyncAt?: string;
  lastSyncError?: string;
  isSyncedToLuca: boolean;
  lucaSyncStatus: "synced" | "error" | "not_synced";
  lines: SalesOrderLine[];
}

interface SyncResult {
  isSuccess: boolean;
  message: string;
  lucaOrderId?: number;
  syncedAt?: string;
  errorDetails?: string;
}

interface LucaFieldsForm {
  belgeSeri: string;
  belgeNo: string;
  duzenlemeSaati: string;
  belgeTurDetayId: number;
  nakliyeBedeliTuru: number;
  teklifSiparisTur: number;
  onayFlag: boolean;
  belgeAciklama: string;
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
    <Chip
      icon={<PendingIcon />}
      label="Bekliyor"
      color="default"
      size="small"
    />
  );
};

// CSS Grid helper styles
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
  fullWidth: {
    gridColumn: "1 / -1",
  },
};

const SalesOrders: React.FC = () => {
  // List state
  const [orders, setOrders] = useState<SalesOrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [syncStatusFilter, setSyncStatusFilter] = useState<string>("");

  // Detail state
  const [selectedOrder, setSelectedOrder] = useState<SalesOrderDetail | null>(
    null
  );
  const [detailLoading, setDetailLoading] = useState(false);
  const [showDetail, setShowDetail] = useState(false);

  // Form state
  const [lucaFields, setLucaFields] = useState<LucaFieldsForm>({
    belgeSeri: "",
    belgeNo: "",
    duzenlemeSaati: "",
    belgeTurDetayId: 17,
    nakliyeBedeliTuru: 0,
    teklifSiparisTur: 1,
    onayFlag: true,
    belgeAciklama: "",
  });
  const [saving, setSaving] = useState(false);

  // Sync state
  const [syncing, setSyncing] = useState<number | null>(null);
  const [syncingFromKatana, setSyncingFromKatana] = useState(false);

  // Admin approval state
  const [approving, setApproving] = useState<number | null>(null);
  const [approvalDialog, setApprovalDialog] = useState<{
    open: boolean;
    orderId: number | null;
    orderNo: string;
  }>({ open: false, orderId: null, orderNo: "" });

  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "info";
  }>({
    open: false,
    message: "",
    severity: "success",
  });

  // Sync orders from Katana
  const syncFromKatana = async () => {
    try {
      setSyncingFromKatana(true);
      const response = await api.post<{
        isSuccess: boolean;
        message?: string;
        processedRecords?: number;
      }>("/sync/from-katana/sales-orders?days=7");
      const result = response.data;

      setSnackbar({
        open: true,
        message:
          result.message ||
          `${result.processedRecords || 0} sipariş senkronize edildi`,
        severity: result.isSuccess ? "success" : "error",
      });

      // Listeyi yenile
      await fetchOrders();
    } catch (err: any) {
      console.error("Failed to sync from Katana:", err);
      setSnackbar({
        open: true,
        message:
          err.response?.data?.message || "Katana senkronizasyonu başarısız",
        severity: "error",
      });
    } finally {
      setSyncingFromKatana(false);
    }
  };

  // Fetch orders
  const fetchOrders = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      params.append("page", String(page + 1));
      params.append("pageSize", String(rowsPerPage));
      if (statusFilter) params.append("status", statusFilter);
      if (syncStatusFilter) params.append("syncStatus", syncStatusFilter);

      const response = await api.get<SalesOrderSummary[]>(
        `/sales-orders?${params}`
      );
      setOrders(response.data);
    } catch (err) {
      console.error("Failed to fetch orders:", err);
      setSnackbar({
        open: true,
        message: "Siparişler yüklenemedi",
        severity: "error",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, [page, rowsPerPage, statusFilter, syncStatusFilter]);

  // Fetch order detail
  const fetchOrderDetail = async (id: number) => {
    try {
      setDetailLoading(true);
      const response = await api.get<SalesOrderDetail>(`/sales-orders/${id}`);
      setSelectedOrder(response.data);

      // Populate form
      setLucaFields({
        belgeSeri: response.data.belgeSeri || "",
        belgeNo: response.data.belgeNo || "",
        duzenlemeSaati: response.data.duzenlemeSaati || "",
        belgeTurDetayId: response.data.belgeTurDetayId || 17,
        nakliyeBedeliTuru: response.data.nakliyeBedeliTuru || 0,
        teklifSiparisTur: response.data.teklifSiparisTur || 1,
        onayFlag: response.data.onayFlag ?? true,
        belgeAciklama: response.data.additionalInfo || "",
      });

      setShowDetail(true);
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

  // Save Luca fields
  const saveLucaFields = async () => {
    if (!selectedOrder) return;

    try {
      setSaving(true);
      await api.patch(
        `/sales-orders/${selectedOrder.id}/luca-fields`,
        lucaFields
      );
      setSnackbar({
        open: true,
        message: "Koza alanları kaydedildi",
        severity: "success",
      });
      // Refresh detail
      await fetchOrderDetail(selectedOrder.id);
    } catch (err) {
      console.error("Failed to save Luca fields:", err);
      setSnackbar({
        open: true,
        message: "Kaydetme başarısız",
        severity: "error",
      });
    } finally {
      setSaving(false);
    }
  };

  // Sync to Luca
  const syncToLuca = async (orderId: number) => {
    try {
      setSyncing(orderId);
      const response = await api.post<SyncResult>(
        `/sales-orders/${orderId}/sync`
      );

      if (response.data.isSuccess) {
        setSnackbar({
          open: true,
          message: `Koza'ya senkronize edildi (ID: ${response.data.lucaOrderId})`,
          severity: "success",
        });
      } else {
        setSnackbar({
          open: true,
          message: response.data.message || "Senkronizasyon başarısız",
          severity: "error",
        });
      }

      // Refresh data
      if (showDetail && selectedOrder?.id === orderId) {
        await fetchOrderDetail(orderId);
      }
      await fetchOrders();
    } catch (err) {
      console.error("Sync failed:", err);
      setSnackbar({
        open: true,
        message: "Senkronizasyon hatası",
        severity: "error",
      });
    } finally {
      setSyncing(null);
    }
  };

  // Admin Approval - Siparişi onayla ve Katana'ya stok olarak ekle
  const handleApproveOrder = async (orderId: number) => {
    try {
      setApproving(orderId);
      setApprovalDialog({ open: false, orderId: null, orderNo: "" });

      // Backend'e onay isteği gönder - bu Katana'ya stok ekleyecek
      const response = await api.post<{ success: boolean; message?: string }>(
        `/sales-orders/${orderId}/approve`
      );

      if (response.data.success) {
        setSnackbar({
          open: true,
          message: `✅ Sipariş onaylandı ve Katana'ya stok olarak eklendi!`,
          severity: "success",
        });
      } else {
        setSnackbar({
          open: true,
          message: response.data.message || "Onaylama başarısız",
          severity: "error",
        });
      }

      // Refresh data
      if (showDetail && selectedOrder?.id === orderId) {
        await fetchOrderDetail(orderId);
      }
      await fetchOrders();
    } catch (err: any) {
      console.error("Approval failed:", err);
      setSnackbar({
        open: true,
        message: err.response?.data?.message || "Onaylama hatası",
        severity: "error",
      });
    } finally {
      setApproving(null);
    }
  };

  // Open approval dialog
  const openApprovalDialog = (orderId: number, orderNo: string) => {
    setApprovalDialog({ open: true, orderId, orderNo });
  };

  // Close approval dialog
  const closeApprovalDialog = () => {
    setApprovalDialog({ open: false, orderId: null, orderNo: "" });
  };

  // Back to list
  const handleBackToList = () => {
    setShowDetail(false);
    setSelectedOrder(null);
    fetchOrders();
  };

  // Format date
  const formatDate = (dateStr?: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleDateString("tr-TR");
  };

  const formatDateTime = (dateStr?: string) => {
    if (!dateStr) return "-";
    return new Date(dateStr).toLocaleString("tr-TR");
  };

  const formatCurrency = (amount?: number, currency?: string) => {
    if (amount == null) return "-";
    return new Intl.NumberFormat("tr-TR", {
      style: "currency",
      currency: currency || "TRY",
    }).format(amount);
  };

  // Render list view
  const renderList = () => (
    <Box>
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "flex-end",
          alignItems: "center",
          gap: 2,
          mb: 3,
        }}
      >
        <Button
          variant="contained"
          color="primary"
          startIcon={
            syncingFromKatana ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <SyncIcon />
            )
          }
          onClick={syncFromKatana}
          disabled={syncingFromKatana || loading}
        >
          {syncingFromKatana ? "Senkronize ediliyor..." : "Katana'dan Çek"}
        </Button>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={fetchOrders}
          disabled={loading}
        >
          Yenile
        </Button>
      </Box>

      {/* Filters */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={gridStyles.threeCol}>
          <FormControl size="small">
            <InputLabel>Sipariş Durumu</InputLabel>
            <Select
              value={statusFilter}
              label="Sipariş Durumu"
              onChange={(e) => setStatusFilter(e.target.value)}
            >
              <MenuItem value="">Tümü</MenuItem>
              <MenuItem value="NOT_SHIPPED">Gönderilmedi</MenuItem>
              <MenuItem value="PARTIALLY_SHIPPED">Kısmen Gönderildi</MenuItem>
              <MenuItem value="SHIPPED">Gönderildi</MenuItem>
              <MenuItem value="CANCELLED">İptal</MenuItem>
            </Select>
          </FormControl>

          <FormControl size="small">
            <InputLabel>Koza Durumu</InputLabel>
            <Select
              value={syncStatusFilter}
              label="Koza Durumu"
              onChange={(e) => setSyncStatusFilter(e.target.value)}
            >
              <MenuItem value="">Tümü</MenuItem>
              <MenuItem value="synced">Senkronize</MenuItem>
              <MenuItem value="error">Hatalı</MenuItem>
              <MenuItem value="not_synced">Bekliyor</MenuItem>
            </Select>
          </FormControl>
        </Box>
      </Paper>

      {/* Table */}
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Sipariş No</TableCell>
              <TableCell>Müşteri</TableCell>
              <TableCell>Tarih</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell align="right">Tutar</TableCell>
              <TableCell>Koza</TableCell>
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
                  </TableCell>
                  <TableCell>{order.customerName || "-"}</TableCell>
                  <TableCell>{formatDate(order.orderCreatedDate)}</TableCell>
                  <TableCell>
                    <Chip
                      label={order.status}
                      size="small"
                      color={
                        order.status === "SHIPPED"
                          ? "success"
                          : order.status === "CANCELLED"
                          ? "error"
                          : "default"
                      }
                    />
                  </TableCell>
                  <TableCell align="right">
                    {formatCurrency(order.total, order.currency)}
                  </TableCell>
                  <TableCell>
                    <LucaStatusBadge
                      status={order.lucaSyncStatus}
                      error={order.lastSyncError}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Detay">
                      <IconButton
                        size="small"
                        onClick={() => fetchOrderDetail(order.id)}
                        disabled={detailLoading}
                      >
                        <ViewIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {/* Admin Onay Butonu - PENDING durumundaki siparişler için */}
                    {(order.status === "PENDING" ||
                      order.status === "NOT_SHIPPED") && (
                      <Tooltip title="Onayla ve Katana'ya Stok Ekle">
                        <IconButton
                          size="small"
                          onClick={() =>
                            openApprovalDialog(order.id, order.orderNo)
                          }
                          disabled={approving === order.id}
                          color="success"
                        >
                          {approving === order.id ? (
                            <CircularProgress size={16} />
                          ) : (
                            <ApproveIcon fontSize="small" />
                          )}
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Koza'ya Senkronize Et">
                      <IconButton
                        size="small"
                        onClick={() => syncToLuca(order.id)}
                        disabled={syncing === order.id}
                        color="primary"
                      >
                        {syncing === order.id ? (
                          <CircularProgress size={16} />
                        ) : (
                          <SyncIcon fontSize="small" />
                        )}
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={-1}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e) => {
            setRowsPerPage(parseInt(e.target.value, 10));
            setPage(0);
          }}
          labelRowsPerPage="Sayfa başına"
        />
      </TableContainer>
    </Box>
  );

  // Render detail view
  const renderDetail = () => {
    if (!selectedOrder) return null;

    return (
      <Box>
        {/* Header */}
        <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
          <IconButton onClick={handleBackToList}>
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h5">
            Satış Siparişi: {selectedOrder.orderNo}
          </Typography>
          <LucaStatusBadge
            status={selectedOrder.lucaSyncStatus}
            error={selectedOrder.lastSyncError}
          />
        </Box>

        <Box
          sx={{
            display: "grid",
            gap: 3,
            gridTemplateColumns: { xs: "1fr", lg: "2fr 1fr" },
          }}
        >
          {/* Left Column */}
          <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
            {/* (A) Katana Info */}
            <Card>
              <CardHeader title="Sipariş Bilgileri (Katana)" />
              <Divider />
              <CardContent>
                <Box sx={gridStyles.container}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Sipariş No
                    </Typography>
                    <Typography>{selectedOrder.orderNo}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Katana ID
                    </Typography>
                    <Typography>{selectedOrder.katanaOrderId}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Müşteri
                    </Typography>
                    <Typography>{selectedOrder.customerName || "-"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Sipariş Tarihi
                    </Typography>
                    <Typography>
                      {formatDate(selectedOrder.orderCreatedDate)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Teslim Tarihi
                    </Typography>
                    <Typography>
                      {formatDate(selectedOrder.deliveryDate)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Durum
                    </Typography>
                    <Chip label={selectedOrder.status} size="small" />
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Para Birimi
                    </Typography>
                    <Typography>{selectedOrder.currency || "TRY"}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Toplam
                    </Typography>
                    <Typography fontWeight="bold">
                      {formatCurrency(
                        selectedOrder.total,
                        selectedOrder.currency
                      )}
                    </Typography>
                  </Box>
                </Box>

                {selectedOrder.additionalInfo && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="caption" color="textSecondary">
                      Ek Bilgi
                    </Typography>
                    <Typography>{selectedOrder.additionalInfo}</Typography>
                  </Box>
                )}
              </CardContent>
            </Card>

            {/* Lines Table */}
            <Card>
              <CardHeader title="Satış Siparişi Kalemleri" />
              <Divider />
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>SKU</TableCell>
                      <TableCell>Ürün</TableCell>
                      <TableCell align="right">Miktar</TableCell>
                      <TableCell align="right">Birim Fiyat</TableCell>
                      <TableCell align="right">KDV %</TableCell>
                      <TableCell align="right">Toplam</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {selectedOrder.lines.map((line) => (
                      <TableRow key={line.id}>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace">
                            {line.sku}
                          </Typography>
                        </TableCell>
                        <TableCell>{line.productName || "-"}</TableCell>
                        <TableCell align="right">{line.quantity}</TableCell>
                        <TableCell align="right">
                          {formatCurrency(
                            line.pricePerUnit,
                            selectedOrder.currency
                          )}
                        </TableCell>
                        <TableCell align="right">
                          {line.taxRate || 0}%
                        </TableCell>
                        <TableCell align="right">
                          {formatCurrency(line.total, selectedOrder.currency)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>

            {/* (B) Luca Fields Form */}
            <Card>
              <CardHeader title="Koza Entegrasyon Alanları" />
              <Divider />
              <CardContent>
                <Box sx={gridStyles.threeCol}>
                  <TextField
                    label="Belge Seri"
                    value={lucaFields.belgeSeri}
                    onChange={(e) =>
                      setLucaFields({
                        ...lucaFields,
                        belgeSeri: e.target.value,
                      })
                    }
                    size="small"
                    placeholder="SAT"
                  />
                  <TextField
                    label="Belge No"
                    value={lucaFields.belgeNo}
                    onChange={(e) =>
                      setLucaFields({ ...lucaFields, belgeNo: e.target.value })
                    }
                    size="small"
                  />
                  <TextField
                    label="Düzenleme Saati"
                    value={lucaFields.duzenlemeSaati}
                    onChange={(e) =>
                      setLucaFields({
                        ...lucaFields,
                        duzenlemeSaati: e.target.value,
                      })
                    }
                    size="small"
                    placeholder="HH:mm"
                  />
                </Box>

                <Box sx={{ ...gridStyles.threeCol, mt: 2 }}>
                  <FormControl size="small">
                    <InputLabel>Onay Durumu</InputLabel>
                    <Select
                      value={lucaFields.onayFlag ? 1 : 0}
                      label="Onay Durumu"
                      onChange={(e) =>
                        setLucaFields({
                          ...lucaFields,
                          onayFlag: e.target.value === 1,
                        })
                      }
                    >
                      <MenuItem value={1}>Onaylı</MenuItem>
                      <MenuItem value={0}>Onaysız</MenuItem>
                    </Select>
                  </FormControl>

                  <FormControl size="small">
                    <InputLabel>Sipariş Türü</InputLabel>
                    <Select
                      value={lucaFields.teklifSiparisTur}
                      label="Sipariş Türü"
                      onChange={(e) =>
                        setLucaFields({
                          ...lucaFields,
                          teklifSiparisTur: e.target.value as number,
                        })
                      }
                    >
                      <MenuItem value={0}>Standart</MenuItem>
                      <MenuItem value={1}>Teklif</MenuItem>
                      <MenuItem value={2}>Proforma</MenuItem>
                    </Select>
                  </FormControl>

                  <FormControl size="small">
                    <InputLabel>Nakliye Bedeli Türü</InputLabel>
                    <Select
                      value={lucaFields.nakliyeBedeliTuru}
                      label="Nakliye Bedeli Türü"
                      onChange={(e) =>
                        setLucaFields({
                          ...lucaFields,
                          nakliyeBedeliTuru: e.target.value as number,
                        })
                      }
                    >
                      <MenuItem value={0}>Net</MenuItem>
                      <MenuItem value={1}>Brüt</MenuItem>
                    </Select>
                  </FormControl>
                </Box>

                <Box sx={{ mt: 2 }}>
                  <TextField
                    label="Belge Açıklaması"
                    value={lucaFields.belgeAciklama}
                    onChange={(e) =>
                      setLucaFields({
                        ...lucaFields,
                        belgeAciklama: e.target.value,
                      })
                    }
                    size="small"
                    fullWidth
                    multiline
                    rows={2}
                  />
                </Box>

                <Box
                  sx={{ mt: 2, display: "flex", justifyContent: "flex-end" }}
                >
                  <Button
                    variant="contained"
                    onClick={saveLucaFields}
                    disabled={saving}
                  >
                    {saving ? <CircularProgress size={20} /> : "Kaydet"}
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Box>

          {/* Right Column - (C) Sync Panel */}
          <Card>
            <CardHeader title="Koza Senkronizasyon" />
            <Divider />
            <CardContent>
              <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Koza Sipariş ID
                  </Typography>
                  <Typography variant="h6">
                    {selectedOrder.lucaOrderId || "-"}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Durum
                  </Typography>
                  <Box sx={{ mt: 0.5 }}>
                    <LucaStatusBadge
                      status={selectedOrder.lucaSyncStatus}
                      error={selectedOrder.lastSyncError}
                    />
                  </Box>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Son Senkronizasyon
                  </Typography>
                  <Typography>
                    {formatDateTime(selectedOrder.lastSyncAt)}
                  </Typography>
                </Box>

                {selectedOrder.lastSyncError && (
                  <Alert severity="error" sx={{ mt: 1 }}>
                    <Typography variant="body2">
                      {selectedOrder.lastSyncError}
                    </Typography>
                  </Alert>
                )}

                <Divider sx={{ my: 1 }} />

                <Button
                  variant="contained"
                  color="primary"
                  startIcon={
                    syncing === selectedOrder.id ? (
                      <CircularProgress size={20} color="inherit" />
                    ) : (
                      <CloudUploadIcon />
                    )
                  }
                  onClick={() => syncToLuca(selectedOrder.id)}
                  disabled={syncing === selectedOrder.id}
                  fullWidth
                  size="large"
                >
                  Koza'ya Senkronize Et
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>
    );
  };

  return (
    <Box>
      {showDetail ? renderDetail() : renderList()}

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

      {/* Admin Onay Dialog */}
      <Dialog
        open={approvalDialog.open}
        onClose={closeApprovalDialog}
        aria-labelledby="approval-dialog-title"
      >
        <DialogTitle id="approval-dialog-title">
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <ApproveIcon color="success" />
            Siparişi Onayla
          </Box>
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            <strong>{approvalDialog.orderNo}</strong> numaralı siparişi
            onaylamak istediğinize emin misiniz?
            <br />
            <br />
            <Box
              sx={{
                p: 2,
                bgcolor: "success.light",
                borderRadius: 1,
                color: "success.contrastText",
              }}
            >
              <StockIcon sx={{ mr: 1, verticalAlign: "middle" }} />
              Bu işlem siparişi onaylayacak ve ürünleri{" "}
              <strong>Katana sistemine stok olarak ekleyecektir</strong>.
            </Box>
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeApprovalDialog} disabled={approving !== null}>
            İptal
          </Button>
          <Button
            onClick={() =>
              approvalDialog.orderId &&
              handleApproveOrder(approvalDialog.orderId)
            }
            variant="contained"
            color="success"
            disabled={approving !== null}
            startIcon={
              approving !== null ? (
                <CircularProgress size={16} />
              ) : (
                <ApproveIcon />
              )
            }
          >
            Onayla ve Stoğa Ekle
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SalesOrders;
