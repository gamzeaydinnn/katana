import React, { useState, useEffect } from "react";
import {
  ThemeProvider,
  createTheme,
  CssBaseline,
  Box,
  Toolbar,
  Button,
} from "@mui/material";
import { BrowserRouter, Routes, Route } from "react-router-dom";

import {
  loginToLuca,
  getBranchList,
  selectBranch,
} from "./services/authService";

// Components
import Header from "./components/Layout/Header";
import Sidebar from "./components/Layout/Sidebar";
import Dashboard from "./components/Dashboard/Dashboard";
import StockManagement from "./components/StockManagement/StockManagement";
import SyncManagement from "./components/SyncManagement/SyncManagement";
import Reports from "./components/Reports/Reports";
import Settings from "./components/Settings/Settings";
import AdminPanel from "./components/AdminPanel/AdminPanel";
import LogsViewer from "./components/AdminPanel/LogsViewer";
import ErrorBoundary from "./components/ErrorBoundary";
import Login from "./components/Login/Login";
import ProtectedRoute from "./components/Auth/ProtectedRoute";

// Professional Theme
const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#2563eb",
      light: "#60a5fa",
      dark: "#1e40af",
    },
    secondary: {
      main: "#7c3aed",
      light: "#a78bfa",
      dark: "#5b21b6",
    },
    success: {
      main: "#10b981",
      light: "#34d399",
      dark: "#059669",
    },
    warning: {
      main: "#f59e0b",
      light: "#fbbf24",
      dark: "#d97706",
    },
    error: {
      main: "#ef4444",
      light: "#f87171",
      dark: "#dc2626",
    },
    background: {
      default: "#f8fafc",
      paper: "#ffffff",
    },
    text: {
      primary: "#1e293b",
      secondary: "#64748b",
    },
  },
  typography: {
    fontFamily: '"Inter", "Segoe UI", "Roboto", sans-serif',
    h4: {
      fontWeight: 700,
      letterSpacing: "-0.02em",
    },
    h5: {
      fontWeight: 600,
      letterSpacing: "-0.01em",
    },
    h6: {
      fontWeight: 600,
      letterSpacing: "-0.01em",
    },
    button: {
      fontWeight: 600,
      letterSpacing: "0.02em",
    },
  },
  shape: {
    borderRadius: 12,
  },
  shadows: [
    "none",
    "0 1px 2px 0 rgb(0 0 0 / 0.05)",
    "0 1px 3px 0 rgb(0 0 0 / 0.1)",
    "0 4px 6px -1px rgb(0 0 0 / 0.1)",
    "0 10px 15px -3px rgb(0 0 0 / 0.1)",
    "0 20px 25px -5px rgb(0 0 0 / 0.1)",
    "0 25px 50px -12px rgb(0 0 0 / 0.25)",
    ...Array(18).fill("none"),
  ] as any,
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: "none",
          borderRadius: 10,
          fontWeight: 600,
          padding: "10px 24px",
          boxShadow: "none",
          "&:hover": {
            boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 16,
          boxShadow: "0 1px 3px 0 rgb(0 0 0 / 0.1)",
          border: "1px solid rgba(0,0,0,0.05)",
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          borderRadius: 8,
        },
      },
    },
  },
});

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);

  // Luca KOZA oturum başlatma fonksiyonu
  const initializeLucaSession = async () => {
    const isLoggedIn = await loginToLuca();
    if (isLoggedIn) {
      const branchId = await getBranchList();
      if (branchId) {
        const selected = await selectBranch(branchId);
        if (selected) {
          console.log("Luca KOZA oturumu ve şube seçimi başarılı!");
        } else {
          console.error("Şube seçimi başarısız.");
        }
      }
    }
  };

  useEffect(() => {
    initializeLucaSession();
  }, []);

  return (
    <ErrorBoundary>
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
                      onMenuClick={() => setSidebarOpen(!sidebarOpen)}
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
                        p: 3,
                        width: {
                          sm: `calc(100% - ${sidebarOpen ? 280 : 0}px)`,
                        },
                        ml: { sm: sidebarOpen ? "280px" : 0 },
                        transition: "all 0.3s ease-in-out",
                        minHeight: "100vh",
                      }}
                    >
                      <Toolbar />
                      <Box sx={{ mt: 2 }}>
                        <Routes>
                          <Route path="/" element={<Dashboard />} />
                          <Route path="/stock" element={<StockManagement />} />
                          <Route path="/sync" element={<SyncManagement />} />
                          <Route path="/reports" element={<Reports />} />
                          <Route path="/settings" element={<Settings />} />
                          <Route path="/admin" element={<AdminPanel />} />
                          <Route path="/admin/logs" element={<LogsViewer />} />
                        </Routes>
                      </Box>
                    </Box>
                  </Box>
                </ProtectedRoute>
              }
            />
          </Routes>
        </BrowserRouter>
      </ThemeProvider>
    </ErrorBoundary>
  );
};

export default App;
