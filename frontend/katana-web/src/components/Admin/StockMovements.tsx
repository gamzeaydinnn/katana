import React, { useState, useEffect, useCallback } from "react";
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
  CircularProgress,
  Alert,
  IconButton,
  Stack,
  TextField,
  InputAdornment,
  useMediaQuery,
  Tooltip,
  Pagination,
} from "@mui/material";
import {
  Refresh,
  Search,
  TrendingUp,
  TrendingDown,
  SwapHoriz,
} from "@mui/icons-material";
import api from "../../services/api";

interface StockMovement {
  id: number;
  productId: number;
  productSku: string;
  productName?: string;
  changeQuantity: number;
  movementType: string; // "In" | "Out" | "Adjustment"
  sourceDocument: string;
  timestamp: string;
  warehouseCode: string;
  isSynced: boolean;
  syncedAt?: string;
}

const StockMovements: React.FC = () => {
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [filteredMovements, setFilteredMovements] = useState<StockMovement[]>(
    []
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const isMobile = useMediaQuery("(max-width:900px)");

  const fetchMovements = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // Local StockMovements tablosundan son 30 günlük hareketleri çek
      const endDate = new Date().toISOString();
      const startDate = new Date(
        Date.now() - 30 * 24 * 60 * 60 * 1000
      ).toISOString();
      const response = await api.get<any>(
        `/Stock/local/movements/range?startDate=${startDate}&endDate=${endDate}`
      );
      const data = response.data?.data || response.data || [];
      setMovements(Array.isArray(data) ? data : []);
      setFilteredMovements(Array.isArray(data) ? data : []);
    } catch (err: any) {
      setError(err.response?.data?.error || "Stok hareketleri yüklenemedi");
      console.error("Stok hareketleri yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchMovements();
  }, [fetchMovements]);

  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredMovements(movements);
    } else {
      const filtered = movements.filter(
        (m) =>
          m.productSku?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          m.sourceDocument?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          m.warehouseCode?.toLowerCase().includes(searchTerm.toLowerCase())
      );
      setFilteredMovements(filtered);
    }
    setPage(1);
  }, [searchTerm, movements]);

  const getMovementTypeIcon = (type: string) => {
    switch (type?.toLowerCase()) {
      case "in":
        return <TrendingUp color="success" />;
      case "out":
        return <TrendingDown color="error" />;
      default:
        return <SwapHoriz color="info" />;
    }
  };

  const getMovementTypeLabel = (type: string) => {
    switch (type?.toLowerCase()) {
      case "in":
        return { label: "Giriş", color: "success" as const };
      case "out":
        return { label: "Çıkış", color: "error" as const };
      default:
        return { label: "Düzeltme", color: "info" as const };
    }
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return "-";
    const date = new Date(dateStr);
    return date.toLocaleString("tr-TR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const totalPages = Math.ceil(filteredMovements.length / pageSize);
  const paginatedMovements = filteredMovements.slice(
    (page - 1) * pageSize,
    page * pageSize
  );

  // Özet istatistikler
  const inCount = movements.filter(
    (m) => m.movementType?.toLowerCase() === "in"
  ).length;
  const outCount = movements.filter(
    (m) => m.movementType?.toLowerCase() === "out"
  ).length;
  const adjustmentCount = movements.filter(
    (m) => m.movementType?.toLowerCase() === "adjustment"
  ).length;

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
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            mb={2}
          >
            <Typography variant="h6" fontWeight="bold">
              Stok Hareketleri
            </Typography>
            <Tooltip title="Yenile">
              <IconButton onClick={fetchMovements} color="primary">
                <Refresh />
              </IconButton>
            </Tooltip>
          </Stack>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <TextField
            fullWidth
            placeholder="SKU, belge veya depo kodu ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Search />
                </InputAdornment>
              ),
            }}
            sx={{ mb: 2 }}
          />

          <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
            <Chip label={`Toplam: ${movements.length}`} color="primary" />
            <Chip
              icon={<TrendingUp />}
              label={`Giriş: ${inCount}`}
              color="success"
              variant="outlined"
            />
            <Chip
              icon={<TrendingDown />}
              label={`Çıkış: ${outCount}`}
              color="error"
              variant="outlined"
            />
            <Chip
              icon={<SwapHoriz />}
              label={`Düzeltme: ${adjustmentCount}`}
              color="info"
              variant="outlined"
            />
          </Stack>
        </CardContent>
      </Card>

      {isMobile ? (
        <Stack spacing={1.5}>
          {paginatedMovements.length === 0 && (
            <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
              {searchTerm
                ? "Arama sonucu bulunamadı"
                : "Stok hareketi bulunamadı"}
            </Typography>
          )}
          {paginatedMovements.map((movement) => {
            const typeInfo = getMovementTypeLabel(movement.movementType);
            return (
              <Paper key={movement.id} sx={{ p: 1.5, borderRadius: 2 }}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="flex-start"
                >
                  <Box>
                    <Typography variant="subtitle1" fontWeight={600}>
                      {movement.productSku}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {movement.sourceDocument}
                    </Typography>
                  </Box>
                  <Stack direction="row" alignItems="center" spacing={1}>
                    {getMovementTypeIcon(movement.movementType)}
                    <Chip
                      label={typeInfo.label}
                      color={typeInfo.color}
                      size="small"
                    />
                  </Stack>
                </Stack>
                <Stack direction="row" justifyContent="space-between" mt={1}>
                  <Typography variant="body2">
                    Miktar:{" "}
                    <strong>
                      {movement.changeQuantity > 0 ? "+" : ""}
                      {movement.changeQuantity}
                    </strong>
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatDate(movement.timestamp)}
                  </Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between" mt={0.5}>
                  <Typography variant="caption" color="text.secondary">
                    Depo: {movement.warehouseCode}
                  </Typography>
                  <Chip
                    label={movement.isSynced ? "Senkron" : "Bekliyor"}
                    color={movement.isSynced ? "success" : "warning"}
                    size="small"
                    variant="outlined"
                  />
                </Stack>
              </Paper>
            );
          })}
        </Stack>
      ) : (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Tarih</TableCell>
                <TableCell>SKU</TableCell>
                <TableCell>Tip</TableCell>
                <TableCell align="right">Miktar</TableCell>
                <TableCell>Belge</TableCell>
                <TableCell>Depo</TableCell>
                <TableCell>Durum</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {paginatedMovements.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    {searchTerm
                      ? "Arama sonucu bulunamadı"
                      : "Stok hareketi bulunamadı"}
                  </TableCell>
                </TableRow>
              )}
              {paginatedMovements.map((movement) => {
                const typeInfo = getMovementTypeLabel(movement.movementType);
                return (
                  <TableRow key={movement.id} hover>
                    <TableCell>{formatDate(movement.timestamp)}</TableCell>
                    <TableCell>
                      <Typography variant="body2" fontWeight={600}>
                        {movement.productSku}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" alignItems="center" spacing={0.5}>
                        {getMovementTypeIcon(movement.movementType)}
                        <Chip
                          label={typeInfo.label}
                          color={typeInfo.color}
                          size="small"
                        />
                      </Stack>
                    </TableCell>
                    <TableCell align="right">
                      <Typography
                        fontWeight={600}
                        color={
                          movement.changeQuantity > 0
                            ? "success.main"
                            : movement.changeQuantity < 0
                            ? "error.main"
                            : "text.primary"
                        }
                      >
                        {movement.changeQuantity > 0 ? "+" : ""}
                        {movement.changeQuantity}
                      </Typography>
                    </TableCell>
                    <TableCell>{movement.sourceDocument}</TableCell>
                    <TableCell>{movement.warehouseCode}</TableCell>
                    <TableCell>
                      <Chip
                        label={movement.isSynced ? "Senkron" : "Bekliyor"}
                        color={movement.isSynced ? "success" : "warning"}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {totalPages > 1 && (
        <Box display="flex" justifyContent="center" mt={2}>
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_, newPage) => setPage(newPage)}
            color="primary"
          />
        </Box>
      )}
    </Box>
  );
};

export default StockMovements;
