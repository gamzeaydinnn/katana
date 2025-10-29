import React, { useMemo, useState, useEffect } from "react";
import { ThemeProvider, CssBaseline, Box, Toolbar } from "@mui/material";
import { BrowserRouter, Routes, Route } from "react-router-dom";

import {
  loginToLuca,
  getBranchList,
  selectBranch,
} from "./services/authService";
import BranchSelector, { Branch } from "./components/Luca/BranchSelector";

// Layout Components
import Header from "./components/Layout/Header";
import Sidebar from "./components/Layout/Sidebar";
import Dashboard from "./components/Dashboard/Dashboard";
import StockManagement from "./components/StockManagement/StockManagement";
import SyncManagement from "./components/SyncManagement/SyncManagement";
import Reports from "./components/Reports/Reports";
import AdminPanel from "./components/AdminPanel/AdminPanel";
import ErrorBoundary from "./components/ErrorBoundary";
import Login from "./components/Login/Login";
import ProtectedRoute from "./components/Auth/ProtectedRoute";
import { createAppTheme, type ColorMode } from "./theme";

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mode, setMode] = useState<ColorMode>(
    () => (localStorage.getItem("ui-mode") as ColorMode) || "light"
  );

  const theme = useMemo(() => createAppTheme(mode), [mode]);

  const toggleMode = () => {
    setMode((prev) => {
      const next = prev === "light" ? "dark" : "light";
      localStorage.setItem("ui-mode", next);
      return next;
    });
  };

  // 🌿 Branch / session setup
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
        {/* 🌈 Gradient Background */}
        <Box
          sx={{
            position: "fixed",
            inset: 0,
            zIndex: 0,
            pointerEvents: "none",
            background: (t) => `
              radial-gradient(800px 400px at 10% -10%, ${
                t.palette.primary.main
              }26, transparent),
              radial-gradient(600px 300px at 90% 0%, ${
                t.palette.secondary.main
              }22, transparent),
              radial-gradient(600px 300px at 50% 100%, ${
                t.palette.success.main
              }1f, transparent),
              linear-gradient(180deg, ${t.palette.background.default} 0%, ${
              t.palette.mode === "dark" ? "#0b1020" : "#ecf2f7"
            } 100%)
            `,
          }}
        />
        {/* Router Yapısı */}
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
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

                    {/* 🎯 Main Content: Soldan Hizalı */}
                    <Box
                      component="main"
                      sx={{
                        flexGrow: 1,
                        bgcolor: "transparent",
                        p: { xs: 2, sm: 3, md: 4 },
                        transition: "all 0.3s ease",
                        minHeight: "100vh",
                        display: "flex",
                        flexDirection: "column",
                        alignItems: "flex-start",
                      }}
                    >
                      <Toolbar />
                      <Box sx={{ width: "100%", maxWidth: "1440px", mx: 0 }}>
                        <Routes>
                          <Route path="/" element={<Dashboard />} />
                          <Route path="/stock" element={<StockManagement />} />
                          <Route path="/sync" element={<SyncManagement />} />
                          <Route path="/reports" element={<Reports />} />
                          <Route path="/admin" element={<AdminPanel />} />
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
