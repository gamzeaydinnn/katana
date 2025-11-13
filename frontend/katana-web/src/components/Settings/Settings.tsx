import {
    Api as ApiIcon,
    Key as KeyIcon,
    Refresh as RefreshIcon,
    Save as SaveIcon,
    Sync as SyncIcon,
    Visibility,
    VisibilityOff,
} from "@mui/icons-material";
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    CircularProgress,
    Divider,
    FormControlLabel,
    IconButton,
    InputAdornment,
    Stack,
    Switch,
    TextField,
    Typography,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import api from "../../services/api";

interface SettingsState {
  katanaApiKey: string;
  lucaApiKey: string;
  autoSync: boolean;
  syncInterval: number;
  showApiKey: boolean;
}

const Settings: React.FC = () => {
  const [settings, setSettings] = useState<SettingsState>({
    katanaApiKey: "",
    lucaApiKey: "",
    autoSync: true,
    syncInterval: 60,
    showApiKey: false,
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await api.get("/settings");
      const data = response.data as any;
      setSettings({
        katanaApiKey: data.katanaApiKey || "",
        lucaApiKey: data.lucaApiKey || "",
        autoSync: data.autoSync ?? true,
        syncInterval: data.syncInterval || 60,
        showApiKey: false,
      });
    } catch (err: any) {
      console.error("Settings load error:", err);
      setError("Ayarlar yüklenirken hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);
      await api.post("/settings", {
        katanaApiKey: settings.katanaApiKey,
        lucaApiKey: settings.lucaApiKey,
        autoSync: settings.autoSync,
        syncInterval: settings.syncInterval,
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (err: any) {
      console.error("Settings save error:", err);
      setError("Ayarlar kaydedilirken hata oluştu");
    } finally {
      setSaving(false);
    }
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
          startIcon={
            saving ? (
              <CircularProgress size={20} color="inherit" />
            ) : (
              <SaveIcon />
            )
          }
          onClick={handleSave}
          disabled={saving || loading}
          sx={{ px: 3 }}
        >
          {saving ? "Kaydediliyor..." : "Kaydet"}
        </Button>
      </Box>

      {loading && (
        <Box sx={{ display: "flex", justifyContent: "center", p: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          Ayarlar başarıyla kaydedildi!
        </Alert>
      )}

      {!loading && (
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
      )}
    </Box>
  );
};

export default Settings;
