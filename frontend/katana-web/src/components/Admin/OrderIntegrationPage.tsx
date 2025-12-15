import { Receipt, ShoppingCart } from "@mui/icons-material";
import {
  Box,
  Chip,
  Paper,
  Tab,
  Tabs,
  useMediaQuery,
  Typography
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import React, { useState } from "react";
import PurchaseOrders from "./PurchaseOrders";
import SalesOrders from "./SalesOrders";

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`order-tabpanel-${index}`}
      aria-labelledby={`order-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
    </div>
  );
}

const OrderIntegrationPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  return (
    <Box sx={{ width: "100%", pt: 2 }}>
      {/* Compact Hero Section - Header + Tabs as single block */}
      <Box 
        sx={{ 
          maxWidth: "1400px",
          mx: "auto",
          px: { xs: 1.5, sm: 3 },
        }}
      >
        {/* Single compact block: Title + Description + Tabs */}
        <Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
          {/* Title Row */}
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 2,
              mb: 0.5,
              flexWrap: "wrap",
            }}
          >
            <Typography
              variant="h4"
              sx={{ fontWeight: 600, fontSize: { xs: "1.4rem", sm: "2rem" } }}
            >
              Sipariş Entegrasyonu
            </Typography>
            <Chip 
              label="Katana → Koza" 
              size="small" 
              color="primary" 
              variant="outlined"
            />
          </Box>
          
          {/* Description */}
          <Typography variant="body2" color="textSecondary" sx={{ mb: 1.5 }}>
            Katana'daki satış ve satınalma siparişlerini Koza'ya entegre edin
          </Typography>

          {/* Tabs - Directly below description */}
          <Paper elevation={1}>
            <Tabs
              value={activeTab}
              onChange={handleTabChange}
              indicatorColor="primary"
              textColor="primary"
              variant="fullWidth"
              sx={{
                borderBottom: 1,
                borderColor: "divider",
              }}
            >
              <Tab
                icon={<ShoppingCart />}
                iconPosition={isMobile ? "top" : "start"}
                label={isMobile ? "Sipariş" : "Sipariş Entegrasyon Paneli"}
                id="order-tab-0"
                aria-controls="order-tabpanel-0"
                sx={{
                  textTransform: "none",
                  fontSize: { xs: "0.75rem", sm: "0.95rem" },
                  py: { xs: 1, sm: 2 },
                  minHeight: { xs: 56, sm: 72 },
                  "& .MuiTab-iconWrapper": { mb: { xs: 0.25, sm: 0 } },
                }}
              />
              <Tab
                icon={<Receipt />}
                iconPosition={isMobile ? "top" : "start"}
                label={isMobile ? "Tedarik" : "Tedarik Entegrasyon Paneli"}
                id="order-tab-1"
                aria-controls="order-tabpanel-1"
                sx={{
                  textTransform: "none",
                  fontSize: { xs: "0.75rem", sm: "0.95rem" },
                  py: { xs: 1, sm: 2 },
                  minHeight: { xs: 56, sm: 72 },
                  "& .MuiTab-iconWrapper": { mb: { xs: 0.25, sm: 0 } },
                }}
              />
            </Tabs>
          </Paper>
        </Box>
      </Box>

      {/* Tab Panels - Content Area with minimal top spacing */}
      <Box sx={{ maxWidth: "1400px", mx: "auto", px: { xs: 1.5, sm: 3 } }}>
        <TabPanel value={activeTab} index={0}>
          <SalesOrders />
        </TabPanel>
        <TabPanel value={activeTab} index={1}>
          <PurchaseOrders />
        </TabPanel>
      </Box>
    </Box>
  );
};

export default OrderIntegrationPage;
