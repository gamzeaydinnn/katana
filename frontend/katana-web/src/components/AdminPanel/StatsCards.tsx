import React from "react";
import {
  Box,
  Paper,
  Typography,
  useMediaQuery,
  SxProps,
  Theme,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";

export type StatCard = {
  id: string;
  label: string;
  value: string | number;
  icon: React.ReactNode;
  tone: "info" | "success" | "warning" | "error" | "primary";
};

const toneMap: Record<StatCard["tone"], keyof Theme["palette"]> = {
  info: "info",
  success: "success",
  warning: "warning",
  error: "error",
  primary: "primary",
};

interface StatsCardsProps {
  items: StatCard[];
  sx?: SxProps<Theme>;
}

const StatsCards: React.FC<StatsCardsProps> = ({ items, sx }) => {
  const theme = useTheme();
  const isMdUp = useMediaQuery(theme.breakpoints.up("md"));
  const labelColor = "#5B6472";
  const valueColor = "#1F2937";

  return (
    <Box
      sx={{
        display: "grid",
        gap: { xs: 2, md: 2.25 },
        gridTemplateColumns: {
          xs: "repeat(2, minmax(0, 1fr))",
          sm: "repeat(2, minmax(0, 1fr))",
          md: "repeat(auto-fit, minmax(280px, 1fr))",
        },
        ...sx,
      }}
    >
      {items.map((item) => {
        const paletteKey = toneMap[item.tone];
        const color = theme.palette[paletteKey].main;

        return (
          <Paper
            key={item.id}
            elevation={3}
            sx={{
              width: "100%",
              minWidth: 0,
              borderRadius: { xs: 20, md: 28 },
              minHeight: { xs: 92, md: 108 },
              px: { xs: 2, md: 3 },
              py: { xs: 2, md: 2 },
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              boxShadow: theme.shadows[3],
            }}
          >
            <Box
              sx={{
                minWidth: 0,
                display: "flex",
                flexDirection: "column",
                gap: 0.75,
              }}
            >
              <Typography
                variant="body2"
                color="text.secondary"
                noWrap
                sx={{
                  textOverflow: "ellipsis",
                  fontSize: { xs: "0.875rem", md: "0.875rem" },
                  fontWeight: { xs: 500, md: 600 },
                  color: { xs: "text.secondary", md: labelColor },
                }}
              >
                {item.label}
              </Typography>
              <Typography
                variant="h4"
                sx={{
                  fontWeight: { xs: 700, md: 800 },
                  lineHeight: { xs: 1, md: 1.05 },
                  fontSize: { xs: "1.75rem", md: "2.5rem" },
                  color: { xs: "inherit", md: valueColor },
                }}
              >
                {item.value}
              </Typography>
            </Box>

            <Box
              sx={{
                width: { xs: 44, md: 36 },
                height: { xs: 44, md: 36 },
                borderRadius: "50%",
                display: "grid",
                placeItems: "center",
                backgroundColor: alpha(color, 0.12),
                opacity: 0.9,
              }}
            >
              {item.icon}
            </Box>
          </Paper>
        );
      })}
    </Box>
  );
};

export default StatsCards;
