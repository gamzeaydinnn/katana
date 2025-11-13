import React, { useState, useEffect } from "react";
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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  CircularProgress,
} from "@mui/material";
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Visibility,
  VisibilityOff,
  Key as KeyIcon,
  Api as ApiIcon,
  Sync as SyncIcon,
  Shield as ShieldIcon,
  CheckCircle as CheckIcon,
  Cancel as CancelIcon,
  Warning as WarningIcon,
} from "@mui/icons-material";
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

          {/* Rol Yetkilendirmesi */}
          <Card>
            <CardContent>
              <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                <ShieldIcon sx={{ mr: 1, color: "primary.main" }} />
                <Typography variant="h6" fontWeight={600}>
                  Rol Yetkilendirme Rehberi
                </Typography>
              </Box>
              <Divider sx={{ mb: 3 }} />

              <Alert severity="info" sx={{ mb: 3 }}>
                Bu bölüm yalnızca bilgilendirme amaçlıdır. Kullanıcılara hangi
                rolü atayacağınıza karar verirken bu tabloyu referans
                alabilirsiniz.
              </Alert>

              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>
                        <strong>Yetki</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>Admin</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>Manager</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>Staff</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>StockManager</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell>Kullanıcı Yönetimi (Görüntüleme)</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Kullanıcı Ekleme/Düzenleme/Silme</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Rol Atama/Değiştirme</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Admin Paneli Erişimi</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <WarningIcon color="warning" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Senkronizasyon Başlatma</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Ürün Güncelleme</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Stok Onaylama</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Müşteri/Tedarikçi/Kategori Yönetimi</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CancelIcon color="error" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Dashboard/Raporlar Görüntüleme</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell>Canlı Stok Görüntüleme</TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                      <TableCell align="center">
                        <CheckIcon color="success" fontSize="small" />
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>

              <Box sx={{ mt: 3, display: "flex", gap: 2, flexWrap: "wrap" }}>
                <Chip
                  icon={<CheckIcon />}
                  label="Tam Yetki"
                  color="success"
                  size="small"
                />
                <Chip
                  icon={<WarningIcon />}
                  label="Sınırlı Yetki"
                  color="warning"
                  size="small"
                />
                <Chip
                  icon={<CancelIcon />}
                  label="Yetki Yok"
                  color="error"
                  size="small"
                />
              </Box>

              <Box sx={{ mt: 3 }}>
                <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                  Rol Açıklamaları:
                </Typography>
                <Stack spacing={1.5}>
                  <Box>
                    <Typography
                      variant="body2"
                      color="primary.main"
                      fontWeight={600}
                    >
                      • Admin:
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ pl: 2 }}
                    >
                      Sistemin tüm alanlarına tam erişim. Kullanıcı yönetimi,
                      senkronizasyon, tüm CRUD işlemleri.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography
                      variant="body2"
                      color="warning.main"
                      fontWeight={600}
                    >
                      • Manager:
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ pl: 2 }}
                    >
                      Kullanıcı listesini görüntüleme yetkisi. Değişiklik
                      yapamaz, sadece okuma erişimi.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography
                      variant="body2"
                      color="info.main"
                      fontWeight={600}
                    >
                      • Staff:
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ pl: 2 }}
                    >
                      Temel kullanıcı. Dashboard, raporlar ve canlı stok
                      görüntüleme. Değişiklik yapamaz.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography
                      variant="body2"
                      color="secondary.main"
                      fontWeight={600}
                    >
                      • StockManager:
                    </Typography>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ pl: 2 }}
                    >
                      Stok odaklı işlemler. Ürün güncelleme, stok onaylama,
                      hatalı kayıt düzeltme yetkisi.
                    </Typography>
                  </Box>
                </Stack>
              </Box>
            </CardContent>
          </Card>
        </Stack>
      )}
    </Box>
  );
};

export default Settings;
