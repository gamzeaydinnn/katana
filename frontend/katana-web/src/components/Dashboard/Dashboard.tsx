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
    useMediaQuery,
    useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { stockAPI } from "../../services/api";

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
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
      console.log("[Dashboard] ✅ Stats loaded successfully:", data);
    } catch (err: any) {
      const errorMsg = err?.response?.status === 404 
        ? "Dashboard endpoint'i bulunamadı. Backend'i kontrol edin."
        : err?.message || "Kontrol paneli yüklenemedi";
      setError(errorMsg);
      console.error("[Dashboard] ❌ Error loading stats:", {
        message: err?.message,
        status: err?.response?.status,
        url: err?.config?.url
      });
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

  const formatNumber = (
    value?: number,
    options: Intl.NumberFormatOptions = { maximumFractionDigits: 0 }
  ) => {
    if (value === undefined || value === null || Number.isNaN(Number(value))) {
      return "0";
    }
    return new Intl.NumberFormat("tr-TR", options).format(Number(value));
  };

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
      <CardContent sx={{ p: { xs: 2, md: 3 } }}>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            gap: 1.5,
          }}
        >
          <Box
            sx={{
              p: { xs: 1.2, md: 2 },
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
            {React.cloneElement(icon, {
              sx: { fontSize: isMobile ? 26 : 32 },
            })}
          </Box>
          <Box>
            <Typography
              variant="h4"
              sx={{
                fontSize: { xs: "1.5rem", md: "2.25rem" },
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
                opacity: 0.8,
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
    <Container
      maxWidth="lg"
      sx={{
        mt: { xs: 7.5, md: 4 },
        mb: { xs: 2.5, md: 4 },
        px: { xs: 1.5, sm: 2, md: 0 },
      }}
    >
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", sm: "center" },
          flexDirection: { xs: "column", sm: "row" },
          gap: { xs: 2, sm: 0 },
          mb: { xs: 3, md: 4 },
        }}
      >
        <Box>
          <Typography
            variant="h4"
            sx={{
              fontSize: { xs: "1.6rem", md: "2rem" },
              fontWeight: 900,
              letterSpacing: "-0.02em",
              background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
              backgroundClip: "text",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
            }}
          >
            Kontrol Paneli
          </Typography>
          <Typography
            variant="body2"
            sx={{ color: theme.palette.text.secondary, mt: 0.5 }}
          >
            Güncel stok sağlığı ve kritik uyarılar
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<Refresh />}
          onClick={loadDashboard}
          disabled={loading}
          sx={{
            width: { xs: "100%", sm: "auto" },
            mt: { xs: 1, sm: 0 },
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

      {stats && (
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            gap: 1.5,
            mb: 3,
          }}
        >
          {[
            stats?.outOfStock > 0 && (
              <Alert
                severity="error"
                key="out"
                sx={{ borderRadius: 3, fontWeight: 600 }}
              >
                Kritik Uyarı: {formatNumber(stats?.outOfStock)} ürün stokta yok!
                Lütfen yöneticinizle kontrol edin.
              </Alert>
            ),
            stats?.lowStock > 0 && (
              <Alert
                severity="warning"
                key="low"
                sx={{ borderRadius: 3, fontWeight: 600 }}
              >
                Düşük Stok: {formatNumber(stats?.lowStock)} ürün kritik
                seviyede.
              </Alert>
            ),
            stats?.pendingSync > 0 && (
              <Alert
                severity="info"
                key="sync"
                sx={{ borderRadius: 3, fontWeight: 600 }}
              >
                Bekleyen Senkronizasyon: {formatNumber(stats?.pendingSync)}{" "}
                kayıt sırada.
              </Alert>
            ),
          ].filter(Boolean)}
        </Box>
      )}

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "repeat(auto-fit, minmax(180px, 1fr))",
            sm: "repeat(auto-fit, minmax(220px, 1fr))",
            md: "repeat(auto-fit, minmax(260px, 1fr))",
          },
          gap: { xs: 2, md: 3 },
          mb: 4,
        }}
      >
        {[
          {
            key: "totalProducts",
            title: "Toplam Ürün",
            value: formatNumber(stats?.totalProducts),
            icon: <Inventory />,
            color: theme.palette.primary.main,
          },
          {
            key: "activeProducts",
            title: "Aktif Ürün",
            value: formatNumber(stats?.activeProducts ?? stats?.totalProducts),
            icon: <TrendingUp />,
            color: theme.palette.success.main,
          },
          {
            key: "lowStock",
            title: "Düşük Stok",
            value: formatNumber(stats?.lowStock ?? stats?.criticalStock ?? 0),
            icon: <Warning />,
            color: theme.palette.warning.main,
          },
          {
            key: "out",
            title: "Stokta Yok",
            value: formatNumber(
              stats?.outOfStock ?? stats?.outOfStockCount ?? 0
            ),
            icon: <Warning />,
            color: theme.palette.error.main,
          },
          {
            key: "value",
            title: "Toplam Değer",
            value: `₺ ${formatNumber(
              stats?.totalValue ?? stats?.totalStockValue ?? 0,
              { maximumFractionDigits: 0 }
            )}`,
            icon: <TrendingUp />,
            color: theme.palette.info.main,
          },
        ].map(({ key, ...rest }) => (
          <StatCard key={key} {...rest} />
        ))}
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
          p: { xs: 2.5, md: 4 },
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
        <Box
          sx={{
            display: "flex",
            gap: 2,
            flexWrap: "wrap",
            flexDirection: { xs: "column", sm: "row" },
          }}
        >
          <Button
            variant="contained"
            startIcon={<Sync />}
            onClick={handleStartSync}
            disabled={loading}
            sx={{
              width: { xs: "100%", sm: "auto" },
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
              width: { xs: "100%", sm: "auto" },
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
