import React, { useState } from "react";
import {
  Container,
  Box,
  Paper,
  Typography,
  Button,
  Card,
  CardContent,
  Alert,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import {
  Assessment,
  Inventory,
  Sync,
  Download,
  FileDownload,
} from "@mui/icons-material";
import { stockAPI } from "../../services/api";

const Reports: React.FC = () => {
  const [loading, setLoading] = useState<string>("");
  const [error, setError] = useState("");
  const [stockReport, setStockReport] = useState<any[]>([]);
  const [syncReport, setSyncReport] = useState<any[]>([]);

  const handleStockReport = async () => {
    try {
      setLoading("stock");
      setError("");
      const data: any = await stockAPI.getStockReport();
      setStockReport(data || []);
    } catch (err: any) {
      setError(err.message || "Stok raporu yüklenemedi");
    } finally {
      setLoading("");
    }
  };

  const handleSyncReport = async () => {
    try {
      setLoading("sync");
      setError("");
      const data: any = await stockAPI.getSyncReport();
      setSyncReport(data || []);
    } catch (err: any) {
      setError(err.message || "Sync raporu yüklenemedi");
    } finally {
      setLoading("");
    }
  };

  const downloadCSV = (data: any[], filename: string) => {
    if (!data.length) return;

    const headers = Object.keys(data[0]).join(",");
    const rows = data.map((row) => Object.values(row).join(","));
    const csv = [headers, ...rows].join("\n");

    const blob = new Blob([csv], { type: "text/csv" });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${filename}_${new Date().toISOString().split("T")[0]}.csv`;
    a.click();
  };

  const ReportCard = ({
    title,
    description,
    icon,
    onClick,
    reportType,
  }: any) => (
    <Card sx={{ height: "100%" }}>
      <CardContent>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
          <Box
            sx={{
              p: 1.5,
              borderRadius: 2,
              bgcolor: "primary.light",
              color: "white",
            }}
          >
            {icon}
          </Box>
          <Box>
            <Typography variant="h6" fontWeight="bold">
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {description}
            </Typography>
          </Box>
        </Box>
        <Button
          fullWidth
          variant="contained"
          startIcon={
            loading === reportType ? (
              <CircularProgress size={16} />
            ) : (
              <Download />
            )
          }
          onClick={onClick}
          disabled={loading !== ""}
        >
          Rapor Oluştur
        </Button>
      </CardContent>
    </Card>
  );

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Assessment sx={{ fontSize: 32, color: "primary.main" }} />
        <Typography variant="h4" fontWeight="bold">
          Raporlar
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Report Cards */}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
          gap: 3,
          mb: 4,
        }}
      >
        <ReportCard
          title="Stok Raporu"
          description="Mevcut stok durumu ve detayları"
          icon={<Inventory />}
          onClick={handleStockReport}
          reportType="stock"
        />
        <ReportCard
          title="Senkronizasyon Raporu"
          description="Senkronizasyon geçmişi ve istatistikleri"
          icon={<Sync />}
          onClick={handleSyncReport}
          reportType="sync"
        />
      </Box>

      {/* Stock Report Table */}
      {stockReport.length > 0 && (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              mb: 2,
            }}
          >
            <Typography variant="h6">Stok Raporu</Typography>
            <Button
              variant="outlined"
              size="small"
              startIcon={<FileDownload />}
              onClick={() => downloadCSV(stockReport, "stok_raporu")}
            >
              CSV İndir
            </Button>
          </Box>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {Object.keys(stockReport[0]).map((key) => (
                    <TableCell key={key}>
                      <strong>{key}</strong>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {stockReport.slice(0, 10).map((row, idx) => (
                  <TableRow key={idx} hover>
                    {Object.values(row).map((val: any, i) => (
                      <TableCell key={i}>{val}</TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {stockReport.length > 10 && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ mt: 2, display: "block" }}
            >
              İlk 10 kayıt gösteriliyor. Tümünü görmek için CSV indirin.
            </Typography>
          )}
        </Paper>
      )}

      {/* Sync Report Table */}
      {syncReport.length > 0 && (
        <Paper sx={{ p: 3 }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              mb: 2,
            }}
          >
            <Typography variant="h6">Senkronizasyon Raporu</Typography>
            <Button
              variant="outlined"
              size="small"
              startIcon={<FileDownload />}
              onClick={() => downloadCSV(syncReport, "sync_raporu")}
            >
              CSV İndir
            </Button>
          </Box>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {Object.keys(syncReport[0]).map((key) => (
                    <TableCell key={key}>
                      <strong>{key}</strong>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {syncReport.slice(0, 10).map((row, idx) => (
                  <TableRow key={idx} hover>
                    {Object.values(row).map((val: any, i) => (
                      <TableCell key={i}>{val}</TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {syncReport.length > 10 && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ mt: 2, display: "block" }}
            >
              İlk 10 kayıt gösteriliyor. Tümünü görmek için CSV indirin.
            </Typography>
          )}
        </Paper>
      )}
    </Container>
  );
};

export default Reports;
