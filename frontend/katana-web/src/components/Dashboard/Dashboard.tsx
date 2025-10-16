import React, { useState, useEffect } from "react";
import {
  Container,
  Box,
  Card,
  CardContent,
  Typography,
  LinearProgress,
  Paper,
  List,
  ListItem,
  ListItemText,
  Chip,
  Button,
  useTheme,
} from "@mui/material";
import {
  Inventory,
  Sync,
  TrendingUp,
  Warning,
  Refresh,
  Timeline,
} from "@mui/icons-material";
import { stockAPI, DashboardStats } from "../../services/api";

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdate, setLastUpdate] = useState<string>("");

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      // Backend API call - Katana.API Dashboard endpoint
      const dashboardStats = await stockAPI.getDashboardStats();
      setStats(dashboardStats);
      setLastUpdate(new Date().toLocaleTimeString("tr-TR"));
    } catch (error) {
      console.error("Dashboard data loading error:", error);
      // Fallback to mock data if backend unavailable
      const mockStats: DashboardStats = {
        totalProducts: 1250,
        totalStock: 45780,
        pendingSync: 23,
        lastSyncDate: new Date().toISOString(),
      };
      setStats(mockStats);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDashboardData();
    const interval = setInterval(loadDashboardData, 300000); // 5 minutes
    return () => clearInterval(interval);
  }, []);

  const StatCard: React.FC<{
    title: string;
    value: string | number;
    icon: React.ReactNode;
    color: string;
    subtitle?: string;
    trend?: number;
  }> = ({ title, value, icon, color, subtitle, trend }) => (
    <Card sx={{ height: "100%", position: "relative", overflow: "visible" }}>
      <CardContent>
        <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
          <Box
            sx={{
              p: 1,
              borderRadius: 2,
              backgroundColor: color + "20",
              color: color,
              mr: 2,
            }}
          >
            {icon}
          </Box>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h4" component="div" fontWeight="bold">
              {value.toLocaleString("tr-TR")}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {title}
            </Typography>
            {subtitle && (
              <Typography variant="caption" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Box>
          {trend !== undefined && (
            <Box sx={{ textAlign: "right" }}>
              <Typography
                variant="body2"
                color={trend >= 0 ? "success.main" : "error.main"}
                sx={{ display: "flex", alignItems: "center" }}
              >
                <Timeline sx={{ fontSize: 16, mr: 0.5 }} />
                {trend >= 0 ? "+" : ""}
                {trend}%
              </Typography>
            </Box>
          )}
        </Box>
      </CardContent>
    </Card>
  );

  const recentActivities = [
    {
      id: 1,
      action: "Stok güncellendi",
      product: "Ürün A",
      time: "10 dk önce",
      type: "update",
    },
    {
      id: 2,
      action: "Senkronizasyon tamamlandı",
      product: "156 ürün",
      time: "25 dk önce",
      type: "sync",
    },
    {
      id: 3,
      action: "Düşük stok uyarısı",
      product: "Ürün B",
      time: "1 sa önce",
      type: "warning",
    },
    {
      id: 4,
      action: "Yeni ürün eklendi",
      product: "Ürün C",
      time: "2 sa önce",
      type: "add",
    },
  ];

  const getActivityColor = (type: string) => {
    switch (type) {
      case "update":
        return "info";
      case "sync":
        return "success";
      case "warning":
        return "warning";
      case "add":
        return "primary";
      default:
        return "default";
    }
  };

  if (loading && !stats) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <LinearProgress />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          mb: 3,
        }}
      >
        <Box>
          <Typography
            variant="h4"
            component="h1"
            gutterBottom
            fontWeight="bold"
          >
            Dashboard
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Son güncellenme: {lastUpdate}
          </Typography>
        </Box>
        <Button
          variant="outlined"
          startIcon={<Refresh />}
          onClick={loadDashboardData}
          disabled={loading}
        >
          Yenile
        </Button>
      </Box>

      {/* Stats Cards */}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
          gap: 3,
          mb: 4,
        }}
      >
        <StatCard
          title="Toplam Ürün"
          value={stats?.totalProducts || 0}
          icon={<Inventory />}
          color={theme.palette.primary.main}
          trend={5.2}
        />
        <StatCard
          title="Toplam Stok"
          value={stats?.totalStock || 0}
          icon={<TrendingUp />}
          color={theme.palette.success.main}
          subtitle="Adet"
          trend={2.1}
        />
        <StatCard
          title="Bekleyen Senkronizasyon"
          value={stats?.pendingSync || 0}
          icon={<Sync />}
          color={theme.palette.warning.main}
          subtitle="Kayıt"
          trend={-15.3}
        />
        <StatCard
          title="Kritik Stok"
          value={12}
          icon={<Warning />}
          color={theme.palette.error.main}
          subtitle="Ürün"
          trend={-8.7}
        />
      </Box>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", md: "2fr 1fr" },
          gap: 3,
        }}
      >
        {/* Recent Activities */}
        <Paper sx={{ p: 3, height: 400 }}>
          <Typography variant="h6" gutterBottom fontWeight="bold">
            Son Aktiviteler
          </Typography>
          <List>
            {recentActivities.map((activity) => (
              <ListItem key={activity.id} divider>
                <ListItemText
                  primary={
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                      <Typography variant="body1">{activity.action}</Typography>
                      <Chip
                        label={activity.product}
                        size="small"
                        color={getActivityColor(activity.type) as any}
                        variant="outlined"
                      />
                    </Box>
                  }
                  secondary={activity.time}
                />
              </ListItem>
            ))}
          </List>
        </Paper>

        {/* Quick Actions */}
        <Paper sx={{ p: 3, height: 400 }}>
          <Typography variant="h6" gutterBottom fontWeight="bold">
            Hızlı İşlemler
          </Typography>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
            <Button variant="contained" fullWidth startIcon={<Sync />}>
              Senkronizasyon Başlat
            </Button>
            <Button variant="outlined" fullWidth startIcon={<Inventory />}>
              Stok Raporu Al
            </Button>
            <Button variant="outlined" fullWidth startIcon={<Warning />}>
              Kritik Stokları Görüntüle
            </Button>
            <Button variant="outlined" fullWidth startIcon={<TrendingUp />}>
              Satış Analizi
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
};

export default Dashboard;
