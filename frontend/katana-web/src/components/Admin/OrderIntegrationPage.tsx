import { Receipt, ShoppingCart } from "@mui/icons-material";
import {
    Box,
    Chip,
    Paper,
    Tab,
    Tabs,
    Typography
} from "@mui/material";
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
          px: 3,
        }}
      >
        {/* Single compact block: Title + Description + Tabs */}
        <Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
          {/* Title Row */}
          <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 0.5 }}>
            <Typography variant="h4" sx={{ fontWeight: 600 }}>
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
                iconPosition="start"
                label="Sipariş Entegrasyon Paneli"
                id="order-tab-0"
                aria-controls="order-tabpanel-0"
                sx={{ textTransform: "none", fontSize: "0.95rem", py: 2 }}
              />
              <Tab
                icon={<Receipt />}
                iconPosition="start"
                label="Tedarik Entegrasyon Paneli"
                id="order-tab-1"
                aria-controls="order-tabpanel-1"
                sx={{ textTransform: "none", fontSize: "0.95rem", py: 2 }}
              />
            </Tabs>
          </Paper>
        </Box>
      </Box>

      {/* Tab Panels - Content Area with minimal top spacing */}
      <Box sx={{ maxWidth: "1400px", mx: "auto", px: 3 }}>
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
