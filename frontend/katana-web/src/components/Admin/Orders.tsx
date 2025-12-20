import { Refresh } from "@mui/icons-material";
import {
    Alert,
    Box,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    IconButton,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TablePagination,
    TableRow,
    Typography,
    useMediaQuery,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api from "../../services/api";

interface OrderItem {
  id: number;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

interface Order {
  id: number;
  orderNo: string;
  customerId: number;
  customerName?: string;
  status: string;
  total?: number;
  orderCreatedDate?: string;
  createdAt?: string;
  updatedAt?: string;
  currency?: string;
  customer?: {
    id: number;
    title: string;
    taxNo: string;
  };
  items: OrderItem[];
}

const Orders: React.FC = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const isMobile = useMediaQuery("(max-width:900px)");

  const loadOrders = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await api.get<any>("/sales-orders");
      const data = response.data?.value || response.data;
      setOrders(Array.isArray(data) ? data : []);
    } catch (err: any) {
      setError(err.response?.data?.message || "Siparişler yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, []);

  const getStatusColor = (
    status: string
  ): "default" | "primary" | "success" | "warning" | "error" => {
    switch (status) {
      case "Pending":
        return "warning";
      case "Confirmed":
        return "primary";
      case "Shipped":
        return "default";
      case "Delivered":
        return "success";
      case "Cancelled":
        return "error";
      default:
        return "default";
    }
  };

  const getStatusLabel = (status: string): string => {
    const labels: Record<string, string> = {
      Pending: "Beklemede",
      Confirmed: "Onaylandı",
      Shipped: "Kargoda",
      Delivered: "Teslim Edildi",
      Cancelled: "İptal",
    };
    return labels[status] || status;
  };

  if (loading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="400px"
      >
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Card>
      <CardContent>
        <Box
          display="flex"
          justifyContent="space-between"
          alignItems="center"
          mb={2}
        >
          <Typography variant="h6" fontWeight="bold">
            Siparişler
          </Typography>
          <IconButton onClick={loadOrders} size="small" color="primary">
            <Refresh />
          </IconButton>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {isMobile ? (
          <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
            {orders.length === 0 && (
              <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
                Henüz sipariş bulunmuyor
              </Typography>
            )}
            {orders
              .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
              .map((order) => (
                <Card
                  key={order.id}
                  variant="outlined"
                  sx={{
                    borderRadius: 2,
                    padding: 1.5,
                    boxShadow: "0 2px 6px rgba(0,0,0,0.04)",
                  }}
                >
                  {/* Üst satır: Sipariş No + Durum Chip */}
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      mb: 1,
                    }}
                  >
                    <Typography variant="subtitle1" fontWeight="bold">
                      {order.orderNo || `#${order.id}`}
                    </Typography>
                    <Chip
                      label={getStatusLabel(order.status)}
                      color={getStatusColor(order.status)}
                      size="small"
                    />
                  </Box>

                  {/* İkinci satır: Müşteri adı + Tarih */}
                  <Box sx={{ mb: 1.5 }}>
                    <Typography variant="body2" fontWeight={500}>
                      {order.customerName ||
                        order.customer?.title ||
                        `Müşteri #${order.customerId}`}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {new Date(
                        order.orderCreatedDate || order.createdAt || new Date()
                      ).toLocaleString("tr-TR", {
                        day: "2-digit",
                        month: "2-digit",
                        year: "numeric",
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </Typography>
                  </Box>

                  {/* Üçüncü satır: 2 sütunlu bilgi grid */}
                  <Box
                    sx={{
                      display: "grid",
                      gridTemplateColumns: "1fr 1fr",
                      gap: 1.5,
                    }}
                  >
                    {/* Sol üst: Müşteri */}
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Müşteri
                      </Typography>
                      <Typography variant="body2">
                        {order.customerName ||
                          order.customer?.title ||
                          `#${order.customerId}`}
                      </Typography>
                    </Box>

                    {/* Sağ üst: Tutar */}
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Tutar
                      </Typography>
                      <Typography variant="body2" fontWeight="bold">
                        ₺
                        {(order.total || 0).toLocaleString("tr-TR", {
                          minimumFractionDigits: 2,
                        })}
                      </Typography>
                    </Box>

                    {/* Sol alt: Tarih */}
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Tarih
                      </Typography>
                      <Typography variant="body2">
                        {new Date(
                          order.orderCreatedDate ||
                            order.createdAt ||
                            new Date()
                        ).toLocaleDateString("tr-TR")}
                      </Typography>
                    </Box>

                    {/* Sağ alt: İşlem butonları */}
                    <Box
                      sx={{
                        display: "flex",
                        justifyContent: "flex-end",
                        alignItems: "flex-end",
                      }}
                    >
                      <IconButton
                        size="small"
                        color="primary"
                        sx={{ padding: "4px" }}
                      >
                        <Refresh fontSize="small" />
                      </IconButton>
                    </Box>
                  </Box>
                </Card>
              ))}
          </Box>
        ) : (
          <TableContainer component={Paper} variant="outlined">
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: "grey.100" }}>
                  <TableCell>
                    <strong>Sipariş No</strong>
                  </TableCell>
                  <TableCell>
                    <strong>Müşteri</strong>
                  </TableCell>
                  <TableCell>
                    <strong>Durum</strong>
                  </TableCell>
                  <TableCell align="right">
                    <strong>Toplam Tutar</strong>
                  </TableCell>
                  <TableCell align="right">
                    <strong>Ürün Sayısı</strong>
                  </TableCell>
                  <TableCell>
                    <strong>Oluşturulma</strong>
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {orders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      <Typography variant="body2" color="text.secondary" py={3}>
                        Henüz sipariş bulunmuyor
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  orders
                    .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
                    .map((order) => (
                      <TableRow key={order.id} hover>
                        <TableCell>{order.orderNo || `#${order.id}`}</TableCell>
                        <TableCell>
                          {order.customer || order.customerName ? (
                            <Box>
                              <Typography variant="body2">
                                {order.customerName || order.customer?.title}
                              </Typography>
                              {order.customer?.taxNo && (
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                >
                                  {order.customer.taxNo}
                                </Typography>
                              )}
                            </Box>
                          ) : (
                            <Typography variant="body2" color="text.secondary">
                              Müşteri #{order.customerId}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={getStatusLabel(order.status)}
                            color={getStatusColor(order.status)}
                            size="small"
                          />
                        </TableCell>
                        <TableCell align="right">
                          ₺
                          {(order.total || 0).toLocaleString("tr-TR", {
                            minimumFractionDigits: 2,
                          })}
                        </TableCell>
                        <TableCell align="right">
                          {order.items?.length || 0}
                        </TableCell>
                        <TableCell>
                          {new Date(order.orderCreatedDate || order.createdAt || new Date()).toLocaleString("tr-TR", {
                            day: "2-digit",
                            month: "2-digit",
                            year: "numeric",
                            hour: "2-digit",
                            minute: "2-digit",
                          })}
                        </TableCell>
                      </TableRow>
                    ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <TablePagination
          component="div"
          count={orders.length}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e) => {
            setRowsPerPage(parseInt(e.target.value, 10));
            setPage(0);
          }}
          labelRowsPerPage="Sayfa başına:"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}-${to} / ${count}`
          }
        />
      </CardContent>
    </Card>
  );
};

export default Orders;
