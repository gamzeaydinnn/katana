import {
    Box,
    CssBaseline,
    ThemeProvider,
    Toolbar,
    useMediaQuery,
} from "@mui/material";
import React, { useEffect, useMemo, useState } from "react";
import { BrowserRouter, Route, Routes } from "react-router-dom";

import BranchSelector, { Branch } from "./components/Luca/BranchSelector";
import {
    getBranchList,
    loginToLuca,
    selectBranch,
} from "./services/authService";

import AdminPanel from "./components/AdminPanel/AdminPanel";
import ProtectedRoute from "./components/Auth/ProtectedRoute";
import Dashboard from "./components/Dashboard/Dashboard";
import ErrorDebugPanel from "./components/Debug/ErrorDebugPanel";
import ErrorBoundary from "./components/ErrorBoundary";
import Header from "./components/Layout/Header";
import Sidebar from "./components/Layout/Sidebar";
import Login from "./components/Login/Login";
import Profile from "./components/Profile/Profile";
import Reports from "./components/Reports/Reports";
import Settings from "./components/Settings/Settings";

import SyncManagement from "./components/SyncManagement/SyncManagement";
import KozaIntegrationPage from "./pages/KozaIntegrationPage";
import OrderInvoiceSyncPage from "./pages/OrderInvoiceSyncPage";
import StockMovementSyncPage from "./pages/StockMovementSyncPage";
import StockView from "./pages/StockView";
import Unauthorized from "./pages/Unauthorized";
import { FeedbackProvider } from "./providers/FeedbackProvider";
import { createAppTheme, type ColorMode } from "./theme";
import { setupGlobalErrorHandlers } from "./utils/errorLogger";

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mode, setMode] = useState<ColorMode>(
    () => (localStorage.getItem("ui-mode") as ColorMode) || "light"
  );

  const theme = useMemo(() => createAppTheme(mode), [mode]);
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));

  // Setup global error logging
  useEffect(() => {
    setupGlobalErrorHandlers();
  }, []);

  const toggleMode = () => {
    setMode((prev) => {
      const next = prev === "light" ? "dark" : "light";
      localStorage.setItem("ui-mode", next);
      return next;
    });
  };

  const [branchesToSelect, setBranchesToSelect] = useState<Branch[] | null>(
    null
  );
  const [showBranchSelector, setShowBranchSelector] = useState(false);
  const [currentBranchName, setCurrentBranchName] = useState<string | null>(
    null
  );

  const initializeLucaSession = async () => {
    const isLoggedIn = await loginToLuca();
    if (isLoggedIn) {
      const branches = await getBranchList();
      if (!Array.isArray(branches) || branches.length === 0) return;

      const preferredId = localStorage.getItem("preferredBranchId");
      const match =
        branches.find(
          (b) =>
            String(
              b.id ?? b.Id ?? b.branchId ?? b.orgSirketSubeId ?? b.companyId
            ) === preferredId
        ) ||
        branches.find((b) =>
          String(b.sirketSubeAdi ?? b.ack ?? b.name ?? "")
            .toLowerCase()
            .includes("merkez")
        );

      if (match) {
        await selectBranch(match);
        setCurrentBranchName(
          String(match.sirketSubeAdi ?? match.name ?? "Şube")
        );
        return;
      }

      setBranchesToSelect(branches);
      setShowBranchSelector(true);
    }
  };

  useEffect(() => {
    initializeLucaSession();
  }, []);

  useEffect(() => {
    setSidebarOpen(!isMobile);
  }, [isMobile]);

  const handleBranchSelect = async (b: Branch) => {
    setShowBranchSelector(false);
    setBranchesToSelect(null);
    const branchId =
      b.id ?? b.Id ?? b.branchId ?? b.orgSirketSubeId ?? b.companyId;
    if (branchId) localStorage.setItem("preferredBranchId", String(branchId));
    await selectBranch(b);
    setCurrentBranchName(String(b.sirketSubeAdi ?? b.name ?? "Şube"));
  };

  const openBranchSelector = async () => {
    if (!branchesToSelect) {
      const branches = await getBranchList();
      if (Array.isArray(branches) && branches.length > 0)
        setBranchesToSelect(branches);
    }
    setShowBranchSelector(true);
  };

  return (
    <ErrorBoundary>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {}
        <Box
          sx={{
            position: "fixed",
            inset: 0,
            zIndex: 0,
            pointerEvents: "none",
            background: (t) =>
              t.palette.mode === "dark"
                ? `
              radial-gradient(900px 600px at 0% 0%, rgba(148,163,184,0.20), transparent),
              radial-gradient(900px 600px at 100% 100%, rgba(56,189,248,0.18), transparent),
              linear-gradient(180deg, #020617 0%, #020617 100%)
            `
                : `
              radial-gradient(800px 400px at 10% -10%, ${t.palette.primary.main}26, transparent),
              radial-gradient(600px 300px at 90% 0%, ${t.palette.secondary.main}22, transparent),
              radial-gradient(600px 300px at 50% 100%, ${t.palette.success.main}1f, transparent),
              linear-gradient(180deg, ${t.palette.background.default} 0%, #ecf2f7 100%)
            `,
          }}
        />
        {}
        <FeedbackProvider>
          <BrowserRouter>
            <Routes>
              {/* Public route - Login */}
              <Route path="/login" element={<Login />} />
              <Route path="/unauthorized" element={<Unauthorized />} />

              {/* Protected routes - Require authentication */}
              <Route
                path="/*"
                element={
                  <ProtectedRoute>
                    <Box
                      sx={{
                        display: "flex",
                        minHeight: "100vh",
                        position: "relative",
                        zIndex: 1,
                      }}
                    >
                      <Header
                        onMenuClick={() => setSidebarOpen(!sidebarOpen)}
                        sidebarOpen={sidebarOpen}
                        currentBranchName={currentBranchName}
                        onOpenBranchSelector={openBranchSelector}
                        mode={mode}
                        onToggleMode={toggleMode}
                      />
                      <Sidebar
                        open={sidebarOpen}
                        onClose={() => setSidebarOpen(false)}
                      />

                      {}
                      <Box
                        component="main"
                        sx={{
                          flexGrow: 1,
                          bgcolor: "transparent",
                          p: { xs: 1, sm: 2, md: 3, lg: 4 },
                          transition: "all 0.3s ease",
                          minHeight: "100vh",
                          display: "flex",
                          flexDirection: "column",
                          alignItems: "flex-start",
                          width: "100%",
                          maxWidth: "100%",
                          overflowX: "hidden",
                          boxSizing: "border-box",
                        }}
                      >
                        <Toolbar />
                        <Box
                          sx={{
                            width: "100%",
                            maxWidth: "1440px",
                            mx: 0,
                            boxSizing: "border-box",
                          }}
                        >
                          <Routes>
                            <Route path="/" element={<Dashboard />} />
                            <Route path="/stock-view" element={<StockView />} />
                            <Route
                              path="/koza"
                              element={<KozaIntegrationPage />}
                            />
                            <Route path="/sync" element={<SyncManagement />} />
                            <Route
                              path="/order-sync"
                              element={<OrderInvoiceSyncPage />}
                            />
                            <Route
                              path="/stock-movement-sync"
                              element={<StockMovementSyncPage />}
                            />
                            <Route path="/reports" element={<Reports />} />
                            {/* Admin panel - requires Admin/Manager role */}
                            <Route
                              path="/admin"
                              element={
                                <ProtectedRoute requiredRole="Admin">
                                  <AdminPanel />
                                </ProtectedRoute>
                              }
                            />
                            <Route path="/profile" element={<Profile />} />
                            <Route path="/settings" element={<Settings />} />
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
        </FeedbackProvider>
        <ErrorDebugPanel />
      </ThemeProvider>
    </ErrorBoundary>
  );
};

export default App;
