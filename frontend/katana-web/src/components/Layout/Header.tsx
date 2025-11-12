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
  useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api, { stockAPI } from "../../services/api";
import {
  startConnection,
  onPendingCreated,
  offPendingCreated,
  onPendingApproved,
  offPendingApproved,
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

  // Backend health check
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
    const interval = setInterval(checkBackendHealth, 60000); // Check every minute
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    let isMounted = true;

    // Load initial pending adjustments on mount
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
        console.error("[Header] ❌ Failed to load initial notifications:", {
          message: err?.message,
          status: err?.response?.status,
          data: err?.response?.data,
        });
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

  return (
    <AppBar
      position="fixed"
      sx={{
        zIndex: (theme) => theme.zIndex.drawer + 1,
        backdropFilter: "blur(10px)",
        background:
          "linear-gradient(90deg, #2b6ef6 0%, #4f86ff 50%, #79a8ff 100%)",
        backgroundSize: "300% 300%",
        animation: "headerGradient 10s ease-in-out infinite",
        "@keyframes headerGradient": {
          "0%": { backgroundPosition: "0% 50%" },
          "50%": { backgroundPosition: "100% 50%" },
          "100%": { backgroundPosition: "0% 50%" },
        },
        borderBottom: "1px solid rgba(255,255,255,0.06)",
        boxShadow: "0 8px 32px rgba(16,24,40,0.14)",
        transition: "all 0.3s ease",
      }}
    >
      <Toolbar sx={{ minHeight: 64 }}>
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{
            mr: 2,
            color: "#fff",
            transition: "transform 0.2s ease",
            "&:hover": {
              transform: "scale(1.08)",
              backgroundColor: "rgba(255,255,255,0.06)",
            },
          }}
        >
          <MenuIcon />
        </IconButton>

        <Box
          sx={{
            flexGrow: 1,
            display: "flex",
            alignItems: "center",
            gap: 0.1, // tightened spacing so text sits closer to the logo
          }}
        >
          <Box
            component="img"
            src="/logoo.png"
            alt="Beformat Metal Logo"
            sx={{
              height: 80,
              width: "auto",
              objectFit: "contain",
              filter: "drop-shadow(0 2px 8px rgba(0,0,0,0.15))",
            }}
          />
          <Typography
            variant="h6"
            noWrap
            component="div"
            sx={{
              fontWeight: 800,
              letterSpacing: "-0.02em",
              color: "#fff",
              textShadow: "0 2px 10px rgba(0,0,0,0.18)",
            }}
          >
            Beformet Metal
          </Typography>
        </Box>

        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
          {/* Theme toggle */}
          <Tooltip title={mode === "dark" ? "Açık tema" : "Koyu tema"}>
            <IconButton
              size="small"
              onClick={onToggleMode}
              sx={{
                color: theme.palette.primary.main,
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.1)",
                  backgroundColor: theme.palette.action.hover,
                },
              }}
            >
              {mode === "dark" ? (
                // Light mode icon
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
                // Dark mode icon
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

          {/* Backend Status */}
          <Chip
            icon={backendStatus === "connected" ? <CheckCircle /> : <Error />}
            label={
              backendStatus === "connected" ? "API Bağlı" : "API Bağlantısı Yok"
            }
            color={backendStatus === "connected" ? "success" : "error"}
            size="small"
            variant="outlined"
            sx={{
              borderRadius: 2,
              fontWeight: 600,
              transition: "all 0.22s ease",
              ...(backendStatus === "connected"
                ? {
                    animation: "pulse 2400ms ease-in-out infinite",
                    "@keyframes pulse": {
                      "0%": {
                        transform: "scale(1)",
                        boxShadow: "0 0 0 0 rgba(79,110,247,0.35)",
                      },
                      "70%": {
                        transform: "scale(1.03)",
                        boxShadow: "0 10px 30px 6px rgba(79,110,247,0.08)",
                      },
                      "100%": {
                        transform: "scale(1)",
                        boxShadow: "0 0 0 0 rgba(79,110,247,0)",
                      },
                    },
                  }
                : {}),
              "&:hover": { transform: "scale(1.05)" },
            }}
          />

          {/* Branch display + selector */}
          {onOpenBranchSelector && (
            <Chip
              label={currentBranchName ? String(currentBranchName) : "Şube Seç"}
              onClick={onOpenBranchSelector}
              size="small"
              variant="outlined"
              sx={{
                ml: 1,
                cursor: "pointer",
                borderRadius: 2,
                fontWeight: 600,
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.05)",
                  backgroundColor: theme.palette.action.hover,
                },
              }}
            />
          )}

          {/* Sync Status */}
          <Tooltip title="Son senkronizasyon: 10 dakika önce">
            <IconButton
              size="small"
              sx={{
                color: theme.palette.success.main,
                transition: "all 0.22s ease",
                "&:hover": {
                  transform: "scale(1.12)",
                  backgroundColor: theme.palette.action.hover,
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
              <Sync />
            </IconButton>
          </Tooltip>

          {/* Notifications */}
          <Tooltip title={notificationTooltip}>
            <IconButton
              size="large"
              onClick={handleNotificationOpen}
              sx={{
                color:
                  signalrStatus === "error"
                    ? theme.palette.error.main
                    : theme.palette.primary.main,
                transition: "all 0.22s ease",
                "&:hover": {
                  transform: "scale(1.08)",
                  backgroundColor: theme.palette.action.hover,
                },
                "@keyframes bounce": {
                  "0%": { transform: "translateY(0)" },
                  "30%": { transform: "translateY(-6px)" },
                  "60%": { transform: "translateY(0)" },
                  "100%": { transform: "translateY(0)" },
                },
              }}
            >
              <Badge
                badgeContent={pendingCount}
                color={pendingCount > 0 ? "error" : "default"}
                showZero
                sx={{
                  ...(pendingCount > 0
                    ? {
                        "& .MuiBadge-badge": {
                          transformOrigin: "center top",
                          animation: "bounce 1600ms ease-in-out infinite",
                        },
                      }
                    : {}),
                }}
              >
                <NotificationsIcon />
              </Badge>
            </IconButton>
          </Tooltip>

          {/* Profile Menu */}
          <Tooltip title="Profil">
            <IconButton
              size="large"
              edge="end"
              onClick={handleProfileMenuOpen}
              sx={{
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.1)",
                  backgroundColor: theme.palette.action.hover,
                },
              }}
            >
              <Avatar
                sx={{
                  width: 36,
                  height: 36,
                  border: `2px solid ${theme.palette.primary.main}`,
                  transition: "all 0.2s ease",
                  "&:hover": {
                    borderColor: theme.palette.secondary.main,
                    boxShadow: `0 0 12px ${theme.palette.primary.main}40`,
                  },
                }}
              >
                A
              </Avatar>
            </IconButton>
          </Tooltip>
        </Box>

        {/* Profile Menu */}
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

        {/* Notification Menu */}
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
