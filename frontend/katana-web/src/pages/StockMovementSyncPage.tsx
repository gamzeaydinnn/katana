import React, { useEffect, useState } from "react";
import {
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  Chip,
  Alert,
  Snackbar,
  CircularProgress,
  Tabs,
  Tab,
  Box,
  Card,
  CardContent,
  Checkbox,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  IconButton,
  Tooltip,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import SyncIcon from "@mui/icons-material/Sync";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import PendingIcon from "@mui/icons-material/Pending";
import CompareArrowsIcon from "@mui/icons-material/CompareArrows";
import BuildIcon from "@mui/icons-material/Build";
import RefreshIcon from "@mui/icons-material/Refresh";
import InfoIcon from "@mui/icons-material/Info";
import WarehouseIcon from "@mui/icons-material/Warehouse";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import TrendingDownIcon from "@mui/icons-material/TrendingDown";

import {
  StockMovementSyncDto,
  MovementDashboardStatsDto,
  getAllMovements,
  syncMovement,
  syncBatch,
  syncAllPending,
  getDashboardStats,
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
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
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
    setLoading(true);
    try {
      const [movementsData, statsData] = await Promise.all([
        getAllMovements({
          movementType: typeFilter || undefined,
          syncStatus: statusFilter || undefined,
        }),
        getDashboardStats(),
      ]);
      setMovements(movementsData);
      setStats(statsData);
    } catch (error) {
      showNotification("Veri yüklenirken hata oluştu", "error");
    } finally {
      setLoading(false);
    }
  };

  const handleSync = async (movement: StockMovementSyncDto) => {
    const key = `${movement.movementType}-${movement.id}`;
    setSyncingIds((prev) => new Set(prev).add(key));

    try {
      const result = await syncMovement(movement.movementType, movement.id);

      if (result.success) {
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
        showNotification(result.message || "Senkronizasyon başarısız", "error");
      }
    } catch (error: any) {
      showNotification(error.response?.data?.message || "Hata oluştu", "error");
    } finally {
      setSyncingIds((prev) => {
        const newSet = new Set(prev);
        newSet.delete(key);
        return newSet;
      });
    }
  };

  const handleBatchSync = async () => {
    if (selectedIds.size === 0) {
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

      const result = await syncBatch(transferIds, adjustmentIds);
      showNotification(
        `${result.successCount}/${result.totalCount} hareket başarıyla aktarıldı`,
        result.failedCount > 0 ? "error" : "success"
      );
      setSelectedIds(new Set());
      loadData();
    } catch (error: any) {
      showNotification(
        error.response?.data?.message || "Toplu aktarım başarısız",
        "error"
      );
    } finally {
      setLoading(false);
    }
  };

  const handleSyncAllPending = async () => {
    setLoading(true);
    try {
      const result = await syncAllPending();
      showNotification(
        `${result.successCount}/${result.totalCount} hareket başarıyla aktarıldı`,
        result.failedCount > 0 ? "error" : "success"
      );
      loadData();
    } catch (error: any) {
      showNotification(
        error.response?.data?.message || "Aktarım başarısız",
        "error"
      );
    } finally {
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
    <Box
      sx={{
        width: "100%",
        maxWidth: "100%",
        overflowX: "hidden",
      }}
    >
      {/* Page Header */}
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          mb: { xs: 2, sm: 3 },
          pb: { xs: 1.5, sm: 2 },
          borderBottom: "1px solid rgba(102, 126, 234, 0.15)",
        }}
      >
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            gap: { xs: 1, sm: 1.5 },
          }}
        >
          <Box
            sx={{
              width: { xs: 36, sm: 48 },
              height: { xs: 36, sm: 48 },
              borderRadius: 2,
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              boxShadow: "0 4px 12px rgba(102, 126, 234, 0.3)",
            }}
          >
            <WarehouseIcon
              sx={{ color: "white", fontSize: { xs: 20, sm: 28 } }}
            />
          </Box>
          <Box>
            <Typography
              sx={{
                fontSize: { xs: "1rem", sm: "1.5rem" },
                fontWeight: 700,
                color: "#1e293b",
                lineHeight: 1.2,
              }}
            >
              Stok Hareketleri
            </Typography>
            <Typography
              sx={{
                fontSize: { xs: "0.7rem", sm: "0.85rem" },
                color: "#64748b",
                fontWeight: 500,
              }}
            >
              Luca Aktarım Yönetimi
            </Typography>
          </Box>
        </Box>
        <IconButton
          onClick={loadData}
          disabled={loading}
          sx={{
            width: { xs: 36, sm: 44 },
            height: { xs: 36, sm: 44 },
            borderRadius: 2,
            background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
            color: "white",
            boxShadow: "0 4px 12px rgba(102, 126, 234, 0.3)",
            "&:hover": {
              background: "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
              boxShadow: "0 6px 16px rgba(102, 126, 234, 0.4)",
            },
            "&:disabled": {
              background: "#e2e8f0",
              color: "#94a3b8",
            },
          }}
        >
          <RefreshIcon sx={{ fontSize: { xs: 18, sm: 24 } }} />
        </IconButton>
      </Box>
      {/* Dashboard Stats */}
      {stats && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(4, 1fr)" },
            gap: { xs: 1, sm: 2 },
            mb: 2,
            width: "100%",
          }}
        >
          <Box>
            <Card
              sx={{
                background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                color: "white",
                height: "100%",
                boxShadow: "0 4px 12px rgba(102, 126, 234, 0.3)",
                transition: "transform 0.2s",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 6px 16px rgba(102, 126, 234, 0.4)",
                },
              }}
            >
              <CardContent
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  "&:last-child": { pb: { xs: 1.5, sm: 2 } },
                }}
              >
                <Typography
                  sx={{
                    fontWeight: 700,
                    fontSize: { xs: "1.5rem", sm: "2rem" },
                  }}
                >
                  {stats.totalTransfers}
                </Typography>
                <Typography
                  sx={{
                    opacity: 0.95,
                    fontWeight: 500,
                    fontSize: { xs: "0.7rem", sm: "0.875rem" },
                  }}
                >
                  Toplam Transfer
                </Typography>
                <Box
                  sx={{
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 0.5,
                    mt: 0.5,
                  }}
                >
                  <Chip
                    label={`${stats.pendingTransfers} Bekliyor`}
                    size="small"
                    sx={{
                      backgroundColor: "rgba(255,255,255,0.2)",
                      color: "white",
                      height: { xs: 18, sm: 24 },
                      fontSize: { xs: "0.6rem", sm: "0.75rem" },
                    }}
                  />
                  <Chip
                    label={`${stats.syncedTransfers} Aktarıldı`}
                    size="small"
                    sx={{
                      backgroundColor: "rgba(255,255,255,0.2)",
                      color: "white",
                      height: { xs: 18, sm: 24 },
                      fontSize: { xs: "0.6rem", sm: "0.75rem" },
                    }}
                  />
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card
              sx={{
                background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                color: "white",
                height: "100%",
                boxShadow: "0 4px 12px rgba(245, 87, 108, 0.3)",
                transition: "transform 0.2s",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 6px 16px rgba(245, 87, 108, 0.4)",
                },
              }}
            >
              <CardContent
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  "&:last-child": { pb: { xs: 1.5, sm: 2 } },
                }}
              >
                <Typography
                  sx={{
                    fontWeight: 700,
                    fontSize: { xs: "1.5rem", sm: "2rem" },
                  }}
                >
                  {stats.totalAdjustments}
                </Typography>
                <Typography
                  sx={{
                    opacity: 0.95,
                    fontWeight: 500,
                    fontSize: { xs: "0.7rem", sm: "0.875rem" },
                  }}
                >
                  Toplam Düzeltme
                </Typography>
                <Box
                  sx={{
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 0.5,
                    mt: 0.5,
                  }}
                >
                  <Chip
                    label={`${stats.pendingAdjustments} Bekliyor`}
                    size="small"
                    sx={{
                      backgroundColor: "rgba(255,255,255,0.2)",
                      color: "white",
                      height: { xs: 18, sm: 24 },
                      fontSize: { xs: "0.6rem", sm: "0.75rem" },
                    }}
                  />
                  <Chip
                    label={`${stats.syncedAdjustments} Aktarıldı`}
                    size="small"
                    sx={{
                      backgroundColor: "rgba(255,255,255,0.2)",
                      color: "white",
                      height: { xs: 18, sm: 24 },
                      fontSize: { xs: "0.6rem", sm: "0.75rem" },
                    }}
                  />
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card
              sx={{
                background:
                  stats.failedTransfers + stats.failedAdjustments > 0
                    ? "linear-gradient(135deg, #fa709a 0%, #fee140 100%)"
                    : "linear-gradient(135deg, #30cfd0 0%, #330867 100%)",
                color: "white",
                height: "100%",
                boxShadow:
                  stats.failedTransfers + stats.failedAdjustments > 0
                    ? "0 4px 12px rgba(250, 112, 154, 0.3)"
                    : "0 4px 12px rgba(48, 207, 208, 0.3)",
                transition: "transform 0.2s",
                "&:hover": { transform: "translateY(-4px)" },
              }}
            >
              <CardContent
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  "&:last-child": { pb: { xs: 1.5, sm: 2 } },
                }}
              >
                <Typography
                  sx={{
                    fontWeight: 700,
                    fontSize: { xs: "1.5rem", sm: "2rem" },
                  }}
                >
                  {stats.failedTransfers + stats.failedAdjustments}
                </Typography>
                <Typography
                  sx={{
                    opacity: 0.95,
                    fontWeight: 500,
                    fontSize: { xs: "0.7rem", sm: "0.875rem" },
                  }}
                >
                  Hatalı İşlem
                </Typography>
                <Typography sx={{ fontSize: { xs: "0.6rem", sm: "0.75rem" } }}>
                  {stats.failedTransfers} Transfer, {stats.failedAdjustments}{" "}
                  Düzeltme
                </Typography>
              </CardContent>
            </Card>
          </Box>
          <Box>
            <Card
              sx={{
                height: "100%",
                background: stats.lastSyncDate
                  ? "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)"
                  : "linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)",
                color: stats.lastSyncDate ? "white" : "rgba(0, 0, 0, 0.87)",
                boxShadow: "0 4px 12px rgba(79, 172, 254, 0.3)",
                transition: "transform 0.2s",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 6px 16px rgba(79, 172, 254, 0.4)",
                },
              }}
            >
              <CardContent
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  "&:last-child": { pb: { xs: 1.5, sm: 2 } },
                }}
              >
                <Typography
                  sx={{
                    opacity: 0.9,
                    fontWeight: 500,
                    fontSize: { xs: "0.7rem", sm: "0.875rem" },
                  }}
                >
                  Son Senkronizasyon
                </Typography>
                <Typography
                  sx={{
                    mt: 0.5,
                    fontWeight: 700,
                    fontSize: { xs: "0.8rem", sm: "1.1rem" },
                  }}
                >
                  {stats.lastSyncDate
                    ? new Date(stats.lastSyncDate).toLocaleString("tr-TR")
                    : "Henüz yok"}
                </Typography>
                {!stats.lastSyncDate && (
                  <Typography
                    sx={{
                      mt: 0.5,
                      opacity: 0.8,
                      fontSize: { xs: "0.6rem", sm: "0.75rem" },
                    }}
                  >
                    İlk senkronizasyonu başlatın
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Box>
        </Box>
      )}

      {/* Tabs and Filters */}
      <Paper
        elevation={0}
        sx={{
          mb: 1.5,
          borderRadius: 2,
          boxShadow: "0 12px 30px rgba(15,23,42,0.08)",
          overflow: "hidden",
        }}
      >
        <Tabs
          value={tabValue}
          onChange={(_, val) => setTabValue(val)}
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          sx={{
            minHeight: { xs: 36, sm: 48 },
            "& .MuiTabs-indicator": {
              background: "linear-gradient(135deg,#2563eb,#06b6d4)",
              height: 3,
            },
            "& .MuiTab-root": {
              minHeight: { xs: 36, sm: 48 },
              fontSize: { xs: "0.65rem", sm: "0.875rem" },
              px: { xs: 1, sm: 2 },
              py: { xs: 0.5, sm: 1 },
              fontWeight: 600,
              minWidth: { xs: "auto", sm: 90 },
            },
            "& .MuiTab-iconWrapper": {
              fontSize: { xs: "0.9rem", sm: "1.25rem" },
              mr: { xs: 0.25, sm: 1 },
            },
          }}
        >
          <Tab label={`Tümü (${movements.length})`} />
          <Tab
            label={
              isMobile
                ? `(${
                    movements.filter((m) => m.movementType === "TRANSFER")
                      .length
                  })`
                : `Transferler (${
                    movements.filter((m) => m.movementType === "TRANSFER")
                      .length
                  })`
            }
            icon={<CompareArrowsIcon fontSize="small" />}
            iconPosition="start"
          />
          <Tab
            label={
              isMobile
                ? `(${
                    movements.filter((m) => m.movementType === "ADJUSTMENT")
                      .length
                  })`
                : `Düzeltmeler (${
                    movements.filter((m) => m.movementType === "ADJUSTMENT")
                      .length
                  })`
            }
            icon={<BuildIcon fontSize="small" />}
            iconPosition="start"
          />
        </Tabs>
      </Paper>

      {/* Filters and Actions */}
      <Paper
        elevation={0}
        sx={{
          p: { xs: 1, sm: 2 },
          mb: 1.5,
          borderRadius: 2,
          boxShadow: "0 12px 30px rgba(15,23,42,0.08)",
        }}
      >
        <Box
          sx={{
            display: "flex",
            flexDirection: { xs: "column", sm: "row" },
            gap: { xs: 1, sm: 2 },
            alignItems: { xs: "stretch", sm: "center" },
          }}
        >
          <Box
            sx={{
              display: "flex",
              gap: 1,
              flex: { xs: "1 1 auto", sm: "0 0 auto" },
            }}
          >
            <FormControl
              size="small"
              sx={{ minWidth: { xs: 90, sm: 120 }, flex: 1 }}
            >
              <InputLabel sx={{ fontSize: { xs: "0.75rem", sm: "1rem" } }}>
                Durum
              </InputLabel>
              <Select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                label="Durum"
                sx={{ fontSize: { xs: "0.75rem", sm: "1rem" } }}
              >
                <MenuItem value="">Tümü</MenuItem>
                <MenuItem value="PENDING">Bekliyor</MenuItem>
                <MenuItem value="SYNCED">Aktarıldı</MenuItem>
                <MenuItem value="ERROR">Hatalı</MenuItem>
              </Select>
            </FormControl>
          </Box>
          <Box
            sx={{
              display: "flex",
              gap: 1,
              flex: 1,
              justifyContent: { xs: "stretch", sm: "flex-end" },
            }}
          >
            <Button
              variant="outlined"
              startIcon={!isMobile && <SyncIcon />}
              onClick={handleBatchSync}
              disabled={selectedIds.size === 0 || loading}
              size="small"
              sx={{
                flex: { xs: 1, sm: "initial" },
                whiteSpace: "nowrap",
                borderWidth: 2,
                borderColor: "#667eea",
                color: "#667eea",
                fontWeight: 600,
                fontSize: { xs: "0.65rem", sm: "0.875rem" },
                px: { xs: 1, sm: 2 },
                py: { xs: 0.5, sm: 1 },
                "&:hover": {
                  borderWidth: 2,
                  borderColor: "#764ba2",
                  background: "rgba(102, 126, 234, 0.08)",
                },
              }}
            >
              {isMobile
                ? `Seçili (${selectedIds.size})`
                : `Seçilenleri Aktar (${selectedIds.size})`}
            </Button>
            <Button
              variant="contained"
              startIcon={!isMobile && <SyncIcon />}
              onClick={handleSyncAllPending}
              disabled={pendingCount === 0 || loading}
              size="small"
              sx={{
                flex: { xs: 1, sm: "initial" },
                whiteSpace: "nowrap",
                background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                fontWeight: 600,
                fontSize: { xs: "0.65rem", sm: "0.875rem" },
                px: { xs: 1, sm: 2 },
                py: { xs: 0.5, sm: 1 },
                boxShadow: "0 4px 12px rgba(102, 126, 234, 0.3)",
                "&:hover": {
                  background:
                    "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
                  boxShadow: "0 6px 16px rgba(102, 126, 234, 0.4)",
                },
              }}
            >
              {isMobile
                ? `Tümü (${pendingCount})`
                : `Tümünü Aktar (${pendingCount})`}
            </Button>
          </Box>
        </Box>
      </Paper>

      {/* Data Table / Mobile Cards */}
      <Paper
        elevation={0}
        sx={{
          overflow: "hidden",
          borderRadius: 3,
          boxShadow: "0 12px 30px rgba(15,23,42,0.08)",
          border: "1px solid rgba(0,0,0,0.06)",
        }}
      >
        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
            <CircularProgress />
          </Box>
        ) : isMobile ? (
          <Box
            sx={{ p: 1, display: "flex", flexDirection: "column", gap: 1.5 }}
          >
            <Box sx={{ display: "flex", alignItems: "center", mb: 0.5 }}>
              <Checkbox
                indeterminate={
                  selectedIds.size > 0 &&
                  selectedIds.size <
                    filteredMovements.filter((m) => m.syncStatus === "PENDING")
                      .length
                }
                checked={
                  selectedIds.size ===
                    filteredMovements.filter((m) => m.syncStatus === "PENDING")
                      .length && selectedIds.size > 0
                }
                onChange={handleSelectAll}
              />
              <Typography variant="body2" color="textSecondary">
                Bekleyenlerin tümünü seç
              </Typography>
            </Box>

            {filteredMovements.map((movement) => {
              const key = `${movement.movementType}-${movement.id}`;
              const isSyncing = syncingIds.has(key);
              const isSelected = selectedIds.has(key);

              return (
                <Card
                  key={key}
                  variant="outlined"
                  sx={{
                    borderRadius: 2,
                    boxShadow: "0 2px 6px rgba(0,0,0,0.04)",
                    borderLeft: 4,
                    borderLeftColor:
                      movement.syncStatus === "ERROR"
                        ? "#fa709a"
                        : movement.syncStatus === "SYNCED"
                        ? "#30cfd0"
                        : "#667eea",
                    transition: "all 0.2s ease",
                    "&:hover": {
                      transform: "translateX(4px)",
                      boxShadow: "0 8px 16px rgba(102, 126, 234, 0.15)",
                    },
                  }}
                >
                  <CardContent sx={{ p: 1.5, "&:last-child": { pb: 1.5 } }}>
                    <Box
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        mb: 1,
                        gap: 1,
                      }}
                    >
                      <Box
                        sx={{ display: "flex", alignItems: "center", gap: 1 }}
                      >
                        <Checkbox
                          size="small"
                          checked={isSelected}
                          onChange={() => handleSelectOne(movement)}
                          disabled={movement.syncStatus === "SYNCED"}
                        />
                        <Box>
                          <Typography
                            variant="subtitle2"
                            sx={{ fontWeight: 600, wordBreak: "break-all" }}
                          >
                            {movement.documentNo}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {new Date(movement.movementDate).toLocaleDateString(
                              "tr-TR"
                            )}
                          </Typography>
                        </Box>
                      </Box>
                      {getStatusChip(
                        movement.syncStatus,
                        movement.errorMessage
                      )}
                    </Box>

                    <Box
                      sx={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        mb: 1,
                        gap: 1,
                      }}
                    >
                      <Box sx={{ display: "flex", alignItems: "center" }}>
                        {getTypeIcon(movement.movementType)}
                        <Typography variant="body2">
                          {movement.movementType === "TRANSFER"
                            ? "Transfer"
                            : "Düzeltme"}
                        </Typography>
                      </Box>
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
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {movement.totalQuantity.toLocaleString("tr-TR")}
                        </Typography>
                      </Box>
                    </Box>

                    {movement.locationInfo && (
                      <Typography
                        variant="caption"
                        color="textSecondary"
                        sx={{ display: "block", mb: 1 }}
                      >
                        {movement.locationInfo}
                      </Typography>
                    )}

                    {movement.adjustmentReason && (
                      <Typography
                        variant="caption"
                        color="textSecondary"
                        sx={{ display: "block", mb: 1 }}
                      >
                        {movement.adjustmentReason}
                      </Typography>
                    )}

                    <Box
                      sx={{
                        display: "flex",
                        justifyContent: "flex-end",
                        gap: 1,
                        mt: 0.5,
                      }}
                    >
                      {movement.syncStatus !== "SYNCED" && (
                        <Button
                          variant="contained"
                          size="small"
                          startIcon={
                            isSyncing ? (
                              <CircularProgress size={16} color="inherit" />
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
                        <InfoIcon fontSize="small" />
                      </IconButton>
                    </Box>
                  </CardContent>
                </Card>
              );
            })}

            {filteredMovements.length === 0 && (
              <Paper
                elevation={0}
                sx={{
                  p: 8,
                  textAlign: "center",
                  background:
                    "linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%)",
                  border: "3px dashed",
                  borderImage:
                    "linear-gradient(135deg, #667eea 0%, #764ba2 100%) 1",
                  borderRadius: 3,
                  position: "relative",
                  overflow: "hidden",
                  "&::before": {
                    content: '""',
                    position: "absolute",
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: 0,
                    background:
                      "linear-gradient(135deg, rgba(102, 126, 234, 0.03) 0%, rgba(118, 75, 162, 0.03) 100%)",
                    zIndex: 0,
                  },
                }}
              >
                <WarehouseIcon
                  sx={{
                    fontSize: 80,
                    color: "#667eea",
                    mb: 2,
                    opacity: 0.8,
                    position: "relative",
                    zIndex: 1,
                  }}
                />
                <Typography
                  variant="h6"
                  sx={{
                    mb: 1,
                    color: "rgba(0, 0, 0, 0.87)",
                    fontWeight: 600,
                  }}
                >
                  Kayıt Bulunamadı
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Seçili filtrelere uygun stok hareketi bulunmuyor
                </Typography>
              </Paper>
            )}
          </Box>
        ) : (
          <TableContainer
            sx={{
              width: "100%",
              overflowX: "auto",
            }}
          >
            <Table
              size="small"
              sx={{
                "& .MuiTableCell-root": {
                  px: { xs: 0.75, sm: 2 },
                  py: { xs: 0.75, sm: 1 },
                  fontSize: { xs: "0.75rem", sm: "0.875rem" },
                },
                minWidth: { xs: 0, sm: 650 },
              }}
            >
              <TableHead>
                <TableRow
                  sx={{
                    background:
                      "linear-gradient(90deg, #1e3a8a 0%, #2563eb 60%, #38bdf8 100%)",
                    "& .MuiTableCell-root": {
                      color: "rgba(255,255,255,0.95)",
                      fontWeight: 700,
                      fontSize: "0.85rem",
                      letterSpacing: "0.4px",
                      borderBottom: "none",
                      textTransform: "uppercase",
                    },
                  }}
                >
                  <TableCell padding="checkbox">
                    <Checkbox
                      sx={{
                        color: "rgba(255,255,255,0.7)",
                        "&.Mui-checked": { color: "white" },
                      }}
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
                  <TableCell sx={{ whiteSpace: "nowrap" }}>Belge No</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>Tip</TableCell>
                  <TableCell
                    sx={{
                      whiteSpace: "nowrap",
                      display: { xs: "none", sm: "table-cell" },
                    }}
                  >
                    Depo / Konum
                  </TableCell>
                  <TableCell
                    sx={{
                      whiteSpace: "nowrap",
                      display: { xs: "none", sm: "table-cell" },
                    }}
                  >
                    Tarih
                  </TableCell>
                  <TableCell align="right">Miktar</TableCell>
                  <TableCell align="center">Durum</TableCell>
                  <TableCell align="center">İşlem</TableCell>
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
                            ? "rgba(244, 67, 54, 0.08)"
                            : movement.syncStatus === "SYNCED"
                            ? "rgba(76, 175, 80, 0.05)"
                            : undefined,
                        "&:hover": {
                          backgroundColor:
                            movement.syncStatus === "ERROR"
                              ? "rgba(244, 67, 54, 0.12) !important"
                              : movement.syncStatus === "SYNCED"
                              ? "rgba(76, 175, 80, 0.08) !important"
                              : undefined,
                        },
                        transition: "background-color 0.2s",
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
                      <TableCell
                        sx={{ display: { xs: "none", sm: "table-cell" } }}
                      >
                        {movement.locationInfo}
                      </TableCell>
                      <TableCell
                        sx={{ display: { xs: "none", sm: "table-cell" } }}
                      >
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
                                  <CircularProgress size={16} color="inherit" />
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
