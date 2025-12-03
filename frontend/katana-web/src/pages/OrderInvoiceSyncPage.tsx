import React, { useEffect, useState, useCallback } from "react";
import {
  Container,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  Chip,
  Alert,
  Snackbar,
  CircularProgress,
  Box,
  Card,
  CardContent,
  IconButton,
  Tooltip,
  Checkbox,
  TablePagination,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Tabs,
  Tab,
  LinearProgress,
} from "@mui/material";
import {
  Sync as SyncIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Pending as PendingIcon,
  Cancel as CancelIcon,
  Refresh as RefreshIcon,
  Visibility as VisibilityIcon,
  Payment as PaymentIcon,
  Delete as DeleteIcon,
  CloudUpload as CloudUploadIcon,
} from "@mui/icons-material";
import {
  getOrders,
  syncOrder,
  syncBatch,
  syncAllPending,
  closeInvoice,
  deleteInvoice,
  getDashboardStats,
  getOrderDetail,
  OrderListItem,
  OrderDetail,
  DashboardStats,
} from "../services/orderInvoiceSyncApi";

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div role="tabpanel" hidden={value !== index} {...other}>
      {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
    </div>
  );
}

const OrderInvoiceSyncPage: React.FC = () => {
  // State
  const [orders, setOrders] = useState<OrderListItem[]>([]);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [syncLoading, setSyncLoading] = useState<{ [key: number]: boolean }>(
    {}
  );
  const [batchSyncLoading, setBatchSyncLoading] = useState(false);
  const [selectedOrders, setSelectedOrders] = useState<number[]>([]);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "warning" | "info";
  }>({ open: false, message: "", severity: "success" });
  const [tabValue, setTabValue] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState<string>("");

  // Detail Dialog
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  const [selectedOrderDetail, setSelectedOrderDetail] =
    useState<OrderDetail | null>(null);

  // Payment Dialog
  const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);
  const [paymentOrderId, setPaymentOrderId] = useState<number | null>(null);
  const [paymentAmount, setPaymentAmount] = useState<string>("");

  // Load Orders
  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      const response = await getOrders(statusFilter, page + 1, rowsPerPage);
      setOrders(response.data);
      setTotalCount(response.pagination.totalCount);
    } catch (error) {
      showNotification("Siparişler yüklenirken hata oluştu", "error");
    } finally {
      setLoading(false);
    }
  }, [statusFilter, page, rowsPerPage]);

  // Load Dashboard Stats
  const loadStats = useCallback(async () => {
    try {
      const data = await getDashboardStats();
      setStats(data);
    } catch (error) {
      console.error("Dashboard stats yüklenemedi:", error);
    }
  }, []);

  useEffect(() => {
    loadOrders();
    loadStats();
  }, [loadOrders, loadStats]);

  // Notification Helper
  const showNotification = (
    message: string,
    severity: "success" | "error" | "warning" | "info"
  ) => {
    setNotification({ open: true, message, severity });
  };

  // Single Order Sync
  const handleSync = async (orderId: number) => {
    setSyncLoading((prev) => ({ ...prev, [orderId]: true }));
    try {
      const result = await syncOrder(orderId);
      if (result.success) {
        setOrders((prev) =>
          prev.map((o) =>
            o.id === orderId
              ? {
                  ...o,
                  status: "SYNCED" as const,
                  lucaId: result.lucaId,
                  errorMessage: undefined,
                }
              : o
          )
        );
        showNotification(
          `Sipariş başarıyla Luca'ya aktarıldı! Fatura ID: ${result.lucaId}`,
          "success"
        );
        loadStats();
      } else {
        setOrders((prev) =>
          prev.map((o) =>
            o.id === orderId
              ? { ...o, status: "ERROR" as const, errorMessage: result.message }
              : o
          )
        );
        showNotification(`Hata: ${result.message}`, "error");
      }
    } catch (error: any) {
      const errorMsg =
        error.response?.data?.message || "Bilinmeyen bir hata oluştu";
      setOrders((prev) =>
        prev.map((o) =>
          o.id === orderId
            ? { ...o, status: "ERROR" as const, errorMessage: errorMsg }
            : o
        )
      );
      showNotification(`Hata: ${errorMsg}`, "error");
    } finally {
      setSyncLoading((prev) => ({ ...prev, [orderId]: false }));
    }
  };

  // Batch Sync Selected
  const handleBatchSync = async () => {
    if (selectedOrders.length === 0) {
      showNotification("Lütfen en az bir sipariş seçin", "warning");
      return;
    }
    setBatchSyncLoading(true);
    try {
      const result = await syncBatch(selectedOrders);
      showNotification(result.message, result.success ? "success" : "warning");
      setSelectedOrders([]);
      loadOrders();
      loadStats();
    } catch (error: any) {
      showNotification(
        `Toplu senkronizasyon hatası: ${error.message}`,
        "error"
      );
    } finally {
      setBatchSyncLoading(false);
    }
  };

  // Sync All Pending
  const handleSyncAllPending = async () => {
    setBatchSyncLoading(true);
    try {
      const result = await syncAllPending();
      showNotification(result.message, result.success ? "success" : "warning");
      loadOrders();
      loadStats();
    } catch (error: any) {
      showNotification(`Hata: ${error.message}`, "error");
    } finally {
      setBatchSyncLoading(false);
    }
  };

  // Order Detail View
  const handleViewDetail = async (orderId: number) => {
    try {
      const detail = await getOrderDetail(orderId);
      setSelectedOrderDetail(detail);
      setDetailDialogOpen(true);
    } catch (error) {
      showNotification("Sipariş detayı yüklenemedi", "error");
    }
  };

  // Payment Dialog
  const handleOpenPaymentDialog = (orderId: number, amount: number) => {
    setPaymentOrderId(orderId);
    setPaymentAmount(amount.toString());
    setPaymentDialogOpen(true);
  };

  const handleCloseInvoice = async () => {
    if (!paymentOrderId) return;
    try {
      const result = await closeInvoice(
        paymentOrderId,
        parseFloat(paymentAmount)
      );
      if (result.success) {
        showNotification("Fatura başarıyla kapatıldı", "success");
        loadOrders();
      } else {
        showNotification(`Hata: ${result.message}`, "error");
      }
    } catch (error: any) {
      showNotification(`Hata: ${error.message}`, "error");
    } finally {
      setPaymentDialogOpen(false);
      setPaymentOrderId(null);
      setPaymentAmount("");
    }
  };

  // Delete Invoice
  const handleDeleteInvoice = async (orderId: number) => {
    if (!window.confirm("Bu faturayı silmek istediğinizden emin misiniz?"))
      return;
    try {
      const result = await deleteInvoice(orderId);
      if (result.success) {
        showNotification("Fatura başarıyla silindi", "success");
        loadOrders();
        loadStats();
      } else {
        showNotification(`Hata: ${result.message}`, "error");
      }
    } catch (error: any) {
      showNotification(`Hata: ${error.message}`, "error");
    }
  };

  // Selection Handlers
  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      const pendingIds = orders
        .filter((o) => o.status === "PENDING" || o.status === "ERROR")
        .map((o) => o.id);
      setSelectedOrders(pendingIds);
    } else {
      setSelectedOrders([]);
    }
  };

  const handleSelectOrder = (orderId: number) => {
    setSelectedOrders((prev) =>
      prev.includes(orderId)
        ? prev.filter((id) => id !== orderId)
        : [...prev, orderId]
    );
  };

  // Status Chip Renderer
  const getStatusChip = (order: OrderListItem) => {
    switch (order.status) {
      case "SYNCED":
        return (
          <Chip
            icon={<CheckCircleIcon />}
            label="Luca'da Kayıtlı"
            color="success"
            variant="outlined"
            size="small"
          />
        );
      case "ERROR":
        return (
          <Tooltip title={order.errorMessage || "Hata detayı yok"}>
            <Chip
              icon={<ErrorIcon />}
              label="Hata"
              color="error"
              variant="outlined"
              size="small"
              onClick={() =>
                showNotification(
                  order.errorMessage || "Hata detayı yok",
                  "error"
                )
              }
            />
          </Tooltip>
        );
      case "CANCELLED":
        return (
          <Chip
            icon={<CancelIcon />}
            label="İptal"
            color="default"
            variant="outlined"
            size="small"
          />
        );
      default:
        return (
          <Chip
            icon={<PendingIcon />}
            label="Bekliyor"
            color="warning"
            variant="outlined"
            size="small"
          />
        );
    }
  };

  return (
    <Container maxWidth="xl" sx={{ py: 3 }}>
      {/* Header */}
      <Box
        sx={{
          mb: 3,
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
        }}
      >
        <Typography variant="h4" fontWeight="bold">
          Katana ↔ Luca Fatura Entegrasyonu
        </Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => {
              loadOrders();
              loadStats();
            }}
            sx={{ mr: 1 }}
          >
            Yenile
          </Button>
          <Button
            variant="contained"
            color="primary"
            startIcon={
              batchSyncLoading ? (
                <CircularProgress size={20} color="inherit" />
              ) : (
                <CloudUploadIcon />
              )
            }
            onClick={handleSyncAllPending}
            disabled={batchSyncLoading || !stats?.pendingOrders}
          >
            Tümünü Gönder ({stats?.pendingOrders || 0})
          </Button>
        </Box>
      </Box>

      {/* Dashboard Stats */}
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
        <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Toplam Sipariş
              </Typography>
              <Typography variant="h4">{stats?.totalOrders || 0}</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
          <Card sx={{ borderLeft: 4, borderColor: "success.main" }}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Senkronize
              </Typography>
              <Typography variant="h4" color="success.main">
                {stats?.syncedOrders || 0}
              </Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
          <Card sx={{ borderLeft: 4, borderColor: "warning.main" }}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Bekleyen
              </Typography>
              <Typography variant="h4" color="warning.main">
                {stats?.pendingOrders || 0}
              </Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Başarı Oranı
              </Typography>
              <Box sx={{ display: "flex", alignItems: "center" }}>
                <Typography variant="h4">
                  {stats?.syncPercentage || 0}%
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={stats?.syncPercentage || 0}
                  sx={{ ml: 2, flexGrow: 1 }}
                />
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Tabs */}
      <Paper sx={{ mb: 2 }}>
        <Tabs
          value={tabValue}
          onChange={(_, v) => {
            setTabValue(v);
            setStatusFilter(
              v === 0 ? "" : v === 1 ? "PENDING" : v === 2 ? "SYNCED" : "ERROR"
            );
          }}
        >
          <Tab label="Tümü" />
          <Tab label="Bekleyenler" />
          <Tab label="Senkronize" />
          <Tab label="Hatalı" />
        </Tabs>
      </Paper>

      {/* Orders Table */}
      <Paper sx={{ width: "100%", overflow: "hidden" }}>
        {batchSyncLoading && <LinearProgress />}

        {/* Batch Actions */}
        {selectedOrders.length > 0 && (
          <Box sx={{ p: 2, bgcolor: "primary.light", color: "white" }}>
            <Typography component="span" sx={{ mr: 2 }}>
              {selectedOrders.length} sipariş seçildi
            </Typography>
            <Button
              variant="contained"
              color="secondary"
              size="small"
              startIcon={
                batchSyncLoading ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <SyncIcon />
                )
              }
              onClick={handleBatchSync}
              disabled={batchSyncLoading}
            >
              Seçilenleri Gönder
            </Button>
          </Box>
        )}

        <TableContainer sx={{ maxHeight: 600 }}>
          <Table stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    onChange={handleSelectAll}
                    checked={
                      selectedOrders.length > 0 &&
                      selectedOrders.length ===
                        orders.filter((o) => o.status !== "SYNCED").length
                    }
                  />
                </TableCell>
                <TableCell>
                  <strong>Sipariş No</strong>
                </TableCell>
                <TableCell>
                  <strong>Müşteri</strong>
                </TableCell>
                <TableCell>
                  <strong>Tarih</strong>
                </TableCell>
                <TableCell align="right">
                  <strong>Tutar</strong>
                </TableCell>
                <TableCell align="center">
                  <strong>Durum</strong>
                </TableCell>
                <TableCell align="center">
                  <strong>İşlemler</strong>
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <CircularProgress />
                  </TableCell>
                </TableRow>
              ) : orders.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <Typography color="textSecondary">
                      Sipariş bulunamadı
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                orders.map((order) => (
                  <TableRow
                    key={order.id}
                    hover
                    sx={{
                      backgroundColor:
                        order.status === "ERROR" ? "error.light" : "inherit",
                      "&:hover": {
                        backgroundColor:
                          order.status === "ERROR" ? "error.light" : undefined,
                      },
                    }}
                  >
                    <TableCell padding="checkbox">
                      {order.status !== "SYNCED" &&
                        order.status !== "CANCELLED" && (
                          <Checkbox
                            checked={selectedOrders.includes(order.id)}
                            onChange={() => handleSelectOrder(order.id)}
                          />
                        )}
                    </TableCell>
                    <TableCell>
                      <Typography fontWeight="medium">
                        {order.orderNo}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {order.itemCount} ürün
                      </Typography>
                    </TableCell>
                    <TableCell>{order.customer}</TableCell>
                    <TableCell>{order.date}</TableCell>
                    <TableCell align="right">
                      <Typography fontWeight="medium">
                        {order.total.toLocaleString("tr-TR", {
                          minimumFractionDigits: 2,
                        })}{" "}
                        {order.currency}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">{getStatusChip(order)}</TableCell>
                    <TableCell align="center">
                      <Box
                        sx={{
                          display: "flex",
                          justifyContent: "center",
                          gap: 0.5,
                        }}
                      >
                        <Tooltip title="Detay">
                          <IconButton
                            size="small"
                            onClick={() => handleViewDetail(order.id)}
                          >
                            <VisibilityIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>

                        {order.status !== "SYNCED" &&
                          order.status !== "CANCELLED" && (
                            <Tooltip title="Luca'ya Gönder">
                              <IconButton
                                size="small"
                                color="primary"
                                onClick={() => handleSync(order.id)}
                                disabled={syncLoading[order.id]}
                              >
                                {syncLoading[order.id] ? (
                                  <CircularProgress size={20} />
                                ) : (
                                  <SyncIcon fontSize="small" />
                                )}
                              </IconButton>
                            </Tooltip>
                          )}

                        {order.status === "SYNCED" && (
                          <>
                            <Tooltip title="Fatura Kapat">
                              <IconButton
                                size="small"
                                color="success"
                                onClick={() =>
                                  handleOpenPaymentDialog(order.id, order.total)
                                }
                              >
                                <PaymentIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Fatura Sil">
                              <IconButton
                                size="small"
                                color="error"
                                onClick={() => handleDeleteInvoice(order.id)}
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </>
                        )}
                      </Box>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <TablePagination
          component="div"
          count={totalCount}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e) => {
            setRowsPerPage(parseInt(e.target.value, 10));
            setPage(0);
          }}
          labelRowsPerPage="Sayfa başına:"
          rowsPerPageOptions={[10, 25, 50, 100]}
        />
      </Paper>

      {/* Order Detail Dialog */}
      <Dialog
        open={detailDialogOpen}
        onClose={() => setDetailDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          Sipariş Detayı - {selectedOrderDetail?.orderNo}
        </DialogTitle>
        <DialogContent>
          {selectedOrderDetail && (
            <Box>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <Box sx={{ flex: "1 1 calc(50% - 8px)", minWidth: 150 }}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Müşteri
                  </Typography>
                  <Typography>{selectedOrderDetail.customer.name}</Typography>
                </Box>
                <Box sx={{ flex: "1 1 calc(50% - 8px)", minWidth: 150 }}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Vergi No
                  </Typography>
                  <Typography>{selectedOrderDetail.customer.taxNo}</Typography>
                </Box>
                <Box sx={{ flex: "1 1 calc(50% - 8px)", minWidth: 150 }}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Tarih
                  </Typography>
                  <Typography>
                    {new Date(selectedOrderDetail.date).toLocaleDateString(
                      "tr-TR"
                    )}
                  </Typography>
                </Box>
                <Box sx={{ flex: "1 1 calc(50% - 8px)", minWidth: 150 }}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Durum
                  </Typography>
                  <Chip
                    label={
                      selectedOrderDetail.isSynced ? "Senkronize" : "Bekliyor"
                    }
                    color={selectedOrderDetail.isSynced ? "success" : "warning"}
                    size="small"
                  />
                </Box>
              </Box>

              <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
                Ürünler
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>SKU</TableCell>
                      <TableCell>Ürün</TableCell>
                      <TableCell align="right">Miktar</TableCell>
                      <TableCell align="right">Birim Fiyat</TableCell>
                      <TableCell align="right">Toplam</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {selectedOrderDetail.items.map((item, idx) => (
                      <TableRow key={idx}>
                        <TableCell>{item.sku}</TableCell>
                        <TableCell>{item.productName}</TableCell>
                        <TableCell align="right">{item.quantity}</TableCell>
                        <TableCell align="right">
                          {item.unitPrice.toLocaleString("tr-TR", {
                            minimumFractionDigits: 2,
                          })}
                        </TableCell>
                        <TableCell align="right">
                          {item.lineTotal.toLocaleString("tr-TR", {
                            minimumFractionDigits: 2,
                          })}
                        </TableCell>
                      </TableRow>
                    ))}
                    <TableRow>
                      <TableCell colSpan={4} align="right">
                        <strong>Genel Toplam</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>
                          {selectedOrderDetail.total.toLocaleString("tr-TR", {
                            minimumFractionDigits: 2,
                          })}{" "}
                          {selectedOrderDetail.currency}
                        </strong>
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailDialogOpen(false)}>Kapat</Button>
          {selectedOrderDetail && !selectedOrderDetail.isSynced && (
            <Button
              variant="contained"
              color="primary"
              startIcon={<SyncIcon />}
              onClick={() => {
                handleSync(selectedOrderDetail.id);
                setDetailDialogOpen(false);
              }}
            >
              Luca'ya Gönder
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Payment Dialog */}
      <Dialog
        open={paymentDialogOpen}
        onClose={() => setPaymentDialogOpen(false)}
      >
        <DialogTitle>Fatura Kapama</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Tutar"
            type="number"
            fullWidth
            value={paymentAmount}
            onChange={(e) => setPaymentAmount(e.target.value)}
            InputProps={{ endAdornment: "₺" }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPaymentDialogOpen(false)}>İptal</Button>
          <Button
            variant="contained"
            color="primary"
            onClick={handleCloseInvoice}
          >
            Kapat
          </Button>
        </DialogActions>
      </Dialog>

      {/* Notification Snackbar */}
      <Snackbar
        open={notification.open}
        autoHideDuration={6000}
        onClose={() => setNotification({ ...notification, open: false })}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          onClose={() => setNotification({ ...notification, open: false })}
          severity={notification.severity}
          sx={{ width: "100%" }}
        >
          {notification.message}
        </Alert>
      </Snackbar>
    </Container>
  );
};

export default OrderInvoiceSyncPage;
