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
  TablePagination,
  Paper,
  Chip,
  IconButton,
  Alert,
  CircularProgress,
  Divider,
} from "@mui/material";
import {
  Inventory,
  TrendingUp,
  CheckCircle,
  Error as ErrorIcon,
  Refresh,
} from "@mui/icons-material";
import { stockAPI, AdminProduct, AdminSyncLog } from "../../services/api";

interface Statistics {
  totalProducts: number;
  totalStock: number;
  successfulSyncs: number;
  failedSyncs: number;
}

const AdminPanel: React.FC = () => {
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [syncLogs, setSyncLogs] = useState<AdminSyncLog[]>([]);
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalSyncLogs, setTotalSyncLogs] = useState(0);
  const [katanaHealth, setKatanaHealth] = useState<boolean>(false);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      console.log("Loading admin data...");

      const [stats, logsData, productsData, health] = await Promise.all([
        stockAPI.getAdminStatistics().catch((err) => {
          console.error("Statistics error:", err);
          return {
            totalProducts: 0,
            totalStock: 0,
            successfulSyncs: 0,
            failedSyncs: 0,
          };
        }),
        stockAPI.getAdminSyncLogs(page + 1, rowsPerPage).catch((err) => {
          console.error("Sync logs error:", err);
          return { logs: [], total: 0 };
        }),
        stockAPI.getAdminProducts(1, 10).catch((err) => {
          console.error("Products error:", err);
          return { products: [], total: 0 };
        }),
        stockAPI.getKatanaHealth().catch((err) => {
          console.error("Health check error:", err);
          return { isHealthy: false };
        }),
      ]);

      console.log("Loaded data:", { stats, logsData, productsData, health });

      setStatistics(stats);
      setSyncLogs(logsData.logs);
      setTotalSyncLogs(logsData.total);
      setProducts(productsData.products);
      setKatanaHealth(health.isHealthy);
    } catch (err: any) {
      console.error("Failed to load admin data:", err);
      setError(
        `Hata: ${err.message || "Admin verileri yüklenirken hata oluştu"}`
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, rowsPerPage]);

  const StatCard: React.FC<{
    title: string;
    value: string | number;
    icon: React.ReactNode;
    color: string;
  }> = ({ title, value, icon, color }) => (
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="textSecondary" gutterBottom variant="body2">
              {title}
            </Typography>
            <Typography variant="h4" fontWeight="bold">
              {value}
            </Typography>
          </Box>
          <Box sx={{ color, fontSize: 48 }}>{icon}</Box>
        </Box>
      </CardContent>
    </Card>
  );

  if (loading && !statistics) {
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
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        mb={3}
      >
        <Typography variant="h4" fontWeight="bold">
          Admin Paneli
        </Typography>
        <Box display="flex" gap={2} alignItems="center">
          {katanaHealth !== null && (
            <Chip
              icon={katanaHealth ? <CheckCircle /> : <ErrorIcon />}
              label={
                katanaHealth ? "Katana API Bağlı" : "Katana API Bağlı Değil"
              }
              color={katanaHealth ? "success" : "error"}
            />
          )}
          <IconButton onClick={loadData} color="primary">
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Statistics Cards */}
      {statistics && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "1fr 1fr",
              md: "1fr 1fr 1fr 1fr",
            },
            gap: 3,
            mb: 3,
          }}
        >
          <StatCard
            title="Toplam Ürün"
            value={statistics.totalProducts}
            icon={<Inventory />}
            color="#1976d2"
          />
          <StatCard
            title="Toplam Stok"
            value={statistics.totalStock.toLocaleString("tr-TR")}
            icon={<TrendingUp />}
            color="#2e7d32"
          />
          <StatCard
            title="Başarılı Sync"
            value={statistics.successfulSyncs}
            icon={<CheckCircle />}
            color="#388e3c"
          />
          <StatCard
            title="Başarısız Sync"
            value={statistics.failedSyncs}
            icon={<ErrorIcon />}
            color="#d32f2f"
          />
        </Box>
      )}

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" },
          gap: 3,
        }}
      >
        {/* Recent Products */}
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>
            Son Eklenen Ürünler
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>SKU</TableCell>
                  <TableCell>Ürün Adı</TableCell>
                  <TableCell align="right">Stok</TableCell>
                  <TableCell>Durum</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {products.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>{product.sku}</TableCell>
                    <TableCell>{product.name}</TableCell>
                    <TableCell align="right">{product.stock}</TableCell>
                    <TableCell>
                      <Chip
                        label={product.isActive ? "Aktif" : "Pasif"}
                        color={product.isActive ? "success" : "default"}
                        size="small"
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>

        {/* Sync Logs */}
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>
            Senkronizasyon Logları
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Entegrasyon</TableCell>
                  <TableCell>Tarih</TableCell>
                  <TableCell>Durum</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {syncLogs.map((log) => (
                  <TableRow key={log.id}>
                    <TableCell>{log.integrationName}</TableCell>
                    <TableCell>
                      {new Date(log.createdAt).toLocaleString("tr-TR")}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={log.isSuccess ? "Başarılı" : "Başarısız"}
                        color={log.isSuccess ? "success" : "error"}
                        size="small"
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={totalSyncLogs}
            page={page}
            onPageChange={(_, newPage) => setPage(newPage)}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={(e) => {
              setRowsPerPage(parseInt(e.target.value, 10));
              setPage(0);
            }}
            rowsPerPageOptions={[5, 10, 25]}
            labelRowsPerPage="Sayfa başına:"
          />
        </Paper>
      </Box>
    </Box>
  );
};

export default AdminPanel;
