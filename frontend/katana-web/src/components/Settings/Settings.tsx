import React from "react";
import { Container, Typography, Box } from "@mui/material";

const Settings: React.FC = () => {
  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ textAlign: "center", py: 8 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Ayarlar
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Bu sayfa geliştirme aşamasındadır.
        </Typography>
      </Box>
    </Container>
  );
};

export default Settings;
