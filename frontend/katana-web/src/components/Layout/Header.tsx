import React, { useState, useEffect } from "react";
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Box,
  Avatar,
  Menu,
  MenuItem,
  Badge,
  Tooltip,
  useTheme,
  Chip,
} from "@mui/material";
import {
  Menu as MenuIcon,
  Notifications as NotificationsIcon,
  AccountCircle,
  Logout,
  Settings,
  Sync,
  CheckCircle,
  Error,
} from "@mui/icons-material";
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
        color: theme.palette.text.primary,
        boxShadow: "0 10px 30px rgba(0,0,0,0.10)",
        background: (t) =>
          `linear-gradient(180deg, ${t.palette.background.paper} 0%, ${t.palette.background.paper} 100%)`,
        borderBottom: (t) => `1px solid ${t.palette.divider}`,
      }}
    >
      <Toolbar>
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={onMenuClick}
          edge="start"
          sx={{ mr: 2 }}
        >
          <MenuIcon />
        </IconButton>

        <Typography
          variant="h6"
          noWrap
          component="div"
          sx={{ flexGrow: 1, fontWeight: 600 }}
        >
          Beformet Metal ERP
        </Typography>

        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          {/* Theme toggle */}
          <Tooltip title={mode === "dark" ? "Açık tema" : "Koyu tema"}>
            <IconButton size="small" color="inherit" onClick={onToggleMode}>
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
          />

          {/* Branch display + selector */}
          {onOpenBranchSelector && (
            <Chip
              label={currentBranchName ? String(currentBranchName) : "Şube Seç"}
              onClick={onOpenBranchSelector}
              size="small"
              variant="outlined"
              sx={{ ml: 1, cursor: "pointer" }}
            />
          )}

          {/* Sync Status */}
          <Tooltip title="Son senkronizasyon: 10 dakika önce">
            <IconButton size="small" color="success">
              <Sync />
            </IconButton>
          </Tooltip>

          {/* Notifications */}
          <Tooltip title="Bildirimler">
            <IconButton
              size="large"
              color="inherit"
              onClick={handleNotificationOpen}
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
              color="inherit"
            >
              <Avatar sx={{ width: 32, height: 32 }}>A</Avatar>
            </IconButton>
          </Tooltip>
        </Box>

        {/* Profile Menu */}
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          onClick={handleMenuClose}
        >
          <MenuItem onClick={handleMenuClose}>
            <AccountCircle sx={{ mr: 1 }} /> Profil
          </MenuItem>
          <MenuItem onClick={handleMenuClose}>
            <Settings sx={{ mr: 1 }} /> Ayarlar
          </MenuItem>
          <MenuItem onClick={handleLogout}>
            <Logout sx={{ mr: 1 }} /> Çıkış Yap
          </MenuItem>
        </Menu>

        {/* Notification Menu */}
        <Menu
          anchorEl={notificationAnchor}
          open={Boolean(notificationAnchor)}
          onClose={handleNotificationClose}
          onClick={handleNotificationClose}
        >
          <MenuItem>
            <Box>
              <Typography variant="body2" fontWeight="bold">
                Senkronizasyon Tamamlandı
              </Typography>
              <Typography variant="caption" color="text.secondary">
                5 dakika önce
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem>
            <Box>
              <Typography variant="body2" fontWeight="bold">
                Stok Seviyesi Düşük
              </Typography>
              <Typography variant="caption" color="text.secondary">
                15 dakika önce
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem>
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
