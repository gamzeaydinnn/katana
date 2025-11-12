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
import * as XLSX from "xlsx";
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
    loadStockReport();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadStockReport = async () => {
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
      console.warn("Stock report warning:", {
        message: err?.message,
        status: err?.response?.status,
      });
      setError(
        err?.response?.data?.error || err?.message || "Stok raporu yüklenemedi"
      );
    } finally {
      setLoading(false);
    }
  };

  const downloadExcel = () => {
    if (!stockReport?.stockData.length) return;

    const worksheet = XLSX.utils.json_to_sheet(
      stockReport.stockData.map((item) => ({
        "Ürün Adı": item.name,
        SKU: item.sku,
        Stok: item.quantity,
        Fiyat: item.price.toFixed(2),
        Değer: item.stockValue.toFixed(2),
        Durum: item.isOutOfStock
          ? "Tükendi"
          : item.isLowStock
          ? "Düşük Stok"
          : "Normal",
        Güncelleme: new Date(item.lastUpdated).toLocaleDateString("tr-TR"),
      }))
    );

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Stok Raporu");
    XLSX.writeFile(
      workbook,
      `stok_raporu_${new Date().toISOString().split("T")[0]}.xlsx`
    );
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
                startIcon={<FileDownload />}
                onClick={downloadExcel}
                disabled={!stockReport || stockReport.stockData.length === 0}
                sx={{ minWidth: 150 }}
              >
                Excel İndir
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
                onClick={downloadExcel}
              >
                Excel İndir
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
