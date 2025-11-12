import {
  Assessment,
  CheckCircle,
  Download,
  FileDownload,
  Inventory,
  TrendingDown,
  TrendingUp,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  FormControlLabel,
  Paper,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { stockAPI } from "../../services/api";

interface StockReportData {
  id: number;
  name: string;
  sku: string;
  categoryId: number;
  quantity: number;
  price: number;
  stockValue: number;
  isLowStock: boolean;
  isOutOfStock: boolean;
  isActive: boolean;
  lastUpdated: string;
}

interface StockReportResponse {
  stockData: StockReportData[];
  summary: {
    totalProducts: number;
    totalStockValue: number;
    averagePrice: number;
    totalStock: number;
    lowStockCount: number;
    outOfStockCount: number;
    activeProductsCount: number;
  };
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

const Reports: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [stockReport, setStockReport] = useState<StockReportResponse | null>(
    null
  );
  const [search, setSearch] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);

  useEffect(() => {
    handleStockReport();
  }, []);

  const handleStockReport = async () => {
    try {
      setLoading(true);
      setError("");
      const params = new URLSearchParams({
        page: "1",
        pageSize: "100",
        ...(search && { search }),
        ...(lowStockOnly && { lowStockOnly: "true" }),
      });
      const response = await stockAPI.getStockReport(params.toString());
      setStockReport(response as StockReportResponse);
    } catch (err: any) {
      console.error("Stock report error:", err);
      setError(
        err.response?.data?.error || err.message || "Stok raporu yüklenemedi"
      );
    } finally {
      setLoading(false);
    }
  };

  const downloadCSV = () => {
    if (!stockReport?.stockData.length) return;
    const headers = [
      "Ürün Adı",
      "SKU",
      "Stok",
      "Fiyat",
      "Değer",
      "Durum",
      "Güncelleme",
    ];
    const rows = stockReport.stockData.map((item) => [
      item.name,
      item.sku,
      item.quantity,
      item.price.toFixed(2),
      item.stockValue.toFixed(2),
      item.isOutOfStock ? "Tükendi" : item.isLowStock ? "Düşük Stok" : "Normal",
      new Date(item.lastUpdated).toLocaleDateString("tr-TR"),
    ]);
    const csvContent = [
      headers.join(","),
      ...rows.map((r) => r.join(",")),
    ].join("\n");
    const BOM = "\uFEFF";
    const blob = new Blob([BOM + csvContent], {
      type: "text/csv;charset=utf-8;",
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `stok_raporu_${new Date().toISOString().split("T")[0]}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Assessment sx={{ fontSize: 32, color: "primary.main" }} />
        <Typography
          variant="h4"
          sx={{
            fontWeight: 900,
            letterSpacing: "-0.02em",
            background: "linear-gradient(135deg, #4f46e5 0%, #0891b2 100%)",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
            backgroundClip: "text",
          }}
        >
          Stok Raporu
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError("")}>
          {error}
        </Alert>
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <TextField
              fullWidth
              size="small"
              label="Ürün Adı veya SKU ile Ara"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Ürün adı veya SKU girin"
            />
            <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={lowStockOnly}
                    onChange={(e) => setLowStockOnly(e.target.checked)}
                  />
                }
                label="Sadece Düşük Stok"
              />
              <Button
                variant="contained"
                startIcon={
                  loading ? <CircularProgress size={16} /> : <Download />
                }
                onClick={handleStockReport}
                disabled={loading}
                sx={{ minWidth: 150 }}
              >
                Rapor Oluştur
              </Button>
            </Box>
          </Box>
        </CardContent>
      </Card>

      {stockReport && (
        <>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
              gap: 2,
              mb: 3,
            }}
          >
            <Card>
              <CardContent>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    mb: 1,
                  }}
                >
                  <Inventory color="primary" />
                  <Typography variant="body2" color="text.secondary">
                    Toplam Ürün
                  </Typography>
                </Box>
                <Typography variant="h4" fontWeight="bold">
                  {stockReport.summary.totalProducts}
                </Typography>
              </CardContent>
            </Card>
            <Card>
              <CardContent>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    mb: 1,
                  }}
                >
                  <TrendingUp color="success" />
                  <Typography variant="body2" color="text.secondary">
                    Toplam Stok Değeri
                  </Typography>
                </Box>
                <Typography variant="h4" fontWeight="bold">
                  {stockReport.summary.totalStockValue.toFixed(0)} ₺
                </Typography>
              </CardContent>
            </Card>
            <Card>
              <CardContent>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    mb: 1,
                  }}
                >
                  <TrendingDown color="error" />
                  <Typography variant="body2" color="text.secondary">
                    Düşük Stok
                  </Typography>
                </Box>
                <Typography variant="h4" fontWeight="bold" color="error">
                  {stockReport.summary.lowStockCount}
                </Typography>
              </CardContent>
            </Card>
            <Card>
              <CardContent>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    mb: 1,
                  }}
                >
                  <CheckCircle color="success" />
                  <Typography variant="body2" color="text.secondary">
                    Aktif Ürün
                  </Typography>
                </Box>
                <Typography variant="h4" fontWeight="bold" color="success.main">
                  {stockReport.summary.activeProductsCount}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          <Paper sx={{ p: 3 }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 2,
              }}
            >
              <Typography variant="h6">
                Stok Detayları ({stockReport.pagination.totalCount} ürün)
              </Typography>
              <Button
                variant="outlined"
                size="small"
                startIcon={<FileDownload />}
                onClick={downloadCSV}
              >
                CSV İndir
              </Button>
            </Box>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <strong>Ürün Adı</strong>
                    </TableCell>
                    <TableCell>
                      <strong>SKU</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Stok</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Fiyat</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Değer</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell>
                      <strong>Güncelleme</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {stockReport.stockData.map((item) => (
                    <TableRow key={item.id} hover>
                      <TableCell>{item.name}</TableCell>
                      <TableCell>{item.sku}</TableCell>
                      <TableCell align="right">{item.quantity}</TableCell>
                      <TableCell align="right">
                        {item.price.toFixed(2)} ₺
                      </TableCell>
                      <TableCell align="right">
                        {item.stockValue.toFixed(2)} ₺
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={
                            item.isOutOfStock
                              ? "Tükendi"
                              : item.isLowStock
                              ? "Düşük Stok"
                              : "Normal"
                          }
                          color={
                            item.isOutOfStock
                              ? "error"
                              : item.isLowStock
                              ? "warning"
                              : "success"
                          }
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {new Date(item.lastUpdated).toLocaleDateString("tr-TR")}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </>
      )}
    </Container>
  );
};

export default Reports;
