import React, { useState } from "react";
import {
  ThemeProvider,
  createTheme,
  CssBaseline,
  Box,
  Toolbar,
} from "@mui/material";
import { BrowserRouter, Routes, Route } from "react-router-dom";

// Components
import Header from "./components/Layout/Header";
import Sidebar from "./components/Layout/Sidebar";
import Dashboard from "./components/Dashboard/Dashboard";
import StockManagement from "./components/StockManagement/StockManagement";
import SyncManagement from "./components/SyncManagement/SyncManagement";
import Reports from "./components/Reports/Reports";
import Settings from "./components/Settings/Settings";
import AdminPanel from "./components/AdminPanel/AdminPanel";
import Login from "./components/Login/Login";
import ProtectedRoute from "./components/Auth/ProtectedRoute";

// Create theme
const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#1976d2",
      light: "#42a5f5",
      dark: "#1565c0",
    },
    secondary: {
      main: "#dc004e",
    },
    background: {
      default: "#f5f5f5",
      paper: "#ffffff",
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h4: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: "none",
          borderRadius: 8,
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: "0 2px 12px rgba(0,0,0,0.08)",
        },
      },
    },
  },
});

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const handleSidebarToggle = () => {
    setSidebarOpen(!sidebarOpen);
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <Box sx={{ display: "flex", minHeight: "100vh" }}>
                  <Header
                    onMenuClick={handleSidebarToggle}
                    sidebarOpen={sidebarOpen}
                  />
                  <Sidebar
                    open={sidebarOpen}
                    onClose={() => setSidebarOpen(false)}
                  />
                  <Box
                    component="main"
                    sx={{
                      flexGrow: 1,
                      bgcolor: "background.default",
                      p: 0,
                      width: { sm: `calc(100% - ${sidebarOpen ? 280 : 0}px)` },
                      ml: { sm: sidebarOpen ? "280px" : 0 },
                      transition: theme.transitions.create(
                        ["margin", "width"],
                        {
                          easing: theme.transitions.easing.sharp,
                          duration: theme.transitions.duration.leavingScreen,
                        }
                      ),
                    }}
                  >
                    <Toolbar />
                    <Routes>
                      <Route path="/" element={<Dashboard />} />
                      <Route path="/stock" element={<StockManagement />} />
                      <Route path="/sync" element={<SyncManagement />} />
                      <Route path="/reports" element={<Reports />} />
                      <Route path="/settings" element={<Settings />} />
                      <Route path="/admin" element={<AdminPanel />} />
                    </Routes>
                  </Box>
                </Box>
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
};

export default App;
