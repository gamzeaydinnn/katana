import type { PaletteMode } from "@mui/material";
import { alpha, createTheme } from "@mui/material/styles";

export type ColorMode = PaletteMode;

export const createAppTheme = (mode: ColorMode) => {
  const isDark = mode === "dark";

  // Professional color palette - Katana brand colors
  const primaryColor = isDark ? "#6366f1" : "#4f46e5"; // Indigo
  const secondaryColor = isDark ? "#06b6d4" : "#0891b2"; // Cyan

  return createTheme({
    palette: {
      mode,
      primary: {
        main: primaryColor,
        light: isDark ? "#818cf8" : "#6366f1",
        dark: isDark ? "#4f46e5" : "#3730a3",
        contrastText: "#ffffff",
      },
      secondary: {
        main: secondaryColor,
        light: isDark ? "#22d3ee" : "#06b6d4",
        dark: isDark ? "#0891b2" : "#0e7490",
        contrastText: "#ffffff",
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
      info: {
        main: "#3b82f6",
        light: "#60a5fa",
        dark: "#2563eb",
      },
      background: {
        default: isDark ? "#020617" : "#f8fafc",
        paper: isDark ? alpha("#0f172a", 0.98) : alpha("#ffffff", 0.98),
      },
      text: {
        primary: isDark ? "#f1f5f9" : "#0f172a",
        secondary: isDark ? "#94a3b8" : "#475569",
      },
      divider: isDark ? alpha("#ffffff", 0.08) : alpha("#000000", 0.06),
      grey: {
        50: "#f8fafc",
        100: "#f1f5f9",
        200: "#e2e8f0",
        300: "#cbd5e1",
        400: "#94a3b8",
        500: "#64748b",
        600: "#475569",
        700: "#334155",
        800: "#1e293b",
        900: "#0f172a",
      },
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
      button: {
        fontWeight: 600,
        letterSpacing: "0.02em",
        textTransform: "none",
      },
    },
    shape: { borderRadius: 16 },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: isDark ? "#020617" : "#f1f5f9",
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
            border: `1px solid ${
              isDark ? "rgba(255,255,255,0.1)" : "rgba(0,0,0,0.05)"
            }`,
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
            fontWeight: 600,
            transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
            "@media (max-width: 600px)": {
              padding: "10px 18px",
              fontSize: "0.875rem",
              borderRadius: 10,
            },
          },
          contained: {
            background: isDark
              ? `linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%)`
              : `linear-gradient(135deg, ${primaryColor} 0%, ${secondaryColor} 100%)`,
            color: "#ffffff",
            "&:hover": {
              transform: "translateY(-2px)",
              boxShadow: isDark
                ? "0 12px 24px rgba(99,102,241,0.35)"
                : "0 12px 24px rgba(79,70,229,0.25)",
              "@media (hover: none)": {
                transform: "none",
              },
            },
            "&:active": {
              transform: "translateY(0)",
            },
          },
          outlined: {
            background: "transparent",
            border: `2px solid ${isDark ? "#6366f1" : "#4f46e5"}`,
            color: isDark ? "#6366f1" : "#4f46e5",
            "&:hover": {
              background: alpha(isDark ? "#6366f1" : "#4f46e5", 0.08),
              borderColor: isDark ? "#818cf8" : "#6366f1",
            },
          },
          text: {
            color: isDark ? "#6366f1" : "#4f46e5",
            "&:hover": {
              background: alpha(isDark ? "#6366f1" : "#4f46e5", 0.08),
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 16,
            transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
            "@media (max-width: 600px)": {
              borderRadius: 12,
            },
            "&:hover": {
              transform: "translateY(-4px)",
              "@media (hover: none)": {
                transform: "none",
              },
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
            borderBottom: `1px solid ${
              isDark ? "rgba(255,255,255,0.1)" : "rgba(0,0,0,0.05)"
            }`,
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
              "@media (max-width: 600px)": {
                borderRadius: 10,
              },
              "&:hover .MuiOutlinedInput-notchedOutline": {
                borderColor: isDark ? "#6366f1" : "#4f46e5",
              },
              "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
                borderWidth: 2,
                boxShadow: `0 0 0 3px ${alpha(
                  isDark ? "#6366f1" : "#4f46e5",
                  0.15
                )}`,
              },
            },
          },
        },
      },
      MuiTabs: {
        styleOverrides: {
          root: {
            minHeight: 48,
            "@media (max-width: 600px)": {
              minHeight: 40,
            },
          },
        },
      },
      MuiTab: {
        styleOverrides: {
          root: {
            textTransform: "none",
            fontWeight: 600,
            fontSize: "0.875rem",
            minHeight: 48,
            padding: "12px 20px",
            borderRadius: 10,
            "@media (max-width: 600px)": {
              minHeight: 40,
              padding: "8px 12px",
              fontSize: "0.75rem",
              minWidth: "auto",
            },
            "&.Mui-selected": {
              background: alpha(isDark ? "#6366f1" : "#4f46e5", 0.1),
            },
          },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: {
            borderRadius: 10,
            transition: "all 0.2s ease",
            "@media (max-width: 600px)": {
              padding: 10,
            },
          },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: {
            borderRadius: 20,
            "@media (max-width: 600px)": {
              borderRadius: 16,
              margin: 16,
              width: "calc(100% - 32px)",
              maxWidth: "none",
            },
          },
        },
      },
      MuiAlert: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            "@media (max-width: 600px)": {
              borderRadius: 10,
              padding: "8px 12px",
            },
          },
        },
      },
      MuiTooltip: {
        styleOverrides: {
          tooltip: {
            borderRadius: 8,
            fontSize: "0.75rem",
            padding: "8px 12px",
          },
        },
      },
    },
  });
};
