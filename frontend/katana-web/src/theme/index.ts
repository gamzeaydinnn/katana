import type { PaletteMode } from "@mui/material";
import { alpha, createTheme } from "@mui/material/styles";

export type ColorMode = PaletteMode;

export const createAppTheme = (mode: ColorMode) => {
  const isDark = mode === "dark";

  return createTheme({
    palette: {
      mode,
      primary: {
        main: isDark ? "#6366f1" : "#4f46e5", // deeper indigo
        light: isDark ? "#818cf8" : "#6366f1",
        dark: isDark ? "#4f46e5" : "#3730a3",
      },
      secondary: {
        main: isDark ? "#06b6d4" : "#0891b2", // deeper cyan
        light: isDark ? "#22d3ee" : "#06b6d4",
        dark: isDark ? "#0891b2" : "#0e7490",
      },
      success: { main: "#10b981" },
      warning: { main: "#f59e0b" },
      error: { main: "#ef4444" },
      background: {
        default: isDark ? "#0f172a" : "#f1f5f9", // darker for dark mode, lighter for light
        paper: isDark
          ? alpha("#1e293b", 0.8)
          : alpha("#ffffff", 0.9),
      },
      text: {
        primary: isDark ? "#f1f5f9" : "#0f172a",
        secondary: isDark ? "#94a3b8" : "#475569",
      },
      divider: isDark ? alpha("#ffffff", 0.1) : alpha("#000000", 0.08),
    },
    typography: {
      fontFamily: '"Inter", "Segoe UI", "Roboto", sans-serif',
      h1: { fontWeight: 900, letterSpacing: "-0.04em", lineHeight: 1.1 },
      h2: { fontWeight: 800, letterSpacing: "-0.03em", lineHeight: 1.2 },
      h3: { fontWeight: 700, letterSpacing: "-0.02em", lineHeight: 1.3 },
      h4: { fontWeight: 700, letterSpacing: "-0.01em", lineHeight: 1.4 },
      h5: { fontWeight: 600, lineHeight: 1.5 },
      h6: { fontWeight: 600, lineHeight: 1.6 },
      body1: { lineHeight: 1.7 },
      body2: { lineHeight: 1.6 },
      button: { fontWeight: 600, letterSpacing: "0.02em", textTransform: "none" },
    },
    shape: { borderRadius: 16 },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            background: "transparent",
            transition: "background 0.3s ease",
          },
          "::selection": {
            background: alpha("#6366f1", 0.4),
          },
          "*": {
            transition: "color 0.2s ease, background-color 0.2s ease",
          },
        },
      },
      MuiTypography: {
        defaultProps: {
          translate: "no",
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backdropFilter: "blur(20px)",
            backgroundImage: isDark
              ? "linear-gradient(135deg, rgba(30,41,59,0.9) 0%, rgba(15,23,42,0.9) 100%)"
              : "linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(248,250,252,0.95) 100%)",
            border: `1px solid ${isDark ? "rgba(255,255,255,0.1)" : "rgba(0,0,0,0.05)"}`,
            boxShadow: isDark
              ? "0 20px 40px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
              : "0 20px 40px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
            transition: "box-shadow 0.3s ease, transform 0.2s ease",
            "&:hover": {
              transform: "translateY(-2px)",
              boxShadow: isDark
                ? "0 25px 50px rgba(0,0,0,0.5), inset 0 1px 0 rgba(255,255,255,0.15)"
                : "0 25px 50px rgba(0,0,0,0.15), inset 0 1px 0 rgba(255,255,255,0.9)",
            },
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            padding: "12px 24px",
            boxShadow: "none",
            transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
            background: isDark
              ? "linear-gradient(135deg, #6366f1 0%, #06b6d4 100%)"
              : "linear-gradient(135deg, #4f46e5 0%, #0891b2 100%)",
            "&:hover": {
              transform: "translateY(-2px) scale(1.02)",
              boxShadow: isDark
                ? "0 15px 30px rgba(99,102,241,0.4)"
                : "0 15px 30px rgba(79,70,229,0.3)",
            },
            "&:active": {
              transform: "translateY(0) scale(1)",
            },
          },
          outlined: {
            background: "transparent",
            border: `2px solid ${isDark ? "#6366f1" : "#4f46e5"}`,
            "&:hover": {
              background: alpha(isDark ? "#6366f1" : "#4f46e5", 0.1),
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 20,
            transition: "transform 0.3s ease, box-shadow 0.3s ease",
            "&:hover": {
              transform: "translateY(-4px)",
            },
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            fontWeight: 600,
            borderRadius: 10,
            transition: "all 0.2s ease",
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backdropFilter: "blur(20px)",
            backgroundImage: isDark
              ? "linear-gradient(135deg, rgba(15,23,42,0.95) 0%, rgba(30,41,59,0.95) 100%)"
              : "linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(241,245,249,0.95) 100%)",
            borderBottom: `1px solid ${isDark ? "rgba(255,255,255,0.1)" : "rgba(0,0,0,0.05)"}`,
            boxShadow: "none",
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: {
            transition: "background-color 0.2s ease",
            "&:hover": {
              backgroundColor: alpha(isDark ? "#ffffff" : "#000000", 0.04),
            },
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            "& .MuiOutlinedInput-root": {
              borderRadius: 12,
              transition: "box-shadow 0.3s ease",
              "&:hover .MuiOutlinedInput-notchedOutline": {
                borderColor: isDark ? "#6366f1" : "#4f46e5",
              },
              "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
                borderWidth: 2,
                boxShadow: `0 0 0 3px ${alpha(isDark ? "#6366f1" : "#4f46e5", 0.2)}`,
              },
            },
          },
        },
      },
    },
  });
};

