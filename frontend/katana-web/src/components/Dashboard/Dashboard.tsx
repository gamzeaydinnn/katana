import {
  Inventory,
  Refresh,
  Sync,
  TrendingUp,
  Warning,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Paper,
  Typography,
  useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { stockAPI } from "../../services/api";

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const [stats, setStats] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

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

  const handleStartSync = () => {
    window.location.href = "/sync";
  };

  const handleStockReport = () => {
    window.location.href = "/reports";
  };

  const handleSalesAnalysis = () => {
    window.location.href = "/reports";
  };

  useEffect(() => {
    loadDashboard();
  }, []);

  const StatCard = ({ title, value, icon, color }: any) => (
    <Card
      sx={{
        backdropFilter: "blur(20px)",
        background:
          theme.palette.mode === "dark"
            ? "rgba(30,41,59,0.8)"
            : "rgba(255,255,255,0.8)",
        border:
          theme.palette.mode === "dark"
            ? "1px solid rgba(255,255,255,0.1)"
            : "1px solid rgba(0,0,0,0.05)",
        borderRadius: 3,
        boxShadow:
          theme.palette.mode === "dark"
            ? "0 8px 32px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
            : "0 8px 32px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
        transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
        "&:hover": {
          transform: "translateY(-4px)",
          boxShadow:
            theme.palette.mode === "dark"
              ? "0 20px 40px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.1)"
              : "0 20px 40px rgba(0,0,0,0.2), inset 0 1px 0 rgba(255,255,255,0.8)",
        },
      }}
    >
      <CardContent sx={{ p: 3 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Box
            sx={{
              p: 2,
              borderRadius: 3,
              background: `linear-gradient(135deg, ${color}20, ${color}10)`,
              color,
              boxShadow: `0 4px 12px ${color}30`,
              transition: "all 0.2s ease",
              "&:hover": {
                transform: "scale(1.05)",
                boxShadow: `0 6px 16px ${color}40`,
              },
            }}
          >
            {icon}
          </Box>
          <Box>
            <Typography
              variant="h4"
              sx={{
                fontWeight: 900,
                letterSpacing: "-0.02em",
                background: `linear-gradient(135deg, ${color}, ${color}dd)`,
                backgroundClip: "text",
                WebkitBackgroundClip: "text",
                WebkitTextFillColor: "transparent",
              }}
            >
              {value || 0}
            </Typography>
            <Typography
              variant="body2"
              sx={{
                color: theme.palette.text.secondary,
                fontWeight: 600,
                letterSpacing: "0.01em",
              }}
            >
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
        <CircularProgress
          sx={{
            color: theme.palette.primary.main,
            "& .MuiCircularProgress-circle": {
              strokeLinecap: "round",
            },
          }}
        />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", mb: 4 }}>
        <Typography
          variant="h4"
          sx={{
            fontWeight: 900,
            letterSpacing: "-0.02em",
            background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
            backgroundClip: "text",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
          }}
        >
          Dashboard
        </Typography>
        <Button
          variant="contained"
          startIcon={<Refresh />}
          onClick={loadDashboard}
          disabled={loading}
          sx={{
            borderRadius: 3,
            px: 3,
            py: 1.5,
            fontWeight: 700,
            letterSpacing: "0.01em",
            color: "#fff",
            background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
            boxShadow: `0 4px 12px ${theme.palette.primary.main}40`,
            transition: "all 0.2s ease",
            "&:hover": {
              transform: "translateY(-2px)",
              boxShadow: `0 8px 20px ${theme.palette.primary.main}60`,
            },
            "&:disabled": {
              opacity: 0.6,
            },
          }}
        >
          Yenile
        </Button>
      </Box>

      {error && (
        <Alert
          severity="error"
          sx={{
            mb: 3,
            borderRadius: 3,
            backdropFilter: "blur(10px)",
            background:
              theme.palette.mode === "dark"
                ? "rgba(239,68,68,0.1)"
                : "rgba(239,68,68,0.05)",
            border: "1px solid rgba(239,68,68,0.2)",
          }}
        >
          {error}
        </Alert>
      )}

      {success && (
        <Alert
          severity="success"
          sx={{
            mb: 3,
            borderRadius: 3,
            backdropFilter: "blur(10px)",
            background:
              theme.palette.mode === "dark"
                ? "rgba(34,197,94,0.1)"
                : "rgba(34,197,94,0.05)",
            border: "1px solid rgba(34,197,94,0.2)",
          }}
        >
          {success}
        </Alert>
      )}

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
          gap: 3,
          mb: 4,
        }}
      >
        <StatCard
          title="Toplam Ürün"
          value={stats?.totalProducts}
          icon={<Inventory sx={{ fontSize: 32 }} />}
          color={theme.palette.primary.main}
        />
        <StatCard
          title="Toplam Stok"
          value={stats?.totalStock}
          icon={<TrendingUp sx={{ fontSize: 32 }} />}
          color={theme.palette.success.main}
        />
        <StatCard
          title="Bekleyen Sync"
          value={stats?.pendingSync}
          icon={<Sync sx={{ fontSize: 32 }} />}
          color={theme.palette.warning.main}
        />
        <StatCard
          title="Kritik Stok"
          value={stats?.criticalStock || 0}
          icon={<Warning sx={{ fontSize: 32 }} />}
          color={theme.palette.error.main}
        />
      </Box>

      <Paper
        elevation={0}
        sx={{
          backdropFilter: "blur(20px)",
          background:
            theme.palette.mode === "dark"
              ? "rgba(30,41,59,0.8)"
              : "rgba(255,255,255,0.8)",
          border:
            theme.palette.mode === "dark"
              ? "1px solid rgba(255,255,255,0.1)"
              : "1px solid rgba(0,0,0,0.05)",
          borderRadius: 3,
          boxShadow:
            theme.palette.mode === "dark"
              ? "0 8px 32px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
              : "0 8px 32px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
          p: 4,
          transition: "all 0.3s ease",
        }}
      >
        <Typography
          variant="h6"
          gutterBottom
          sx={{
            fontWeight: 800,
            letterSpacing: "-0.01em",
            mb: 3,
          }}
        >
          Hızlı İşlemler
        </Typography>
        <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
          <Button
            variant="contained"
            startIcon={<Sync />}
            onClick={handleStartSync}
            disabled={loading}
            sx={{
              borderRadius: 3,
              px: 3,
              py: 1.5,
              fontWeight: 700,
              letterSpacing: "0.01em",
              background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
              boxShadow: `0 4px 12px ${theme.palette.primary.main}40`,
              transition: "all 0.2s ease",
              "&:hover": {
                transform: "translateY(-2px)",
                boxShadow: `0 8px 20px ${theme.palette.primary.main}60`,
              },
              "&:disabled": {
                opacity: 0.6,
              },
            }}
          >
            Senkronizasyon Başlat
          </Button>
          <Button
            variant="outlined"
            startIcon={<Inventory />}
            onClick={handleStockReport}
            sx={{
              borderRadius: 3,
              px: 3,
              py: 1.5,
              fontWeight: 700,
              letterSpacing: "0.01em",
              border: `2px solid ${theme.palette.primary.main}`,
              color: theme.palette.primary.main,
              transition: "all 0.2s ease",
              "&:hover": {
                transform: "translateY(-2px)",
                backgroundColor: theme.palette.primary.main,
                color: "white",
                boxShadow: `0 8px 20px ${theme.palette.primary.main}40`,
              },
            }}
          >
            Stok Raporu
          </Button>
          <Button
            variant="outlined"
            startIcon={<TrendingUp />}
            onClick={handleSalesAnalysis}
            sx={{
              borderRadius: 3,
              px: 3,
              py: 1.5,
              fontWeight: 700,
              letterSpacing: "0.01em",
              border: `2px solid ${theme.palette.primary.main}`,
              color: theme.palette.primary.main,
              transition: "all 0.2s ease",
              "&:hover": {
                transform: "translateY(-2px)",
                backgroundColor: theme.palette.primary.main,
                color: "white",
                boxShadow: `0 8px 20px ${theme.palette.primary.main}40`,
              },
            }}
          >
            Satış Analizi
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default Dashboard;
