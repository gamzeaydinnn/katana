import {
  CheckCircle,
  Error,
  History,
  PlayArrow,
  Refresh,
  Sync,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
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
  Typography,
  useMediaQuery,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { stockAPI } from "../../services/api";
import { showGlobalToast } from "../../providers/FeedbackProvider";

interface SyncHistory {
  id: number;
  syncType: string;
  status: string;
  startTime: string;
  endTime?: string;
  processedRecords: number;
  successfulRecords: number;
  failedRecords: number;
  errorMessage?: string;
}

const SyncManagement: React.FC = () => {
  const [history, setHistory] = useState<SyncHistory[]>([]);
  const [loading, setLoading] = useState(false);
  // Note: we use global toast for errors instead of rendering a persistent Alert banner
  const [openDialog, setOpenDialog] = useState(false);
  const [syncType, setSyncType] = useState("CurrentStock");
  const [syncing, setSyncing] = useState(false);
  const isMobile = useMediaQuery("(max-width:900px)");

  const loadHistory = async () => {
    try {
      setLoading(true);
      const data: any = await stockAPI.getSyncHistory();
      setHistory(data || []);
    } catch (err: any) {
      // Show a friendly toast; avoid rendering raw error text in page
      try {
        showGlobalToast({
          message:
            err?.response?.data?.error || err.message || "Geçmiş yüklenemedi",
          severity: "error",
          durationMs: 4000,
        });
      } catch {
        // fallback: console log
        console.error("Failed to show toast for sync history error:", err);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHistory();
  }, []);

  const handleStartSync = async () => {
    try {
      setSyncing(true);
      await stockAPI.startSync(syncType);
      setOpenDialog(false);
      await loadHistory();
      alert("Senkronizasyon başlatıldı!");
    } catch (err: any) {
      alert("Hata: " + (err.message || "Senkronizasyon başlatılamadı"));
    } finally {
      setSyncing(false);
    }
  };

  const getStatusChip = (status: string) => {
    const statusMap: Record<string, { label: string; color: any }> = {
      Success: { label: "Başarılı", color: "success" },
      Failed: { label: "Başarısız", color: "error" },
      Running: { label: "Çalışıyor", color: "info" },
      Pending: { label: "Bekliyor", color: "warning" },
    };
    const config = statusMap[status] || { label: status, color: "default" };
    return <Chip label={config.label} color={config.color} size="small" />;
  };

  const formatDate = (date: string) => {
    if (!date) return "-";
    const d = new Date(date);
    return new Intl.DateTimeFormat("tr-TR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: false,
    }).format(d);
  };

  return (
    <Container
      maxWidth="lg"
      sx={{ mt: { xs: 5.5, md: 4 }, mb: { xs: 2.5, md: 4 }, px: { xs: 1.5, sm: 2, md: 0 } }}
    >
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", sm: "center" },
          flexDirection: { xs: "column", sm: "row" },
          gap: { xs: 2, sm: 0 },
          mb: 3,
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Sync sx={{ fontSize: 32, color: "primary.main" }} />
          <Typography
            variant="h4"
            sx={{
              fontSize: { xs: "1.6rem", md: "2rem" },
              fontWeight: 900,
              letterSpacing: "-0.02em",
              background: "linear-gradient(135deg, #4f46e5 0%, #0891b2 100%)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            Senkronizasyon Yönetimi
          </Typography>
        </Box>
        <Box
          sx={{
            display: "flex",
            gap: 2,
            flexWrap: "wrap",
            width: { xs: "100%", md: "auto" },
          }}
        >
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={loadHistory}
            disabled={loading}
            sx={{ flex: { xs: 1, sm: "none" }, minWidth: { xs: "48%", sm: 120 } }}
          >
            Yenile
          </Button>
          <Button
            variant="contained"
            startIcon={<PlayArrow />}
            onClick={() => setOpenDialog(true)}
            sx={{ flex: { xs: 1, sm: "none" }, minWidth: { xs: "48%", sm: 180 } }}
          >
            Senkronizasyon Başlat
          </Button>
        </Box>
      </Box>

      {/* Error notifications are shown with global toast; remove inline error banner */}

      <Paper sx={{ p: { xs: 2, md: 3 } }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 3 }}>
          <History />
          <Typography variant="h6">Senkronizasyon Geçmişi</Typography>
        </Box>

        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : isMobile ? (
          <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
            {history.length === 0 ? (
              <Paper
                variant="outlined"
                sx={{
                  p: 2,
                  borderRadius: 2,
                  textAlign: "center",
                  backgroundColor: "background.default",
                }}
              >
                <Typography color="text.secondary">
                  Henüz senkronizasyon geçmişi yok
                </Typography>
              </Paper>
            ) : (
              history.map((item) => (
                <Box
                  key={item.id}
                  sx={{
                    border: "1px solid",
                    borderColor: "divider",
                    borderRadius: 2,
                    p: 1.5,
                    backgroundColor: "background.paper",
                    boxShadow: (t) => t.shadows[1],
                  }}
                >
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "flex-start",
                      gap: 1,
                      mb: 1,
                    }}
                  >
                    <Box>
                      <Typography variant="subtitle1" fontWeight={600}>
                        {item.syncType}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {formatDate(item.startTime)}
                      </Typography>
                    </Box>
                    {getStatusChip(item.status)}
                  </Box>

                  <Box
                    sx={{
                      display: "grid",
                      gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                      columnGap: 1,
                      rowGap: 1,
                    }}
                  >
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        İşlenen Kayıt
                      </Typography>
                      <Typography fontWeight={600}>
                        {item.processedRecords}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Başarılı
                      </Typography>
                      <Box
                        sx={{
                          display: "flex",
                          alignItems: "center",
                          gap: 0.5,
                          mt: 0.25,
                        }}
                      >
                        <CheckCircle
                          sx={{ fontSize: 16, color: "success.main" }}
                        />
                        <Typography fontWeight={600}>
                          {item.successfulRecords || "-"}
                        </Typography>
                      </Box>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Başarısız
                      </Typography>
                      <Box
                        sx={{
                          display: "flex",
                          alignItems: "center",
                          gap: 0.5,
                          mt: 0.25,
                        }}
                      >
                        <Error sx={{ fontSize: 16, color: "error.main" }} />
                        <Typography fontWeight={600}>
                          {item.failedRecords || "-"}
                        </Typography>
                      </Box>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Durum Mesajı
                      </Typography>
                      <Typography
                        variant="body2"
                        sx={{ wordBreak: "break-word" }}
                        color="text.secondary"
                      >
                        {item.errorMessage || "-"}
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              ))
            )}
          </Box>
        ) : (
          <TableContainer
            sx={{
              overflowX: "auto",
              borderRadius: 2,
              "&::-webkit-scrollbar": { height: 6 },
              "&::-webkit-scrollbar-thumb": {
                backgroundColor: "#cbd5f5",
                borderRadius: 3,
              },
            }}
          >
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>
                    <strong>Tip</strong>
                  </TableCell>
                  <TableCell>
                    <strong>Durum</strong>
                  </TableCell>
                  <TableCell>
                    <strong>Başlangıç</strong>
                  </TableCell>
                  <TableCell align="center">
                    <strong>İşlenen</strong>
                  </TableCell>
                  <TableCell align="center">
                    <strong>Başarılı</strong>
                  </TableCell>
                  <TableCell align="center">
                    <strong>Başarısız</strong>
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {history.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      <Typography color="text.secondary">
                        Henüz senkronizasyon geçmişi yok
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  history.map((item) => (
                    <TableRow key={item.id} hover>
                      <TableCell>{item.syncType}</TableCell>
                      <TableCell>{getStatusChip(item.status)}</TableCell>
                      <TableCell>{formatDate(item.startTime)}</TableCell>
                      <TableCell align="center">
                        {item.processedRecords}
                      </TableCell>
                      <TableCell align="center">
                        {item.status === "Success" ||
                        item.status === "SUCCESS" ? (
                          <Box
                            sx={{
                              display: "flex",
                              alignItems: "center",
                              gap: 0.5,
                              justifyContent: "center",
                            }}
                          >
                            <CheckCircle
                              sx={{ fontSize: 16, color: "success.main" }}
                            />
                            <Typography
                              variant="body2"
                              fontWeight={600}
                              color="success.main"
                            >
                              Başarılı
                            </Typography>
                          </Box>
                        ) : item.successfulRecords > 0 ? (
                          <Box
                            sx={{
                              display: "flex",
                              alignItems: "center",
                              gap: 0.5,
                              justifyContent: "center",
                            }}
                          >
                            <CheckCircle
                              sx={{ fontSize: 16, color: "success.main" }}
                            />
                            {item.successfulRecords}
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell align="center">
                        {item.status === "Failed" ||
                        item.status === "FAILED" ? (
                          <Box
                            sx={{
                              display: "flex",
                              alignItems: "center",
                              gap: 0.5,
                              justifyContent: "center",
                            }}
                          >
                            <Error sx={{ fontSize: 16, color: "error.main" }} />
                            <Typography
                              variant="body2"
                              fontWeight={600}
                              color="error.main"
                            >
                              Başarısız
                            </Typography>
                          </Box>
                        ) : item.failedRecords > 0 ? (
                          <Box
                            sx={{
                              display: "flex",
                              alignItems: "center",
                              gap: 0.5,
                              justifyContent: "center",
                            }}
                          >
                            <Error sx={{ fontSize: 16, color: "error.main" }} />
                            <Typography
                              variant="body2"
                              fontWeight={600}
                              color="error.main"
                            >
                              {item.failedRecords} Başarısız
                            </Typography>
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>

      {/* Start Sync Dialog */}
      <Dialog open={openDialog} onClose={() => setOpenDialog(false)}>
        <DialogTitle>Senkronizasyon Başlat</DialogTitle>
        <DialogContent sx={{ minWidth: { xs: "auto", sm: 400 }, pt: 2 }}>
          <FormControl fullWidth>
            <InputLabel>Senkronizasyon Tipi</InputLabel>
            <Select
              value={syncType}
              label="Senkronizasyon Tipi"
              onChange={(e) => setSyncType(e.target.value)}
            >
              <MenuItem value="CurrentStock">Cari Stok Senkronizasyonu</MenuItem>
              <MenuItem value="Stock">Stok Senkronizasyonu</MenuItem>
              <MenuItem value="Product">Ürün Senkronizasyonu</MenuItem>
              <MenuItem value="Invoice">Fatura Senkronizasyonu</MenuItem>
              <MenuItem value="Customer">Müşteri Senkronizasyonu</MenuItem>
              <MenuItem value="All">Tümü</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>İptal</Button>
          <Button
            variant="contained"
            onClick={handleStartSync}
            disabled={syncing}
            startIcon={syncing ? <CircularProgress size={16} /> : <PlayArrow />}
          >
            Başlat
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default SyncManagement;
