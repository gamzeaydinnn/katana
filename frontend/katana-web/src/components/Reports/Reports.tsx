import {
    Assessment,
    CheckCircle,
    FileDownload,
    Inventory,
    TrendingDown,
    TrendingUp
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
import { salesOrdersAPI, stockAPI } from "../../services/api";

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

interface GroupedSalesOrderLine {
  id: number;
  salesOrderId: number;
  katanaRowId: number;
  variantId: number;
  sku: string;
  productName?: string;
  quantity: number;
  pricePerUnit?: number;
  total?: number;
  taxRate?: number;
}

interface GroupedSalesOrder {
  groupKatanaOrderId: number;
  orderNo: string;
  orderNos: string[];
  customerId: number;
  customerName?: string;
  orderCreatedDate?: string;
  deliveryDate?: string;
  currency?: string;
  status: string;
  total?: number;
  totalInBaseCurrency?: number;
  isSyncedToLuca: boolean;
  lastSyncError?: string;
  lastSyncAt?: string;
  lines: GroupedSalesOrderLine[];
}

const Reports: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [excelLoading, setExcelLoading] = useState(false);
  const [error, setError] = useState("");
  const [stockReport, setStockReport] = useState<StockReportResponse | null>(
    null
  );
  const [groupedLoading, setGroupedLoading] = useState(false);
  const [groupedExcelLoading, setGroupedExcelLoading] = useState(false);
  const [groupedError, setGroupedError] = useState("");
  const [groupedOrders, setGroupedOrders] = useState<GroupedSalesOrder[]>([]);
  const [search, setSearch] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const isMobile = useMediaQuery("(max-width:900px)");

  useEffect(() => {
    loadStockReport();
    loadGroupedOrders();
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

  const loadGroupedOrders = async () => {
    try {
      setGroupedLoading(true);
      setGroupedError("");
      const params = new URLSearchParams({
        page: "1",
        pageSize: "200",
      });
      const response = (await salesOrdersAPI.getGrouped(
        params.toString()
      )) as GroupedSalesOrder[];
      setGroupedOrders(Array.isArray(response) ? response : []);
    } catch (err: any) {
      console.warn("Grouped sales orders warning:", {
        message: err?.message,
        status: err?.response?.status,
      });
      setGroupedError(
        err?.response?.data?.error ||
          err?.message ||
          "Gruplu satış siparişleri yüklenemedi"
      );
    } finally {
      setGroupedLoading(false);
    }
  };

  const downloadGroupedExcel = async () => {
    try {
      setGroupedExcelLoading(true);
      setGroupedError("");
      const params = new URLSearchParams({
        page: "1",
        pageSize: "10000",
      });
      const response = (await salesOrdersAPI.getGrouped(
        params.toString()
      )) as GroupedSalesOrder[];

      if (!response?.length) {
        setGroupedError("İndirilecek veri bulunamadı");
        return;
      }

      const rows = response.flatMap((order) => {
        if (!order.lines?.length) {
          return [
            {
              "Katana Order Id": order.groupKatanaOrderId,
              "Sipariş No": order.orderNo,
              "Sipariş Nolar": order.orderNos?.join(", ") || order.orderNo,
              Müşteri: order.customerName || "",
              "Sipariş Tarihi": order.orderCreatedDate
                ? new Date(order.orderCreatedDate).toLocaleDateString("tr-TR")
                : "",
              Durum: order.status,
              Para: order.currency || "",
              "Toplam Tutar": order.total ?? 0,
              "Satır SKU": "",
              Miktar: 0,
              "Birim Fiyat": 0,
              "Satır Tutarı": 0,
            },
          ];
        }

        return order.lines.map((line) => ({
          "Katana Order Id": order.groupKatanaOrderId,
          "Sipariş No": order.orderNo,
          "Sipariş Nolar": order.orderNos?.join(", ") || order.orderNo,
          Müşteri: order.customerName || "",
          "Sipariş Tarihi": order.orderCreatedDate
            ? new Date(order.orderCreatedDate).toLocaleDateString("tr-TR")
            : "",
          Durum: order.status,
          Para: order.currency || "",
          "Toplam Tutar": order.total ?? 0,
          "Satır SKU": line.sku,
          Miktar: line.quantity ?? 0,
          "Birim Fiyat": line.pricePerUnit ?? 0,
          "Satır Tutarı": line.total ?? 0,
        }));
      });

      const worksheet = XLSX.utils.json_to_sheet(rows);
      const workbook = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(workbook, worksheet, "Sipariş Grupları");
      XLSX.writeFile(
        workbook,
        `siparis_gruplari_${new Date().toISOString().split("T")[0]}.xlsx`
      );
    } catch (err: any) {
      setGroupedError(
        "Excel indirme hatası: " + (err?.message || "Bilinmeyen hata")
      );
    } finally {
      setGroupedExcelLoading(false);
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
                disabled={
                  excelLoading ||
                  !stockReport ||
                  stockReport.stockData.length === 0
                }
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

      <Box sx={{ mt: 4 }}>
        <Box
          sx={{
            display: "flex",
            alignItems: { xs: "flex-start", sm: "center" },
            flexDirection: { xs: "column", sm: "row" },
            gap: { xs: 1, sm: 2 },
            mb: 2,
          }}
        >
          <Assessment sx={{ fontSize: 28, color: "primary.main" }} />
          <Typography
            variant="h5"
            sx={{ fontWeight: 800, letterSpacing: "-0.01em" }}
          >
            Satış Siparişi Grupları (Katana OrderId)
          </Typography>
        </Box>

        {groupedError && (
          <Alert
            severity="error"
            sx={{ mb: 2 }}
            onClose={() => setGroupedError("")}
          >
            {groupedError}
          </Alert>
        )}

        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Box
              sx={{
                display: "flex",
                flexDirection: { xs: "column", sm: "row" },
                gap: 1.5,
                alignItems: { xs: "stretch", sm: "center" },
                justifyContent: "space-between",
              }}
            >
              <Typography color="text.secondary">
                Toplam Grup: {groupedOrders.length}
              </Typography>
              <Box
                sx={{
                  display: "flex",
                  gap: 1.5,
                  flexDirection: { xs: "column", sm: "row" },
                }}
              >
                <Button
                  variant="outlined"
                  onClick={loadGroupedOrders}
                  disabled={groupedLoading}
                  sx={{ minWidth: 140 }}
                >
                  {groupedLoading ? "Yükleniyor..." : "Yenile"}
                </Button>
                <Button
                  variant="contained"
                  startIcon={
                    groupedExcelLoading ? (
                      <CircularProgress size={18} color="inherit" />
                    ) : (
                      <FileDownload />
                    )
                  }
                  onClick={downloadGroupedExcel}
                  disabled={
                    groupedExcelLoading ||
                    groupedLoading ||
                    groupedOrders.length === 0
                  }
                  sx={{ minWidth: 160 }}
                >
                  {groupedExcelLoading ? "İndiriliyor..." : "Excel İndir"}
                </Button>
              </Box>
            </Box>
          </CardContent>
        </Card>

        <Paper sx={{ p: { xs: 2, md: 3 } }}>
          {groupedLoading ? (
            <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
              <CircularProgress />
            </Box>
          ) : groupedOrders.length === 0 ? (
            <Typography color="text.secondary" align="center">
              Görüntülenecek veri yok
            </Typography>
          ) : isMobile ? (
            <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
              {groupedOrders.map((order) => (
                <Box
                  key={order.groupKatanaOrderId}
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
                      <Typography variant="subtitle1" fontWeight={700}>
                        {order.orderNo}
                      </Typography>
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{ mt: 0.25 }}
                      >
                        Katana ID: {order.groupKatanaOrderId}
                      </Typography>
                    </Box>
                    <Chip label={order.status} size="small" />
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
                      <Typography variant="caption" color="text.secondary">
                        Müşteri
                      </Typography>
                      <Typography fontWeight={600}>
                        {order.customerName || "-"}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Satır Sayısı
                      </Typography>
                      <Typography fontWeight={600}>
                        {order.lines?.length || 0}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Toplam
                      </Typography>
                      <Typography fontWeight={600}>
                        {order.total?.toFixed(2) || "0.00"}{" "}
                        {order.currency || ""}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Tarih
                      </Typography>
                      <Typography fontWeight={600}>
                        {order.orderCreatedDate
                          ? new Date(order.orderCreatedDate).toLocaleDateString(
                              "tr-TR"
                            )
                          : "-"}
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              ))}
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
                      <strong>Katana ID</strong>
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
                    <TableCell>
                      <strong>Durum</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Satır</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>Toplam</strong>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {groupedOrders.map((order) => (
                    <TableRow key={order.groupKatanaOrderId} hover>
                      <TableCell>{order.groupKatanaOrderId}</TableCell>
                      <TableCell>{order.orderNo}</TableCell>
                      <TableCell>{order.customerName || "-"}</TableCell>
                      <TableCell>
                        {order.orderCreatedDate
                          ? new Date(order.orderCreatedDate).toLocaleDateString(
                              "tr-TR"
                            )
                          : "-"}
                      </TableCell>
                      <TableCell>
                        <Chip label={order.status} size="small" />
                      </TableCell>
                      <TableCell align="right">
                        {order.lines?.length || 0}
                      </TableCell>
                      <TableCell align="right">
                        {order.total?.toFixed(2) || "0.00"}{" "}
                        {order.currency || ""}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Paper>
      </Box>
    </Container>
  );
};

export default Reports;
