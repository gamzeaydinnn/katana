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
  useMediaQuery,
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
  const [excelLoading, setExcelLoading] = useState(false);
  const [error, setError] = useState("");
  const [stockReport, setStockReport] = useState<StockReportResponse | null>(
    null
  );
  const [search, setSearch] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const isMobile = useMediaQuery("(max-width:900px)");

  useEffect(() => {
    loadStockReport();
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

  const downloadExcel = async () => {
    try {
      setExcelLoading(true);
      // Tüm ürünleri çekmek için büyük pageSize kullan
      const params = new URLSearchParams({
        page: "1",
        pageSize: "10000",
        ...(search && { search }),
        ...(lowStockOnly && { lowStockOnly: "true" }),
      });
      const response = (await stockAPI.getStockReport(
        params.toString()
      )) as StockReportResponse;

      if (!response?.stockData?.length) {
        setError("İndirilecek veri bulunamadı");
        return;
      }

      const worksheet = XLSX.utils.json_to_sheet(
        response.stockData.map((item) => ({
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
    } catch (err: any) {
      setError("Excel indirme hatası: " + (err?.message || "Bilinmeyen hata"));
    } finally {
      setExcelLoading(false);
    }
  };

  return (
    <Container
      maxWidth="lg"
      sx={{
        mt: { xs: 5.5, md: 4 },
        mb: { xs: 2.5, md: 4 },
        px: { xs: 1.5, sm: 2, md: 0 },
      }}
    >
      <Box
        sx={{
          display: "flex",
          alignItems: { xs: "flex-start", sm: "center" },
          flexDirection: { xs: "column", sm: "row" },
          gap: { xs: 1, sm: 2 },
          mb: 3,
        }}
      >
        <Assessment sx={{ fontSize: 32, color: "primary.main" }} />
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
            <Box
              sx={{
                display: "flex",
                flexWrap: "wrap",
                gap: { xs: 1, sm: 2 },
                alignItems: "center",
                justifyContent: { xs: "space-between", sm: "flex-start" },
              }}
            >
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
                  excelLoading ? (
                    <CircularProgress size={20} color="inherit" />
                  ) : (
                    <FileDownload />
                  )
                }
                onClick={downloadExcel}
                disabled={
                  excelLoading ||
                  !stockReport ||
                  stockReport.stockData.length === 0
                }
                sx={{ minWidth: 150, width: { xs: "100%", sm: "auto" } }}
              >
                {excelLoading ? "İndiriliyor..." : "Excel İndir"}
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
              gridTemplateColumns: {
                xs: "repeat(auto-fit, minmax(180px, 1fr))",
                sm: "repeat(auto-fit, minmax(220px, 1fr))",
                md: "repeat(auto-fit, minmax(250px, 1fr))",
              },
              gap: { xs: 1.5, md: 2 },
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

          <Paper sx={{ p: { xs: 2, md: 3 } }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: { xs: "flex-start", sm: "center" },
                flexDirection: { xs: "column", sm: "row" },
                gap: { xs: 1.5, sm: 0 },
                mb: 2,
              }}
            >
              <Typography variant="h6">
                Stok Detayları ({stockReport.pagination.totalCount} ürün)
              </Typography>
              <Button
                variant="outlined"
                size="small"
                startIcon={
                  excelLoading ? (
                    <CircularProgress size={16} />
                  ) : (
                    <FileDownload />
                  )
                }
                onClick={downloadExcel}
                disabled={excelLoading}
                sx={{ width: { xs: "100%", sm: "auto" } }}
              >
                {excelLoading ? "İndiriliyor..." : "Excel İndir"}
              </Button>
            </Box>
            {isMobile ? (
              <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
                {stockReport.stockData.length === 0 ? (
                  <Typography color="text.secondary" align="center">
                    Görüntülenecek veri yok
                  </Typography>
                ) : (
                  stockReport.stockData.map((item) => {
                    const statusLabel = item.isOutOfStock
                      ? "Tükendi"
                      : item.isLowStock
                      ? "Düşük Stok"
                      : "Normal";
                    const statusColor = item.isOutOfStock
                      ? "error"
                      : item.isLowStock
                      ? "warning"
                      : "success";

                    return (
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
                            gap: 1,
                            alignItems: "flex-start",
                          }}
                        >
                          <Box sx={{ minWidth: 0 }}>
                            <Typography
                              variant="subtitle1"
                              fontWeight={600}
                              sx={{ wordBreak: "break-word" }}
                            >
                              {item.name}
                            </Typography>
                            <Typography
                              variant="body2"
                              color="text.secondary"
                              sx={{ mt: 0.25 }}
                            >
                              SKU: <strong>{item.sku}</strong>
                            </Typography>
                          </Box>
                          <Chip
                            label={statusLabel}
                            color={statusColor}
                            size="small"
                            sx={{ flexShrink: 0 }}
                          />
                        </Box>

                        <Box
                          sx={{
                            display: "grid",
                            gridTemplateColumns:
                              "repeat(auto-fit, minmax(140px, 1fr))",
                            columnGap: 1,
                            rowGap: 1.25,
                            mt: 1.5,
                          }}
                        >
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Stok
                            </Typography>
                            <Typography fontWeight={600}>
                              {item.quantity}
                            </Typography>
                          </Box>
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Fiyat
                            </Typography>
                            <Typography fontWeight={600}>
                              {item.price.toFixed(2)} ₺
                            </Typography>
                          </Box>
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Stok Değeri
                            </Typography>
                            <Typography fontWeight={600}>
                              {item.stockValue.toFixed(2)} ₺
                            </Typography>
                          </Box>
                          <Box>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Güncelleme
                            </Typography>
                            <Typography fontWeight={600}>
                              {new Date(item.lastUpdated).toLocaleDateString(
                                "tr-TR"
                              )}
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    );
                  })
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
                          {new Date(item.lastUpdated).toLocaleDateString(
                            "tr-TR"
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </>
      )}
    </Container>
  );
};

export default Reports;
