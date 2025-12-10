import {
  Business,
  Inventory,
  People,
  Refresh,
  Search as SearchIcon,
  Sync as SyncIcon,
  Warehouse,
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
  InputAdornment,
  Paper,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
  useMediaQuery,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import type {
  KatanaLocation,
  KozaStkDepo,
  KozaStokKarti,
} from "../../features/integrations/luca-koza";
import {
  mapKatanaLocationToKozaDepo,
  mapKatanaProductToKozaStokKarti,
} from "../../features/integrations/luca-koza";
import api, { kozaAPI } from "../../services/api";

// Tedarikçi Cari tipi
interface KozaSupplierListItem {
  finansalNesneId: number | null;
  kod: string | null;
  tanim: string | null;
  vergiNo: string | null;
  telefon: string | null;
  email: string | null;
}

// Müşteri Cari tipi (Koza)
interface KozaCustomerListItem {
  finansalNesneId: number | null;
  kod: string;
  tanim: string;
  vergiNo: string | null;
  telefon: string | null;
  email: string | null;
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
  const [totalDepots, setTotalDepots] = useState<number>(0);
  const [loadingDepots, setLoadingDepots] = useState(false);
  const [syncingDepots, setSyncingDepots] = useState(false);

  // Stok kartı state
  const [stockCards, setStockCards] = useState<KozaStokKarti[]>([]);
  const [filteredStockCards, setFilteredStockCards] = useState<KozaStokKarti[]>(
    []
  );
  const [searchTerm, setSearchTerm] = useState("");
  const [loadingStockCards, setLoadingStockCards] = useState(false);
  const [syncingStockCards, setSyncingStockCards] = useState(false);
  const [katanaProductCount, setKatanaProductCount] = useState<number>(0);
  const isMobile = useMediaQuery("(max-width:900px)");

  // Tedarikçi state
  const [suppliers, setSuppliers] = useState<KozaSupplierListItem[]>([]);
  const [loadingSuppliers, setLoadingSuppliers] = useState(false);
  const [syncingSuppliers, setSyncingSuppliers] = useState(false);
  const [supplierSyncResult, setSupplierSyncResult] =
    useState<SupplierSyncResult | null>(null);

  // Müşteri state
  const [customers, setCustomers] = useState<KozaCustomerListItem[]>([]);
  const [loadingCustomers, setLoadingCustomers] = useState(false);
  const [syncingCustomers, setSyncingCustomers] = useState(false);
  const [customerSyncResult, setCustomerSyncResult] = useState<{
    successCount: number;
    errorCount: number;
  } | null>(null);

  // Depo listesini yükle
  const loadDepots = async () => {
    try {
      setLoadingDepots(true);
      setError(null);
      const data = await kozaAPI.depots.list();

      // Backend response: { data: [], pagination: { totalItems } } (camelCase)
      const itemsData = Array.isArray((data as any)?.data)
        ? (data as any).data
        : undefined;
      const itemsAlt = Array.isArray((data as any)?.items)
        ? (data as any).items
        : undefined;
      const items = itemsData ?? itemsAlt ?? (Array.isArray(data) ? data : []);
      const paginationTotal = (data as any)?.pagination?.totalItems;
      const total =
        typeof paginationTotal === "number" ? paginationTotal : items.length;

      setDepots(items);
      setTotalDepots(total);
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

      // Paralel olarak hem Koza stok kartlarını hem Katana ürünlerini yükle
      const [kozaData, katanaRes] = await Promise.all([
        kozaAPI.stockCards.list(),
        api.get("/Products/katana").catch(() => ({ data: [] as any[] })),
      ]);

      setStockCards(Array.isArray(kozaData) ? kozaData : []);

      // Katana ürün sayısını ayarla - API yanıtı { data: [...] } veya direkt [...] olabilir
      const rawKatanaData = katanaRes?.data as any;
      const katanaProducts = Array.isArray(rawKatanaData)
        ? rawKatanaData
        : rawKatanaData?.data || [];
      setKatanaProductCount(
        Array.isArray(katanaProducts) ? katanaProducts.length : 0
      );
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

      // Katana Location'ları al - başarısız olursa varsayılan kullan
      let locations: KatanaLocation[] = [];
      try {
        const locationsRes = await api.get<KatanaLocation[]>("/Locations");
        locations = Array.isArray(locationsRes.data) ? locationsRes.data : [];
      } catch (katanaErr: any) {
        console.warn(
          "Katana API bağlantısı yok, varsayılan depo kullanılacak:",
          katanaErr
        );
        // Varsayılan location listesini dene
        try {
          const defaultRes = await api.get<KatanaLocation[]>(
            "/Locations/defaults"
          );
          locations = Array.isArray(defaultRes.data) ? defaultRes.data : [];
        } catch {
          // Varsayılan depoyu manuel oluştur
          locations = [
            {
              id: 1,
              name: "MERKEZ DEPO",
              legal_name: "Ana Depo",
              address: { country: "Türkiye" },
              is_primary: true,
            },
          ];
        }
      }

      if (!Array.isArray(locations) || locations.length === 0) {
        setError("Senkronize edilecek location bulunamadı");
        return;
      }

      let successCount = 0;
      let errorCount = 0;
      const errors: string[] = [];

      // Her location'ı Koza'ya gönder
      for (const location of locations) {
        try {
          const kozaDepo = mapKatanaLocationToKozaDepo(
            location,
            "01" // kategoriKod - numerik olmalı (MERKEZ değil)
          );

          console.log("Depo oluşturuluyor:", kozaDepo);
          await kozaAPI.depots.create({ stkDepo: kozaDepo });
          successCount++;
        } catch (err: any) {
          console.error(`Location ${location.id} senkronize edilemedi:`, err);
          const errMsg =
            err.response?.data?.message ||
            err.response?.data?.error ||
            err.message ||
            "Bilinmeyen hata";
          errors.push(`${location.name || location.id}: ${errMsg}`);
          errorCount++;
        }
      }

      if (successCount > 0) {
        setSuccess(
          `Senkronizasyon tamamlandı: ${successCount} başarılı, ${errorCount} hatalı`
        );
      } else if (errorCount > 0) {
        setError(`Tüm senkronizasyonlar başarısız: ${errors.join(", ")}`);
      }

      // Listeyi yenile
      await loadDepots();
    } catch (err: any) {
      console.error("Depo senkronizasyon hatası:", err);
      const errMsg =
        err.response?.data?.error ||
        err.response?.data?.message ||
        err.message ||
        "Depo senkronizasyonu sırasında hata oluştu";
      setError(errMsg);
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
      // API yanıtı { data: [...] } veya direkt [...] olabilir
      const rawData = productsRes.data as any;
      const products = Array.isArray(rawData) ? rawData : rawData?.data || [];

      if (!Array.isArray(products) || products.length === 0) {
        setSuccess("Tüm ürünler zaten Koza'ya senkronize edilmiş");
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
      setError(
        err.message || "Stok kartı senkronizasyonu sırasında hata oluştu"
      );
    } finally {
      setSyncingStockCards(false);
    }
  };

  // Tedarikçi listesini yükle
  const loadSuppliers = async () => {
    try {
      setLoadingSuppliers(true);
      setError(null);
      const res = await api.get<KozaSupplierListItem[]>(
        "/admin/koza/cari/suppliers"
      );
      const rows = Array.isArray(res.data) ? res.data : [];
      setSuppliers(
        rows.map((item) => ({
          finansalNesneId: item.finansalNesneId ?? null,
          kod: item.kod ?? null,
          tanim: item.tanim ?? null,
          vergiNo: item.vergiNo ?? null,
          telefon: item.telefon ?? null,
          email: item.email ?? null,
        }))
      );
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
      const result = res.data as SupplierSyncResult;

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
      setError(
        err.response?.data?.error ||
          err.message ||
          "Tedarikçi senkronizasyonu sırasında hata oluştu"
      );
    } finally {
      setSyncingSuppliers(false);
    }
  };

  // Müşteri listesini yükle
  const loadCustomers = async () => {
    try {
      setLoadingCustomers(true);
      setError(null);
      const res = await api.get<KozaCustomerListItem[]>(
        "/admin/koza/cari/customers"
      );
      const rows = Array.isArray(res.data) ? res.data : [];
      setCustomers(
        rows.map((item) => ({
          finansalNesneId: item.finansalNesneId ?? null,
          kod: item.kod ?? "",
          tanim: item.tanim ?? "",
          vergiNo: item.vergiNo ?? null,
          telefon: item.telefon ?? null,
          email: item.email ?? null,
        }))
      );
    } catch (err: any) {
      console.error("Müşteri yükleme hatası:", err);
      setError(err.message || "Müşteriler yüklenirken hata oluştu");
    } finally {
      setLoadingCustomers(false);
    }
  };

  // Müşteri Koza senkronizasyonu
  const syncCustomers = async () => {
    try {
      setSyncingCustomers(true);
      setError(null);
      setSuccess(null);
      setCustomerSyncResult(null);

      // Backend'e sync isteği at (tedarikçi gibi)
      interface CustomerSyncResponse {
        successCount?: number;
        errorCount?: number;
        errorMessage?: string;
      }
      const res = await api.post<CustomerSyncResponse>(
        "/admin/koza/cari/customers/sync"
      );
      const result = res.data;

      // Sonuç yapısı tedarikçi ile aynı olabilir
      const successCount = result.successCount ?? 0;
      const errorCount = result.errorCount ?? 0;

      setCustomerSyncResult({ successCount, errorCount });

      if (result.errorMessage) {
        setError(result.errorMessage);
      } else {
        setSuccess(
          `Müşteri senkronizasyonu tamamlandı: ${successCount} başarılı, ${errorCount} hatalı`
        );
      }

      await loadCustomers();
    } catch (err: any) {
      console.error("Müşteri senkronizasyon hatası:", err);
      setError(
        err.response?.data?.error ||
          err.message ||
          "Müşteri senkronizasyonu sırasında hata oluştu"
      );
    } finally {
      setSyncingCustomers(false);
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
    } else if (activeTab === 3) {
      loadCustomers();
    }
  }, [activeTab]);

  // Stok kartı arama filtresi
  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredStockCards(stockCards);
    } else {
      const term = searchTerm.toLowerCase();
      const filtered = stockCards.filter((card) => {
        const kod = (card.kartKodu || "").toLowerCase();
        const ad = (card.kartAdi || "").toLowerCase();
        return kod.includes(term) || ad.includes(term);
      });
      setFilteredStockCards(filtered);
    }
  }, [searchTerm, stockCards]);

  return (
    <Box sx={{ px: { xs: 0, sm: 0 }, mx: { xs: -1, sm: 0 } }}>
      {/* Header */}
      <Box
        sx={{
          mb: 2,
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          px: { xs: 1, sm: 0 },
        }}
      >
        <Typography
          variant="h6"
          fontWeight={600}
          sx={{ fontSize: { xs: "1rem", sm: "1.25rem" } }}
        >
          Koza Entegrasyon Yönetimi
        </Typography>
        <IconButton
          size="small"
          onClick={() => {
            if (activeTab === 0) loadDepots();
            else if (activeTab === 1) loadStockCards();
            else if (activeTab === 2) loadSuppliers();
            else loadCustomers();
          }}
          color="primary"
        >
          <Refresh fontSize="small" />
        </IconButton>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Success Alert */}
      {success && (
        <Alert
          severity="success"
          sx={{ mb: 2 }}
          onClose={() => setSuccess(null)}
        >
          {success}
        </Alert>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 2, mx: { xs: 1, sm: 0 }, overflowX: "auto" }}>
        <Tabs
          value={activeTab}
          onChange={(_, v) => setActiveTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          sx={{
            minHeight: 40,
            "& .MuiTab-root": {
              textTransform: "none",
              fontWeight: 500,
              fontSize: { xs: "0.7rem", sm: "0.875rem" },
              minHeight: 40,
              minWidth: { xs: "auto", sm: 120 },
              px: { xs: 1, sm: 2 },
              py: 0.5,
            },
            "& .MuiTab-iconWrapper": {
              fontSize: { xs: "1rem", sm: "1.25rem" },
            },
          }}
        >
          <Tab icon={<Warehouse />} label="Depo" iconPosition="start" />
          <Tab icon={<Inventory />} label="Stok" iconPosition="start" />
          <Tab icon={<Business />} label="Tedarikçi" iconPosition="start" />
          <Tab icon={<People />} label="Müşteri" iconPosition="start" />
        </Tabs>
      </Paper>

      {/* Depo Kartları Tab */}
      {activeTab === 0 && (
        <Box sx={{ px: { xs: 1, sm: 0 } }}>
          {/* İstatistikler */}
          <Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
            <Card sx={{ flex: "1 1 auto", minWidth: 100 }}>
              <CardContent sx={{ py: 1.5, px: 2, "&:last-child": { pb: 1.5 } }}>
                <Typography color="textSecondary" variant="caption">
                  Toplam Depo
                </Typography>
                <Typography variant="h5" fontWeight={700}>
                  {totalDepots}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Depo Listesi */}
          <Paper sx={{ p: { xs: 1, sm: 2 } }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 1.5,
                gap: 1,
              }}
            >
              <Typography
                variant="subtitle1"
                fontWeight={600}
                sx={{ fontSize: { xs: "0.9rem", sm: "1rem" } }}
              >
                Depo Listesi
              </Typography>
              <Button
                size="small"
                variant="contained"
                startIcon={
                  syncingDepots ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncDepots}
                disabled={syncingDepots}
                sx={{ fontSize: "0.7rem", px: 1, py: 0.5, minWidth: "auto" }}
              >
                {syncingDepots ? "Sync..." : "Senkronize"}
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
              <TableContainer sx={{ width: "100%", overflowX: "auto" }}>
                <Table
                  size="small"
                  sx={{
                    "& .MuiTableCell-root": {
                      px: { xs: 0.75, sm: 2 },
                      py: { xs: 0.5, sm: 1 },
                      fontSize: { xs: "0.7rem", sm: "0.875rem" },
                    },
                  }}
                >
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>ID</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Kod</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Tanım</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>
                        Kategori
                      </TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Şehir</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {depots.map((depo) => (
                      <TableRow
                        key={`depot-${depo.depoId ?? depo.id ?? depo.kod}-${
                          depo.kod
                        }`}
                      >
                        <TableCell>{depo.depoId ?? depo.id ?? "-"}</TableCell>
                        <TableCell>
                          <Chip
                            label={depo.kod}
                            size="small"
                            color="primary"
                            variant="outlined"
                          />
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
        <Box sx={{ px: { xs: 1, sm: 0 } }}>
          {/* İstatistikler */}
          <Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
            <Card sx={{ flex: "1 1 auto", minWidth: 100 }}>
              <CardContent sx={{ py: 1.5, px: 2, "&:last-child": { pb: 1.5 } }}>
                <Typography color="textSecondary" variant="caption">
                  Toplam Stok Kartı
                </Typography>
                <Typography variant="h5" fontWeight={700}>
                  {stockCards.length}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Stok Kartı Listesi */}
          <Paper sx={{ p: { xs: 1, sm: 2 } }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 1.5,
                gap: 1,
              }}
            >
              <Typography
                variant="subtitle1"
                fontWeight={600}
                sx={{ fontSize: { xs: "0.9rem", sm: "1rem" } }}
              >
                Stok Kartı Listesi
              </Typography>
              <Button
                size="small"
                variant="contained"
                startIcon={
                  syncingStockCards ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncStockCards}
                disabled={syncingStockCards}
                sx={{ fontSize: "0.7rem", px: 1, py: 0.5, minWidth: "auto" }}
              >
                {syncingStockCards ? "Sync..." : "Senkronize"}
              </Button>
            </Box>

            <TextField
              fullWidth
              placeholder="Ürün kodu veya adı ara..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
              sx={{ mb: 2 }}
              size="small"
            />

            <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
              <Chip label={`Toplam: ${stockCards.length}`} color="secondary" />
              <Chip label={`Görüntülenen: ${filteredStockCards.length}`} />
            </Stack>

            {loadingStockCards ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : stockCards.length === 0 ? (
              <Alert severity="info">Henüz stok kartı kaydı yok.</Alert>
            ) : filteredStockCards.length === 0 ? (
              <Alert severity="info">Arama sonucu bulunamadı</Alert>
            ) : isMobile ? (
              /* Mobil Kart Görünümü */
              <Stack spacing={1.5}>
                {filteredStockCards.map((stok, idx) => {
                  const kod = stok.kartKodu || "";
                  const ad = stok.kartAdi || "";
                  const barkod = (stok as any).barkod || "";
                  const kategori = stok.kategoriAgacKod || "";
                  const birim = (stok as any).olcumBirimi || "";
                  const miktar = (stok as any).miktar ?? 0;
                  const birimFiyat = (stok as any).birimFiyat ?? 0;
                  const kdvOran = stok.kartSatisKdvOran ?? 0;
                  const durum = (stok as any).durum ?? true;
                  const sonGuncelleme = (stok as any).sonGuncelleme || "";

                  return (
                    <Paper
                      key={stok.stokKartId || `stok-mobile-${idx}`}
                      sx={{
                        p: 1.5,
                        borderRadius: 2,
                        border: "1px solid",
                        borderColor: "divider",
                      }}
                    >
                      <Box
                        sx={{
                          display: "flex",
                          justifyContent: "space-between",
                          gap: 1,
                        }}
                      >
                        <Box>
                          <Typography variant="subtitle1" fontWeight={600}>
                            {ad}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Kod: <strong>{kod}</strong>
                          </Typography>
                          {barkod && (
                            <Typography variant="body2" color="text.secondary">
                              Barkod: {barkod}
                            </Typography>
                          )}
                          {kategori && (
                            <Chip
                              label={kategori}
                              size="small"
                              variant="outlined"
                              sx={{ mt: 0.5 }}
                            />
                          )}
                        </Box>
                        <Chip
                          label={durum ? "Aktif" : "Pasif"}
                          color={durum ? "success" : "default"}
                          size="small"
                        />
                      </Box>
                      <Box
                        sx={{
                          display: "grid",
                          gridTemplateColumns:
                            "repeat(auto-fit, minmax(100px, 1fr))",
                          gap: 1,
                          mt: 1.25,
                        }}
                      >
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            Birim
                          </Typography>
                          <Typography fontWeight={600}>
                            {birim || "-"}
                          </Typography>
                        </Box>
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            Miktar
                          </Typography>
                          <Typography fontWeight={600}>{miktar}</Typography>
                        </Box>
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            Birim Fiyat
                          </Typography>
                          <Typography fontWeight={600}>
                            {birimFiyat ? `${birimFiyat.toFixed(2)} ₺` : "-"}
                          </Typography>
                        </Box>
                        <Box>
                          <Typography variant="caption" color="text.secondary">
                            KDV
                          </Typography>
                          <Typography fontWeight={600}>
                            %{(kdvOran * 100).toFixed(0)}
                          </Typography>
                        </Box>
                      </Box>
                      {sonGuncelleme && (
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ mt: 1, display: "block" }}
                        >
                          Güncelleme: {sonGuncelleme}
                        </Typography>
                      )}
                    </Paper>
                  );
                })}
              </Stack>
            ) : (
              /* Desktop Tablo Görünümü */
              <TableContainer component={Paper}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>
                        <strong>Ürün Kodu</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ürün Adı</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Barkod</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Kategori</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ölçü Birimi</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Miktar</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Birim Fiyat</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>KDV Oranı</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Durum</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Son Güncelleme</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {filteredStockCards.map((stok, idx) => {
                      const kod = stok.kartKodu || "";
                      const ad = stok.kartAdi || "";
                      const barkod = (stok as any).barkod || "";
                      const kategori = stok.kategoriAgacKod || "";
                      const birim = (stok as any).olcumBirimi || "";
                      const miktar = (stok as any).miktar ?? 0;
                      const birimFiyat = (stok as any).birimFiyat ?? 0;
                      const kdvOran = stok.kartSatisKdvOran ?? 0;
                      const durum = (stok as any).durum ?? true;
                      const sonGuncelleme = (stok as any).sonGuncelleme || "";

                      return (
                        <TableRow
                          key={stok.stokKartId || `stok-desktop-${idx}`}
                          hover
                        >
                          <TableCell>
                            <Typography variant="body2" fontWeight="bold">
                              {kod}
                            </Typography>
                          </TableCell>
                          <TableCell>{ad}</TableCell>
                          <TableCell>{barkod || "-"}</TableCell>
                          <TableCell>{kategori || "-"}</TableCell>
                          <TableCell>{birim || "-"}</TableCell>
                          <TableCell align="right">{miktar}</TableCell>
                          <TableCell align="right">
                            {birimFiyat ? `${birimFiyat.toFixed(2)} ₺` : "-"}
                          </TableCell>
                          <TableCell align="right">
                            %{(kdvOran * 100).toFixed(0)}
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={durum ? "Aktif" : "Pasif"}
                              color={durum ? "success" : "default"}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>{sonGuncelleme || "-"}</TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}

      {/* Tedarikçi Kartları Tab */}
      {activeTab === 2 && (
        <Box sx={{ px: { xs: 1, sm: 0 } }}>
          {/* İstatistikler */}
          <Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
            <Card sx={{ flex: "1 1 160px", minWidth: 120 }}>
              <CardContent sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}>
                <Typography color="textSecondary" variant="caption">
                  Katana Sync Toplamı
                </Typography>
                <Typography variant="h6" fontWeight={700}>
                  {supplierSyncResult?.totalCount ?? suppliers.length}
                </Typography>
              </CardContent>
            </Card>
            <Card sx={{ flex: "1 1 160px", minWidth: 120 }}>
              <CardContent sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}>
                <Typography color="textSecondary" variant="caption">
                  Koza Tedarikçi Sayısı
                </Typography>
                <Typography variant="h6" fontWeight={700}>
                  {suppliers.length}
                </Typography>
              </CardContent>
            </Card>
            {supplierSyncResult && (
              <>
                <Card sx={{ flex: "1 1 auto", minWidth: 60 }}>
                  <CardContent
                    sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}
                  >
                    <Typography color="textSecondary" variant="caption">
                      Başarılı
                    </Typography>
                    <Typography
                      variant="h6"
                      fontWeight={700}
                      color="success.main"
                    >
                      {supplierSyncResult.successCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ flex: "1 1 auto", minWidth: 60 }}>
                  <CardContent
                    sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}
                  >
                    <Typography color="textSecondary" variant="caption">
                      Atlandı
                    </Typography>
                    <Typography variant="h6" fontWeight={700} color="info.main">
                      {supplierSyncResult.skippedCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ flex: "1 1 auto", minWidth: 60 }}>
                  <CardContent
                    sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}
                  >
                    <Typography color="textSecondary" variant="caption">
                      Hatalı
                    </Typography>
                    <Typography
                      variant="h6"
                      fontWeight={700}
                      color="error.main"
                    >
                      {supplierSyncResult.errorCount}
                    </Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>

          {/* Tedarikçi Listesi */}
          <Paper sx={{ p: { xs: 1, sm: 2 } }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 1.5,
                gap: 1,
              }}
            >
              <Typography
                variant="subtitle1"
                fontWeight={600}
                sx={{ fontSize: { xs: "0.9rem", sm: "1rem" } }}
              >
                Tedarikçi Listesi
              </Typography>
              <Button
                size="small"
                variant="contained"
                color="primary"
                startIcon={
                  syncingSuppliers ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncSuppliers}
                disabled={syncingSuppliers}
                sx={{ fontSize: "0.7rem", px: 1, py: 0.5, minWidth: "auto" }}
              >
                {syncingSuppliers ? "Sync..." : "Senkronize"}
              </Button>
            </Box>

            {loadingSuppliers ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                <CircularProgress size={24} />
              </Box>
            ) : suppliers.length === 0 ? (
              <Alert severity="info" sx={{ fontSize: "0.8rem" }}>
                Henüz tedarikçi kaydı yok.
              </Alert>
            ) : (
              <TableContainer sx={{ width: "100%", overflowX: "auto" }}>
                <Table
                  size="small"
                  sx={{
                    "& .MuiTableCell-root": {
                      px: { xs: 0.75, sm: 2 },
                      py: { xs: 0.5, sm: 1 },
                      fontSize: { xs: "0.7rem", sm: "0.875rem" },
                    },
                  }}
                >
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>ID</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Kod</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Tanım</TableCell>
                      <TableCell
                        sx={{
                          whiteSpace: "nowrap",
                          display: { xs: "none", sm: "table-cell" },
                        }}
                      >
                        Vergi No
                      </TableCell>
                      <TableCell
                        sx={{
                          whiteSpace: "nowrap",
                          display: { xs: "none", sm: "table-cell" },
                        }}
                      >
                        İletişim
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {suppliers.map((sup, idx) => {
                      const hasValidId =
                        typeof sup.finansalNesneId === "number" &&
                        sup.finansalNesneId > 0;
                      let rowKey: string;
                      if (hasValidId) {
                        rowKey = `fin-${sup.finansalNesneId}`;
                      } else if (sup.kod) {
                        rowKey = `kod-${sup.kod}`;
                      } else {
                        rowKey = `supplier-${idx}`;
                      }
                      const displayId = hasValidId ? sup.finansalNesneId : "-";

                      return (
                        <TableRow key={rowKey}>
                          <TableCell>{displayId}</TableCell>
                          <TableCell>
                            <Chip
                              label={sup.kod ?? "-"}
                              size="small"
                              color="warning"
                              variant="outlined"
                              sx={{ fontSize: "0.65rem" }}
                            />
                          </TableCell>
                          <TableCell>{sup.tanim ?? "-"}</TableCell>
                          <TableCell
                            sx={{ display: { xs: "none", sm: "table-cell" } }}
                          >
                            {sup.vergiNo ?? "-"}
                          </TableCell>
                          <TableCell
                            sx={{ display: { xs: "none", sm: "table-cell" } }}
                          >
                            {sup.telefon ?? sup.email ?? "-"}
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}

      {/* Müşteri Kartları Tab */}
      {activeTab === 3 && (
        <Box sx={{ px: { xs: 1, sm: 0 } }}>
          {/* İstatistikler */}
          <Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
            <Card sx={{ flex: "1 1 160px", minWidth: 120 }}>
              <CardContent sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}>
                <Typography color="textSecondary" variant="caption">
                  Koza Müşteri Sayısı
                </Typography>
                <Typography variant="h6" fontWeight={700}>
                  {customers.length}
                </Typography>
              </CardContent>
            </Card>
            {customerSyncResult && (
              <>
                <Card sx={{ flex: "1 1 auto", minWidth: 60 }}>
                  <CardContent
                    sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}
                  >
                    <Typography color="textSecondary" variant="caption">
                      Başarılı
                    </Typography>
                    <Typography
                      variant="h6"
                      fontWeight={700}
                      color="success.main"
                    >
                      {customerSyncResult.successCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ flex: "1 1 auto", minWidth: 60 }}>
                  <CardContent
                    sx={{ py: 1, px: 1.5, "&:last-child": { pb: 1 } }}
                  >
                    <Typography color="textSecondary" variant="caption">
                      Hatalı
                    </Typography>
                    <Typography
                      variant="h6"
                      fontWeight={700}
                      color="error.main"
                    >
                      {customerSyncResult.errorCount}
                    </Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>

          {/* Müşteri Listesi */}
          <Paper sx={{ p: { xs: 1, sm: 2 } }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 1.5,
                gap: 1,
              }}
            >
              <Typography
                variant="subtitle1"
                fontWeight={600}
                sx={{ fontSize: { xs: "0.9rem", sm: "1rem" } }}
              >
                Müşteri Listesi
              </Typography>
              <Button
                size="small"
                variant="contained"
                color="primary"
                startIcon={
                  syncingCustomers ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncCustomers}
                disabled={syncingCustomers}
                sx={{ fontSize: "0.7rem", px: 1, py: 0.5, minWidth: "auto" }}
              >
                {syncingCustomers ? "Sync..." : "Senkronize"}
              </Button>
            </Box>

            {loadingCustomers ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                <CircularProgress size={24} />
              </Box>
            ) : customers.length === 0 ? (
              <Alert severity="info" sx={{ fontSize: "0.8rem" }}>
                Henüz müşteri kaydı yok.
              </Alert>
            ) : (
              <TableContainer sx={{ width: "100%", overflowX: "auto" }}>
                <Table
                  size="small"
                  sx={{
                    "& .MuiTableCell-root": {
                      px: { xs: 0.75, sm: 2 },
                      py: { xs: 0.5, sm: 1 },
                      fontSize: { xs: "0.7rem", sm: "0.875rem" },
                    },
                  }}
                >
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>ID</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Kod</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>Tanım</TableCell>
                      <TableCell
                        sx={{
                          whiteSpace: "nowrap",
                          display: { xs: "none", sm: "table-cell" },
                        }}
                      >
                        Vergi No
                      </TableCell>
                      <TableCell
                        sx={{
                          whiteSpace: "nowrap",
                          display: { xs: "none", sm: "table-cell" },
                        }}
                      >
                        Telefon
                      </TableCell>
                      <TableCell
                        sx={{
                          whiteSpace: "nowrap",
                          display: { xs: "none", sm: "table-cell" },
                        }}
                      >
                        Email
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {customers.map((customer, idx) => {
                      const hasValidId =
                        typeof customer.finansalNesneId === "number" &&
                        customer.finansalNesneId > 0;
                      let rowKey: string;
                      if (hasValidId) {
                        rowKey = `fin-${customer.finansalNesneId}`;
                      } else if (customer.kod) {
                        rowKey = `kod-${customer.kod}`;
                      } else {
                        rowKey = `customer-${idx}`;
                      }
                      const displayId = hasValidId
                        ? customer.finansalNesneId
                        : "-";

                      return (
                        <TableRow key={rowKey}>
                          <TableCell>{displayId}</TableCell>
                          <TableCell>
                            <Chip
                              label={customer.kod ?? "-"}
                              size="small"
                              color="info"
                              variant="outlined"
                              sx={{ fontSize: "0.65rem" }}
                            />
                          </TableCell>
                          <TableCell>{customer.tanim ?? "-"}</TableCell>
                          <TableCell
                            sx={{ display: { xs: "none", sm: "table-cell" } }}
                          >
                            {customer.vergiNo ?? "-"}
                          </TableCell>
                          <TableCell
                            sx={{ display: { xs: "none", sm: "table-cell" } }}
                          >
                            {customer.telefon ?? "-"}
                          </TableCell>
                          <TableCell
                            sx={{ display: { xs: "none", sm: "table-cell" } }}
                          >
                            {customer.email ?? "-"}
                          </TableCell>
                        </TableRow>
                      );
                    })}
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
