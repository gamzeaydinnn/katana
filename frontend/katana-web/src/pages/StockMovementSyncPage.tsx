import BuildIcon from "@mui/icons-material/Build";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import CompareArrowsIcon from "@mui/icons-material/CompareArrows";
import ErrorIcon from "@mui/icons-material/Error";
import InfoIcon from "@mui/icons-material/Info";
import PendingIcon from "@mui/icons-material/Pending";
import RefreshIcon from "@mui/icons-material/Refresh";
import SyncIcon from "@mui/icons-material/Sync";
import TrendingDownIcon from "@mui/icons-material/TrendingDown";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import WarehouseIcon from "@mui/icons-material/Warehouse";
import {
    Alert,
    AppBar,
    Box,
    Button,
    Card,
    CardContent,
    Checkbox,
    Chip,
    CircularProgress,
    Container,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    IconButton,
    InputLabel,
    List,
    ListItem,
    ListItemText,
    MenuItem,
    Paper,
    Select,
    Snackbar,
    Tab,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Tabs,
    Toolbar,
    Tooltip,
    Typography
} from "@mui/material";
import React, { useEffect, useState } from "react";

import {
    MovementDashboardStatsDto,
    StockMovementSyncDto,
    getAllMovements,
    getDashboardStats,
    syncAllPending,
    syncBatch,
    syncMovement,
} from "../services/stockMovementSyncApi";

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

const StockMovementSyncPage: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [movements, setMovements] = useState<StockMovementSyncDto[]>([]);
  const [stats, setStats] = useState<MovementDashboardStatsDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [syncingIds, setSyncingIds] = useState<Set<string>>(new Set());
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: "success" | "error" | "info";
  }>({
    open: false,
    message: "",
    severity: "success",
  });

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [typeFilter, setTypeFilter] = useState<string>("");

  // Detail Dialog
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  const [selectedMovement, setSelectedMovement] =
    useState<StockMovementSyncDto | null>(null);

  // Load data on tab change
  useEffect(() => {
    loadData();
  }, [tabValue, statusFilter, typeFilter]);

  const loadData = async () => {
    console.log('[StockMovementSyncPage] loadData başlatıldı:', { tabValue, statusFilter, typeFilter });
    setLoading(true);
    try {
      const [movementsData, statsData] = await Promise.all([
        getAllMovements({
          movementType: typeFilter || undefined,
          syncStatus: statusFilter || undefined,
        }),
        getDashboardStats(),
      ]);
      console.log('[StockMovementSyncPage] loadData başarılı:', {
        movementsCount: movementsData.length,
        stats: statsData
      });
      setMovements(movementsData);
      setStats(statsData);
    } catch (error) {
      console.error('[StockMovementSyncPage] loadData HATA:', error);
      showNotification("Veri yüklenirken hata oluştu", "error");
    } finally {
      setLoading(false);
    }
  };

  const handleSync = async (movement: StockMovementSyncDto) => {
    console.log('[StockMovementSyncPage] handleSync başlatıldı:', {
      movementId: movement.id,
      movementType: movement.movementType,
      documentNo: movement.documentNo
    });
    const key = `${movement.movementType}-${movement.id}`;
    setSyncingIds((prev) => new Set(prev).add(key));

    try {
      console.log('[StockMovementSyncPage] syncMovement API çağrısı yapılıyor...');
      const result = await syncMovement(movement.movementType, movement.id);
      console.log('[StockMovementSyncPage] syncMovement yanıtı:', result);

      if (result.success) {
        console.log('[StockMovementSyncPage] Senkronizasyon başarılı!');
        showNotification(
          `${movement.documentNo} başarıyla Luca'ya aktarıldı!`,
          "success"
        );
        // Update local state
        setMovements((prev) =>
          prev.map((m) =>
            m.id === movement.id && m.movementType === movement.movementType
              ? {
                  ...m,
                  syncStatus: "SYNCED",
                  lucaDocumentId: result.lucaId,
                  errorMessage: undefined,
                }
              : m
          )
        );
      } else {
        console.error('[StockMovementSyncPage] Senkronizasyon başarısız:', result.message);
        showNotification(result.message || "Senkronizasyon başarısız", "error");
      }
    } catch (error: any) {
      console.error('[StockMovementSyncPage] handleSync exception:', {
        movement,
        error: error.message,
        errorResponse: error.response?.data,
        errorStatus: error.response?.status,
        timestamp: new Date().toISOString()
      });
      showNotification(error.response?.data?.message || "Hata oluştu", "error");
    } finally {
      console.log('[StockMovementSyncPage] handleSync tamamlandı:', { movementId: movement.id });
      setSyncingIds((prev) => {
        const newSet = new Set(prev);
        newSet.delete(key);
        return newSet;
      });
    }
  };

  const handleBatchSync = async () => {
    console.log('[StockMovementSyncPage] handleBatchSync başlatıldı:', {
      selectedCount: selectedIds.size,
      selectedIds: Array.from(selectedIds)
    });
    
    if (selectedIds.size === 0) {
      console.warn('[StockMovementSyncPage] handleBatchSync: Hiç hareket seçilmemiş');
      showNotification("Lütfen en az bir hareket seçin", "info");
      return;
    }

    setLoading(true);
    try {
      const transferIds: number[] = [];
      const adjustmentIds: number[] = [];

      selectedIds.forEach((key) => {
        const [type, id] = key.split("-");
        if (type === "TRANSFER") transferIds.push(parseInt(id));
        else adjustmentIds.push(parseInt(id));
      });

      console.log('[StockMovementSyncPage] syncBatch API çağrısı yapılıyor...', { transferIds, adjustmentIds });
      const result = await syncBatch(transferIds, adjustmentIds);
      console.log('[StockMovementSyncPage] syncBatch yanıtı:', result);
      
      showNotification(
        `${result.successCount}/${result.totalCount} hareket başarıyla aktarıldı`,
        result.failedCount > 0 ? "error" : "success"
      );
      setSelectedIds(new Set());
      loadData();
    } catch (error: any) {
      console.error('[StockMovementSyncPage] handleBatchSync HATA:', {
        error: error.message,
        errorResponse: error.response?.data,
        errorStatus: error.response?.status,
        timestamp: new Date().toISOString()
      });
      showNotification(
        error.response?.data?.message || "Toplu aktarım başarısız",
        "error"
      );
    } finally {
      console.log('[StockMovementSyncPage] handleBatchSync tamamlandı');
      setLoading(false);
    }
  };

  const handleSyncAllPending = async () => {
    console.log('[StockMovementSyncPage] handleSyncAllPending başlatıldı');
    setLoading(true);
    try {
      console.log('[StockMovementSyncPage] syncAllPending API çağrısı yapılıyor...');
      const result = await syncAllPending();
      console.log('[StockMovementSyncPage] syncAllPending yanıtı:', result);
      
      showNotification(
        `${result.successCount}/${result.totalCount} hareket başarıyla aktarıldı`,
        result.failedCount > 0 ? "error" : "success"
      );
      loadData();
    } catch (error: any) {
      console.error('[StockMovementSyncPage] handleSyncAllPending HATA:', {
        error: error.message,
        errorResponse: error.response?.data,
        errorStatus: error.response?.status,
        timestamp: new Date().toISOString()
      });
      showNotification(
        error.response?.data?.message || "Aktarım başarısız",
        "error"
      );
    } finally {
      console.log('[StockMovementSyncPage] handleSyncAllPending tamamlandı');
      setLoading(false);
    }
  };

  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      const pendingIds = movements
        .filter((m) => m.syncStatus === "PENDING")
        .map((m) => `${m.movementType}-${m.id}`);
      setSelectedIds(new Set(pendingIds));
    } else {
      setSelectedIds(new Set());
    }
  };

  const handleSelectOne = (movement: StockMovementSyncDto) => {
    const key = `${movement.movementType}-${movement.id}`;
    setSelectedIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(key)) {
        newSet.delete(key);
      } else {
        newSet.add(key);
      }
      return newSet;
    });
  };

  const showNotification = (
    message: string,
    severity: "success" | "error" | "info"
  ) => {
    setNotification({ open: true, message, severity });
  };

  const getStatusChip = (status: string, errorMessage?: string) => {
    switch (status) {
      case "SYNCED":
        return (
          <Chip
            icon={<CheckCircleIcon />}
            label="Aktarıldı"
            color="success"
            size="small"
            variant="outlined"
          />
        );
      case "ERROR":
        return (
          <Tooltip title={errorMessage || "Hata"}>
            <Chip
              icon={<ErrorIcon />}
              label="Hata"
              color="error"
              size="small"
              variant="outlined"
            />
          </Tooltip>
        );
      default:
        return (
          <Chip
            icon={<PendingIcon />}
            label="Bekliyor"
            color="warning"
            size="small"
            variant="outlined"
          />
        );
    }
  };

  const getTypeIcon = (type: string) => {
    if (type === "TRANSFER") {
      return (
        <CompareArrowsIcon
          color="primary"
          fontSize="small"
          style={{ marginRight: 4 }}
        />
      );
    }
    return (
      <BuildIcon
        color="secondary"
        fontSize="small"
        style={{ marginRight: 4 }}
      />
    );
  };

  const filteredMovements = movements.filter((m) => {
    if (tabValue === 1 && m.movementType !== "TRANSFER") return false;
    if (tabValue === 2 && m.movementType !== "ADJUSTMENT") return false;
    return true;
  });

  const pendingCount = movements.filter(
    (m) => m.syncStatus === "PENDING"
  ).length;

  return (
    <Box sx={{ backgroundColor: "#f4f6f8", minHeight: "100vh" }}>
      {/* Header */}
      <AppBar position="static" sx={{ backgroundColor: "#2c3e50" }}>
        <Toolbar>
          <WarehouseIcon sx={{ mr: 2 }} />
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Stok Hareketleri Luca Aktarımı
          </Typography>
          <Button
            color="inherit"
            startIcon={<RefreshIcon />}
            onClick={loadData}
            disabled={loading}
          >
            Yenile
          </Button>
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl" sx={{ mt: 3 }}>
        {/* Dashboard Stats */}
        {stats && (
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
            <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
              <Card
                sx={{
                  backgroundColor: "#3498db",
                  color: "white",
                  height: "100%",
                }}
              >
                <CardContent>
                  <Typography variant="h4">{stats.totalTransfers}</Typography>
                  <Typography variant="body2">Toplam Transfer</Typography>
                  <Box sx={{ display: "flex", gap: 1, mt: 1 }}>
                    <Chip
                      label={`${stats.pendingTransfers} Bekliyor`}
                      size="small"
                      sx={{
                        backgroundColor: "rgba(255,255,255,0.2)",
                        color: "white",
                      }}
                    />
                    <Chip
                      label={`${stats.syncedTransfers} Aktarıldı`}
                      size="small"
                      sx={{
                        backgroundColor: "rgba(255,255,255,0.2)",
                        color: "white",
                      }}
                    />
                  </Box>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
              <Card
                sx={{
                  backgroundColor: "#9b59b6",
                  color: "white",
                  height: "100%",
                }}
              >
                <CardContent>
                  <Typography variant="h4">{stats.totalAdjustments}</Typography>
                  <Typography variant="body2">Toplam Düzeltme</Typography>
                  <Box sx={{ display: "flex", gap: 1, mt: 1 }}>
                    <Chip
                      label={`${stats.pendingAdjustments} Bekliyor`}
                      size="small"
                      sx={{
                        backgroundColor: "rgba(255,255,255,0.2)",
                        color: "white",
                      }}
                    />
                    <Chip
                      label={`${stats.syncedAdjustments} Aktarıldı`}
                      size="small"
                      sx={{
                        backgroundColor: "rgba(255,255,255,0.2)",
                        color: "white",
                      }}
                    />
                  </Box>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
              <Card
                sx={{
                  backgroundColor:
                    stats.failedTransfers + stats.failedAdjustments > 0
                      ? "#e74c3c"
                      : "#27ae60",
                  color: "white",
                  height: "100%",
                }}
              >
                <CardContent>
                  <Typography variant="h4">
                    {stats.failedTransfers + stats.failedAdjustments}
                  </Typography>
                  <Typography variant="body2">Hatalı İşlem</Typography>
                  <Typography variant="caption">
                    {stats.failedTransfers} Transfer, {stats.failedAdjustments}{" "}
                    Düzeltme
                  </Typography>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: "1 1 calc(25% - 12px)", minWidth: 200 }}>
              <Card sx={{ height: "100%" }}>
                <CardContent>
                  <Typography variant="body2" color="textSecondary">
                    Son Senkronizasyon
                  </Typography>
                  <Typography variant="h6">
                    {stats.lastSyncDate
                      ? new Date(stats.lastSyncDate).toLocaleString("tr-TR")
                      : "Henüz yok"}
                  </Typography>
                </CardContent>
              </Card>
            </Box>
          </Box>
        )}

        {/* Tabs and Filters */}
        <Paper sx={{ mb: 2 }}>
          <Tabs
            value={tabValue}
            onChange={(_, val) => setTabValue(val)}
            centered
          >
            <Tab label={`Tümü (${movements.length})`} />
            <Tab
              label={`Transferler (${
                movements.filter((m) => m.movementType === "TRANSFER").length
              })`}
              icon={<CompareArrowsIcon />}
              iconPosition="start"
            />
            <Tab
              label={`Düzeltmeler (${
                movements.filter((m) => m.movementType === "ADJUSTMENT").length
              })`}
              icon={<BuildIcon />}
              iconPosition="start"
            />
          </Tabs>
        </Paper>

        {/* Filters and Actions */}
        <Paper sx={{ p: 2, mb: 2 }}>
          <Box
            sx={{
              display: "flex",
              flexWrap: "wrap",
              gap: 2,
              alignItems: "center",
            }}
          >
            <Box sx={{ flex: "1 1 200px", maxWidth: 250 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Durum</InputLabel>
                <Select
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                  label="Durum"
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="PENDING">Bekliyor</MenuItem>
                  <MenuItem value="SYNCED">Aktarıldı</MenuItem>
                  <MenuItem value="ERROR">Hatalı</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ flex: "1 1 200px", maxWidth: 250 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Tip</InputLabel>
                <Select
                  value={typeFilter}
                  onChange={(e) => setTypeFilter(e.target.value)}
                  label="Tip"
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="TRANSFER">Transfer</MenuItem>
                  <MenuItem value="ADJUSTMENT">Düzeltme</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box sx={{ flex: "1 1 auto", textAlign: "right" }}>
              <Button
                variant="outlined"
                startIcon={<SyncIcon />}
                onClick={handleBatchSync}
                disabled={selectedIds.size === 0 || loading}
                sx={{ mr: 1 }}
              >
                Seçilenleri Aktar ({selectedIds.size})
              </Button>
              <Button
                variant="contained"
                startIcon={<SyncIcon />}
                onClick={handleSyncAllPending}
                disabled={pendingCount === 0 || loading}
                color="primary"
              >
                Tümünü Aktar ({pendingCount})
              </Button>
            </Box>
          </Box>
        </Paper>

        {/* Data Table */}
        <Paper sx={{ overflow: "hidden" }}>
          {loading ? (
            <Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                    <TableCell padding="checkbox">
                      <Checkbox
                        indeterminate={
                          selectedIds.size > 0 &&
                          selectedIds.size <
                            filteredMovements.filter(
                              (m) => m.syncStatus === "PENDING"
                            ).length
                        }
                        checked={
                          selectedIds.size ===
                            filteredMovements.filter(
                              (m) => m.syncStatus === "PENDING"
                            ).length && selectedIds.size > 0
                        }
                        onChange={handleSelectAll}
                      />
                    </TableCell>
                    <TableCell>
                      <strong>Belge No</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Tip</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Depo / Konum</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Tarih</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Miktar</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell align="center">
                      <strong>İşlem</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredMovements.map((movement) => {
                    const key = `${movement.movementType}-${movement.id}`;
                    const isSyncing = syncingIds.has(key);
                    const isSelected = selectedIds.has(key);

                    return (
                      <TableRow
                        key={key}
                        hover
                        selected={isSelected}
                        sx={{
                          backgroundColor:
                            movement.syncStatus === "ERROR"
                              ? "rgba(231, 76, 60, 0.05)"
                              : undefined,
                        }}
                      >
                        <TableCell padding="checkbox">
                          <Checkbox
                            checked={isSelected}
                            onChange={() => handleSelectOne(movement)}
                            disabled={movement.syncStatus === "SYNCED"}
                          />
                        </TableCell>
                        <TableCell>
                          <strong>{movement.documentNo}</strong>
                        </TableCell>
                        <TableCell>
                          <Box sx={{ display: "flex", alignItems: "center" }}>
                            {getTypeIcon(movement.movementType)}
                            {movement.movementType === "TRANSFER"
                              ? "Transfer"
                              : "Düzeltme"}
                          </Box>
                          {movement.adjustmentReason && (
                            <Typography
                              variant="caption"
                              color="textSecondary"
                              display="block"
                            >
                              {movement.adjustmentReason}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>{movement.locationInfo}</TableCell>
                        <TableCell>
                          {new Date(movement.movementDate).toLocaleDateString(
                            "tr-TR"
                          )}
                        </TableCell>
                        <TableCell align="right">
                          <Box
                            sx={{
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "flex-end",
                            }}
                          >
                            {movement.movementType === "ADJUSTMENT" &&
                              (movement.adjustmentReason?.includes("Fire") ||
                              movement.adjustmentReason?.includes("Sarf") ? (
                                <TrendingDownIcon
                                  color="error"
                                  fontSize="small"
                                  sx={{ mr: 0.5 }}
                                />
                              ) : (
                                <TrendingUpIcon
                                  color="success"
                                  fontSize="small"
                                  sx={{ mr: 0.5 }}
                                />
                              ))}
                            {movement.totalQuantity.toLocaleString("tr-TR")}
                          </Box>
                        </TableCell>
                        <TableCell align="center">
                          {getStatusChip(
                            movement.syncStatus,
                            movement.errorMessage
                          )}
                        </TableCell>
                        <TableCell align="center">
                          <Box
                            sx={{
                              display: "flex",
                              gap: 1,
                              justifyContent: "center",
                            }}
                          >
                            {movement.syncStatus !== "SYNCED" && (
                              <Button
                                variant="contained"
                                size="small"
                                startIcon={
                                  isSyncing ? (
                                    <CircularProgress
                                      size={16}
                                      color="inherit"
                                    />
                                  ) : (
                                    <SyncIcon />
                                  )
                                }
                                disabled={isSyncing}
                                onClick={() => handleSync(movement)}
                              >
                                Aktar
                              </Button>
                            )}
                            <IconButton
                              size="small"
                              onClick={() => {
                                setSelectedMovement(movement);
                                setDetailDialogOpen(true);
                              }}
                            >
                              <InfoIcon />
                            </IconButton>
                          </Box>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                  {filteredMovements.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={8} align="center">
                        Kayıt bulunamadı
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Paper>
      </Container>

      {/* Detail Dialog */}
      <Dialog
        open={detailDialogOpen}
        onClose={() => setDetailDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>{selectedMovement?.documentNo} - Detay</DialogTitle>
        <DialogContent>
          {selectedMovement && (
            <Box>
              <Typography variant="body2" gutterBottom>
                <strong>Tip:</strong>{" "}
                {selectedMovement.movementType === "TRANSFER"
                  ? "Depo Transferi"
                  : "Stok Düzeltmesi"}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Konum:</strong> {selectedMovement.locationInfo}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Tarih:</strong>{" "}
                {new Date(selectedMovement.movementDate).toLocaleString(
                  "tr-TR"
                )}
              </Typography>
              <Typography variant="body2" gutterBottom>
                <strong>Durum:</strong> {selectedMovement.syncStatus}
              </Typography>
              {selectedMovement.lucaDocumentId && (
                <Typography variant="body2" gutterBottom>
                  <strong>Luca Belge ID:</strong>{" "}
                  {selectedMovement.lucaDocumentId}
                </Typography>
              )}
              {selectedMovement.errorMessage && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {selectedMovement.errorMessage}
                </Alert>
              )}

              <Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
                Ürünler:
              </Typography>
              <List dense>
                {selectedMovement.rows.map((row) => (
                  <ListItem key={row.id}>
                    <ListItemText
                      primary={`${row.productCode} - ${row.productName}`}
                      secondary={`Miktar: ${row.quantity}${
                        row.unitCost ? ` | Birim Fiyat: ${row.unitCost} TL` : ""
                      }`}
                    />
                  </ListItem>
                ))}
              </List>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailDialogOpen(false)}>Kapat</Button>
        </DialogActions>
      </Dialog>

      {/* Notification */}
      <Snackbar
        open={notification.open}
        autoHideDuration={4000}
        onClose={() => setNotification({ ...notification, open: false })}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert severity={notification.severity} variant="filled">
          {notification.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default StockMovementSyncPage;
