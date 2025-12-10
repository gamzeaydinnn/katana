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
          console.log("[Header] ✅ SignalR connected successfully");
        }
      })
      .catch((err: any) => {
        console.warn("[Header] ⚠️ SignalR connection failed:", {
          message: err?.message,
          statusCode: err?.statusCode,
          errorType: err?.constructor?.name,
        });
        if (isMounted) {
          setSignalrStatus("error");
          setSignalrError(err?.message || "SignalR bağlantısı kurulamadı");
          // Don't block UI - just show warning
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
          minHeight: { xs: 56, sm: 64 },
          px: { xs: 1, sm: 2, md: 3 },
          gap: { xs: 0.5, sm: 1 },
          flexWrap: "nowrap",
          alignItems: "center",
          justifyContent: "space-between",
          width: "100%",
          maxWidth: "100%",
          boxSizing: "border-box",
          overflow: "hidden",
        }}
      >
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{
            mr: { xs: 0.5, sm: 1, md: 2 },
            color: "#1e40af",
            transition: "transform 0.2s ease",
            flexShrink: 0,
            p: { xs: 0.5, sm: 1 },
            "&:hover": {
              transform: "scale(1.08)",
              backgroundColor: "rgba(79, 134, 255, 0.1)",
            },
          }}
        >
          <MenuIcon sx={{ fontSize: { xs: 20, sm: 24, md: 26 } }} />
        </IconButton>

        <Box
          sx={{
            flexGrow: 0,
            flexShrink: 0,
            display: "flex",
            alignItems: "center",
            minWidth: 0,
            mr: { xs: 0.5, sm: 1, md: 1.5 },
            gap: { xs: 0.25, sm: 0.5, md: 0.75 },
          }}
        >
          <Box
            component="img"
            src="/logoo.png"
            alt="BeforMet Metal Logo"
            sx={{
              display: { xs: "none", sm: "block" },
              height: { sm: 36, md: 48 },
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
              fontSize: { xs: "0.8rem", sm: "0.95rem", md: "1.15rem" },
              display: "flex",
              flexDirection: "column",
              lineHeight: 1.05,
              whiteSpace: "nowrap",
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
            gap: { xs: 0.4, sm: 0.75, md: 1.25 },
            flexWrap: "nowrap",
            justifyContent: "flex-end",
            flexGrow: 1,
            ml: "auto",
            pr: { xs: 0.25, sm: 0 },
          }}
        >
          <Chip
            icon={
              <CheckCircle
                sx={{
                  fontSize: { xs: 11, sm: 14, md: 16 },
                  color: "#10b981 !important",
                }}
              />
            }
            label="Bağlı"
            sx={{
              backgroundColor: "rgba(16, 185, 129, 0.15)",
              border: "1px solid #10b981",
              borderRadius: "14px",
              height: { xs: 26, sm: 30, md: 34 },
              color: "#10b981",
              fontWeight: 600,
              fontSize: { xs: "10px", sm: "11px", md: "12px" },
              flexShrink: 0,
              "& .MuiChip-icon": {
                color: "#10b981",
                ml: { xs: 0.5, sm: 0.75 },
                mr: { xs: -0.25, sm: 0 },
              },
              "& .MuiChip-label": {
                px: { xs: 1, sm: 1.25, md: 1.5 },
                whiteSpace: "nowrap",
              },
            }}
          />

          <IconButton
            onClick={onToggleMode}
            sx={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              width: { xs: 30, sm: 36, md: 42 },
              height: { xs: 30, sm: 36, md: 42 },
              minWidth: { xs: 30, sm: 36, md: 42 },
              borderRadius: "50%",
              color: "#fff",
              boxShadow: "0 2px 8px rgba(102, 126, 234, 0.4)",
              p: 0,
              flexShrink: 0,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              "&:hover": {
                background: "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
                transform: "scale(1.05)",
              },
            }}
          >
            {mode === "dark" ? (
              <span style={{ fontSize: "16px", lineHeight: 1 }}>☀️</span>
            ) : (
              <span style={{ fontSize: "16px", lineHeight: 1 }}>🌙</span>
            )}
          </IconButton>

          <IconButton
            sx={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              width: { xs: 28, sm: 34, md: 40 },
              height: { xs: 28, sm: 34, md: 40 },
              minWidth: { xs: 28, sm: 34, md: 40 },
              borderRadius: "50%",
              color: "#fff",
              boxShadow: "0 2px 8px rgba(102, 126, 234, 0.4)",
              p: 0,
              flexShrink: 0,
              "&:hover": {
                background: "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
                transform: "scale(1.05)",
              },
              ...(isChecking && {
                animation: "spin 1s linear infinite",
                "@keyframes spin": {
                  "0%": { transform: "rotate(0deg)" },
                  "100%": { transform: "rotate(360deg)" },
                },
              }),
            }}
          >
            <Sync sx={{ fontSize: { xs: 13, sm: 16, md: 19 } }} />
          </IconButton>

          <IconButton
            onClick={handleNotificationOpen}
            sx={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              width: { xs: 28, sm: 34, md: 40 },
              height: { xs: 28, sm: 34, md: 40 },
              minWidth: { xs: 28, sm: 34, md: 40 },
              borderRadius: "50%",
              color: "#fff",
              boxShadow: "0 2px 8px rgba(102, 126, 234, 0.4)",
              p: 0,
              flexShrink: 0,
              "&:hover": {
                background: "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
                transform: "scale(1.05)",
              },
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
                  fontSize: { xs: "7px", sm: "8px", md: "9px" },
                  minWidth: { xs: 11, sm: 13, md: 15 },
                  height: { xs: 11, sm: 13, md: 15 },
                  borderRadius: "5px",
                  border: "1.5px solid #764ba2",
                  top: -1,
                  right: -1,
                },
              }}
            >
              <NotificationsIcon
                sx={{ fontSize: { xs: 13, sm: 16, md: 19 } }}
              />
            </Badge>
          </IconButton>

          <IconButton
            onClick={handleProfileMenuOpen}
            sx={{
              background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
              width: { xs: 28, sm: 34, md: 40 },
              height: { xs: 28, sm: 34, md: 40 },
              minWidth: { xs: 28, sm: 34, md: 40 },
              borderRadius: "50%",
              color: "#fff",
              boxShadow: "0 2px 8px rgba(102, 126, 234, 0.4)",
              p: 0,
              flexShrink: 0,
              "&:hover": {
                background: "linear-gradient(135deg, #764ba2 0%, #667eea 100%)",
                transform: "scale(1.05)",
              },
            }}
          >
            <Avatar
              sx={{
                width: { xs: 18, sm: 22, md: 28 },
                height: { xs: 18, sm: 22, md: 28 },
                backgroundColor: "rgba(255,255,255,0.25)",
                color: "#fff",
                fontWeight: 700,
                fontSize: { xs: "8px", sm: "10px", md: "12px" },
              }}
            >
              A
            </Avatar>
          </IconButton>
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
