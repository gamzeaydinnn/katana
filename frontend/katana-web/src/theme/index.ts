import { alpha, createTheme } from "@mui/material/styles";
import type { PaletteMode } from "@mui/material";

export type ColorMode = PaletteMode;

export const createAppTheme = (mode: ColorMode) => {
  const isDark = mode === "dark";

  return createTheme({
    palette: {
      mode,
      primary: {
        main: isDark ? "#8b5cf6" : "#6366f1", // violet-indigo
      },
      secondary: {
        main: isDark ? "#06b6d4" : "#0ea5e9", // cyan-sky
      },
      success: { main: "#10b981" },
      warning: { main: "#f59e0b" },
      error: { main: "#ef4444" },
      background: {
        default: isDark ? "#0b1220" : "#f8fafc",
        paper: isDark
          ? alpha("#0b1220", 0.7)
          : alpha("#ffffff", 0.7),
      },
      text: {
        primary: isDark ? "#e5e7eb" : "#1e293b",
        secondary: isDark ? "#9ca3af" : "#64748b",
      },
      divider: isDark ? alpha("#ffffff", 0.12) : alpha("#000000", 0.08),
    },
    typography: {
      fontFamily: '"Inter", "Segoe UI", "Roboto", sans-serif',
      h1: { fontWeight: 800, letterSpacing: "-0.03em" },
      h2: { fontWeight: 800, letterSpacing: "-0.02em" },
      h3: { fontWeight: 700, letterSpacing: "-0.02em" },
      h4: { fontWeight: 700, letterSpacing: "-0.01em" },
      h5: { fontWeight: 600 },
      h6: { fontWeight: 600 },
      button: { fontWeight: 600, letterSpacing: "0.02em", textTransform: "none" },
    },
    shape: { borderRadius: 14 },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            background: "transparent",
          },
          "::selection": {
            background: alpha("#6366f1", 0.3),
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backdropFilter: "blur(10px)",
            backgroundImage: "none",
            border: `1px solid ${isDark ? "rgba(255,255,255,0.08)" : "rgba(0,0,0,0.06)"}`,
            boxShadow: isDark
              ? "0 10px 30px rgba(0,0,0,0.35)"
              : "0 10px 30px rgba(0,0,0,0.08)",
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 10,
            padding: "10px 20px",
            boxShadow: "none",
            transition: "transform .15s ease, box-shadow .2s ease",
            "&:hover": {
              transform: "translateY(-1px)",
              boxShadow: isDark
                ? "0 10px 20px rgba(0,0,0,0.35)"
                : "0 8px 18px rgba(99,102,241,0.25)",
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 18,
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: { fontWeight: 600, borderRadius: 8 },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backdropFilter: "blur(8px)",
            backgroundImage: "none",
          },
        },
      },
    },
  });
};

