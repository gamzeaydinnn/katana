import React, { useEffect, useState } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  Chip,
  IconButton,
  Alert,
  Switch,
} from "@mui/material";
import { Add as AddIcon, Delete as DeleteIcon, Refresh as RefreshIcon, Edit as EditIcon } from "@mui/icons-material";
import { usersAPI, type CreateUserRequest, type UserDto } from "../../services/api";
import { showGlobalToast } from "../../providers/FeedbackProvider";
import { decodeJwtPayload, getJwtRoles } from "../../utils/jwt";

const roleOptions = ["Admin", "Manager", "Staff"] as const;

const UsersManagement: React.FC = () => {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [creating, setCreating] = useState<boolean>(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [editing, setEditing] = useState<null | (UserDto & { password?: string })>(null);

  const [form, setForm] = useState<CreateUserRequest>({
    username: "",
    password: "",
    role: "Staff",
    email: "",
  });

  // Validation now handled inline on submit with toasts

  const load = async () => {
    try {
      setLoading(true);
      const data = await usersAPI.list();
      setUsers(data);
    } catch (err: any) {
      console.error("Failed to load users", err);
      showGlobalToast?.({
        message: `Kullanıcılar yüklenemedi: ${err?.response?.data?.error || err.message || "Hata"}`,
        severity: "error",
        durationMs: 3500,
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const rolesFromToken = (() => {
    const token = typeof window !== "undefined" ? window.localStorage.getItem("authToken") : null;
    const payload = decodeJwtPayload(token);
    return getJwtRoles(payload);
  })();
  const isAdmin = rolesFromToken.includes("admin");

  const handleCreate = async () => {
    try {
      // Inline validation so kullanıcı butona basabilsin ve sebebini görsün
      if (form.username.trim().length < 3) {
        showGlobalToast?.({ message: "Kullanıcı adı en az 3 karakter olmalı", severity: "warning", durationMs: 3000 });
        return;
      }
      if (form.password.trim().length < 6) {
        showGlobalToast?.({ message: "Şifre en az 6 karakter olmalı", severity: "warning", durationMs: 3000 });
        return;
      }
      if (!form.role) {
        showGlobalToast?.({ message: "Lütfen bir rol seçin", severity: "warning", durationMs: 3000 });
        return;
      }

      setCreating(true);
      const payload: CreateUserRequest = {
        username: form.username.trim(),
        password: form.password,
        role: form.role,
        email: form.email?.trim() || undefined,
      };
      await usersAPI.create(payload);
      showGlobalToast?.({ message: "Kullanıcı oluşturuldu", severity: "success", durationMs: 2500 });
      setForm({ username: "", password: "", role: "Staff", email: "" });
      await load();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.response?.data?.message || err.message || "Hata";
      showGlobalToast?.({ message: `Oluşturma başarısız: ${msg}`, severity: "error", durationMs: 4000 });
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (typeof window !== "undefined" && !window.confirm("Kullanıcıyı silmek istediğinize emin misiniz?")) return;
    try {
      setDeletingId(id);
      await usersAPI.delete(id);
      showGlobalToast?.({ message: "Kullanıcı silindi", severity: "success", durationMs: 2500 });
      await load();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.response?.data?.message || err.message || "Hata";
      showGlobalToast?.({ message: `Silme başarısız: ${msg}`, severity: "error", durationMs: 4000 });
    } finally {
      setDeletingId(null);
    }
  };

  const openEdit = (u: UserDto) => {
    setEditing({ ...u });
  };

  const closeEdit = () => setEditing(null);

  const saveEdit = async () => {
    if (!editing) return;
    try {
      const { id, username, email, role, isActive, password } = editing;
      if (username.trim().length < 3) {
        showGlobalToast?.({ message: "Kullanıcı adı en az 3 karakter olmalı", severity: "warning", durationMs: 3000 });
        return;
      }
      await usersAPI.update(id, {
        username: username.trim(),
        email: email?.trim() || undefined,
        role,
        isActive,
        password: password && password.trim() !== "" ? password : undefined,
      });
      showGlobalToast?.({ message: "Kullanıcı güncellendi", severity: "success", durationMs: 2500 });
      setEditing(null);
      await load();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.response?.data?.message || err.message || "Hata";
      showGlobalToast?.({ message: `Güncelleme başarısız: ${msg}`, severity: "error", durationMs: 4000 });
    }
  };

  return (
    <Stack spacing={3}>
      <Card>
        <CardContent>
          <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
            <Typography variant="h6" fontWeight={700}>
              Alt Kullanıcılar / Kullanıcı Yönetimi
            </Typography>
            <Box>
              <IconButton onClick={load} color="primary" title="Yenile">
                <RefreshIcon />
              </IconButton>
            </Box>
          </Box>

          {!isAdmin && (
            <Alert severity="info" sx={{ mb: 2 }}>
              Bu bölümde yalnızca Admin kullanıcılar değişiklik yapabilir. Siz kullanıcıları görüntüleyebilirsiniz.
            </Alert>
          )}

          {isAdmin && (
            <>
              <Divider sx={{ mb: 2 }} />
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <TextField
                  label="Kullanıcı Adı"
                  value={form.username}
                  onChange={(e) => setForm((f) => ({ ...f, username: e.target.value }))}
                  required
                  sx={{ minWidth: 220 }}
                />
                <TextField
                  type="email"
                  label="E-posta (opsiyonel)"
                  value={form.email}
                  onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                  sx={{ minWidth: 260 }}
                />
                <TextField
                  label="Şifre"
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                  required
                  sx={{ minWidth: 220 }}
                  helperText="En az 6 karakter"
                />
                <FormControl sx={{ minWidth: 180 }}>
                  <InputLabel id="role-label">Rol</InputLabel>
                  <Select
                    labelId="role-label"
                    label="Rol"
                    value={form.role}
                    onChange={(e) => setForm((f) => ({ ...f, role: e.target.value as any }))}
                  >
                    {roleOptions.map((r) => (
                      <MenuItem key={r} value={r}>
                        {r}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <Button
                  variant="contained"
                  startIcon={<AddIcon />}
                  onClick={handleCreate}
                  disabled={creating}
                >
                  {creating ? "Ekleniyor..." : "Kullanıcı Ekle"}
                </Button>
              </Stack>
            </>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h6" fontWeight={700} gutterBottom>
            Kullanıcı Listesi
          </Typography>
          <Divider sx={{ mb: 2 }} />

          {loading ? (
            <Box display="flex" alignItems="center" justifyContent="center" minHeight={160}>
              <CircularProgress />
            </Box>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Kullanıcı Adı</TableCell>
                    <TableCell>E-posta</TableCell>
                    <TableCell>Rol</TableCell>
                    <TableCell>Durum</TableCell>
                    <TableCell align="right">İşlemler</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {users.map((u) => (
                    <TableRow key={u.id} hover>
                      <TableCell>{u.username}</TableCell>
                      <TableCell>{u.email}</TableCell>
                      <TableCell>{u.role}</TableCell>
                      <TableCell>
                        <Chip size="small" label={u.isActive ? "Aktif" : "Pasif"} color={u.isActive ? "success" : "default"} />
                      </TableCell>
                      <TableCell align="right">
                        <IconButton
                          color="primary"
                          onClick={() => openEdit(u)}
                          title="Düzenle"
                          disabled={!isAdmin}
                          sx={{ mr: 1 }}
                        >
                          <EditIcon />
                        </IconButton>
                        <IconButton
                          color="error"
                          onClick={() => handleDelete(u.id)}
                          disabled={deletingId === u.id || !isAdmin}
                          title="Sil"
                        >
                          {deletingId === u.id ? <CircularProgress size={18} /> : <DeleteIcon />}
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                  {users.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        Kayıt bulunamadı.
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <Dialog open={!!editing} onClose={closeEdit} fullWidth maxWidth="sm">
        <DialogTitle>Kullanıcıyı Düzenle</DialogTitle>
        <DialogContent>
          {editing && (
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Kullanıcı Adı"
                value={editing.username}
                onChange={(e) => setEditing({ ...(editing as any), username: e.target.value })}
                required
              />
              <TextField
                label="E-posta (opsiyonel)"
                type="email"
                value={editing.email}
                onChange={(e) => setEditing({ ...(editing as any), email: e.target.value })}
              />
              <FormControl fullWidth>
                <InputLabel id="edit-role-label">Rol</InputLabel>
                <Select
                  labelId="edit-role-label"
                  label="Rol"
                  value={editing.role}
                  onChange={(e) => setEditing({ ...(editing as any), role: e.target.value as any })}
                >
                  {roleOptions.map((r) => (
                    <MenuItem key={r} value={r}>
                      {r}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControlLabel
                control={
                  <Switch
                    checked={!!editing.isActive}
                    onChange={(e) => setEditing({ ...(editing as any), isActive: e.target.checked })}
                  />
                }
                label="Aktif"
              />
              <TextField
                label="Yeni Şifre (opsiyonel)"
                type="password"
                value={editing.password ?? ""}
                onChange={(e) => setEditing({ ...(editing as any), password: e.target.value })}
                helperText="Boş bırakırsanız şifre değişmez"
              />
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit}>Vazgeç</Button>
          <Button variant="contained" onClick={saveEdit} disabled={!isAdmin}>Kaydet</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
};

export default UsersManagement;
