import React, { useState } from "react";
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Switch,
  FormControlLabel,
  Divider,
  Alert,
  IconButton,
  InputAdornment,
  Stack,
} from "@mui/material";
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Visibility,
  VisibilityOff,
  Key as KeyIcon,
  Api as ApiIcon,
  Sync as SyncIcon,
} from "@mui/icons-material";

interface SettingsState {
  katanaApiKey: string;
  lucaApiKey: string;
  autoSync: boolean;
  syncInterval: number;
  showApiKey: boolean;
}

const Settings: React.FC = () => {
  const [settings, setSettings] = useState<SettingsState>({
    katanaApiKey: "ed8c38d1-4015-45e5-9c28-381d3fe148b6",
    lucaApiKey: "",
    autoSync: true,
    syncInterval: 60,
    showApiKey: false,
  });
  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    setSaved(true);
    setTimeout(() => setSaved(false), 3000);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box
        sx={{
          mb: 4,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
        }}
      >
        <Typography variant="h4" fontWeight={700}>
          Ayarlar
        </Typography>
        <Button
          variant="contained"
          startIcon={<SaveIcon />}
          onClick={handleSave}
          sx={{ px: 3, fontWeight: 600 }}
        >
          Kaydet
        </Button>
      </Box>

      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          Ayarlar başarıyla kaydedildi!
        </Alert>
      )}

      <Stack spacing={3}>
        {/* API Ayarları ve Senkronizasyon yan yana */}
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", lg: "1fr 1fr" },
            gap: 3,
          }}
        >
          {/* API Ayarları */}
          <Card>
            <CardContent>
              <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                <ApiIcon sx={{ mr: 1, color: "primary.main" }} />
                <Typography variant="h6" fontWeight={600}>
                  API Ayarları
                </Typography>
              </Box>
              <Divider sx={{ mb: 3 }} />

              <TextField
                fullWidth
                label="Katana API Key"
                value={settings.katanaApiKey}
                onChange={(e) =>
                  setSettings({ ...settings, katanaApiKey: e.target.value })
                }
                type={settings.showApiKey ? "text" : "password"}
                sx={{ mb: 3 }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <KeyIcon />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() =>
                          setSettings({
                            ...settings,
                            showApiKey: !settings.showApiKey,
                          })
                        }
                        edge="end"
                      >
                        {settings.showApiKey ? (
                          <VisibilityOff />
                        ) : (
                          <Visibility />
                        )}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />

              <TextField
                fullWidth
                label="Luca API Key (Opsiyonel)"
                value={settings.lucaApiKey}
                onChange={(e) =>
                  setSettings({ ...settings, lucaApiKey: e.target.value })
                }
                type={settings.showApiKey ? "text" : "password"}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <KeyIcon />
                    </InputAdornment>
                  ),
                }}
              />
            </CardContent>
          </Card>

          {/* Senkronizasyon Ayarları */}
          <Card>
            <CardContent>
              <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                <SyncIcon sx={{ mr: 1, color: "secondary.main" }} />
                <Typography variant="h6" fontWeight={600}>
                  Senkronizasyon
                </Typography>
              </Box>
              <Divider sx={{ mb: 3 }} />

              <FormControlLabel
                control={
                  <Switch
                    checked={settings.autoSync}
                    onChange={(e) =>
                      setSettings({ ...settings, autoSync: e.target.checked })
                    }
                  />
                }
                label="Otomatik Senkronizasyon"
                sx={{ mb: 3 }}
              />

              <TextField
                fullWidth
                label="Senkronizasyon Aralığı (dakika)"
                type="number"
                value={settings.syncInterval}
                onChange={(e) =>
                  setSettings({
                    ...settings,
                    syncInterval: parseInt(e.target.value),
                  })
                }
                disabled={!settings.autoSync}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <RefreshIcon />
                    </InputAdornment>
                  ),
                }}
              />
            </CardContent>
          </Card>
        </Box>

        {/* Sistem Bilgisi */}
        <Card>
          <CardContent>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Sistem Bilgisi
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: {
                  xs: "repeat(2, 1fr)",
                  sm: "repeat(4, 1fr)",
                },
                gap: 2,
              }}
            >
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Versiyon
                </Typography>
                <Typography variant="body1" fontWeight={600}>
                  v1.0.0
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  API Durumu
                </Typography>
                <Typography
                  variant="body1"
                  fontWeight={600}
                  color="success.main"
                >
                  Aktif
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Katana Bağlantı
                </Typography>
                <Typography
                  variant="body1"
                  fontWeight={600}
                  color="success.main"
                >
                  Bağlı
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Son Güncelleme
                </Typography>
                <Typography variant="body1" fontWeight={600}>
                  18.10.2025
                </Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
};

export default Settings;
