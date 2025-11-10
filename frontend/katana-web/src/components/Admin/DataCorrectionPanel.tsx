import React, { useState, useEffect } from "react";
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
  Paper,
  Chip,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  IconButton,
  Tooltip,
  Stack,
} from "@mui/material";
import CompareArrowsIcon from "@mui/icons-material/CompareArrows";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import WarningIcon from "@mui/icons-material/Warning";
import EditIcon from "@mui/icons-material/Edit";
import RefreshIcon from "@mui/icons-material/Refresh";
import api from "../../services/api";

interface DataIssue {
  field: string;
  issue: string;
  katanaValue?: string;
  lucaValue?: string;
  severity: "Critical" | "Warning" | "Info";
}

interface ComparisonProduct {
  sku: string;
  name: string;
  katanaData?: {
    sku: string;
    name: string;
    salesPrice?: number;
    costPrice?: number;
    onHand?: number;
    available?: number;
    isActive: boolean;
  };
  lucaData?: {
    id: number;
    sku: string;
    name: string;
    price: number;
    stock: number;
    isActive: boolean;
  };
  issues: DataIssue[];
}

interface DataCorrection {
  id: number;
  sourceSystem: string;
  entityType: string;
  entityId: string;
  fieldName: string;
  originalValue?: string;
  correctedValue?: string;
  correctionReason: string;
  isApproved: boolean;
  createdAt: string;
}

const DataCorrectionPanel: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [comparisons, setComparisons] = useState<ComparisonProduct[]>([]);
  const [corrections, setCorrections] = useState<DataCorrection[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Correction dialog
  const [openDialog, setOpenDialog] = useState(false);
  const [selectedProduct, setSelectedProduct] =
    useState<ComparisonProduct | null>(null);
  const [correctionForm, setCorrectionForm] = useState({
    field: "",
    correctedValue: "",
    reason: "",
  });

  const fetchComparisons = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await api.get("/DataCorrection/compare/products");
      const responseData: any = response.data;
      setComparisons(responseData?.data || responseData || []);
    } catch (err: any) {
      setError(err.response?.data?.error || "Karşılaştırma yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  const fetchCorrections = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await api.get("/DataCorrection/pending");
      const responseData: any = response.data;
      setCorrections(responseData?.data || responseData || []);
    } catch (err: any) {
      setError(err.response?.data?.error || "Düzeltmeler yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === 0) fetchComparisons();
    else if (activeTab === 1) fetchCorrections();
  }, [activeTab]);

  const handleOpenDialog = (product: ComparisonProduct, field: string) => {
    setSelectedProduct(product);
    setCorrectionForm({
      field,
      correctedValue: "",
      reason: "",
    });
    setOpenDialog(true);
  };

  const handleCreateCorrection = async () => {
    if (!selectedProduct) return;

    try {
      await api.post("/DataCorrection", {
        sourceSystem: selectedProduct.katanaData ? "Katana" : "Luca",
        entityType: "Product",
        entityId: selectedProduct.sku,
        fieldName: correctionForm.field,
        originalValue:
          correctionForm.field === "Price"
            ? selectedProduct.katanaData?.salesPrice?.toString()
            : selectedProduct.katanaData?.name,
        correctedValue: correctionForm.correctedValue,
        correctionReason: correctionForm.reason,
      });

      setSuccess("Düzeltme kaydı oluşturuldu");
      setOpenDialog(false);
      fetchCorrections();
    } catch (err: any) {
      setError(err.response?.data?.error || "Düzeltme oluşturulamadı");
    }
  };

  const handleApproveCorrection = async (id: number) => {
    try {
      await api.post(`/DataCorrection/${id}/approve`);
      setSuccess("Düzeltme onaylandı");
      fetchCorrections();
    } catch (err: any) {
      setError(err.response?.data?.error || "Onaylama başarısız");
    }
  };

  const handleApplyToLuca = async (id: number) => {
    try {
      await api.post(`/DataCorrection/${id}/apply-to-luca`);
      setSuccess("Düzeltme Luca'ya uygulandı");
      fetchCorrections();
      fetchComparisons();
    } catch (err: any) {
      setError(err.response?.data?.error || "Uygulama başarısız");
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case "Critical":
        return <ErrorIcon color="error" />;
      case "Warning":
        return <WarningIcon color="warning" />;
      default:
        return <CheckCircleIcon color="info" />;
    }
  };

  const getSeverityColor = (severity: string): "error" | "warning" | "info" => {
    switch (severity) {
      case "Critical":
        return "error";
      case "Warning":
        return "warning";
      default:
        return "info";
    }
  };

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
              <CompareArrowsIcon color="primary" />
              <Typography variant="h5">
                Veri Düzeltme ve Karşılaştırma
              </Typography>
            </Stack>
            <Tooltip title="Yenile">
              <IconButton
                onClick={() =>
                  activeTab === 0 ? fetchComparisons() : fetchCorrections()
                }
                disabled={loading}
              >
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Stack>

          <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)}>
            <Tab label="Katana ↔ Luca Karşılaştırma" />
            <Tab label="Bekleyen Düzeltmeler" />
          </Tabs>
        </CardContent>
      </Card>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
      {success && (
        <Alert
          severity="success"
          sx={{ mb: 2 }}
          onClose={() => setSuccess(null)}
        >
          {success}
        </Alert>
      )}

      {loading ? (
        <Box display="flex" justifyContent="center" p={4}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* Tab 0: Karşılaştırma */}
          {activeTab === 0 && (
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
                      <strong>Katana Veri</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Luca Veri</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Sorunlar</strong>
                    </TableCell>
                    <TableCell>
                      <strong>İşlemler</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {comparisons.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography color="textSecondary">
                          Uyuşmazlık bulunamadı
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    comparisons.map((comp) => (
                      <TableRow key={comp.sku} hover>
                        <TableCell>
                          <Typography variant="body2" fontWeight="bold">
                            {comp.sku}
                          </Typography>
                        </TableCell>
                        <TableCell>{comp.name}</TableCell>
                        <TableCell>
                          {comp.katanaData ? (
                            <Box>
                              <Typography variant="caption">
                                Fiyat: {comp.katanaData.salesPrice?.toFixed(2)}{" "}
                                ₺
                              </Typography>
                              <br />
                              <Typography variant="caption">
                                Stok: {comp.katanaData.onHand}
                              </Typography>
                            </Box>
                          ) : (
                            <Chip label="Yok" size="small" color="error" />
                          )}
                        </TableCell>
                        <TableCell>
                          {comp.lucaData ? (
                            <Box>
                              <Typography variant="caption">
                                Fiyat: {comp.lucaData.price.toFixed(2)} ₺
                              </Typography>
                              <br />
                              <Typography variant="caption">
                                Stok: {comp.lucaData.stock}
                              </Typography>
                            </Box>
                          ) : (
                            <Chip label="Yok" size="small" color="error" />
                          )}
                        </TableCell>
                        <TableCell>
                          <Stack spacing={0.5}>
                            {comp.issues.map((issue, idx) => (
                              <Chip
                                key={idx}
                                label={`${issue.field}: ${issue.issue}`}
                                size="small"
                                color={getSeverityColor(issue.severity)}
                                icon={getSeverityIcon(issue.severity)}
                              />
                            ))}
                          </Stack>
                        </TableCell>
                        <TableCell>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<EditIcon />}
                            onClick={() => handleOpenDialog(comp, "Price")}
                          >
                            Düzelt
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {/* Tab 1: Bekleyen Düzeltmeler */}
          {activeTab === 1 && (
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>Kaynak</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Varlık</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Alan</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Orijinal</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Düzeltilmiş</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Sebep</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Tarih</strong>
                    </TableCell>
                    <TableCell>
                      <strong>İşlemler</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {corrections.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={8} align="center">
                        <Typography color="textSecondary">
                          Bekleyen düzeltme yok
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    corrections.map((corr) => (
                      <TableRow key={corr.id}>
                        <TableCell>
                          <Chip label={corr.sourceSystem} size="small" />
                        </TableCell>
                        <TableCell>{corr.entityId}</TableCell>
                        <TableCell>{corr.fieldName}</TableCell>
                        <TableCell>
                          <Typography variant="caption">
                            {corr.originalValue}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" fontWeight="bold">
                            {corr.correctedValue}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption">
                            {corr.correctionReason}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption">
                            {new Date(corr.createdAt).toLocaleString("tr-TR")}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          {!corr.isApproved ? (
                            <Button
                              size="small"
                              variant="contained"
                              color="success"
                              onClick={() => handleApproveCorrection(corr.id)}
                            >
                              Onayla
                            </Button>
                          ) : (
                            <Button
                              size="small"
                              variant="contained"
                              onClick={() => handleApplyToLuca(corr.id)}
                            >
                              Luca'ya Uygula
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </>
      )}

      {/* Correction Dialog */}
      <Dialog
        open={openDialog}
        onClose={() => setOpenDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Veri Düzeltmesi Oluştur</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 2 }}>
            <TextField
              label="Alan"
              value={correctionForm.field}
              onChange={(e) =>
                setCorrectionForm({ ...correctionForm, field: e.target.value })
              }
              fullWidth
            />
            <TextField
              label="Düzeltilmiş Değer"
              value={correctionForm.correctedValue}
              onChange={(e) =>
                setCorrectionForm({
                  ...correctionForm,
                  correctedValue: e.target.value,
                })
              }
              fullWidth
            />
            <TextField
              label="Düzeltme Sebebi"
              value={correctionForm.reason}
              onChange={(e) =>
                setCorrectionForm({ ...correctionForm, reason: e.target.value })
              }
              multiline
              rows={3}
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>İptal</Button>
          <Button onClick={handleCreateCorrection} variant="contained">
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default DataCorrectionPanel;
