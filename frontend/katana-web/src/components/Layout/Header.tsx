import {
  AccountCircle,
  CheckCircle,
  Error,
  Logout,
  Menu as MenuIcon,
  Notifications as NotificationsIcon,
  Settings,
  Sync,
} from "@mui/icons-material";
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api, { stockAPI } from "../../services/api";
import {
  offPendingApproved,
  offPendingCreated,
  onPendingApproved,
  onPendingCreated,
  startConnection,
} from "../../services/signalr";

interface HeaderProps {
  onMenuClick: () => void;
  sidebarOpen: boolean;
  currentBranchName?: string | null;
  onOpenBranchSelector?: () => void;
  mode?: "light" | "dark";
  onToggleMode?: () => void;
}

type NotificationStatus = "pending" | "approved" | "rejected";

interface NotificationItem {
  id: string;
  title: string;
  description?: string;
  status: NotificationStatus;
  createdAt: string;
  referenceId?: number;
}

const MAX_NOTIFICATIONS = 20;

const notificationStatusMeta: Record<
  NotificationStatus,
  { label: string; color: "warning" | "success" | "error" }
> = {
  pending: { label: "Bekliyor", color: "warning" },
  approved: { label: "Onaylandı", color: "success" },
  rejected: { label: "Reddedildi", color: "error" },
};

const formatRelativeTime = (value?: string) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const diff = Date.now() - date.getTime();
  if (diff < 60_000) return "Az önce";
  if (diff < 3_600_000) return `${Math.floor(diff / 60_000)} dk önce`;
  if (diff < 86_400_000) return `${Math.floor(diff / 3_600_000)} sa önce`;

  return date.toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const Header: React.FC<HeaderProps> = ({
  onMenuClick,
  sidebarOpen,
  currentBranchName,
  onOpenBranchSelector,
  mode = "light",
  onToggleMode,
}) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [notificationAnchor, setNotificationAnchor] =
    React.useState<null | HTMLElement>(null);
  const [backendStatus, setBackendStatus] = useState<
    "connected" | "disconnected" | "checking"
  >("checking");
  const [isChecking, setIsChecking] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [signalrStatus, setSignalrStatus] = useState<
    "connecting" | "connected" | "error"
  >("connecting");
  const [signalrError, setSignalrError] = useState<string | null>(null);

  
  useEffect(() => {
    const checkBackendHealth = async () => {
      setIsChecking(true);
      setBackendStatus("checking");
      try {
        await stockAPI.getHealthStatus();
        setBackendStatus("connected");
      } catch (error) {
        setBackendStatus("disconnected");
      } finally {
        setIsChecking(false);
      }
    };

    checkBackendHealth();
    const interval = setInterval(checkBackendHealth, 60000); 
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    let isMounted = true;

    
    const loadInitialNotifications = async () => {
      try {
        const response = await api.get<{ pendingAdjustments: any[] }>(
          "/adminpanel/pending-adjustments"
        );
        if (isMounted && response.data?.pendingAdjustments) {
          const pending = response.data.pendingAdjustments.slice(
            0,
            MAX_NOTIFICATIONS
          );
          const notifications: NotificationItem[] = pending.map(
            (item: any) => ({
              id: `pending-${item.id}`,
              referenceId: item.id,
              title: item.sku
                ? `Bekleyen: ${item.sku}`
                : `Bekleyen: #${item.id}`,
              description: item.quantity ? `Adet: ${item.quantity}` : undefined,
              status: "pending" as const,
              createdAt: item.requestedAt || new Date().toISOString(),
            })
          );
          setNotifications(notifications);
          console.log(
            `[Header] ✅ Loaded ${notifications.length} initial notifications`
          );
        }
      } catch (err: any) {
        console.warn("[Header] ⚠ Notification loading warning:", {
          message: err?.message,
          status: err?.response?.status,
        });
        
        setNotifications([]);
      }
    };

    loadInitialNotifications();

    setSignalrStatus("connecting");
    startConnection()
      .then(() => {
        if (isMounted) {
          setSignalrStatus("connected");
          setSignalrError(null);
        }
      })
      .catch((err) => {
        console.warn("SignalR connection failed", err);
        if (isMounted) {
          setSignalrStatus("error");
          setSignalrError(err?.message || "SignalR bağlantısı kurulamadı");
        }
      });

    const createdHandler = (payload: any) => {
      const pending = payload?.pending ?? payload;
      if (!pending?.id) return;

      const idNumber =
        typeof pending.id === "number"
          ? pending.id
          : Number.parseInt(String(pending.id), 10);
      const createdAt =
        typeof pending.requestedAt === "string"
          ? pending.requestedAt
          : new Date().toISOString();

      const title =
        pending.sku && typeof pending.sku === "string"
          ? `Yeni bekleyen: ${pending.sku}`
          : `Yeni bekleyen: #${pending.id}`;

      const descriptionParts: string[] = [];
      if (pending.quantity !== undefined && pending.quantity !== null) {
        descriptionParts.push(`Adet: ${pending.quantity}`);
      }
      if (pending.requestedBy) {
        descriptionParts.push(`Talep eden: ${pending.requestedBy}`);
      }

      setNotifications((prev) => {
        const next: NotificationItem = {
          id: `pending-${pending.id}`,
          referenceId: Number.isNaN(idNumber) ? undefined : idNumber,
          title,
          description:
            descriptionParts.length > 0
              ? descriptionParts.join(" • ")
              : undefined,
          status: "pending" as const,
          createdAt,
        };

        const filtered = prev.filter((item) => item.id !== next.id);
        return [next, ...filtered].slice(0, MAX_NOTIFICATIONS);
      });
    };

    const approvedHandler = (payload: any) => {
      const pendingId = payload?.pendingId ?? payload?.id ?? payload;
      if (!pendingId) return;

      const idNumber =
        typeof pendingId === "number"
          ? pendingId
          : Number.parseInt(String(pendingId), 10);
      if (Number.isNaN(idNumber)) return;

      const approvedAt =
        typeof payload?.approvedAt === "string"
          ? payload.approvedAt
          : new Date().toISOString();

      const approvedBy =
        typeof payload?.approvedBy === "string"
          ? payload.approvedBy
          : undefined;

      setNotifications((prev) => {
        let updatedExisting = false;
        const updated = prev.map((item) => {
          if (item.referenceId === idNumber) {
            updatedExisting = true;
            return {
              ...item,
              status: "approved" as const,
              title: `Onaylandı: #${idNumber}`,
              description: approvedBy
                ? `Onaylayan: ${approvedBy}`
                : item.description,
              createdAt: approvedAt,
            };
          }
          return item;
        });

        if (updatedExisting) {
          return updated;
        }

        const next: NotificationItem = {
          id: `approved-${idNumber}-${Date.now()}`,
          referenceId: idNumber,
          title: `Onaylandı: #${idNumber}`,
          description: approvedBy ? `Onaylayan: ${approvedBy}` : undefined,
          status: "approved" as const,
          createdAt: approvedAt,
        };

        return [next, ...prev].slice(0, MAX_NOTIFICATIONS);
      });
    };

    onPendingCreated(createdHandler);
    onPendingApproved(approvedHandler);

    return () => {
      isMounted = false;
      offPendingCreated(createdHandler);
      offPendingApproved(approvedHandler);
    };
  }, []);

  const navigate = useNavigate();

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleNotificationOpen = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationAnchor(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleNotificationClose = () => {
    setNotificationAnchor(null);
  };

  const handleProfileClick = () => {
    handleMenuClose();
    navigate("/profile");
  };

  const handleSettingsClick = () => {
    handleMenuClose();
    navigate("/settings");
  };

  const handleLogout = () => {
    handleMenuClose();
    localStorage.removeItem("authToken");
    window.location.href = "/login";
  };

  const pendingCount = notifications.reduce(
    (count, item) => (item.status === "pending" ? count + 1 : count),
    0
  );

  const notificationTooltip =
    signalrStatus === "connected"
      ? "Bildirimler (canlı)"
      : signalrStatus === "error"
      ? `Bildirimler (SignalR hatası${signalrError ? `: ${signalrError}` : ""})`
      : "Bildirimler (bağlanıyor...)";
  const branchLabel =
    currentBranchName && String(currentBranchName).trim() !== ""
      ? String(currentBranchName)
      : "Şube Seç";
  const branchChipLabel = isMobile ? "Ş" : branchLabel;

  return (
    <AppBar
      position="fixed"
      sx={{
        zIndex: (theme) => theme.zIndex.drawer + 1,
        backdropFilter: "blur(20px)",
        background: "rgba(79, 134, 255, 0.15)",
        borderBottom: "none",
        boxShadow: "0 4px 20px rgba(43, 110, 246, 0.08)",
        transition: "all 0.3s ease",
      }}
    >
      <Toolbar
        sx={{
          minHeight: isMobile ? 72 : 64,
          px: { xs: 1.5, sm: 2, md: 3 },
          gap: isMobile ? 0.75 : 0,
          flexWrap: "nowrap",
          alignItems: "center",
        }}
      >
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{
            mr: isMobile ? 1 : 2,
            color: "#1e40af",
            transition: "transform 0.2s ease",
            "&:hover": {
              transform: "scale(1.08)",
              backgroundColor: "rgba(79, 134, 255, 0.1)",
            },
          }}
        >
          <MenuIcon sx={{ fontSize: isMobile ? 22 : 26 }} />
        </IconButton>

        <Box
          sx={{
            flexGrow: 0,
            flexShrink: 1,
            display: "flex",
            alignItems: "center",
            minWidth: 0,
            mr: { xs: 0.5, md: 1.5 },
            gap: { xs: 0.25, md: 0.75 },
          }}
        >
          <Box
            component="img"
            src="/logoo.png"
            alt="BeforMet Metal Logo"
            sx={{
              display: { xs: "none", md: "block" },
              height: 48,
              width: "auto",
              objectFit: "contain",
              filter: "drop-shadow(0 2px 8px rgba(0,0,0,0.15))",
            }}
          />
          <Typography
            variant="h6"
            component="div"
            sx={{
              fontFamily: '"Poppins", "Inter", sans-serif',
              fontWeight: 600,
              letterSpacing: "-0.5px",
              color: "#1e40af",
              textShadow: "0 1px 2px rgba(0,0,0,0.05)",
              fontSize: isMobile ? "0.95rem" : "1.15rem",
              display: "flex",
              flexDirection: "column",
              lineHeight: 1.05,
            }}
          >
            <span>Beformet</span>
            <span>Metal</span>
          </Typography>
        </Box>

        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            gap: { xs: 0.3, md: 1.5 },
            rowGap: { xs: 0.75, md: 0 },
            flexWrap: { xs: "wrap", md: "nowrap" },
            justifyContent: { xs: "flex-start", md: "flex-end" },
            width: "auto",
            flexGrow: 1,
            ml: "auto",
            mt: { xs: 0.5, md: 0 },
          }}
        >
          {}
          <Tooltip title={mode === "dark" ? "Açık tema" : "Koyu tema"}>
            <IconButton
              size="small"
              onClick={onToggleMode}
              sx={{
                color: "#1e40af",
                transition: "all 0.2s ease",
                width: isMobile ? 26 : 38,
                height: isMobile ? 26 : 38,
                borderRadius: isMobile ? "8px" : "12px",
                minWidth: isMobile ? 26 : 38,
                "&:hover": {
                  transform: "scale(1.1)",
                  backgroundColor: "rgba(30, 64, 175, 0.1)",
                },
              }}
            >
              {mode === "dark" ? (
                
                <svg
                  width="22"
                  height="22"
                  viewBox="0 0 24 24"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    d="M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.8 1.42-1.42zM1 13h3v-2H1v2zm10-9h-2v3h2V4zm7.04 1.46l-1.41-1.41-1.8 1.79 1.42 1.42 1.79-1.8zM20 11v2h3v-2h-3zm-9 9h2v-3h-2v3zm6.24-1.84l1.8 1.79 1.41-1.41-1.79-1.8-1.42 1.42zM4.96 18.54l1.41 1.41 1.8-1.79-1.42-1.42-1.79 1.8zM12 8a4 4 0 100 8 4 4 0 000-8z"
                    fill="currentColor"
                  />
                </svg>
              ) : (
                
                <svg
                  width="22"
                  height="22"
                  viewBox="0 0 24 24"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    d="M20.742 13.045A8.001 8.001 0 0110.955 3.258 9.003 9.003 0 1020.742 13.045z"
                    fill="currentColor"
                  />
                </svg>
              )}
            </IconButton>
          </Tooltip>

          {}
          <Chip
            icon={backendStatus === "connected" ? <CheckCircle /> : <Error />}
            label={
              backendStatus === "connected" ? "API Bağlı" : "API Bağlantısı Yok"
            }
            sx={{
              backgroundColor: "rgba(255, 255, 255, 0.95)",
              borderRadius: isMobile ? "10px" : "12px",
              fontWeight: 700,
              fontSize: `${isMobile ? 10 : 13}px !important`,
              height: `${isMobile ? 26 : 40}px !important`,
              px: isMobile ? "6px" : "14px",
              minWidth: isMobile ? "auto" : undefined,
              border:
                backendStatus === "connected"
                  ? "2px solid #10b981"
                  : "2px solid #ef4444",
              color: backendStatus === "connected" ? "#10b981" : "#ef4444",
              boxShadow:
                backendStatus === "connected"
                  ? "0 4px 14px rgba(16, 185, 129, 0.3)"
                  : "0 4px 14px rgba(239, 68, 68, 0.3)",
              transition: "all 0.3s ease",
              "& .MuiChip-icon": {
                color: backendStatus === "connected" ? "#10b981" : "#ef4444",
                fontSize: isMobile ? "14px" : "18px",
                marginLeft: isMobile ? "0px" : "4px",
              },
              "& .MuiChip-label": {
                padding: isMobile ? "0 4px" : "0 8px",
                fontSize: `${isMobile ? 10 : 13}px !important`,
                letterSpacing: isMobile ? "0.02em" : 0,
              },
              "&:hover": {
                transform: "scale(1.05)",
                boxShadow:
                  backendStatus === "connected"
                    ? "0 6px 20px rgba(16, 185, 129, 0.4)"
                    : "0 6px 20px rgba(239, 68, 68, 0.4)",
              },
            }}
          />

        {}
          {onOpenBranchSelector && (
            <Tooltip title={branchLabel}>
              <Chip
                label={branchChipLabel}
                onClick={onOpenBranchSelector}
                sx={{
                  backgroundColor: "rgba(255, 255, 255, 0.95)",
                  borderRadius: isMobile ? "10px" : "12px",
                  fontWeight: 700,
                  fontSize: `${isMobile ? 10 : 13}px !important`,
                  height: `${isMobile ? 26 : 40}px !important`,
                  px: isMobile ? "6px" : "14px",
                  border: "2px solid #3b82f6",
                  color: "#3b82f6",
                  cursor: "pointer",
                  boxShadow: "0 4px 14px rgba(59, 130, 246, 0.3)",
                  transition: "all 0.3s ease",
                  maxWidth: { xs: 120, sm: 240 },
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                  "& .MuiChip-label": {
                    padding: isMobile ? "0 4px" : "0 6px",
                    fontSize: `${isMobile ? 10 : 13}px !important`,
                    letterSpacing: isMobile ? "0.02em" : 0,
                  },
                  "&:hover": {
                    transform: "translateY(-2px) scale(1.05)",
                    backgroundColor: "#3b82f6",
                    color: "#fff",
                  boxShadow: "0 6px 20px rgba(59, 130, 246, 0.5)",
                },
              }}
            />
          </Tooltip>
        )}

          {}
          <Tooltip title="Son senkronizasyon: 10 dakika önce">
            <IconButton
              sx={{
                backgroundColor: "rgba(255, 255, 255, 0.95)",
                width: isMobile ? 26 : 40,
                height: isMobile ? 26 : 40,
                borderRadius: isMobile ? "8px" : "12px",
                border: "2px solid #10b981",
                color: "#10b981",
                boxShadow: "0 4px 14px rgba(16, 185, 129, 0.25)",
                transition: "all 0.3s ease",
                padding: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                "&:hover": {
                  transform: "translateY(-2px) scale(1.1)",
                  backgroundColor: "#10b981",
                  color: "#fff",
                  boxShadow: "0 6px 20px rgba(16, 185, 129, 0.4)",
                },
                ...(isChecking
                  ? {
                      animation: "spin 1000ms linear infinite",
                      "@keyframes spin": {
                        "0%": { transform: "rotate(0deg)" },
                        "100%": { transform: "rotate(360deg)" },
                      },
                    }
                  : {}),
              }}
            >
              <Sync sx={{ fontSize: "18px" }} />
            </IconButton>
          </Tooltip>

          {}
          <Tooltip title={notificationTooltip}>
            <IconButton
              onClick={handleNotificationOpen}
              sx={{
                backgroundColor: "rgba(255, 255, 255, 0.95)",
                width: isMobile ? 26 : 40,
                height: isMobile ? 26 : 40,
                borderRadius: isMobile ? "8px" : "12px",
                border:
                  pendingCount > 0
                    ? "2px solid #ef4444"
                    : "2px solid rgba(59, 130, 246, 0.3)",
                color: pendingCount > 0 ? "#ef4444" : "#3b82f6",
                boxShadow:
                  pendingCount > 0
                    ? "0 4px 14px rgba(239, 68, 68, 0.3)"
                    : "0 4px 14px rgba(59, 130, 246, 0.2)",
                transition: "all 0.3s ease",
                padding: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                "&:hover": {
                  transform: "translateY(-2px) scale(1.1)",
                  backgroundColor: pendingCount > 0 ? "#ef4444" : "#3b82f6",
                  color: "#fff",
                  borderColor: pendingCount > 0 ? "#ef4444" : "#3b82f6",
                  boxShadow:
                    pendingCount > 0
                      ? "0 6px 20px rgba(239, 68, 68, 0.5)"
                      : "0 6px 20px rgba(59, 130, 246, 0.4)",
                },
                ...(pendingCount > 0 && {
                  animation: "bellShake 1.5s ease-in-out infinite",
                  "@keyframes bellShake": {
                    "0%, 100%": { transform: "rotate(0deg)" },
                    "10%, 30%": { transform: "rotate(-10deg)" },
                    "20%, 40%": { transform: "rotate(10deg)" },
                    "50%": { transform: "rotate(0deg)" },
                  },
                }),
              }}
            >
              <Badge
                badgeContent={pendingCount}
                color="error"
                max={99}
                sx={{
                  "& .MuiBadge-badge": {
                    backgroundColor: "#ef4444",
                    color: "#fff",
                    fontWeight: 700,
                    fontSize: "10px",
                    minWidth: 18,
                    height: 18,
                    borderRadius: "9px",
                    border: "2px solid #fff",
                    boxShadow: "0 2px 8px rgba(239, 68, 68, 0.4)",
                  },
                }}
              >
                <NotificationsIcon sx={{ fontSize: "18px" }} />
              </Badge>
            </IconButton>
          </Tooltip>

          {}
          <Tooltip title="Profil">
            <IconButton
              edge="end"
              onClick={handleProfileMenuOpen}
              sx={{
                backgroundColor: "rgba(255, 255, 255, 0.95)",
                width: isMobile ? 30 : 40,
                height: isMobile ? 30 : 40,
                borderRadius: isMobile ? "8px" : "12px",
                border: "2px solid #8b5cf6",
                padding: 0,
                boxShadow: "0 4px 14px rgba(139, 92, 246, 0.25)",
                transition: "all 0.3s ease",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                "&:hover": {
                  transform: "translateY(-2px) scale(1.08)",
                  backgroundColor: "#8b5cf6",
                  borderColor: "#8b5cf6",
                  boxShadow: "0 6px 20px rgba(139, 92, 246, 0.4)",
                  "& .MuiAvatar-root": {
                    borderColor: "#fff",
                    boxShadow: "0 0 16px rgba(255, 255, 255, 0.6)",
                  },
                },
              }}
            >
              <Avatar
                sx={{
                  width: isMobile ? 24 : 32,
                  height: isMobile ? 24 : 32,
                  border: "2px solid #3b82f6",
                  backgroundColor: "#3b82f6",
                  color: "#fff",
                  fontWeight: 700,
                  fontSize: isMobile ? "11px" : "14px",
                  transition: "all 0.3s ease",
                }}
              >
                A
              </Avatar>
            </IconButton>
          </Tooltip>
        </Box>

        {}
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          onClick={handleMenuClose}
          PaperProps={{
            sx: {
              backdropFilter: "blur(20px)",
              background:
                theme.palette.mode === "dark"
                  ? "rgba(15,23,42,0.95)"
                  : "rgba(255,255,255,0.95)",
              border:
                theme.palette.mode === "dark"
                  ? "1px solid rgba(255,255,255,0.1)"
                  : "1px solid rgba(0,0,0,0.05)",
              borderRadius: 3,
              boxShadow:
                theme.palette.mode === "dark"
                  ? "0 20px 40px rgba(0,0,0,0.4)"
                  : "0 20px 40px rgba(0,0,0,0.1)",
            },
          }}
        >
          <MenuItem
            onClick={handleProfileClick}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              transition: "all 0.2s ease",
              "&:hover": {
                backgroundColor: theme.palette.action.hover,
                transform: "translateX(4px)",
              },
            }}
          >
            <AccountCircle sx={{ mr: 2, color: theme.palette.primary.main }} />{" "}
            Profil
          </MenuItem>
          <MenuItem
            onClick={handleSettingsClick}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              transition: "all 0.2s ease",
              "&:hover": {
                backgroundColor: theme.palette.action.hover,
                transform: "translateX(4px)",
              },
            }}
          >
            <Settings sx={{ mr: 2, color: theme.palette.primary.main }} />{" "}
            Ayarlar
          </MenuItem>
          <MenuItem
            onClick={handleLogout}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              transition: "all 0.2s ease",
              "&:hover": {
                backgroundColor: theme.palette.action.hover,
                transform: "translateX(4px)",
              },
            }}
          >
            <Logout sx={{ mr: 2, color: theme.palette.primary.main }} /> Çıkış
            Yap
          </MenuItem>
        </Menu>

        {}
        <Menu
          anchorEl={notificationAnchor}
          open={Boolean(notificationAnchor)}
          onClose={handleNotificationClose}
          onClick={handleNotificationClose}
          PaperProps={{
            sx: {
              backdropFilter: "blur(20px)",
              background:
                theme.palette.mode === "dark"
                  ? "rgba(15,23,42,0.95)"
                  : "rgba(255,255,255,0.95)",
              border:
                theme.palette.mode === "dark"
                  ? "1px solid rgba(255,255,255,0.1)"
                  : "1px solid rgba(0,0,0,0.05)",
              borderRadius: 3,
              boxShadow:
                theme.palette.mode === "dark"
                  ? "0 20px 40px rgba(0,0,0,0.4)"
                  : "0 20px 40px rgba(0,0,0,0.1)",
              minWidth: 280,
            },
          }}
        >
          {notifications.length === 0 ? (
            <MenuItem
              disabled
              sx={{
                borderRadius: 2,
                mx: 1,
                my: 0.5,
                opacity: 0.85,
                "&:hover": { backgroundColor: "transparent" },
              }}
            >
              <Box>
                <Typography variant="body2" fontWeight="bold">
                  Bildirim yok
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Yeni bildirimler burada görünecek.
                </Typography>
              </Box>
            </MenuItem>
          ) : (
            notifications.map((notification) => {
              const statusMeta = notificationStatusMeta[notification.status];
              return (
                <MenuItem
                  key={notification.id}
                  sx={{
                    borderRadius: 2,
                    mx: 1,
                    my: 0.5,
                    transition: "all 0.2s ease",
                    backgroundColor:
                      notification.status === "pending"
                        ? theme.palette.action.hover
                        : "transparent",
                    "&:hover": {
                      backgroundColor: theme.palette.action.hover,
                      transform: "translateX(4px)",
                    },
                  }}
                >
                  <Box
                    sx={{
                      display: "flex",
                      flexDirection: "column",
                      gap: 0.5,
                      minWidth: 240,
                    }}
                  >
                    <Box
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 1,
                      }}
                    >
                      <Typography variant="body2" fontWeight="bold">
                        {notification.title}
                      </Typography>
                      <Chip
                        label={statusMeta.label}
                        color={statusMeta.color}
                        size="small"
                        sx={{ fontWeight: 700 }}
                      />
                    </Box>
                    {notification.description && (
                      <Typography variant="body2" color="text.secondary">
                        {notification.description}
                      </Typography>
                    )}
                    <Typography variant="caption" color="text.secondary">
                      {formatRelativeTime(notification.createdAt) || "—"}
                    </Typography>
                  </Box>
                </MenuItem>
              );
            })
          )}
        </Menu>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
