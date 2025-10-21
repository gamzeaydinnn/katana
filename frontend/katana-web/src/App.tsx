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
import BranchSelector, { Branch } from "./components/Luca/BranchSelector";

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
      const branches = await getBranchList();
      if (Array.isArray(branches) && branches.length > 0) {
        // Auto-selection rules
        const preferredId = localStorage.getItem("preferredBranchId");

        // If only one branch, select it
        if (branches.length === 1) {
          const sel = branches[0];
          await selectBranch(sel);
          console.log("Otomatik: tek şube bulundu ve seçildi");
          return;
        }

        // If preferredId stored, try to match
        if (preferredId) {
          const found = branches.find(
            (b) =>
              String(
                b.id ?? b.Id ?? b.branchId ?? b.orgSirketSubeId ?? b.companyId
              ) === String(preferredId)
          );
          if (found) {
            await selectBranch(found);
            console.log("Otomatik: tercih edilen şube bulundu ve seçildi");
            return;
          }
        }

        // Name-matching rule (e.g., contains 'Merkez')
        const byName = branches.find((b) => {
          const name = String(
            b.sirketSubeAdi ?? b.ack ?? b.name ?? ""
          ).toLowerCase();
          return name.includes("merkez");
        });
        if (byName) {
          await selectBranch(byName);
          console.log("Otomatik: isim eşleşmesine göre şube seçildi");
          return;
        }

        // Otherwise, show UI for user to pick
        setBranchesToSelect(branches as Branch[]);
        setShowBranchSelector(true);
      }
    }
  };

  const [branchesToSelect, setBranchesToSelect] = React.useState<
    Branch[] | null
  >(null);
  const [showBranchSelector, setShowBranchSelector] = React.useState(false);
  const [currentBranchName, setCurrentBranchName] = React.useState<
    string | null
  >(null);

  const handleBranchSelect = async (b: Branch) => {
    setShowBranchSelector(false);
    setBranchesToSelect(null);
    // store preferred id
    const branchId =
      b.id ?? b.Id ?? b.branchId ?? b.orgSirketSubeId ?? b.companyId;
    if (branchId) localStorage.setItem("preferredBranchId", String(branchId));
    const ok = await selectBranch(b);
    if (ok) console.log("Şube seçildi:", branchId);
    else console.error("Şube seçimi başarısız.");
    // store/display name
    const name = b.sirketSubeAdi ?? b.ack ?? b.name ?? b.label ?? null;
    if (name) setCurrentBranchName(String(name));
  };

  const openBranchSelector = async () => {
    if (!branchesToSelect) {
      const branches = await getBranchList();
      if (Array.isArray(branches) && branches.length > 0)
        setBranchesToSelect(branches as Branch[]);
    }
    setShowBranchSelector(true);
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
                      currentBranchName={currentBranchName}
                      onOpenBranchSelector={openBranchSelector}
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
                      {/* Floating control to open branch selector */}
                      {/* removed floating button; header now contains branch control */}
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
                    <BranchSelector
                      open={showBranchSelector}
                      onClose={() => setShowBranchSelector(false)}
                      branches={branchesToSelect ?? []}
                      onSelect={handleBranchSelect}
                    />
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
