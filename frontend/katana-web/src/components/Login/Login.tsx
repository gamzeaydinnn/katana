import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Alert,
  InputAdornment,
  IconButton,
  useTheme,
} from "@mui/material";
import { Visibility, VisibilityOff, Lock, Person } from "@mui/icons-material";
import { authAPI } from "../../services/api";

const Login: React.FC = () => {
  const theme = useTheme();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await authAPI.login(username, password);
      const token = response?.token || response?.Token;

      if (
        !token ||
        typeof token !== "string" ||
        token.split(".").length !== 3
      ) {
        throw new Error("Geçersiz token formatı");
      }

      localStorage.setItem("authToken", token);
      navigate("/admin");
    } catch (err: any) {
      const serverMessage = err?.response?.data?.message;
      const fallback = err?.message || "Giriş başarısız";
      setError(serverMessage || fallback);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: `
          radial-gradient(800px 400px at 10% -10%, ${
            theme.palette.primary.main
          }26, transparent),
          radial-gradient(600px 300px at 90% 0%, ${
            theme.palette.secondary.main
          }22, transparent),
          radial-gradient(600px 300px at 50% 100%, ${
            theme.palette.success.main
          }1f, transparent),
          linear-gradient(180deg, ${theme.palette.background.default} 0%, ${
          theme.palette.mode === "dark" ? "#0b1020" : "#ecf2f7"
        } 100%)
        `,
        position: "relative",
        "&::before": {
          content: '""',
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23${theme.palette.primary.main.slice(
            1
          )}' fill-opacity='0.03'%3E%3Ccircle cx='30' cy='30' r='2'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          opacity: 0.5,
        },
      }}
    >
      <Card
        sx={{
          maxWidth: 420,
          width: "100%",
          m: 2,
          backdropFilter: "blur(20px)",
          background:
            theme.palette.mode === "dark"
              ? "rgba(30,41,59,0.9)"
              : "rgba(255,255,255,0.9)",
          border:
            theme.palette.mode === "dark"
              ? "1px solid rgba(255,255,255,0.1)"
              : "1px solid rgba(0,0,0,0.05)",
          borderRadius: 4,
          boxShadow:
            theme.palette.mode === "dark"
              ? "0 20px 40px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.1)"
              : "0 20px 40px rgba(0,0,0,0.1), inset 0 1px 0 rgba(255,255,255,0.8)",
          transition: "box-shadow 0.3s ease",
          // Sabit yerleşim: hover'da kartın yer değiştirmesini engelle
          "&:hover": {
            transform: "none",
            boxShadow:
              theme.palette.mode === "dark"
                ? "0 32px 64px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.1)"
                : "0 32px 64px rgba(0,0,0,0.2), inset 0 1px 0 rgba(255,255,255,0.8)",
          },
        }}
      >
        <CardContent sx={{ p: 5 }}>
          <Box sx={{ textAlign: "center", mb: 4 }}>
            <Box
              sx={{
                mx: "auto",
                mb: 3,
                display: "flex",
                justifyContent: "center",
              }}
            >
              <Box
                component="img"
                src="/logoo.png"
                alt="Beformet Metal Logo"
                sx={{
                  height: 160,
                  width: "auto",
                  objectFit: "contain",
                  filter: "drop-shadow(0 8px 24px rgba(43,110,246,0.25))",
                }}
              />
            </Box>
            <Typography
              variant="h5"
              sx={{
                fontWeight: 900,
                letterSpacing: "-0.02em",
                background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.secondary.main} 100%)`,
                backgroundClip: "text",
                WebkitBackgroundClip: "text",
                WebkitTextFillColor: "transparent",
                mb: 1,
              }}
            >
              Beformet Metal
            </Typography>
            <Typography
              variant="body2"
              sx={{
                color: theme.palette.text.secondary,
                fontWeight: 600,
                letterSpacing: "0.01em",
              }}
            >
              Devam etmek için giriş yapın
            </Typography>
          </Box>

          {error && (
            <Alert
              severity="error"
              sx={{
                mb: 3,
                borderRadius: 3,
                backdropFilter: "blur(10px)",
                background:
                  theme.palette.mode === "dark"
                    ? "rgba(239,68,68,0.1)"
                    : "rgba(239,68,68,0.05)",
                border: "1px solid rgba(239,68,68,0.2)",
              }}
            >
              {error}
            </Alert>
          )}

          <form onSubmit={handleLogin}>
            <Box sx={{ mb: 2 }}>
              <Typography
                variant="body2"
                sx={{
                  mb: 1,
                  fontWeight: 600,
                  color: theme.palette.primary.main,
                }}
              >
                Kullanıcı Adı *
              </Typography>
              <TextField
                fullWidth
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                placeholder="admin"
                sx={{
                  "& .MuiOutlinedInput-root": {
                    borderRadius: 3,
                    "& fieldset": {
                      borderColor: theme.palette.primary.main,
                      borderWidth: "2px",
                    },
                    "&:hover fieldset": {
                      borderColor: theme.palette.primary.main,
                    },
                    "&.Mui-focused fieldset": {
                      borderColor: theme.palette.primary.main,
                    },
                  },
                  "& .MuiInputBase-input": {
                    paddingLeft: "8px",
                  },
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person sx={{ color: theme.palette.primary.main }} />
                    </InputAdornment>
                  ),
                }}
              />
            </Box>

            <Box sx={{ mb: 3 }}>
              <Typography
                variant="body2"
                sx={{
                  mb: 1,
                  fontWeight: 600,
                  color: theme.palette.primary.main,
                }}
              >
                Şifre *
              </Typography>
              <TextField
                fullWidth
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                placeholder="••••••••"
                sx={{
                  "& .MuiOutlinedInput-root": {
                    borderRadius: 3,
                    "& fieldset": {
                      borderColor: theme.palette.primary.main,
                      borderWidth: "2px",
                    },
                    "&:hover fieldset": {
                      borderColor: theme.palette.primary.main,
                    },
                    "&.Mui-focused fieldset": {
                      borderColor: theme.palette.primary.main,
                    },
                  },
                  "& .MuiInputBase-input": {
                    paddingLeft: "8px",
                  },
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Lock sx={{ color: theme.palette.primary.main }} />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowPassword(!showPassword)}
                        edge="end"
                        sx={{
                          color: theme.palette.primary.main,
                          "&:hover": {
                            backgroundColor: "transparent",
                          },
                        }}
                      >
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />
            </Box>

            <Button
              fullWidth
              type="submit"
              variant="contained"
              size="large"
              disabled={loading}
              sx={{
                py: 1.75,
                borderRadius: 3,
                fontWeight: 700,
                letterSpacing: "0.01em",
                background: `linear-gradient(135deg, ${theme.palette.primary.main}, ${theme.palette.secondary.main})`,
                boxShadow: `0 4px 12px ${theme.palette.primary.main}40`,
                transition: "box-shadow 0.2s ease, opacity 0.2s ease",
                "&:hover": {
                  boxShadow: `0 8px 20px ${theme.palette.primary.main}60`,
                },
                "&:disabled": {
                  opacity: 0.6,
                  transform: "none",
                },
              }}
            >
              {loading ? "Giriş yapılıyor..." : "Giriş Yap"}
            </Button>

            <Box
              sx={{
                mt: 4,
                p: 3,
                backdropFilter: "blur(10px)",
                background:
                  theme.palette.mode === "dark"
                    ? "rgba(30,41,59,0.5)"
                    : "rgba(0,0,0,0.02)",
                borderRadius: 3,
                border:
                  theme.palette.mode === "dark"
                    ? "1px solid rgba(255,255,255,0.1)"
                    : "1px solid rgba(0,0,0,0.05)",
              }}
            >
              <Typography
                variant="caption"
                sx={{
                  color: theme.palette.text.secondary,
                  display: "block",
                  fontWeight: 700,
                  letterSpacing: "0.01em",
                  mb: 1,
                }}
              >
                Test Hesabı:
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  color: theme.palette.text.primary,
                  fontWeight: 600,
                  mb: 0.5,
                }}
              >
                Kullanıcı Adı: <strong>admin</strong>
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  color: theme.palette.text.primary,
                  fontWeight: 600,
                }}
              >
                Şifre: <strong>Katana2025!</strong>
              </Typography>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default Login;
