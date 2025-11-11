import React from "react";
import { Box, Typography, Button } from "@mui/material";
import { useNavigate, useLocation } from "react-router-dom";

const Unauthorized: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as any)?.from?.pathname || "/";

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-start",
        gap: 2,
        p: 2,
      }}
    >
      <Typography variant="h5" color="error" fontWeight={700}>
        Yetkisiz erişim
      </Typography>
      <Typography variant="body1" color="text.secondary">
        Bu alana erişim yetkiniz bulunmuyor. Eğer bunun bir hata olduğunu
        düşünüyorsanız lütfen yönetici ile iletişime geçin.
      </Typography>
      <Box sx={{ display: "flex", gap: 1 }}>
        <Button
          variant="contained"
          color="primary"
          onClick={() => navigate(from)}
          sx={{ fontWeight: 600 }}
        >
          Geri Dön
        </Button>
        <Button
          variant="outlined"
          color="inherit"
          onClick={() => navigate("/")}
          sx={{ fontWeight: 600 }}
        >
          Ana Sayfa
        </Button>
      </Box>
    </Box>
  );
};

export default Unauthorized;
