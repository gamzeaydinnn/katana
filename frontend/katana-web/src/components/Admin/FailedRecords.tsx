import React, { useEffect, useState } from "react";
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Paper,
  Chip,
  IconButton,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
  Alert,
  Tooltip,
  Tabs,
  Tab,
  Stack,
  useMediaQuery,
} from "@mui/material";
import {
  Refresh,
  Visibility,
  Edit,
  CheckCircle,
  Block,
  RestartAlt,
} from "@mui/icons-material";
import api from "../../services/api";

interface FailedRecord {
  id: number;
  recordType: string;
  recordId: string | null;
  errorMessage: string;
  errorCode: string | null;
  failedAt: string;
  retryCount: number;
  lastRetryAt: string | null;
  status: string;
  resolvedAt: string | null;
  resolvedBy: string | null;
  integrationLogId: number;
  sourceSystem: string;
}

interface FailedRecordDetail extends FailedRecord {
  originalData: string;
  nextRetryAt: string | null;
  resolution: string | null;
  integrationLog: {
    id: number;
    syncType: string;
    status: string;
    startTime: string;
  } | null;
}

const FailedRecords: React.FC = () => {
  const [records, setRecords] = useState<FailedRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [total, setTotal] = useState(0);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [recordTypeFilter, setRecordTypeFilter] = useState<string>("");
  const [sourceTab, setSourceTab] = useState<"all" | "katana" | "luca">("all");

  const [selectedRecord, setSelectedRecord] =
    useState<FailedRecordDetail | null>(null);
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  const [resolveDialogOpen, setResolveDialogOpen] = useState(false);

  const [resolution, setResolution] = useState("");
  const [correctedData, setCorrectedData] = useState("");
  const [parsedData, setParsedData] = useState<any>(null);
  const [resend, setResend] = useState(false);
  const [ignoreReason, setIgnoreReason] = useState("");
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchRecords = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: String(page + 1),
        pageSize: String(pageSize),
      });
      if (statusFilter) params.append("status", statusFilter);
      if (recordTypeFilter) params.append("recordType", recordTypeFilter);
      if (sourceTab !== "all") {
        params.append("sourceSystem", sourceTab.toUpperCase());
      }

      const response = await api.get<{ items: FailedRecord[]; total: number }>(
        `/adminpanel/failed-records?${params}`
      );
      setRecords(response.data.items);
      setTotal(response.data.total);
    } catch (error) {
      console.error("Failed to fetch failed records:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecords();
  }, [page, pageSize, statusFilter, recordTypeFilter, sourceTab]);

  const handleViewDetails = async (id: number) => {
    try {
      const response = await api.get<FailedRecordDetail>(
        `/adminpanel/failed-records/${id}`
      );
      setSelectedRecord(response.data);
      setDetailDialogOpen(true);
      setCorrectedData(response.data.originalData);

      // Parse JSON for form editing
      try {
        const parsed = JSON.parse(response.data.originalData);
        setParsedData(parsed);
      } catch {
        setParsedData(null);
      }
    } catch (error) {
      console.error("Failed to fetch record details:", error);
    }
  };

  const handleResolve = async () => {
    if (!selectedRecord) return;

    try {
      const finalData = parsedData ? JSON.stringify(parsedData) : correctedData;
      await api.put(`/adminpanel/failed-records/${selectedRecord.id}/resolve`, {
        resolution,
        correctedData:
          finalData !== selectedRecord.originalData ? finalData : null,
        resend,
      });
      setResolveDialogOpen(false);
      setDetailDialogOpen(false);
      fetchRecords();
      setResolution("");
      setCorrectedData("");
      setParsedData(null);
      setResend(false);
    } catch (error) {
      console.error("Failed to resolve record:", error);
    }
  };

  const handleIgnore = async (id: number) => {
    try {
      await api.put(`/adminpanel/failed-records/${id}/ignore`, {
        reason: ignoreReason || "Ignored by admin",
      });
      setDetailDialogOpen(false);
      fetchRecords();
      setIgnoreReason("");
      setParsedData(null);
      setCorrectedData("");
    } catch (error) {
      console.error("Failed to ignore record:", error);
    }
  };

  const handleRetry = async (id: number) => {
    try {
      await api.post(`/adminpanel/failed-records/${id}/retry`);
      fetchRecords();
    } catch (error) {
      console.error("Failed to retry record:", error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case "FAILED":
        return "error";
      case "RETRYING":
        return "warning";
      case "RESOLVED":
        return "success";
      case "IGNORED":
        return "default";
      default:
        return "default";
    }
  };

  return (
    <Box>
      <Card>
        <CardContent>
          <Box
            display="flex"
            justifyContent="space-between"
            alignItems={{ xs: "flex-start", md: "center" }}
            flexDirection={{ xs: "column", md: "row" }}
            gap={{ xs: 1.5, md: 0 }}
            mb={2}
          >
            <Typography variant="h5">Hatalı Kayıtlar</Typography>
            <Box
              sx={{
                display: "flex",
                flexWrap: "wrap",
                gap: { xs: 1, sm: 2 },
                width: { xs: "100%", md: "auto" },
              }}
            >
              <FormControl size="small" fullWidth sx={{ minWidth: { md: 150 } }}>
                <InputLabel>Durum</InputLabel>
                <Select
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                  label="Durum"
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="FAILED">Başarısız</MenuItem>
                  <MenuItem value="RETRYING">Yeniden Deneniyor</MenuItem>
                  <MenuItem value="RESOLVED">Çözüldü</MenuItem>
                  <MenuItem value="IGNORED">Göz Ardı Edildi</MenuItem>
                </Select>
              </FormControl>
              <FormControl size="small" fullWidth sx={{ minWidth: { md: 150 } }}>
                <InputLabel>Kayıt Tipi</InputLabel>
                <Select
                  value={recordTypeFilter}
                  onChange={(e) => setRecordTypeFilter(e.target.value)}
                  label="Kayıt Tipi"
                >
                  <MenuItem value="">Tümü</MenuItem>
                  <MenuItem value="STOCK">Stok</MenuItem>
                  <MenuItem value="INVOICE">Fatura</MenuItem>
                  <MenuItem value="CUSTOMER">Müşteri</MenuItem>
                  <MenuItem value="ORDER">Sipariş</MenuItem>
                </Select>
              </FormControl>
              <IconButton onClick={fetchRecords} color="primary" sx={{ alignSelf: "center" }}>
                <Refresh />
              </IconButton>
            </Box>
          </Box>

          {/* Source System Tabs */}
          <Box sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}>
            <Tabs
              value={sourceTab}
              onChange={(_, newValue) => {
                setSourceTab(newValue);
                setPage(0);
              }}
            >
              <Tab label="Tümü" value="all" />
              <Tab label="Katana Hataları" value="katana" />
              <Tab label="Luca Hataları" value="luca" />
            </Tabs>
          </Box>

          {loading ? (
            <Box display="flex" justifyContent="center" p={4}>
              <CircularProgress />
            </Box>
          ) : (
            <>
              {isMobile ? (
                <Stack spacing={1.5}>
                  {records.length === 0 && (
                    <Typography
                      color="text.secondary"
                      align="center"
                      sx={{ py: 2 }}
                    >
                      Kayıt bulunamadı
                    </Typography>
                  )}
                  {records.map((record) => (
                    <Paper
                      key={record.id}
                      sx={{
                        p: 1.5,
                        borderRadius: 2,
                        border: "1px solid",
                        borderColor: "divider",
                      }}
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
                          <Typography variant="subtitle1" fontWeight={700}>
                            #{record.id} • {record.recordType}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {record.sourceSystem} •{" "}
                            {new Date(record.failedAt).toLocaleString("tr-TR")}
                          </Typography>
                        </Box>
                        <Chip
                          label={record.status}
                          size="small"
                          color={getStatusColor(record.status) as any}
                        />
                      </Box>
                      <Typography
                        variant="body2"
                        color="error.main"
                        sx={{ mt: 1 }}
                      >
                        {record.errorMessage}
                      </Typography>
                      <Box
                        sx={{
                          display: "grid",
                          gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                          columnGap: 1,
                          rowGap: 1,
                          mt: 1.5,
                        }}
                      >
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            Kayıt ID
                          </Typography>
                          <Typography fontWeight={600}>
                            {record.recordId || "-"}
                          </Typography>
                        </Box>
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            Deneme
                          </Typography>
                          <Typography fontWeight={600}>
                            {record.retryCount}
                          </Typography>
                        </Box>
                      </Box>
                      <Stack
                        direction="row"
                        spacing={1}
                        sx={{ mt: 1.5 }}
                        flexWrap="wrap"
                      >
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<Visibility />}
                          onClick={() => handleViewDetails(record.id)}
                        >
                          Detay
                        </Button>
                        {record.status === "FAILED" && (
                          <>
                            <Button
                              size="small"
                              variant="outlined"
                              color="success"
                              startIcon={<Edit />}
                              onClick={() => {
                                handleViewDetails(record.id);
                                setResolveDialogOpen(true);
                              }}
                            >
                              Düzelt
                            </Button>
                            <Button
                              size="small"
                              variant="outlined"
                              color="warning"
                              startIcon={<RestartAlt />}
                              onClick={() => handleRetry(record.id)}
                            >
                              Yeniden Dene
                            </Button>
                          </>
                        )}
                      </Stack>
                    </Paper>
                  ))}
                </Stack>
              ) : (
                <TableContainer component={Paper}>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>
                          <strong>ID</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Kayıt Tipi</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Kayıt ID</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Kaynak Sistem</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Hata Mesajı</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Hata Tarihi</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Deneme</strong>
                        </TableCell>
                        <TableCell>
                          <strong>Durum</strong>
                        </TableCell>
                        <TableCell align="right">
                          <strong>İşlemler</strong>
                        </TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {records.map((record) => (
                        <TableRow key={record.id}>
                          <TableCell>{record.id}</TableCell>
                          <TableCell>
                            <Chip
                              label={record.recordType}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>{record.recordId || "-"}</TableCell>
                          <TableCell>{record.sourceSystem}</TableCell>
                          <TableCell>
                            <Tooltip title={record.errorMessage}>
                              <Typography noWrap sx={{ maxWidth: 300 }}>
                                {record.errorMessage}
                              </Typography>
                            </Tooltip>
                          </TableCell>
                          <TableCell>
                            {new Date(record.failedAt).toLocaleString("tr-TR")}
                          </TableCell>
                          <TableCell>{record.retryCount}</TableCell>
                          <TableCell>
                            <Chip
                              label={record.status}
                              size="small"
                              color={getStatusColor(record.status) as any}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <IconButton
                              size="small"
                              onClick={() => handleViewDetails(record.id)}
                              color="primary"
                            >
                              <Visibility />
                            </IconButton>
                            {record.status === "FAILED" && (
                              <>
                                <IconButton
                                  size="small"
                                  onClick={() => {
                                    handleViewDetails(record.id);
                                    setResolveDialogOpen(true);
                                  }}
                                  color="success"
                                >
                                  <Edit />
                                </IconButton>
                                <IconButton
                                  size="small"
                                  onClick={() => handleRetry(record.id)}
                                  color="warning"
                                >
                                  <RestartAlt />
                                </IconButton>
                              </>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
              <TablePagination
                component="div"
                count={total}
                page={page}
                onPageChange={(_, newPage) => setPage(newPage)}
                rowsPerPage={pageSize}
                onRowsPerPageChange={(e) => {
                  setPageSize(parseInt(e.target.value, 10));
                  setPage(0);
                }}
                labelRowsPerPage="Sayfa başına:"
              />
            </>
          )}
        </CardContent>
      </Card>

      {/* Detail Dialog */}
      <Dialog
        open={detailDialogOpen}
        onClose={() => setDetailDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Hatalı Kayıt Detayları</DialogTitle>
        <DialogContent>
          {selectedRecord && (
            <Box>
              <Alert severity="error" sx={{ mb: 2 }}>
                <strong>Hata:</strong> {selectedRecord.errorMessage}
                {selectedRecord.errorCode &&
                  ` (Kod: ${selectedRecord.errorCode})`}
              </Alert>

              {parsedData ? (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Veri Düzenleme:
                  </Typography>
                  <Box display="grid" gap={2}>
                    {Object.keys(parsedData).map((key) => {
                      const originalType = typeof parsedData[key];
                      const isNumber = originalType === "number";
                      return (
                        <TextField
                          key={key}
                          fullWidth
                          label={key}
                          value={parsedData[key]}
                          onChange={(e) => {
                            const rawValue = e.target.value;
                            let finalValue: any = rawValue;

                            if (isNumber) {
                              // Allow empty string during editing
                              if (rawValue === "" || rawValue === "-") {
                                finalValue = rawValue;
                              } else {
                                const parsed = parseFloat(rawValue);
                                finalValue = isNaN(parsed) ? 0 : parsed;
                              }
                            }

                            setParsedData({ ...parsedData, [key]: finalValue });
                          }}
                          onBlur={() => {
                            // Convert to number on blur if it's a number field
                            if (
                              isNumber &&
                              (parsedData[key] === "" ||
                                parsedData[key] === "-")
                            ) {
                              setParsedData({ ...parsedData, [key]: 0 });
                            }
                          }}
                          type={isNumber ? "number" : "text"}
                          size="small"
                          inputProps={isNumber ? { step: "any" } : {}}
                        />
                      );
                    })}
                  </Box>
                </Box>
              ) : (
                <>
                  <Typography variant="subtitle2" gutterBottom>
                    Orijinal Veri (JSON):
                  </Typography>
                  <TextField
                    fullWidth
                    multiline
                    rows={6}
                    value={correctedData}
                    onChange={(e) => setCorrectedData(e.target.value)}
                    variant="outlined"
                    sx={{ mb: 2, fontFamily: "monospace", fontSize: "0.85rem" }}
                  />
                </>
              )}

              <Box display="grid" gridTemplateColumns="1fr 1fr" gap={2}>
                <Box>
                  <Typography variant="body2">
                    <strong>Kayıt Tipi:</strong> {selectedRecord.recordType}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Kayıt ID:</strong> {selectedRecord.recordId || "-"}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Deneme Sayısı:</strong> {selectedRecord.retryCount}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="body2">
                    <strong>Hata Tarihi:</strong>{" "}
                    {new Date(selectedRecord.failedAt).toLocaleString("tr-TR")}
                  </Typography>
                  {selectedRecord.lastRetryAt && (
                    <Typography variant="body2">
                      <strong>Son Deneme:</strong>{" "}
                      {new Date(selectedRecord.lastRetryAt).toLocaleString(
                        "tr-TR"
                      )}
                    </Typography>
                  )}
                  <Typography variant="body2">
                    <strong>Durum:</strong> {selectedRecord.status}
                  </Typography>
                </Box>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setDetailDialogOpen(false)}
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
            Kapat
          </Button>
          {selectedRecord?.status === "FAILED" && (
            <>
              <Button
                startIcon={<Block />}
                onClick={() => {
                  const reason = prompt("Göz ardı etme nedeni:");
                  if (reason) {
                    setIgnoreReason(reason);
                    handleIgnore(selectedRecord.id);
                  }
                }}
                variant="contained"
                color="warning"
                sx={{ fontWeight: 600, color: "white" }}
              >
                Göz Ardı Et
              </Button>
              <Button
                startIcon={<CheckCircle />}
                sx={{ fontWeight: 600, color: "white" }}
                onClick={async () => {
                  if (!parsedData) {
                    alert("Düzenlenecek veri bulunamadı!");
                    return;
                  }
                  setResend(true);
                  setResolution("Veri düzeltildi ve yeniden gönderildi");
                  await handleResolve();
                }}
                variant="contained"
                color="success"
              >
                Düzelt ve Gönder
              </Button>
            </>
          )}
        </DialogActions>
      </Dialog>

      {/* Resolve Dialog */}
      <Dialog
        open={resolveDialogOpen}
        onClose={() => setResolveDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Hatayı Çöz</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Çözüm Açıklaması"
            multiline
            rows={3}
            value={resolution}
            onChange={(e) => setResolution(e.target.value)}
            sx={{ mt: 2, mb: 2 }}
          />
          <FormControl fullWidth>
            <InputLabel>Düzeltilmiş veriyi yeniden gönder</InputLabel>
            <Select
              value={resend ? "yes" : "no"}
              onChange={(e) => setResend(e.target.value === "yes")}
            >
              <MenuItem value="no">Hayır, sadece işaretle</MenuItem>
              <MenuItem value="yes">Evet, yeniden gönder</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setResolveDialogOpen(false)}
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
            onClick={handleResolve}
            variant="contained"
            color="primary"
            sx={{ fontWeight: 600, color: "white" }}
          >
            Çöz
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default FailedRecords;
