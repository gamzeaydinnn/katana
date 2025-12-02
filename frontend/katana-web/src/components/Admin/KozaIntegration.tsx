import {
  Business,
  Inventory,
  Refresh,
  Sync as SyncIcon,
  Warehouse
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Tooltip,
  Typography,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import type {
  KozaStkDepo,
  KozaStokKarti,
} from "../../features/integrations/luca-koza";
import {
  mapKatanaLocationToKozaDepo,
  mapKatanaProductToKozaStokKarti
} from "../../features/integrations/luca-koza";
import api, { kozaAPI } from "../../services/api";

// Tedarikçi Cari tipi
interface KozaTedarikciCari {
  finansalNesneId?: number;
  kod?: string;
  tanim?: string;
  kisaAd?: string;
  vergiNo?: string;
  email?: string;
  telefon?: string;
}

// Supplier Sync sonucu
interface SupplierSyncResult {
  totalCount: number;
  successCount: number;
  errorCount: number;
  skippedCount: number;
  items: {
    katanaSupplierId: string;
    supplierName: string;
    kozaCariKodu?: string;
    success: boolean;
    message?: string;
  }[];
  errorMessage?: string;
}

/**
 * Koza Entegrasyon Yönetim Paneli
 * Depo ve Stok Kartı yönetimi için admin UI
 */
const KozaIntegration: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Depo state
  const [depots, setDepots] = useState<KozaStkDepo[]>([]);
  const [loadingDepots, setLoadingDepots] = useState(false);
  const [syncingDepots, setSyncingDepots] = useState(false);

  // Stok kartı state
  const [stockCards, setStockCards] = useState<KozaStokKarti[]>([]);
  const [loadingStockCards, setLoadingStockCards] = useState(false);
  const [syncingStockCards, setSyncingStockCards] = useState(false);

  // Tedarikçi state
  const [suppliers, setSuppliers] = useState<KozaTedarikciCari[]>([]);
  const [loadingSuppliers, setLoadingSuppliers] = useState(false);
  const [syncingSuppliers, setSyncingSuppliers] = useState(false);
  const [supplierSyncResult, setSupplierSyncResult] = useState<SupplierSyncResult | null>(null);

  // Depo listesini yükle
  const loadDepots = async () => {
    try {
      setLoadingDepots(true);
      setError(null);
      const data = await kozaAPI.depots.list();
      setDepots(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error("Depo yükleme hatası:", err);
      setError(err.message || "Depolar yüklenirken hata oluştu");
    } finally {
      setLoadingDepots(false);
    }
  };

  // Stok kartı listesini yükle
  const loadStockCards = async () => {
    try {
      setLoadingStockCards(true);
      setError(null);
      const data = await kozaAPI.stockCards.list();
      setStockCards(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error("Stok kartı yükleme hatası:", err);
      setError(err.message || "Stok kartları yüklenirken hata oluştu");
    } finally {
      setLoadingStockCards(false);
    }
  };

  // Depo senkronizasyonu
  const syncDepots = async () => {
    try {
      setSyncingDepots(true);
      setError(null);
      setSuccess(null);

      // Katana Location'ları al
      const locationsRes = await api.get("/Locations");
      const locations = locationsRes.data;

      if (!Array.isArray(locations) || locations.length === 0) {
        setError("Katana'da senkronize edilecek location bulunamadı");
        return;
      }

      let successCount = 0;
      let errorCount = 0;

      // Her location'ı Koza'ya gönder
      for (const location of locations) {
        try {
          const kozaDepo = mapKatanaLocationToKozaDepo(
            location,
            "MERKEZ", // kategoriKod
            "TÜRKİYE", // ulke
            location.address?.city || "İSTANBUL" // il
          );

          await kozaAPI.depots.create({ stkDepo: kozaDepo });
          successCount++;
        } catch (err) {
          console.error(`Location ${location.id} senkronize edilemedi:`, err);
          errorCount++;
        }
      }

      setSuccess(
        `Senkronizasyon tamamlandı: ${successCount} başarılı, ${errorCount} hatalı`
      );

      // Listeyi yenile
      await loadDepots();
    } catch (err: any) {
      console.error("Depo senkronizasyon hatası:", err);
      setError(err.message || "Depo senkronizasyonu sırasında hata oluştu");
    } finally {
      setSyncingDepots(false);
    }
  };

  // Stok kartı senkronizasyonu
  const syncStockCards = async () => {
    try {
      setSyncingStockCards(true);
      setError(null);
      setSuccess(null);

      // Katana Product'ları al
      const productsRes = await api.get("/Products/katana");
      const products = productsRes.data;

      if (!Array.isArray(products) || products.length === 0) {
        setError("Katana'da senkronize edilecek product bulunamadı");
        return;
      }

      let successCount = 0;
      let errorCount = 0;

      // Her product'ı Koza'ya gönder
      for (const product of products) {
        try {
          const kozaStok = mapKatanaProductToKozaStokKarti(product, {
            kategoriAgacKod: "URUNLER",
            olcumBirimiId: 1, // Adet
          });

          await kozaAPI.stockCards.create({ stkKart: kozaStok });
          successCount++;
        } catch (err) {
          console.error(`Product ${product.id} senkronize edilemedi:`, err);
          errorCount++;
        }
      }

      setSuccess(
        `Senkronizasyon tamamlandı: ${successCount} başarılı, ${errorCount} hatalı`
      );

      // Listeyi yenile
      await loadStockCards();
    } catch (err: any) {
      console.error("Stok kartı senkronizasyon hatası:", err);
      setError(err.message || "Stok kartı senkronizasyonu sırasında hata oluştu");
    } finally {
      setSyncingStockCards(false);
    }
  };

  // Tedarikçi listesini yükle
  const loadSuppliers = async () => {
    try {
      setLoadingSuppliers(true);
      setError(null);
      const res = await api.get("/admin/koza/cari/suppliers");
      setSuppliers(Array.isArray(res.data) ? res.data : []);
    } catch (err: any) {
      console.error("Tedarikçi yükleme hatası:", err);
      setError(err.message || "Tedarikçiler yüklenirken hata oluştu");
    } finally {
      setLoadingSuppliers(false);
    }
  };

  // Tedarikçi senkronizasyonu - backend sync endpoint'i kullan
  const syncSuppliers = async () => {
    try {
      setSyncingSuppliers(true);
      setError(null);
      setSuccess(null);
      setSupplierSyncResult(null);

      // Backend'e sync isteği at
      const res = await api.post("/admin/koza/cari/suppliers/sync");
      const result: SupplierSyncResult = res.data;
      
      setSupplierSyncResult(result);

      if (result.errorMessage) {
        setError(result.errorMessage);
      } else {
        setSuccess(
          `Senkronizasyon tamamlandı: ${result.successCount} başarılı, ${result.skippedCount} atlandı, ${result.errorCount} hatalı`
        );
      }

      // Listeyi yenile
      await loadSuppliers();
    } catch (err: any) {
      console.error("Tedarikçi senkronizasyon hatası:", err);
      setError(err.response?.data?.error || err.message || "Tedarikçi senkronizasyonu sırasında hata oluştu");
    } finally {
      setSyncingSuppliers(false);
    }
  };

  // İlk yükleme
  useEffect(() => {
    if (activeTab === 0) {
      loadDepots();
    } else if (activeTab === 1) {
      loadStockCards();
    } else if (activeTab === 2) {
      loadSuppliers();
    }
  }, [activeTab]);

  return (
    <Box>
      {/* Header */}
      <Box sx={{ mb: 3, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <Typography variant="h5" fontWeight={600}>
          Koza Entegrasyon Yönetimi
        </Typography>
        <Box sx={{ display: "flex", gap: 1 }}>
          <Tooltip title="Yenile">
            <IconButton
              onClick={() => {
                if (activeTab === 0) loadDepots();
                else if (activeTab === 1) loadStockCards();
                else loadSuppliers();
              }}
              color="primary"
            >
              <Refresh />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Success Alert */}
      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={activeTab}
          onChange={(_, v) => setActiveTab(v)}
          sx={{
            "& .MuiTab-root": {
              textTransform: "none",
              fontWeight: 500,
              fontSize: "0.9375rem",
            },
          }}
        >
          <Tab icon={<Warehouse />} label="Depo Kartları" iconPosition="start" />
          <Tab icon={<Inventory />} label="Stok Kartları" iconPosition="start" />
          <Tab icon={<Business />} label="Tedarikçi Kartları" iconPosition="start" />
        </Tabs>
      </Paper>

      {/* Depo Kartları Tab */}
      {activeTab === 0 && (
        <Box>
          {/* İstatistikler */}
          <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 2, mb: 3 }}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" variant="body2">
                  Toplam Depo
                </Typography>
                <Typography variant="h4" fontWeight={700}>
                  {depots.length}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Depo Listesi */}
          <Paper sx={{ p: 2 }}>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }}>
              <Typography variant="h6" fontWeight={600}>
                Depo Listesi
              </Typography>
              <Button
                variant="contained"
                startIcon={syncingDepots ? <CircularProgress size={20} color="inherit" /> : <SyncIcon />}
                onClick={syncDepots}
                disabled={syncingDepots}
              >
                {syncingDepots ? "Senkronize Ediliyor..." : "Toplu Senkronize"}
              </Button>
            </Box>

            {loadingDepots ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : depots.length === 0 ? (
              <Alert severity="info">
                Henüz depo kaydı yok. Katana Location'larınızı senkronize edin.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Depo ID</TableCell>
                      <TableCell>Kod</TableCell>
                      <TableCell>Tanım</TableCell>
                      <TableCell>Kategori</TableCell>
                      <TableCell>Şehir</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {depots.map((depo, idx) => (
                      <TableRow key={depo.depoId || `depo-${idx}`}>
                        <TableCell>{depo.depoId || "-"}</TableCell>
                        <TableCell>
                          <Chip label={depo.kod} size="small" color="primary" variant="outlined" />
                        </TableCell>
                        <TableCell>{depo.tanim}</TableCell>
                        <TableCell>{depo.kategoriKod}</TableCell>
                        <TableCell>{depo.il || "-"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}

      {/* Stok Kartları Tab */}
      {activeTab === 1 && (
        <Box>
          {/* İstatistikler */}
          <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 2, mb: 3 }}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" variant="body2">
                  Toplam Stok Kartı
                </Typography>
                <Typography variant="h4" fontWeight={700}>
                  {stockCards.length}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Stok Kartı Listesi */}
          <Paper sx={{ p: 2 }}>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }}>
              <Typography variant="h6" fontWeight={600}>
                Stok Kartı Listesi
              </Typography>
              <Button
                variant="contained"
                startIcon={syncingStockCards ? <CircularProgress size={20} color="inherit" /> : <SyncIcon />}
                onClick={syncStockCards}
                disabled={syncingStockCards}
              >
                {syncingStockCards ? "Senkronize Ediliyor..." : "Toplu Senkronize"}
              </Button>
            </Box>

            {loadingStockCards ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : stockCards.length === 0 ? (
              <Alert severity="info">
                Henüz stok kartı kaydı yok. Katana Product'larınızı senkronize edin.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Stok ID</TableCell>
                      <TableCell>Kod</TableCell>
                      <TableCell>Adı</TableCell>
                      <TableCell>Kategori</TableCell>
                      <TableCell>KDV %</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {stockCards.map((stok, idx) => (
                      <TableRow key={stok.stokKartId || `stok-${idx}`}>
                        <TableCell>{stok.stokKartId || "-"}</TableCell>
                        <TableCell>
                          <Chip label={stok.kartKodu} size="small" color="secondary" variant="outlined" />
                        </TableCell>
                        <TableCell>{stok.kartAdi}</TableCell>
                        <TableCell>{stok.kategoriAgacKod}</TableCell>
                        <TableCell>{(stok.kartSatisKdvOran * 100).toFixed(0)}%</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}

      {/* Tedarikçi Kartları Tab */}
      {activeTab === 2 && (
        <Box>
          {/* İstatistikler */}
          <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 2, mb: 3 }}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" variant="body2">
                  Toplam Tedarikçi
                </Typography>
                <Typography variant="h4" fontWeight={700}>
                  {suppliers.length}
                </Typography>
              </CardContent>
            </Card>
            {supplierSyncResult && (
              <>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" variant="body2">
                      Son Sync - Başarılı
                    </Typography>
                    <Typography variant="h4" fontWeight={700} color="success.main">
                      {supplierSyncResult.successCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" variant="body2">
                      Son Sync - Atlandı
                    </Typography>
                    <Typography variant="h4" fontWeight={700} color="info.main">
                      {supplierSyncResult.skippedCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" variant="body2">
                      Son Sync - Hatalı
                    </Typography>
                    <Typography variant="h4" fontWeight={700} color="error.main">
                      {supplierSyncResult.errorCount}
                    </Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>

          {/* Tedarikçi Listesi */}
          <Paper sx={{ p: 2 }}>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 2 }}>
              <Typography variant="h6" fontWeight={600}>
                Koza Tedarikçi Listesi
              </Typography>
              <Button
                variant="contained"
                color="primary"
                startIcon={syncingSuppliers ? <CircularProgress size={20} color="inherit" /> : <SyncIcon />}
                onClick={syncSuppliers}
                disabled={syncingSuppliers}
              >
                {syncingSuppliers ? "Senkronize Ediliyor..." : "Katana'dan Senkronize"}
              </Button>
            </Box>

            {loadingSuppliers ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : suppliers.length === 0 ? (
              <Alert severity="info">
                Henüz tedarikçi kaydı yok. Katana Supplier'larınızı senkronize edin.
              </Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Finansal Nesne ID</TableCell>
                      <TableCell>Cari Kodu</TableCell>
                      <TableCell>Tanım</TableCell>
                      <TableCell>Vergi No</TableCell>
                      <TableCell>İletişim</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {suppliers.map((sup, idx) => (
                      <TableRow key={sup.finansalNesneId || `sup-${idx}`}>
                        <TableCell>{sup.finansalNesneId || "-"}</TableCell>
                        <TableCell>
                          <Chip label={sup.kod || "-"} size="small" color="warning" variant="outlined" />
                        </TableCell>
                        <TableCell>{sup.tanim || sup.kisaAd || "-"}</TableCell>
                        <TableCell>{sup.vergiNo || "-"}</TableCell>
                        <TableCell>{sup.email || sup.telefon || "-"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}
    </Box>
  );
};

export default KozaIntegration;
