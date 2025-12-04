import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import RefreshIcon from '@mui/icons-material/Refresh';
import {
    Alert,
    Box,
    Button,
    Chip,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    IconButton,
    MenuItem,
    Paper,
    Snackbar,
    TextField,
    Typography,
} from '@mui/material';
import { DataGrid, GridColDef, GridRowsProp } from '@mui/x-data-grid';
import axios from 'axios';
import React, { useEffect, useState } from 'react';

interface LucaCategory {
  code: string;
  description: string;
}

interface CategoryMapping {
  id: number;
  katanaCategory: string;
  lucaCategoryCode: string;
  lucaCategoryDescription: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5265/api';

const CategoryMappingPanel: React.FC = () => {
  const [mappings, setMappings] = useState<CategoryMapping[]>([]);
  const [lucaCategories, setLucaCategories] = useState<LucaCategory[]>([]);
  const [loading, setLoading] = useState(false);
  const [openDialog, setOpenDialog] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [currentMapping, setCurrentMapping] = useState<Partial<CategoryMapping>>({});
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' as 'success' | 'error' });

  // Luca kategorilerini yükle
  const loadLucaCategories = async () => {
    try {
      const response = await axios.get<{ categories: LucaCategory[] }>(`${API_BASE_URL}/Mapping/luca-categories`);
      setLucaCategories(response.data.categories || []);
    } catch (error) {
      console.error('Luca kategorileri yüklenemedi:', error);
      showSnackbar('Luca kategorileri yüklenemedi', 'error');
    }
  };

  // Mapping'leri yükle
  const loadMappings = async () => {
    setLoading(true);
    try {
      const response = await axios.get<{ mappings: CategoryMapping[] }>(`${API_BASE_URL}/Mapping/category-mappings`);
      setMappings(response.data.mappings || []);
      showSnackbar('Kategori mapping\'leri yüklendi', 'success');
    } catch (error) {
      console.error('Mapping\'ler yüklenemedi:', error);
      showSnackbar('Mapping\'ler yüklenemedi', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadLucaCategories();
    loadMappings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const showSnackbar = (message: string, severity: 'success' | 'error') => {
    setSnackbar({ open: true, message, severity });
  };

  const handleCloseSnackbar = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  const handleOpenDialog = (mapping?: CategoryMapping) => {
    if (mapping) {
      setEditMode(true);
      setCurrentMapping(mapping);
    } else {
      setEditMode(false);
      setCurrentMapping({ isActive: true });
    }
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setCurrentMapping({});
  };

  const handleSave = async () => {
    try {
      if (editMode && currentMapping.id) {
        // Güncelleme
        await axios.put(`${API_BASE_URL}/Mapping/${currentMapping.id}`, {
          targetValue: currentMapping.lucaCategoryCode,
          description: currentMapping.description,
          isActive: currentMapping.isActive,
        });
        showSnackbar('Mapping güncellendi', 'success');
      } else {
        // Yeni oluşturma
        await axios.post(`${API_BASE_URL}/Mapping`, {
          mappingType: 'PRODUCT_CATEGORY',
          sourceValue: currentMapping.katanaCategory,
          targetValue: currentMapping.lucaCategoryCode,
          description: currentMapping.description,
          isActive: currentMapping.isActive,
        });
        showSnackbar('Mapping oluşturuldu', 'success');
      }
      handleCloseDialog();
      loadMappings();
    } catch (error) {
      console.error('Mapping kaydedilemedi:', error);
      showSnackbar('Mapping kaydedilemedi', 'error');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bu mapping\'i silmek istediğinizden emin misiniz?')) {
      return;
    }

    try {
      await axios.delete(`${API_BASE_URL}/Mapping/${id}`);
      showSnackbar('Mapping silindi', 'success');
      loadMappings();
    } catch (error) {
      console.error('Mapping silinemedi:', error);
      showSnackbar('Mapping silinemedi', 'error');
    }
  };

  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 70 },
    { 
      field: 'katanaCategory', 
      headerName: 'Katana Kategorisi', 
      width: 200,
      renderCell: (params) => (
        <Chip label={params.value} color="primary" variant="outlined" size="small" />
      ),
    },
    { 
      field: 'lucaCategoryCode', 
      headerName: 'Luca Kodu', 
      width: 100,
      renderCell: (params) => (
        <Chip label={params.value} color="secondary" size="small" />
      ),
    },
    { 
      field: 'lucaCategoryDescription', 
      headerName: 'Luca Kategorisi', 
      width: 180 
    },
    { 
      field: 'description', 
      headerName: 'Açıklama', 
      width: 250,
      flex: 1,
    },
    {
      field: 'isActive',
      headerName: 'Durum',
      width: 100,
      renderCell: (params) => (
        <Chip 
          label={params.value ? 'Aktif' : 'Pasif'} 
          color={params.value ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'İşlemler',
      width: 120,
      sortable: false,
      renderCell: (params) => (
        <Box>
          <IconButton 
            size="small" 
            color="primary"
            onClick={() => handleOpenDialog(params.row as CategoryMapping)}
          >
            <EditIcon fontSize="small" />
          </IconButton>
          <IconButton 
            size="small" 
            color="error"
            onClick={() => handleDelete(params.row.id)}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  const rows: GridRowsProp = mappings;

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h5" component="h2">
            Kategori Mapping Yönetimi
          </Typography>
          <Box>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={loadMappings}
              sx={{ mr: 2 }}
            >
              Yenile
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => handleOpenDialog()}
            >
              Yeni Mapping
            </Button>
          </Box>
        </Box>

        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            <strong>Toplam Mapping:</strong> {mappings.length} | 
            <strong> Luca Kategorileri:</strong> {lucaCategories.length}
          </Typography>
          <Typography variant="caption">
            Katana ürün kategorilerini Luca kategori kodlarına eşleyin. 
            Bu mapping'ler ürünler Luca'ya gönderilirken kullanılır.
          </Typography>
        </Alert>

        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          autoHeight
          pageSizeOptions={[10, 25, 50]}
          initialState={{
            pagination: { paginationModel: { pageSize: 25 } },
          }}
          disableRowSelectionOnClick
          sx={{
            '& .MuiDataGrid-cell': {
              borderBottom: '1px solid #f0f0f0',
            },
          }}
        />
      </Paper>

      {/* Ekleme/Düzenleme Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editMode ? 'Mapping Düzenle' : 'Yeni Mapping Oluştur'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
            <TextField
              label="Katana Kategorisi"
              value={currentMapping.katanaCategory || ''}
              onChange={(e) => setCurrentMapping({ ...currentMapping, katanaCategory: e.target.value })}
              disabled={editMode}
              fullWidth
              required
              helperText="Örn: Electronics, Clothing, Raw Material"
            />

            <TextField
              select
              label="Luca Kategorisi"
              value={currentMapping.lucaCategoryCode || ''}
              onChange={(e) => setCurrentMapping({ ...currentMapping, lucaCategoryCode: e.target.value })}
              fullWidth
              required
            >
              {lucaCategories.map((cat) => (
                <MenuItem key={cat.code} value={cat.code}>
                  {cat.code} - {cat.description}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              label="Açıklama"
              value={currentMapping.description || ''}
              onChange={(e) => setCurrentMapping({ ...currentMapping, description: e.target.value })}
              multiline
              rows={2}
              fullWidth
            />

            <TextField
              select
              label="Durum"
              value={currentMapping.isActive ? 'true' : 'false'}
              onChange={(e) => setCurrentMapping({ ...currentMapping, isActive: e.target.value === 'true' })}
              fullWidth
            >
              <MenuItem value="true">Aktif</MenuItem>
              <MenuItem value="false">Pasif</MenuItem>
            </TextField>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>İptal</Button>
          <Button 
            onClick={handleSave} 
            variant="contained"
            disabled={!currentMapping.katanaCategory || !currentMapping.lucaCategoryCode}
          >
            {editMode ? 'Güncelle' : 'Oluştur'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default CategoryMappingPanel;
