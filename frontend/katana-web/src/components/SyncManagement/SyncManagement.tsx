import React, { useState, useEffect } from "react";
import {
  Container,
  Box,
  Paper,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from "@mui/material";
import {
  Sync,
  PlayArrow,
  History,
  CheckCircle,
  Error,
  Refresh,
} from "@mui/icons-material";
import { stockAPI } from "../../services/api";

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
  const [error, setError] = useState("");
  const [openDialog, setOpenDialog] = useState(false);
  const [syncType, setSyncType] = useState("Stock");
  const [syncing, setSyncing] = useState(false);

  const loadHistory = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await stockAPI.getSyncHistory();
      setHistory(data || []);
    } catch (err: any) {
      setError(err.message || "Geçmiş yüklenemedi");
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
    return new Date(date).toLocaleString("tr-TR");
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", mb: 3 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Sync sx={{ fontSize: 32, color: "primary.main" }} />
          <Typography variant="h4" fontWeight="bold">
            Senkronizasyon Yönetimi
          </Typography>
        </Box>
        <Box sx={{ display: "flex", gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={loadHistory}
            disabled={loading}
          >
            Yenile
          </Button>
          <Button
            variant="contained"
            startIcon={<PlayArrow />}
            onClick={() => setOpenDialog(true)}
          >
            Senkronizasyon Başlat
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 3 }}>
          <History />
          <Typography variant="h6">Senkronizasyon Geçmişi</Typography>
        </Box>

        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <TableContainer>
            <Table>
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
                      </TableCell>
                      <TableCell align="center">
                        <Box
                          sx={{
                            display: "flex",
                            alignItems: "center",
                            gap: 0.5,
                            justifyContent: "center",
                          }}
                        >
                          <Error sx={{ fontSize: 16, color: "error.main" }} />
                          {item.failedRecords}
                        </Box>
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
        <DialogContent sx={{ minWidth: 400, pt: 2 }}>
          <FormControl fullWidth>
            <InputLabel>Senkronizasyon Tipi</InputLabel>
            <Select
              value={syncType}
              label="Senkronizasyon Tipi"
              onChange={(e) => setSyncType(e.target.value)}
            >
              <MenuItem value="Stock">Stok Senkronizasyonu</MenuItem>
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
