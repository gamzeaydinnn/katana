import React, { useState } from "react";
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Avatar,
  Stack,
  Alert,
  IconButton,
  InputAdornment,
  Divider,
} from "@mui/material";
import {
  Save as SaveIcon,
  Visibility,
  VisibilityOff,
  Person as PersonIcon,
  Lock as LockIcon,
} from "@mui/icons-material";

const Profile: React.FC = () => {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");

  const handleChangePassword = async () => {
    setError("");
    setSuccess(false);

    if (!currentPassword || !newPassword || !confirmPassword) {
      setError("Lütfen tüm alanları doldurun");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Yeni şifreler eşleşmiyor");
      return;
    }

    if (newPassword.length < 6) {
      setError("Yeni şifre en az 6 karakter olmalı");
      return;
    }

    try {
      const response = await fetch("/api/auth/change-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${localStorage.getItem("authToken")}`,
        },
        body: JSON.stringify({ currentPassword, newPassword }),
      });

      const data = await response.json();

      if (!response.ok) {
        setError(data.message || "Şifre değiştirme başarısız");
        return;
      }

      setSuccess(true);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setTimeout(() => setSuccess(false), 5000);
    } catch (err: any) {
      setError("Şifre değiştirme sırasında bir hata oluştu");
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" fontWeight={700} sx={{ mb: 4 }}>
        Profil
      </Typography>

      <Stack spacing={3}>
        {}
        <Card>
          <CardContent>
            <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
              <Avatar
                sx={{
                  width: 80,
                  height: 80,
                  bgcolor: "primary.main",
                  fontSize: "2rem",
                  mr: 3,
                }}
              >
                A
              </Avatar>
              <Box>
                <Typography variant="h5" fontWeight={600}>
                  Admin
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Yönetici
                </Typography>
              </Box>
            </Box>

            <Divider sx={{ my: 3 }} />

            <Stack spacing={2}>
              <Box>
                <Typography
                  variant="body2"
                  sx={{ mb: 1, fontWeight: 600, color: "primary.main" }}
                >
                  Kullanıcı Adı
                </Typography>
                <TextField
                  value="admin"
                  disabled
                  fullWidth
                  sx={{
                    "& .MuiInputBase-input": {
                      paddingLeft: "8px",
                    },
                  }}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <PersonIcon />
                      </InputAdornment>
                    ),
                  }}
                />
              </Box>
              <Box>
                <Typography
                  variant="body2"
                  sx={{ mb: 1, fontWeight: 600, color: "primary.main" }}
                >
                  Rol
                </Typography>
                <TextField value="Admin" disabled fullWidth />
              </Box>
            </Stack>
          </CardContent>
        </Card>

        {}
        <Card>
          <CardContent>
            <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
              <LockIcon sx={{ mr: 2, color: "primary.main" }} />
              <Typography variant="h6" fontWeight={600}>
                Şifre Değiştir
              </Typography>
            </Box>

            {success && (
              <Alert severity="success" sx={{ mb: 3 }}>
                Şifre başarıyla değiştirildi!
              </Alert>
            )}

            {error && (
              <Alert severity="error" sx={{ mb: 3 }}>
                {error}
              </Alert>
            )}

            <Stack spacing={3}>
              <Box>
                <Typography
                  variant="body2"
                  sx={{ mb: 1, fontWeight: 600, color: "primary.main" }}
                >
                  Mevcut Şifre
                </Typography>
                <TextField
                  type={showCurrent ? "text" : "password"}
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  fullWidth
                  placeholder="••••••••"
                  sx={{
                    "& .MuiInputBase-input": {
                      paddingLeft: "8px",
                    },
                  }}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <LockIcon color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowCurrent(!showCurrent)}
                          edge="end"
                        >
                          {showCurrent ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="body2"
                  sx={{ mb: 1, fontWeight: 600, color: "primary.main" }}
                >
                  Yeni Şifre
                </Typography>
                <TextField
                  type={showNew ? "text" : "password"}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  fullWidth
                  placeholder="••••••••"
                  sx={{
                    "& .MuiInputBase-input": {
                      paddingLeft: "8px",
                    },
                  }}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <LockIcon color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowNew(!showNew)}
                          edge="end"
                        >
                          {showNew ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              </Box>

              <Box>
                <Typography
                  variant="body2"
                  sx={{ mb: 1, fontWeight: 600, color: "primary.main" }}
                >
                  Yeni Şifre (Tekrar)
                </Typography>
                <TextField
                  type={showConfirm ? "text" : "password"}
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  fullWidth
                  placeholder="••••••••"
                  sx={{
                    "& .MuiInputBase-input": {
                      paddingLeft: "8px",
                    },
                  }}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <LockIcon color="action" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton
                          onClick={() => setShowConfirm(!showConfirm)}
                          edge="end"
                        >
                          {showConfirm ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              </Box>

              <Button
                variant="contained"
                startIcon={<SaveIcon />}
                onClick={handleChangePassword}
                size="large"
                sx={{ alignSelf: "flex-start" }}
              >
                Şifreyi Değiştir
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
};

export default Profile;
