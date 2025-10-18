import React, { useState, useEffect } from "react";
import {
  Container,
  Box,
  Card,
  CardContent,
  Typography,
  Paper,
  Button,
  Alert,
  CircularProgress,
} from "@mui/material";
import {
  Inventory,
  Sync,
  TrendingUp,
  Warning,
  Refresh,
} from "@mui/icons-material";
import { stockAPI } from "../../services/api";

const Dashboard: React.FC = () => {
  const [stats, setStats] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadDashboard = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await stockAPI.getDashboardStats();
      setStats(data);
    } catch (err: any) {
      setError(err.message || "Dashboard yüklenemedi");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDashboard();
  }, []);

  const StatCard = ({ title, value, icon, color }: any) => (
    <Card>
      <CardContent>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Box sx={{ p: 1.5, borderRadius: 2, bgcolor: color + "20", color }}>
            {icon}
          </Box>
          <Box>
            <Typography variant="h4" fontWeight="bold">
              {value || 0}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {title}
            </Typography>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );

  if (loading) {
    return (
      <Container sx={{ display: "flex", justifyContent: "center", mt: 4 }}>
        <CircularProgress />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", mb: 3 }}>
        <Typography variant="h4" fontWeight="bold">
          Dashboard
        </Typography>
        <Button
          variant="outlined"
          startIcon={<Refresh />}
          onClick={loadDashboard}
          disabled={loading}
        >
          Yenile
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))",
          gap: 3,
          mb: 3,
        }}
      >
        <StatCard
          title="Toplam Ürün"
          value={stats?.totalProducts}
          icon={<Inventory />}
          color="#1976d2"
        />
        <StatCard
          title="Toplam Stok"
          value={stats?.totalStock}
          icon={<TrendingUp />}
          color="#2e7d32"
        />
        <StatCard
          title="Bekleyen Sync"
          value={stats?.pendingSync}
          icon={<Sync />}
          color="#ed6c02"
        />
        <StatCard
          title="Kritik Stok"
          value={stats?.criticalStock || 0}
          icon={<Warning />}
          color="#d32f2f"
        />
      </Box>

      <Paper sx={{ mt: 3, p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Hızlı İşlemler
        </Typography>
        <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
          <Button variant="contained" startIcon={<Sync />}>
            Senkronizasyon Başlat
          </Button>
          <Button variant="outlined" startIcon={<Inventory />}>
            Stok Raporu
          </Button>
          <Button variant="outlined" startIcon={<TrendingUp />}>
            Satış Analizi
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default Dashboard;
