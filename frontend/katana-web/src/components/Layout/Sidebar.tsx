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
import React, { useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { styled } from "@mui/material/styles";
import { decodeJwtPayload, getJwtRoles } from "../../utils/jwt";

const drawerWidth = 280;

const AnimatedHeader = styled(Box)(({ theme }) => ({
  padding: theme.spacing(3),
  display: "flex",
  alignItems: "center",
  justifyContent: "space-between",
  background: "#ffffff",
  borderBottom: `1px solid ${theme.palette.divider}`,
}));

const GlowingText = styled(Typography)(({ theme }) => ({
  fontWeight: 800,
  letterSpacing: "-0.02em",
  background: "linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%)",
  backgroundClip: "text",
  WebkitBackgroundClip: "text",
  WebkitTextFillColor: "transparent",
}));

const AnimatedIconButton = styled(IconButton)(({ theme }) => ({
  color: theme.palette.text.secondary,
  transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
  "&:hover": {
    transform: "rotate(180deg) scale(1.15)",
    color: "#667eea",
  },
}));

const AnimatedListItem = styled(ListItem)<{ index: number }>(({ index }) => ({
  marginBottom: 12,
  animation: `slideIn 0.5s ease-out ${index * 0.08}s both`,
}));

const ColorfulDivider = styled(Divider)(({ theme }) => ({
  opacity: 0.1,
}));

const AnimatedFooter = styled(Box)(({ theme }) => ({
  padding: 24,
  textAlign: "center",
  borderTop: `1px solid ${theme.palette.divider}`,
}));

const GradientFooterText = styled(Typography)({
  fontWeight: 600,
  letterSpacing: "0.05em",
  fontSize: "0.75rem",
  background: "linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%)",
  backgroundClip: "text",
  WebkitBackgroundClip: "text",
  WebkitTextFillColor: "transparent",
});

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

const menuItems = [
  {
    text: "Dashboard",
    icon: <DashboardIcon />,
    path: "/",
    gradient: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
    color: "#667eea",
    roles: ["Admin", "StockManager", "Staff"], // Herkes erişebilir
  },
  {
    text: "Canlı Stok",
    icon: <InventoryIcon />,
    path: "/stock-view",
    gradient: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
    color: "#f093fb",
    roles: ["Admin", "StockManager", "Staff"], // Herkes erişebilir
  },
  {
    text: "Admin Paneli",
    icon: <AdminIcon />,
    path: "/admin",
    gradient: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
    color: "#4facfe",
    roles: ["Admin"], // Sadece Admin
  },
  {
    text: "Stok Yönetimi",
    icon: <InventoryIcon />,
    path: "/stock",
    gradient: "linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)",
    color: "#43e97b",
    roles: ["Admin", "StockManager"], // Admin ve StockManager
  },
  {
    text: "Senkronizasyon",
    icon: <SyncIcon />,
    path: "/sync",
    gradient: "linear-gradient(135deg, #fa709a 0%, #fee140 100%)",
    color: "#fa709a",
    roles: ["Admin", "StockManager"], // Admin ve StockManager
  },
  {
    text: "Raporlar",
    icon: <ReportsIcon />,
    path: "/reports",
    gradient: "linear-gradient(135deg, #30cfd0 0%, #330867 100%)",
    color: "#30cfd0",
    roles: ["Admin", "StockManager"], // Admin ve StockManager
  },
];

const Sidebar: React.FC<SidebarProps> = ({ open, onClose }) => {
  const theme = useTheme();
  const navigate = useNavigate();
  const location = useLocation();

  // Kullanıcının rollerini al
  const userRoles = useMemo(() => {
    const token = localStorage.getItem("authToken");
    if (!token) return [];
    const payload = decodeJwtPayload(token);
    if (!payload) return [];
    return getJwtRoles(payload);
  }, []);

  // Kullanıcının erişebileceği menüleri filtrele
  const accessibleMenuItems = useMemo(() => {
    return menuItems.filter((item) =>
      item.roles.some((role) =>
        userRoles.some((userRole) => userRole.toLowerCase() === role.toLowerCase())
      )
    );
  }, [userRoles]);

  const handleNavigation = (path: string) => {
    navigate(path);
  };

  const handleAdminClick = () => {
    // Token varsa direkt admin paneline git
    const token = localStorage.getItem("authToken");
    if (token) {
      navigate("/admin");
    } else {
      // Token yoksa login'e yönlendir
      navigate("/login");
    }
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
          background: "#ffffff",
          borderRight: `1px solid ${theme.palette.divider}`,
          boxShadow: "0 0 40px rgba(0,0,0,0.08)",
          transition: "all 0.4s cubic-bezier(0.4, 0, 0.2, 1)",
        },
      }}
    >
      <AnimatedHeader>
        <GlowingText variant="h6">⚡ Katana Stok</GlowingText>
        <AnimatedIconButton onClick={onClose} size="medium">
          <ChevronLeftIcon />
        </AnimatedIconButton>
      </AnimatedHeader>

      <ColorfulDivider />

      <List sx={{ flexGrow: 1, pt: 2, px: 1 }}>
        {accessibleMenuItems.map((item, index) => (
          <AnimatedListItem key={item.text} disablePadding index={index}>
            <ListItemButton
              onClick={() =>
                item.path === "/admin"
                  ? handleAdminClick()
                  : handleNavigation(item.path)
              }
              selected={location.pathname === item.path}
              sx={{
                mx: 1,
                borderRadius: 4,
                py: 1.8,
                px: 2.5,
                transition: "all 0.4s cubic-bezier(0.4, 0, 0.2, 1)",
                position: "relative",
                overflow: "visible",
                background:
                  location.pathname === item.path
                    ? item.gradient
                    : "transparent",
                color: location.pathname === item.path ? "#ffffff" : "inherit",
                boxShadow:
                  location.pathname === item.path
                    ? `0 8px 32px ${item.color}50, 0 0 0 2px ${item.color}30`
                    : "none",
                transform:
                  location.pathname === item.path
                    ? "translateX(8px) scale(1.02)"
                    : "translateX(0) scale(1)",
                "& .MuiListItemIcon-root": {
                  color:
                    location.pathname === item.path ? "#ffffff" : item.color,
                  transform:
                    location.pathname === item.path
                      ? "scale(1.15)"
                      : "scale(1)",
                  filter:
                    location.pathname === item.path
                      ? "drop-shadow(0 2px 8px rgba(0,0,0,0.3))"
                      : "none",
                  transition: "all 0.4s cubic-bezier(0.4, 0, 0.2, 1)",
                },
                "& .MuiListItemText-primary": {
                  fontWeight: location.pathname === item.path ? 800 : 600,
                  color:
                    location.pathname === item.path ? "#ffffff" : "inherit",
                },
                "&::after":
                  location.pathname === item.path
                    ? {
                        content: '""',
                        position: "absolute",
                        left: 0,
                        top: "50%",
                        transform: "translateY(-50%)",
                        width: 5,
                        height: "70%",
                        background: "#ffffff",
                        borderRadius: "0 3px 3px 0",
                        boxShadow: "0 0 15px rgba(255,255,255,0.8)",
                      }
                    : {},
                "&:hover": {
                  transform: "translateX(12px) scale(1.03)",
                  boxShadow: `0 12px 40px ${item.color}40`,
                  background:
                    location.pathname !== item.path
                      ? `linear-gradient(135deg, ${item.color}15, ${item.color}10)`
                      : item.gradient,
                  "& .MuiListItemIcon-root": {
                    transform: "scale(1.25) rotate(10deg)",
                    color: item.color,
                  },
                  "& .MuiListItemText-primary": {
                    color:
                      location.pathname === item.path ? "#ffffff" : item.color,
                    fontWeight: 700,
                  },
                },
              }}
            >
              <ListItemIcon
                sx={{ minWidth: 48, position: "relative", zIndex: 1 }}
              >
                {item.icon}
              </ListItemIcon>
              <ListItemText
                primary={item.text}
                primaryTypographyProps={{
                  fontSize: "0.95rem",
                  letterSpacing: "0.01em",
                  position: "relative",
                  zIndex: 1,
                }}
              />
            </ListItemButton>
          </AnimatedListItem>
        ))}
      </List>

      <ColorfulDivider />

      <AnimatedFooter>
        <GradientFooterText variant="caption">
          🚀 Katana Integration v1.0
        </GradientFooterText>
      </AnimatedFooter>
    </Drawer>
  );
};

export default Sidebar;
