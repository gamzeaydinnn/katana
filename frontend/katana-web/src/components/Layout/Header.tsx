import {
  AccountCircle,
  CheckCircle,
  Logout,
  Menu as MenuIcon,
  Notifications as NotificationsIcon,
  MoreVert,
  Settings,
  Sync,
} from "@mui/icons-material";
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  Button,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useTheme } from "@mui/material/styles";
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
  currentBranchName?: string | null;
  onOpenBranchSelector?: () => void;
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
  currentBranchName,
  onOpenBranchSelector,
}) => {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [notificationAnchor, setNotificationAnchor] =
    React.useState<null | HTMLElement>(null);
  const [mobileActionsAnchor, setMobileActionsAnchor] =
    React.useState<null | HTMLElement>(null);
  const [isChecking, setIsChecking] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  useEffect(() => {
    const checkBackendHealth = async () => {
      setIsChecking(true);
      try {
        await stockAPI.getHealthStatus();
      } catch (error) {
        // Handle error silently
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

    startConnection()
      .then(() => {
        if (isMounted) {
          console.log("[Header] ✅ SignalR connected successfully");
        }
      })
      .catch((err: any) => {
        console.warn("[Header] ⚠️ SignalR connection failed:", {
          message: err?.message,
          statusCode: err?.statusCode,
          errorType: err?.constructor?.name,
        });
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

  const handleMobileActionsOpen = (event: React.MouseEvent<HTMLElement>) => {
    setMobileActionsAnchor(event.currentTarget);
  };

  const handleMobileActionsClose = () => {
    setMobileActionsAnchor(null);
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

  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));

  const ACTION_SIZE = 44;
  const ACTION_RADIUS = 10;
  const ACTION_BORDER = "1.5px solid rgba(79, 134, 255, 0.3)";

  const baseIconButtonSx = {
    backgroundColor: "#fff",
    border: ACTION_BORDER,
    borderRadius: `${ACTION_RADIUS}px`,
    width: ACTION_SIZE,
    height: ACTION_SIZE,
    color: "#4F86FF",
    flexShrink: 0,
    "&:hover": {
      backgroundColor: "rgba(79, 134, 255, 0.05)",
      borderColor: "#4F86FF",
    },
  } as const;

  const DesktopHeaderContent = () => (
    <>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 1.25,
          minWidth: 0,
          flexShrink: 0,
        }}
      >
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{
            color: "#1e40af",
            transition: "transform 0.2s ease",
            flexShrink: 0,
            p: 1,
            width: ACTION_SIZE,
            height: ACTION_SIZE,
            "&:hover": {
              transform: "scale(1.08)",
              backgroundColor: "rgba(79, 134, 255, 0.1)",
            },
          }}
        >
          <MenuIcon sx={{ fontSize: 26 }} />
        </IconButton>

        <Box
          component="img"
          src="/logoo.png"
          alt="BeforMet Metal Logo"
          sx={{
            height: 44,
            width: "auto",
            objectFit: "contain",
            filter: "drop-shadow(0 2px 8px rgba(0,0,0,0.15))",
            flexShrink: 0,
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
            fontSize: "1.15rem",
            whiteSpace: "nowrap",
            overflow: "hidden",
            textOverflow: "ellipsis",
            minWidth: 0,
          }}
        >
          Beformet Metal
        </Typography>
      </Box>

      <Box sx={{ flex: 1, minWidth: 0 }} />

      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "flex-end",
          flexShrink: 0,
          minWidth: 0,
        }}
      >
        <Stack
          direction="row"
          alignItems="center"
          spacing={1}
          sx={{ flexShrink: 0 }}
        >
          <Button
            variant="outlined"
            startIcon={<CheckCircle />}
            sx={{
              borderColor: "#10B981",
              color: "#10B981",
              backgroundColor: "#fff",
              borderRadius: "12px",
              textTransform: "none",
              fontWeight: 600,
              px: 2.25,
              height: ACTION_SIZE,
              minWidth: 0,
              "&:hover": {
                borderColor: "#10B981",
                backgroundColor: "rgba(16, 185, 129, 0.05)",
              },
            }}
          >
            API Bağlı
          </Button>

          <Button
            variant="outlined"
            onClick={onOpenBranchSelector}
            sx={{
              borderColor: "#3B82F6",
              color: "#3B82F6",
              backgroundColor: "#fff",
              borderRadius: "12px",
              textTransform: "none",
              fontWeight: 600,
              px: 2.25,
              height: ACTION_SIZE,
              minWidth: 0,
              "&:hover": {
                borderColor: "#3B82F6",
                backgroundColor: "rgba(59, 130, 246, 0.05)",
              },
            }}
          >
            {currentBranchName || "Şube Seç"}
          </Button>

          <IconButton
            sx={{
              ...baseIconButtonSx,
              color: "#10B981",
              ...(isChecking && {
                animation: "spin 1s linear infinite",
                "@keyframes spin": {
                  "0%": { transform: "rotate(0deg)" },
                  "100%": { transform: "rotate(360deg)" },
                },
              }),
            }}
          >
            <Sync sx={{ fontSize: 20 }} />
          </IconButton>

          <IconButton onClick={handleNotificationOpen} sx={baseIconButtonSx}>
            <Badge
              badgeContent={pendingCount}
              color="error"
              max={99}
              sx={{
                "& .MuiBadge-badge": {
                  backgroundColor: "#ef4444",
                  color: "#fff",
                  fontWeight: 700,
                  fontSize: "9px",
                  minWidth: 16,
                  height: 16,
                  borderRadius: "8px",
                  border: "2px solid #fff",
                },
              }}
            >
              <NotificationsIcon sx={{ fontSize: 20 }} />
            </Badge>
          </IconButton>

          <IconButton
            onClick={handleProfileMenuOpen}
            sx={{ ...baseIconButtonSx, p: 0 }}
          >
            <Avatar
              sx={{
                width: 28,
                height: 28,
                backgroundColor: "#4F86FF",
                color: "#fff",
                fontWeight: 700,
                fontSize: "14px",
              }}
            >
              A
            </Avatar>
          </IconButton>
        </Stack>
      </Box>
    </>
  );

  const MobileHeaderContent = () => (
    <>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 1,
          minWidth: 0,
          flexShrink: 0,
        }}
      >
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{
            color: "#1e40af",
            transition: "transform 0.2s ease",
            flexShrink: 0,
            p: 0.75,
            width: ACTION_SIZE,
            height: ACTION_SIZE,
            "&:hover": {
              transform: "scale(1.08)",
              backgroundColor: "rgba(79, 134, 255, 0.1)",
            },
          }}
        >
          <MenuIcon sx={{ fontSize: 24 }} />
        </IconButton>

        <Box
          component="img"
          src="/logoo.png"
          alt="BeforMet Metal Logo"
          sx={{
            height: 36,
            width: "auto",
            objectFit: "contain",
            filter: "drop-shadow(0 2px 8px rgba(0,0,0,0.12))",
            flexShrink: 0,
          }}
        />
      </Box>

      <Box sx={{ flex: 1, minWidth: 0 }} />

      <Stack
        direction="row"
        alignItems="center"
        spacing={0.75}
        sx={{ flexShrink: 0 }}
      >
        <IconButton
          sx={{
            ...baseIconButtonSx,
            color: "#10B981",
            ...(isChecking && {
              animation: "spin 1s linear infinite",
              "@keyframes spin": {
                "0%": { transform: "rotate(0deg)" },
                "100%": { transform: "rotate(360deg)" },
              },
            }),
          }}
        >
          <Sync sx={{ fontSize: 20 }} />
        </IconButton>

        <IconButton onClick={handleNotificationOpen} sx={baseIconButtonSx}>
          <Badge
            badgeContent={pendingCount}
            color="error"
            max={99}
            sx={{
              "& .MuiBadge-badge": {
                backgroundColor: "#ef4444",
                color: "#fff",
                fontWeight: 700,
                fontSize: "9px",
                minWidth: 16,
                height: 16,
                borderRadius: "8px",
                border: "2px solid #fff",
              },
            }}
          >
            <NotificationsIcon sx={{ fontSize: 20 }} />
          </Badge>
        </IconButton>

        <IconButton
          onClick={handleMobileActionsOpen}
          sx={baseIconButtonSx}
          aria-label="more-actions"
        >
          <MoreVert sx={{ fontSize: 22 }} />
        </IconButton>

        <IconButton
          onClick={handleProfileMenuOpen}
          sx={{ ...baseIconButtonSx, p: 0 }}
        >
          <Avatar
            sx={{
              width: 28,
              height: 28,
              backgroundColor: "#4F86FF",
              color: "#fff",
              fontWeight: 700,
              fontSize: "14px",
            }}
          >
            A
          </Avatar>
        </IconButton>
      </Stack>
    </>
  );

  return (
    <AppBar
      position="fixed"
      sx={{
        zIndex: (theme) => theme.zIndex.drawer + 1,
        background: "linear-gradient(135deg, #E8F0FF 0%, #F0F4FF 100%)",
        borderBottom: "none",
        boxShadow: "0 2px 8px rgba(43, 110, 246, 0.06)",
        transition: "all 0.3s ease",
      }}
    >
      <Toolbar
        sx={{
          minHeight: 64,
          px: 3,
          gap: 1.5,
          flexWrap: "nowrap",
          alignItems: "center",
          width: "100%",
          maxWidth: "100%",
          boxSizing: "border-box",
          overflow: "visible",
        }}
      >
        {isMobile ? <MobileHeaderContent /> : <DesktopHeaderContent />}

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          onClick={handleMenuClose}
          slotProps={{
            paper: {
              sx: {
                background: "rgba(255,255,255,0.98)",
                border: "1px solid rgba(0,0,0,0.05)",
                borderRadius: 3,
                boxShadow: "0 20px 40px rgba(0,0,0,0.1)",
              },
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
                backgroundColor: "rgba(79, 134, 255, 0.08)",
                transform: "translateX(4px)",
              },
            }}
          >
            <AccountCircle sx={{ mr: 2, color: "#4F86FF" }} /> Profil
          </MenuItem>
          <MenuItem
            onClick={handleSettingsClick}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              transition: "all 0.2s ease",
              "&:hover": {
                backgroundColor: "rgba(79, 134, 255, 0.08)",
                transform: "translateX(4px)",
              },
            }}
          >
            <Settings sx={{ mr: 2, color: "#4F86FF" }} /> Ayarlar
          </MenuItem>
          <MenuItem
            onClick={handleLogout}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              transition: "all 0.2s ease",
              "&:hover": {
                backgroundColor: "rgba(79, 134, 255, 0.08)",
                transform: "translateX(4px)",
              },
            }}
          >
            <Logout sx={{ mr: 2, color: "#4F86FF" }} /> Çıkış Yap
          </MenuItem>
        </Menu>

        <Menu
          anchorEl={notificationAnchor}
          open={Boolean(notificationAnchor)}
          onClose={handleNotificationClose}
          onClick={handleNotificationClose}
          slotProps={{
            paper: {
              sx: {
                background: "rgba(255,255,255,0.98)",
                border: "1px solid rgba(0,0,0,0.05)",
                borderRadius: 3,
                boxShadow: "0 20px 40px rgba(0,0,0,0.1)",
                minWidth: 280,
              },
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
                        ? "rgba(79, 134, 255, 0.05)"
                        : "transparent",
                    "&:hover": {
                      backgroundColor: "rgba(79, 134, 255, 0.08)",
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

        <Menu
          anchorEl={mobileActionsAnchor}
          open={Boolean(mobileActionsAnchor)}
          onClose={handleMobileActionsClose}
          onClick={handleMobileActionsClose}
          slotProps={{
            paper: {
              sx: {
                background: "rgba(255,255,255,0.98)",
                border: "1px solid rgba(0,0,0,0.05)",
                borderRadius: 3,
                boxShadow: "0 20px 40px rgba(0,0,0,0.1)",
                minWidth: 200,
              },
            },
          }}
        >
          <MenuItem
            disabled
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              opacity: 0.9,
              "&:hover": { backgroundColor: "transparent" },
            }}
          >
            <CheckCircle sx={{ mr: 2, color: "#10B981" }} /> API Bağlı
          </MenuItem>
          <MenuItem
            onClick={onOpenBranchSelector}
            sx={{
              borderRadius: 2,
              mx: 1,
              my: 0.5,
              "&:hover": {
                backgroundColor: "rgba(59, 130, 246, 0.08)",
              },
            }}
          >
            <Typography sx={{ fontWeight: 600, color: "#3B82F6" }}>
              Şube Seç
            </Typography>
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
