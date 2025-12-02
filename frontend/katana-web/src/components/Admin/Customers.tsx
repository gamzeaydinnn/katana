import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
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
  Alert,
  Snackbar,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Sync as SyncIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  HourglassEmpty as PendingIcon,
  Refresh as RefreshIcon,
  Business as BusinessIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import api from '../../services/api';

interface Customer {
  id: number;
  taxNo: string;
  title: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  // Luca fields
  type: number;
  taxOffice?: string;
  district?: string;
  lucaCode?: string;
  lucaFinansalNesneId?: number;
  lastSyncError?: string;
  groupCode?: string;
  lucaSyncStatus: 'success' | 'error' | 'pending';
  isLucaSynced: boolean;
}

interface SyncResult {
  isSuccess: boolean;
  message?: string;
}

interface CustomerFormData {
  taxNo: string;
  title: string;
  contactPerson: string;
  phone: string;
  email: string;
  address: string;
  city: string;
  country: string;
  postalCode: string;
  type: number;
  taxOffice: string;
  district: string;
  groupCode: string;
  isActive: boolean;
}

const initialFormData: CustomerFormData = {
  taxNo: '',
  title: '',
  contactPerson: '',
  phone: '',
  email: '',
  address: '',
  city: '',
  country: 'Turkey',
  postalCode: '',
  type: 1,
  taxOffice: '',
  district: '',
  groupCode: '',
  isActive: true,
};

// CSS Grid helper styles
const gridStyles = {
  container: {
    display: 'grid',
    gap: 2,
    gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
  },
  fullWidth: {
    gridColumn: '1 / -1',
  },
};

const Customers: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  
  const [openDialog, setOpenDialog] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [formData, setFormData] = useState<CustomerFormData>(initialFormData);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  
  const [syncing, setSyncing] = useState<number | null>(null);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });

  const [lucaInfoDialog, setLucaInfoDialog] = useState<{ open: boolean; customer: Customer | null }>({
    open: false,
    customer: null,
  });

  useEffect(() => {
    fetchCustomers();
  }, []);

  const fetchCustomers = async () => {
    try {
      setLoading(true);
      const response = await api.get<Customer[]>('/customers');
      setCustomers(response.data);
      setError(null);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Müşteriler yüklenirken hata oluştu');
    } finally {
      setLoading(false);
    }
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};
    
    if (!formData.taxNo.trim()) {
      errors.taxNo = 'Vergi numarası zorunludur';
    } else if (formData.type === 1 && formData.taxNo.length !== 10) {
      errors.taxNo = 'Şirket için VKN 10 haneli olmalıdır';
    } else if (formData.type === 2 && formData.taxNo.length !== 11) {
      errors.taxNo = 'Şahıs için TCKN 11 haneli olmalıdır';
    }
    
    if (!formData.title.trim()) {
      errors.title = 'Ünvan zorunludur';
    }
    
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleOpenDialog = (customer?: Customer) => {
    if (customer) {
      setEditingCustomer(customer);
      setFormData({
        taxNo: customer.taxNo,
        title: customer.title,
        contactPerson: customer.contactPerson || '',
        phone: customer.phone || '',
        email: customer.email || '',
        address: customer.address || '',
        city: customer.city || '',
        country: customer.country || 'Turkey',
        postalCode: customer.postalCode || '',
        type: customer.type || 1,
        taxOffice: customer.taxOffice || '',
        district: customer.district || '',
        groupCode: customer.groupCode || '',
        isActive: customer.isActive,
      });
    } else {
      setEditingCustomer(null);
      setFormData(initialFormData);
    }
    setFormErrors({});
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setEditingCustomer(null);
    setFormData(initialFormData);
    setFormErrors({});
  };

  const handleSave = async () => {
    if (!validateForm()) return;
    
    setSaving(true);
    try {
      if (editingCustomer) {
        await api.put(`/customers/${editingCustomer.id}`, formData);
        setSnackbar({ open: true, message: 'Müşteri güncellendi', severity: 'success' });
      } else {
        await api.post('/customers', formData);
        setSnackbar({ open: true, message: 'Müşteri oluşturuldu', severity: 'success' });
      }
      handleCloseDialog();
      fetchCustomers();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setSnackbar({ 
        open: true, 
        message: error.response?.data?.message || 'Kayıt sırasında hata oluştu', 
        severity: 'error' 
      });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bu müşteriyi silmek istediğinizden emin misiniz?')) return;
    
    try {
      await api.delete(`/customers/${id}`);
      setSnackbar({ open: true, message: 'Müşteri silindi', severity: 'success' });
      fetchCustomers();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setSnackbar({ 
        open: true, 
        message: error.response?.data?.message || 'Silme sırasında hata oluştu', 
        severity: 'error' 
      });
    }
  };

  const handleSyncToLuca = async (customer: Customer) => {
    setSyncing(customer.id);
    try {
      const response = await api.post<SyncResult>(`/customers/${customer.id}/sync`);
      if (response.data.isSuccess) {
        setSnackbar({ open: true, message: 'Luca senkronizasyonu başarılı', severity: 'success' });
      } else {
        setSnackbar({ open: true, message: response.data.message || 'Senkronizasyon hatası', severity: 'error' });
      }
      fetchCustomers();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setSnackbar({ 
        open: true, 
        message: error.response?.data?.message || 'Senkronizasyon sırasında hata oluştu', 
        severity: 'error' 
      });
    } finally {
      setSyncing(null);
    }
  };

  const getLucaStatusChip = (customer: Customer) => {
    const status = customer.lucaSyncStatus || 
      (customer.lucaFinansalNesneId && !customer.lastSyncError ? 'success' : 
       customer.lastSyncError ? 'error' : 'pending');
    
    switch (status) {
      case 'success':
        return (
          <Tooltip title={`Luca Kod: ${customer.lucaCode || 'N/A'}`}>
            <Chip 
              icon={<CheckCircleIcon />} 
              label="Senkron" 
              color="success" 
              size="small" 
              onClick={() => setLucaInfoDialog({ open: true, customer })}
              sx={{ cursor: 'pointer' }}
            />
          </Tooltip>
        );
      case 'error':
        return (
          <Tooltip title={customer.lastSyncError || 'Hata'}>
            <Chip 
              icon={<ErrorIcon />} 
              label="Hata" 
              color="error" 
              size="small"
              onClick={() => setLucaInfoDialog({ open: true, customer })}
              sx={{ cursor: 'pointer' }}
            />
          </Tooltip>
        );
      default:
        return (
          <Chip 
            icon={<PendingIcon />} 
            label="Bekliyor" 
            color="default" 
            size="small" 
          />
        );
    }
  };

  const getTypeLabel = (type: number) => {
    return type === 2 ? (
      <Chip icon={<PersonIcon />} label="Şahıs" size="small" variant="outlined" />
    ) : (
      <Chip icon={<BusinessIcon />} label="Şirket" size="small" variant="outlined" color="primary" />
    );
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={400}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Müşteriler</Typography>
        <Box>
          <IconButton onClick={fetchCustomers} sx={{ mr: 1 }}>
            <RefreshIcon />
          </IconButton>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => handleOpenDialog()}
          >
            Yeni Müşteri
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Customers Table */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Tip</TableCell>
              <TableCell>Vergi No</TableCell>
              <TableCell>Ünvan</TableCell>
              <TableCell>Telefon</TableCell>
              <TableCell>E-posta</TableCell>
              <TableCell>Şehir</TableCell>
              <TableCell>Luca</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell align="right">İşlemler</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {customers
              .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
              .map((customer) => (
                <TableRow key={customer.id}>
                  <TableCell>{getTypeLabel(customer.type)}</TableCell>
                  <TableCell>{customer.taxNo}</TableCell>
                  <TableCell>{customer.title}</TableCell>
                  <TableCell>{customer.phone || '-'}</TableCell>
                  <TableCell>{customer.email || '-'}</TableCell>
                  <TableCell>{customer.city || '-'}</TableCell>
                  <TableCell>{getLucaStatusChip(customer)}</TableCell>
                  <TableCell>
                    <Chip
                      label={customer.isActive ? 'Aktif' : 'Pasif'}
                      color={customer.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title="Luca'ya Gönder">
                      <IconButton
                        size="small"
                        onClick={() => handleSyncToLuca(customer)}
                        disabled={syncing === customer.id}
                      >
                        {syncing === customer.id ? (
                          <CircularProgress size={20} />
                        ) : (
                          <SyncIcon />
                        )}
                      </IconButton>
                    </Tooltip>
                    <IconButton size="small" onClick={() => handleOpenDialog(customer)}>
                      <EditIcon />
                    </IconButton>
                    <IconButton size="small" onClick={() => handleDelete(customer.id)}>
                      <DeleteIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            {customers.length === 0 && (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  Müşteri bulunamadı
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={customers.length}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e) => {
            setRowsPerPage(parseInt(e.target.value, 10));
            setPage(0);
          }}
          labelRowsPerPage="Sayfa başına:"
        />
      </TableContainer>

      {/* Customer Form Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>
          {editingCustomer ? 'Müşteri Düzenle' : 'Yeni Müşteri'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ ...gridStyles.container, mt: 2 }}>
            {/* Type Selection */}
            <FormControl fullWidth>
              <InputLabel>Müşteri Tipi</InputLabel>
              <Select
                value={formData.type}
                label="Müşteri Tipi"
                onChange={(e) => setFormData({ ...formData, type: e.target.value as number })}
              >
                <MenuItem value={1}>
                  <Box display="flex" alignItems="center" gap={1}>
                    <BusinessIcon fontSize="small" /> Şirket
                  </Box>
                </MenuItem>
                <MenuItem value={2}>
                  <Box display="flex" alignItems="center" gap={1}>
                    <PersonIcon fontSize="small" /> Şahıs
                  </Box>
                </MenuItem>
              </Select>
            </FormControl>

            {/* Tax Number */}
            <TextField
              fullWidth
              label={formData.type === 1 ? 'VKN (10 hane)' : 'TCKN (11 hane)'}
              value={formData.taxNo}
              onChange={(e) => setFormData({ ...formData, taxNo: e.target.value })}
              error={!!formErrors.taxNo}
              helperText={formErrors.taxNo}
              inputProps={{ maxLength: formData.type === 1 ? 10 : 11 }}
            />

            {/* Title - Full Width */}
            <Box sx={gridStyles.fullWidth}>
              <TextField
                fullWidth
                label="Ünvan"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                error={!!formErrors.title}
                helperText={formErrors.title}
              />
            </Box>

            {/* Tax Office (only for companies) */}
            {formData.type === 1 && (
              <TextField
                fullWidth
                label="Vergi Dairesi"
                value={formData.taxOffice}
                onChange={(e) => setFormData({ ...formData, taxOffice: e.target.value })}
              />
            )}

            {/* Group Code */}
            <TextField
              fullWidth
              label="Grup Kodu"
              value={formData.groupCode}
              onChange={(e) => setFormData({ ...formData, groupCode: e.target.value })}
              sx={formData.type !== 1 ? gridStyles.fullWidth : undefined}
            />

            {/* Contact Person */}
            <TextField
              fullWidth
              label="İletişim Kişisi"
              value={formData.contactPerson}
              onChange={(e) => setFormData({ ...formData, contactPerson: e.target.value })}
            />

            {/* Phone */}
            <TextField
              fullWidth
              label="Telefon"
              value={formData.phone}
              onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
            />

            {/* Email */}
            <TextField
              fullWidth
              label="E-posta"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            />

            {/* Country */}
            <FormControl fullWidth>
              <InputLabel>Ülke</InputLabel>
              <Select
                value={formData.country}
                label="Ülke"
                onChange={(e) => setFormData({ ...formData, country: e.target.value })}
              >
                <MenuItem value="Turkey">Türkiye</MenuItem>
                <MenuItem value="Germany">Almanya</MenuItem>
                <MenuItem value="France">Fransa</MenuItem>
                <MenuItem value="United Kingdom">İngiltere</MenuItem>
                <MenuItem value="United States">ABD</MenuItem>
                <MenuItem value="Other">Diğer</MenuItem>
              </Select>
            </FormControl>

            {/* City */}
            <TextField
              fullWidth
              label="Şehir"
              value={formData.city}
              onChange={(e) => setFormData({ ...formData, city: e.target.value })}
            />

            {/* District */}
            <TextField
              fullWidth
              label="İlçe"
              value={formData.district}
              onChange={(e) => setFormData({ ...formData, district: e.target.value })}
            />

            {/* Address - Full Width */}
            <Box sx={gridStyles.fullWidth}>
              <TextField
                fullWidth
                label="Adres"
                multiline
                rows={2}
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
              />
            </Box>

            {/* Postal Code */}
            <TextField
              fullWidth
              label="Posta Kodu"
              value={formData.postalCode}
              onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>İptal</Button>
          <Button 
            onClick={handleSave} 
            variant="contained" 
            disabled={saving}
          >
            {saving ? <CircularProgress size={24} /> : 'Kaydet'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Luca Info Dialog */}
      <Dialog 
        open={lucaInfoDialog.open} 
        onClose={() => setLucaInfoDialog({ open: false, customer: null })}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Luca Senkronizasyon Bilgisi</DialogTitle>
        <DialogContent>
          {lucaInfoDialog.customer && (
            <Box sx={{ ...gridStyles.container, mt: 2 }}>
              <Box>
                <Typography variant="body2" color="text.secondary">Luca Kodu</Typography>
                <Typography variant="body1" fontWeight="bold">
                  {lucaInfoDialog.customer.lucaCode || 'Henüz atanmadı'}
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">Finansal Nesne ID</Typography>
                <Typography variant="body1" fontWeight="bold">
                  {lucaInfoDialog.customer.lucaFinansalNesneId || 'Henüz atanmadı'}
                </Typography>
              </Box>
              <Box sx={gridStyles.fullWidth}>
                <Typography variant="body2" color="text.secondary">Durum</Typography>
                <Box sx={{ mt: 0.5 }}>
                  {getLucaStatusChip(lucaInfoDialog.customer)}
                </Box>
              </Box>
              {lucaInfoDialog.customer.lastSyncError && (
                <Box sx={gridStyles.fullWidth}>
                  <Alert severity="error" sx={{ mt: 1 }}>
                    <Typography variant="body2" fontWeight="bold">Son Hata:</Typography>
                    <Typography variant="body2">
                      {lucaInfoDialog.customer.lastSyncError}
                    </Typography>
                  </Alert>
                </Box>
              )}
              <Box sx={gridStyles.fullWidth}>
                <Typography variant="body2" color="text.secondary">Son Güncelleme</Typography>
                <Typography variant="body1">
                  {lucaInfoDialog.customer.updatedAt 
                    ? new Date(lucaInfoDialog.customer.updatedAt).toLocaleString('tr-TR')
                    : '-'}
                </Typography>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLucaInfoDialog({ open: false, customer: null })}>
            Kapat
          </Button>
          {lucaInfoDialog.customer && (
            <Button
              variant="contained"
              startIcon={<SyncIcon />}
              onClick={() => {
                handleSyncToLuca(lucaInfoDialog.customer!);
                setLucaInfoDialog({ open: false, customer: null });
              }}
            >
              Luca'ya Gönder
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
      >
        <Alert 
          onClose={() => setSnackbar({ ...snackbar, open: false })} 
          severity={snackbar.severity}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default Customers;
