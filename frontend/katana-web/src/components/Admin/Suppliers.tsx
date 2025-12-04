import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  ArrowBack as BackIcon,
  Save as SaveIcon,
  Cancel as CancelIcon,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormHelperText,
  Grid,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useCallback, useEffect, useState } from "react";
import api from "../../services/api";

// ===== TYPES =====

interface Supplier {
  id: number;
  name: string;
  code?: string;
  taxNo?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  country?: string;
  lucaCode?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

interface CreateSupplierForm {
  name: string;
  code: string;
  taxNo: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  country: string;
  lucaCode: string;
}

interface FormErrors {
  [key: string]: string;
}

interface SnackbarState {
  open: boolean;
  message: string;
  severity: "success" | "error" | "warning" | "info";
}

// ===== COMPONENT =====

const Suppliers: React.FC = () => {
  // ===== STATES =====
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Create/Edit Dialog
  const [openDialog, setOpenDialog] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<CreateSupplierForm>({
    name: "",
    code: "",
    taxNo: "",
    email: "",
    phone: "",
    address: "",
    city: "",
    country: "",
    lucaCode: "",
  });
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [saving, setSaving] = useState(false);

  // Snackbar
  const [snackbar, setSnackbar] = useState<SnackbarState>({
    open: false,
    message: "",
    severity: "success",
  });

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState<{
    open: boolean;
    id?: number;
  }>({ open: false });
  const [deleting, setDeleting] = useState<number | null>(null);

  // ===== EFFECTS =====

  useEffect(() => {
    fetchSuppliers();
  }, [page, rowsPerPage]);

  // ===== API CALLS =====

  const fetchSuppliers = useCallback(async () => {
    try {
      setLoading(true);
      const response = await api.get<{
        data: Supplier[];
        total: number;
      }>("/suppliers", {
        params: {
          page: page + 1,
          pageSize: rowsPerPage,
        },
      });

      const data = response.data;
      if (Array.isArray(data)) {
        setSuppliers(data);
        setTotalCount(data.length);
      } else if (data?.data) {
        setSuppliers(data.data);
        setTotalCount(data.total || 0);
      }
    } catch (err) {
      console.error("Failed to fetch suppliers:", err);
      setSnackbar({
        open: true,
        message: "Tedarikçi listesi yüklenemedi",
        severity: "error",
      });
    } finally {
      setLoading(false);
    }
  }, [page, rowsPerPage]);

  const handleCreateSupplier = async () => {
    if (!validateForm()) {
      setSnackbar({
        open: true,
        message: "Lütfen form hatalarını düzeltin",
        severity: "error",
      });
      return;
    }

    try {
      setSaving(true);
      const payload = {
        name: formData.name,
        code: formData.code || null,
        taxNo: formData.taxNo || null,
        email: formData.email || null,
        phone: formData.phone || null,
        address: formData.address || null,
        city: formData.city || null,
        country: formData.country || null,
        lucaCode: formData.lucaCode || null,
      };

      if (editingId) {
        await api.put(`/suppliers/${editingId}`, payload);
        setSnackbar({
          open: true,
          message: "Tedarikçi güncellendi",
          severity: "success",
        });
      } else {
        await api.post("/suppliers", payload);
        setSnackbar({
          open: true,
          message: "Tedarikçi oluşturuldu",
          severity: "success",
        });
      }

      resetForm();
      setOpenDialog(false);
      await fetchSuppliers();
    } catch (err) {
      console.error("Failed to save supplier:", err);
      setSnackbar({
        open: true,
        message: "Tedarikçi kaydedilemedi",
        severity: "error",
      });
    } finally {
      setSaving(false);
    }
  };

  const handleEditSupplier = (supplier: Supplier) => {
    setEditingId(supplier.id);
    setFormData({
      name: supplier.name,
      code: supplier.code || "",
      taxNo: supplier.taxNo || "",
      email: supplier.email || "",
      phone: supplier.phone || "",
      address: supplier.address || "",
      city: supplier.city || "",
      country: supplier.country || "",
      lucaCode: supplier.lucaCode || "",
    });
    setOpenDialog(true);
  };

  const handleDeleteSupplier = async (id: number) => {
    try {
      setDeleting(id);
      await api.delete(`/suppliers/${id}`);
      setSnackbar({
        open: true,
        message: "Tedarikçi silindi",
        severity: "success",
      });
      setDeleteConfirm({ open: false });
      await fetchSuppliers();
    } catch (err) {
      console.error("Failed to delete supplier:", err);
      setSnackbar({
        open: true,
        message: "Tedarikçi silinemedi",
        severity: "error",
      });
    } finally {
      setDeleting(null);
    }
  };

  // ===== FORM VALIDATION =====

  const validateForm = (): boolean => {
    const errors: FormErrors = {};

    if (!formData.name.trim()) {
      errors.name = "Tedarikçi adı gerekli";
    }

    if (formData.email && !isValidEmail(formData.email)) {
      errors.email = "Geçerli bir e-posta adresi girin";
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const isValidEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  // ===== FORM HANDLERS =====

  const resetForm = () => {
    setFormData({
      name: "",
      code: "",
      taxNo: "",
      email: "",
      phone: "",
      address: "",
      city: "",
      country: "",
      lucaCode: "",
    });
    setFormErrors({});
    setEditingId(null);
  };

  const handleOpenDialog = () => {
    resetForm();
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    resetForm();
  };

  // ===== RENDER =====

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3 }}>
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 600, mb: 0.5 }}>
            Tedarikçi Yönetimi
          </Typography>
          <Typography variant="body2" color="textSecondary">
            Tedarikçileri görüntüleyin, oluşturun ve yönetin
          </Typography>
        </Box>
        <Button
          variant="contained"
          color="primary"
          startIcon={<AddIcon />}
          onClick={handleOpenDialog}
        >
          Yeni Tedarikçi
        </Button>
      </Box>

      {/* Suppliers Table */}
      <Card>
        <CardContent>
          {loading && <CircularProgress />}

          {!loading && suppliers.length === 0 ? (
            <Alert severity="info">Tedarikçi bulunamadı</Alert>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>Ad</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Kod</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Koza Kodu</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Vergi No</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Email</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Telefon</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Durum</TableCell>
                    <TableCell sx={{ fontWeight: 600 }} align="right">
                      İşlemler
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {suppliers.map((supplier) => (
                    <TableRow key={supplier.id}>
                      <TableCell>{supplier.name}</TableCell>
                      <TableCell>
                        {supplier.code ? (
                          <Chip label={supplier.code} size="small" />
                        ) : (
                          <Typography variant="body2" color="textSecondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        {supplier.lucaCode ? (
                          <Chip
                            label={supplier.lucaCode}
                            size="small"
                            color="primary"
                            variant="outlined"
                          />
                        ) : (
                          <Typography variant="body2" color="textSecondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>{supplier.taxNo || "-"}</TableCell>
                      <TableCell>{supplier.email || "-"}</TableCell>
                      <TableCell>{supplier.phone || "-"}</TableCell>
                      <TableCell>
                        <Chip
                          label={supplier.isActive ? "Aktif" : "Pasif"}
                          size="small"
                          color={supplier.isActive ? "success" : "default"}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="Düzenle">
                          <IconButton
                            size="small"
                            onClick={() => handleEditSupplier(supplier)}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Sil">
                          <IconButton
                            size="small"
                            onClick={() =>
                              setDeleteConfirm({ open: true, id: supplier.id })
                            }
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {!loading && suppliers.length > 0 && (
            <TablePagination
              rowsPerPageOptions={[5, 10, 25, 50]}
              component="div"
              count={totalCount}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={(_, newPage) => setPage(newPage)}
              onRowsPerPageChange={(e) =>
                setRowsPerPage(parseInt(e.target.value, 10))
              }
              labelRowsPerPage="Satır sayısı:"
              labelDisplayedRows={({ from, to, count }) =>
                `${from}-${to} / ${count}`
              }
            />
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingId ? "Tedarikçi Düzenle" : "Yeni Tedarikçi Oluştur"}
        </DialogTitle>
        <Divider />
        <DialogContent sx={{ pt: 2, display: "flex", flexDirection: "column", gap: 2 }}>
          <TextField
            label="Tedarikçi Adı *"
            value={formData.name}
            onChange={(e) =>
              setFormData({ ...formData, name: e.target.value })
            }
            fullWidth
            error={!!formErrors.name}
            helperText={formErrors.name}
          />

          <TextField
            label="Kod"
            value={formData.code}
            onChange={(e) =>
              setFormData({ ...formData, code: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Koza Kodu"
            value={formData.lucaCode}
            onChange={(e) =>
              setFormData({ ...formData, lucaCode: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Vergi No"
            value={formData.taxNo}
            onChange={(e) =>
              setFormData({ ...formData, taxNo: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Email"
            type="email"
            value={formData.email}
            onChange={(e) =>
              setFormData({ ...formData, email: e.target.value })
            }
            fullWidth
            error={!!formErrors.email}
            helperText={formErrors.email}
          />

          <TextField
            label="Telefon"
            value={formData.phone}
            onChange={(e) =>
              setFormData({ ...formData, phone: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Adres"
            value={formData.address}
            onChange={(e) =>
              setFormData({ ...formData, address: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Şehir"
            value={formData.city}
            onChange={(e) =>
              setFormData({ ...formData, city: e.target.value })
            }
            fullWidth
          />

          <TextField
            label="Ülke"
            value={formData.country}
            onChange={(e) =>
              setFormData({ ...formData, country: e.target.value })
            }
            fullWidth
          />
        </DialogContent>
        <Divider />
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={handleCloseDialog}>İptal</Button>
          <Button
            variant="contained"
            onClick={handleCreateSupplier}
            disabled={saving}
            startIcon={saving ? <CircularProgress size={16} /> : <SaveIcon />}
          >
            {saving ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteConfirm.open}
        onClose={() => setDeleteConfirm({ open: false })}
      >
        <DialogTitle>Tedarikçi Sil</DialogTitle>
        <DialogContent>
          <Typography>
            Bu tedarikçiyi silmek istediğinizden emin misiniz?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirm({ open: false })}>
            İptal
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() =>
              deleteConfirm.id && handleDeleteSupplier(deleteConfirm.id)
            }
            disabled={deleting !== null}
          >
            {deleting === deleteConfirm.id ? <CircularProgress size={16} /> : "Sil"}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert severity={snackbar.severity} variant="filled">
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default Suppliers;
