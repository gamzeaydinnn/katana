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
import { stockAPI } from "../../services/api";

interface HeaderProps {
  onMenuClick: () => void;
  sidebarOpen: boolean;
  currentBranchName?: string | null;
  onOpenBranchSelector?: () => void;
  mode?: "light" | "dark";
  onToggleMode?: () => void;
}

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

  // Backend health check
  useEffect(() => {
    const checkBackendHealth = async () => {
      try {
        await stockAPI.getHealthStatus();
        setBackendStatus("connected");
      } catch (error) {
        setBackendStatus("disconnected");
      }
    };

    checkBackendHealth();
    const interval = setInterval(checkBackendHealth, 60000); // Check every minute
    return () => clearInterval(interval);
  }, []);

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

  const handleLogout = () => {
    localStorage.removeItem("authToken");
    window.location.href = "/login";
  };

  return (
    <AppBar
      position="fixed"
      sx={{
        zIndex: (theme) => theme.zIndex.drawer + 1,
        backdropFilter: "blur(20px)",
        background: theme.palette.mode === "dark"
          ? "rgba(15,23,42,0.95)"
          : "rgba(255,255,255,0.95)",
        borderBottom: theme.palette.mode === "dark"
          ? "1px solid rgba(255,255,255,0.1)"
          : "1px solid rgba(0,0,0,0.05)",
        boxShadow: theme.palette.mode === "dark"
          ? "0 8px 32px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
          : "0 8px 32px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
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
            color: theme.palette.primary.main,
            transition: "transform 0.2s ease",
            "&:hover": {
              transform: "scale(1.1)",
              backgroundColor: theme.palette.action.hover,
            },
          }}
        >
          <MenuIcon />
        </IconButton>

        <Typography
          variant="h6"
          noWrap
          component="div"
          sx={{
            flexGrow: 1,
            fontWeight: 900,
            letterSpacing: "-0.02em",
            background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
            backgroundClip: "text",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
          }}
        >
          Beformet Metal ERP
        </Typography>

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
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.8 1.42-1.42zM1 13h3v-2H1v2zm10-9h-2v3h2V4zm7.04 1.46l-1.41-1.41-1.8 1.79 1.42 1.42 1.79-1.8zM20 11v2h3v-2h-3zm-9 9h2v-3h-2v3zm6.24-1.84l1.8 1.79 1.41-1.41-1.79-1.8-1.42 1.42zM4.96 18.54l1.41 1.41 1.8-1.79-1.42-1.42-1.79 1.8zM12 8a4 4 0 100 8 4 4 0 000-8z" fill="currentColor"/>
                </svg>
              ) : (
                // Dark mode icon
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M20.742 13.045A8.001 8.001 0 0110.955 3.258 9.003 9.003 0 1020.742 13.045z" fill="currentColor"/>
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
              transition: "all 0.2s ease",
              "&:hover": {
                transform: "scale(1.05)",
              },
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
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.1)",
                  backgroundColor: theme.palette.action.hover,
                },
              }}
            >
              <Sync />
            </IconButton>
          </Tooltip>

          {/* Notifications */}
          <Tooltip title="Bildirimler">
            <IconButton
              size="large"
              onClick={handleNotificationOpen}
              sx={{
                color: theme.palette.primary.main,
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.1)",
                  backgroundColor: theme.palette.action.hover,
                },
              }}
            >
              <Badge badgeContent={3} color="error">
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
              background: theme.palette.mode === "dark"
                ? "rgba(15,23,42,0.95)"
                : "rgba(255,255,255,0.95)",
              border: theme.palette.mode === "dark"
                ? "1px solid rgba(255,255,255,0.1)"
                : "1px solid rgba(0,0,0,0.05)",
              borderRadius: 3,
              boxShadow: theme.palette.mode === "dark"
                ? "0 20px 40px rgba(0,0,0,0.4)"
                : "0 20px 40px rgba(0,0,0,0.1)",
            },
          }}
        >
          <MenuItem
            onClick={handleMenuClose}
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
            <AccountCircle sx={{ mr: 2, color: theme.palette.primary.main }} /> Profil
          </MenuItem>
          <MenuItem
            onClick={handleMenuClose}
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
            <Settings sx={{ mr: 2, color: theme.palette.primary.main }} /> Ayarlar
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
            <Logout sx={{ mr: 2, color: theme.palette.primary.main }} /> Çıkış Yap
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
              background: theme.palette.mode === "dark"
                ? "rgba(15,23,42,0.95)"
                : "rgba(255,255,255,0.95)",
              border: theme.palette.mode === "dark"
                ? "1px solid rgba(255,255,255,0.1)"
                : "1px solid rgba(0,0,0,0.05)",
              borderRadius: 3,
              boxShadow: theme.palette.mode === "dark"
                ? "0 20px 40px rgba(0,0,0,0.4)"
                : "0 20px 40px rgba(0,0,0,0.1)",
              minWidth: 280,
            },
          }}
        >
          <MenuItem
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
            <Box>
              <Typography variant="body2" fontWeight="bold">
                Senkronizasyon Tamamlandı
              </Typography>
              <Typography variant="caption" color="text.secondary">
                5 dakika önce
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem
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
            <Box>
              <Typography variant="body2" fontWeight="bold">
                Stok Seviyesi Düşük
              </Typography>
              <Typography variant="caption" color="text.secondary">
                15 dakika önce
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem
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
            <Box>
              <Typography variant="body2" fontWeight="bold">
                Yeni Sipariş Alındı
              </Typography>
              <Typography variant="caption" color="text.secondary">
                1 saat önce
              </Typography>
            </Box>
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
