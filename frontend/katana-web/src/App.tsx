import React, { useMemo, useState, useEffect } from "react";
import {
  ThemeProvider,
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
import AdminPanel from "./components/AdminPanel/AdminPanel";
import LogsViewer from "./components/AdminPanel/LogsViewer";
import ErrorBoundary from "./components/ErrorBoundary";
import Login from "./components/Login/Login";
import ProtectedRoute from "./components/Auth/ProtectedRoute";
import { createAppTheme, type ColorMode } from "./theme";

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mode, setMode] = useState<ColorMode>(() =>
    (localStorage.getItem("ui-mode") as ColorMode) || "light"
  );
  const theme = useMemo(() => createAppTheme(mode), [mode]);
  const toggleMode = () => {
    setMode((m) => {
      const next = m === "light" ? "dark" : "light";
      localStorage.setItem("ui-mode", next);
      return next;
    });
  };

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
        {/* Background gradients for a more digital, striking look */}
        <Box
          sx={{
            position: "fixed",
            inset: 0,
            zIndex: 0,
            pointerEvents: "none",
            background: (t) => `
              radial-gradient(800px 400px at 10% -10%, ${t.palette.primary.main}26, transparent),
              radial-gradient(600px 300px at 90% 0%, ${t.palette.secondary.main}22, transparent),
              radial-gradient(600px 300px at 50% 100%, ${t.palette.success.main}1f, transparent),
              linear-gradient(180deg, ${t.palette.background.default} 0%, ${t.palette.mode === 'dark' ? '#0b1020' : '#ecf2f7'} 100%)
            `,
          }}
        />
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <Box sx={{ display: "flex", minHeight: "100vh", position: "relative", zIndex: 1 }}>
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
                    <Box
  component="main"
  sx={{
    flexGrow: 1,
    bgcolor: "transparent",
    p: 3,
    width: "100%",
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
