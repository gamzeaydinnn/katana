import {
  AdminPanelSettings as AdminIcon,
  ChevronLeft as ChevronLeftIcon,
  Dashboard as DashboardIcon,
  Inventory as InventoryIcon,
  Assessment as ReportsIcon,
  Sync as SyncIcon,
} from "@mui/icons-material";
import {
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  useTheme,
} from "@mui/material";
import React from "react";
import { useLocation, useNavigate } from "react-router-dom";

const drawerWidth = 280;

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

const menuItems = [
  { text: "Dashboard", icon: <DashboardIcon />, path: "/" },
  { text: "Canlı Stok", icon: <InventoryIcon />, path: "/stock-view" },
  // Admin panel promoted near the top for quick approvals
  { text: "Admin Paneli", icon: <AdminIcon />, path: "/admin" },
  { text: "Stok Yönetimi", icon: <InventoryIcon />, path: "/stock" },
  { text: "Senkronizasyon", icon: <SyncIcon />, path: "/sync" },
  { text: "Raporlar", icon: <ReportsIcon />, path: "/reports" },
];

const Sidebar: React.FC<SidebarProps> = ({ open, onClose }) => {
  const theme = useTheme();
  const navigate = useNavigate();
  const location = useLocation();

  const handleNavigation = (path: string) => {
    navigate(path);
  };

  return (
    <Drawer
      variant="persistent"
      anchor="left"
      open={open}
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        "& .MuiDrawer-paper": {
          width: drawerWidth,
          boxSizing: "border-box",
          backdropFilter: "blur(20px)",
          backgroundImage:
            theme.palette.mode === "dark"
              ? "linear-gradient(135deg, rgba(30,41,59,0.95) 0%, rgba(15,23,42,0.95) 100%)"
              : "linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(248,250,252,0.95) 100%)",
          borderRight: `1px solid ${
            theme.palette.mode === "dark"
              ? "rgba(255,255,255,0.1)"
              : "rgba(0,0,0,0.05)"
          }`,
          boxShadow:
            theme.palette.mode === "dark"
              ? "0 20px 40px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
              : "0 20px 40px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
          transition: "all 0.3s ease",
        },
      }}
    >
      <Box
        sx={{
          p: 3,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
          backgroundClip: "text",
          WebkitBackgroundClip: "text",
          WebkitTextFillColor: "transparent",
          borderBottom: `1px solid ${
            theme.palette.mode === "dark"
              ? "rgba(255,255,255,0.1)"
              : "rgba(0,0,0,0.05)"
          }`,
        }}
      >
        <Typography
          variant="h6"
          component="div"
          sx={{
            fontWeight: 900,
            letterSpacing: "-0.02em",
            background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
            backgroundClip: "text",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
          }}
        >
          Katana Stok
        </Typography>
        <IconButton
          onClick={onClose}
          sx={{
            color: theme.palette.primary.main,
            transition: "transform 0.2s ease",
            "&:hover": {
              transform: "scale(1.1)",
              backgroundColor: theme.palette.action.hover,
            },
          }}
        >
          <ChevronLeftIcon />
        </IconButton>
      </Box>

      <Divider sx={{ opacity: 0.3 }} />

      <List sx={{ flexGrow: 1, pt: 2, px: 1 }}>
        {menuItems.map((item) => (
          <ListItem key={item.text} disablePadding sx={{ mb: 1 }}>
            <ListItemButton
              onClick={() => handleNavigation(item.path)}
              selected={location.pathname === item.path}
              sx={{
                mx: 1,
                borderRadius: 3,
                py: 1.5,
                px: 2,
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                position: "relative",
                "&.Mui-selected": {
                  background: `linear-gradient(135deg, ${theme.palette.primary.main}20, ${theme.palette.secondary.main}20)`,
                  color: theme.palette.primary.main,
                  boxShadow: `0 0 20px ${theme.palette.primary.main}40`,
                  "& .MuiListItemIcon-root": {
                    color: theme.palette.primary.main,
                  },
                  "&::before": {
                    content: '""',
                    position: "absolute",
                    left: 0,
                    top: "50%",
                    transform: "translateY(-50%)",
                    width: 4,
                    height: "60%",
                    background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
                    borderRadius: "0 2px 2px 0",
                  },
                },
                "&:hover": {
                  backgroundColor: theme.palette.action.hover,
                  transform: "translateX(4px)",
                  boxShadow:
                    theme.palette.mode === "dark"
                      ? "0 8px 16px rgba(0,0,0,0.3)"
                      : "0 8px 16px rgba(0,0,0,0.1)",
                },
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: 48,
                  transition: "transform 0.2s ease",
                  "&:hover": {
                    transform: "scale(1.1)",
                  },
                }}
              >
                {item.icon}
              </ListItemIcon>
              <ListItemText
                primary={item.text}
                primaryTypographyProps={{
                  fontSize: "0.95rem",
                  fontWeight: location.pathname === item.path ? 700 : 500,
                  letterSpacing: "0.01em",
                }}
              />
            </ListItemButton>
          </ListItem>
        ))}
      </List>

      <Divider sx={{ opacity: 0.3 }} />

      <Box
        sx={{
          p: 3,
          textAlign: "center",
          background: `linear-gradient(135deg, ${theme.palette.primary.main}05, ${theme.palette.secondary.main}05)`,
          borderTop: `1px solid ${
            theme.palette.mode === "dark"
              ? "rgba(255,255,255,0.1)"
              : "rgba(0,0,0,0.05)"
          }`,
        }}
      >
        <Typography
          variant="caption"
          sx={{
            color: theme.palette.text.secondary,
            fontWeight: 600,
            letterSpacing: "0.02em",
          }}
        >
          Katana Integration v1.0
        </Typography>
      </Box>
    </Drawer>
  );
};

export default Sidebar;
