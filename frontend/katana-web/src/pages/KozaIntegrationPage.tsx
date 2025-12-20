import {
    Business,
    People,
    Refresh,
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
    Container,
    IconButton,
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
    Tooltip,
    Typography,
    useMediaQuery,
    useTheme,
} from "@mui/material";
import React, { useEffect, useState } from "react";
import type {
    KatanaLocation,
    KozaStkDepo,
} from "../features/integrations/luca-koza";
import { mapKatanaLocationToKozaDepo } from "../features/integrations/luca-koza";
import api, { kozaAPI } from "../services/api";

// Tedarikçi Cari tipi
interface KozaSupplierListItem {
  finansalNesneId: number | null;
  kod: string | null;
  tanim: string | null;
  vergiNo: string | null;
  telefon: string | null;
  email: string | null;
}

// Müşteri Cari tipi
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

const KozaIntegrationPage: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));
  const [activeTab, setActiveTab] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Depo state
  const [depots, setDepots] = useState<KozaStkDepo[]>([]);
  const [totalDepots, setTotalDepots] = useState<number>(0);
  const [loadingDepots, setLoadingDepots] = useState(false);
  const [syncingDepots, setSyncingDepots] = useState(false);

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
      setError(err.message || "Depolar yüklenirken hata oluştu");
    } finally {
      setLoadingDepots(false);
    }
  };

  // Depo senkronizasyonu
  const syncDepots = async () => {
    try {
      setSyncingDepots(true);
      setError(null);
      setSuccess(null);

      let locations: KatanaLocation[] = [];
      try {
        const locationsRes = await api.get<KatanaLocation[]>("/Locations");
        locations = Array.isArray(locationsRes.data) ? locationsRes.data : [];
      } catch {
        try {
          const defaultRes = await api.get<KatanaLocation[]>(
            "/Locations/defaults"
          );
          locations = Array.isArray(defaultRes.data) ? defaultRes.data : [];
        } catch {
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

      for (const location of locations) {
        try {
          const kozaDepo = mapKatanaLocationToKozaDepo(location, "01");
          await kozaAPI.depots.create({ stkDepo: kozaDepo });
          successCount++;
        } catch {
          errorCount++;
        }
      }

      if (successCount > 0) {
        setSuccess(
          `Senkronizasyon tamamlandı: ${successCount} başarılı, ${errorCount} hatalı`
        );
      }
      await loadDepots();
    } catch (err: any) {
      setError(err.message || "Depo senkronizasyonu sırasında hata oluştu");
    } finally {
      setSyncingDepots(false);
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
      setError(err.message || "Tedarikçiler yüklenirken hata oluştu");
    } finally {
      setLoadingSuppliers(false);
    }
  };

  // Tedarikçi senkronizasyonu
  const syncSuppliers = async () => {
    try {
      setSyncingSuppliers(true);
      setError(null);
      setSuccess(null);
      setSupplierSyncResult(null);

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
      await loadSuppliers();
    } catch (err: any) {
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
      setError(err.message || "Müşteriler yüklenirken hata oluştu");
    } finally {
      setLoadingCustomers(false);
    }
  };

  // Müşteri senkronizasyonu
  const syncCustomers = async () => {
    try {
      setSyncingCustomers(true);
      setError(null);
      setSuccess(null);
      setCustomerSyncResult(null);

      interface CustomerSyncResponse {
        successCount?: number;
        errorCount?: number;
        errorMessage?: string;
      }
      const res = await api.post<CustomerSyncResponse>(
        "/admin/koza/cari/customers/sync"
      );
      const result = res.data;
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
      setError(
        err.response?.data?.error ||
          err.message ||
          "Müşteri senkronizasyonu sırasında hata oluştu"
      );
    } finally {
      setSyncingCustomers(false);
    }
  };

  useEffect(() => {
    if (activeTab === 0) loadDepots();
    else if (activeTab === 1) loadSuppliers();
    else if (activeTab === 2) loadCustomers();
  }, [activeTab]);

  const handleRefresh = () => {
    if (activeTab === 0) loadDepots();
    else if (activeTab === 1) loadSuppliers();
    else loadCustomers();
  };

  const tabConfig = [
    {
      icon: <Warehouse />,
      label: "Depo",
      color: "#667eea",
      gradient: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
    },
    {
      icon: <Business />,
      label: "Tedarikçi",
      color: "#f59e0b",
      gradient: "linear-gradient(135deg, #f59e0b 0%, #d97706 100%)",
    },
    {
      icon: <People />,
      label: "Müşteri",
      color: "#10b981",
      gradient: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
    },
  ];

  return (
    <Container
      maxWidth="lg"
      sx={{
        mt: { xs: 5.5, md: 4 },
        mb: { xs: 2.5, md: 4 },
        px: { xs: 1.5, sm: 2, md: 0 },
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", sm: "center" },
          flexDirection: { xs: "column", sm: "row" },
          gap: { xs: 2, sm: 0 },
          mb: 3,
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Box
            sx={{
              width: 48,
              height: 48,
              borderRadius: 3,
              background: tabConfig[activeTab].gradient,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              boxShadow: `0 8px 24px ${tabConfig[activeTab].color}40`,
            }}
          >
            {React.cloneElement(tabConfig[activeTab].icon, {
              sx: { color: "white", fontSize: 28 },
            })}
          </Box>
          <Box>
            <Typography
              variant="h4"
              sx={{
                fontSize: { xs: "1.5rem", md: "2rem" },
                fontWeight: 900,
                letterSpacing: "-0.02em",
                background: tabConfig[activeTab].gradient,
                WebkitBackgroundClip: "text",
                WebkitTextFillColor: "transparent",
                backgroundClip: "text",
              }}
            >
              Koza Entegrasyon
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Depo, Tedarikçi ve Müşteri kartlarını yönetin
            </Typography>
          </Box>
        </Box>
        <Tooltip title="Yenile">
          <IconButton
            onClick={handleRefresh}
            color="primary"
            sx={{
              backgroundColor: "rgba(79,134,255,0.08)",
              "&:hover": { backgroundColor: "rgba(79,134,255,0.15)" },
            }}
          >
            <Refresh />
          </IconButton>
        </Tooltip>
      </Box>

      {/* Alerts */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
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
      <Paper
        sx={{
          mb: 3,
          borderRadius: 3,
          overflow: "hidden",
          boxShadow: "0 4px 20px rgba(0,0,0,0.08)",
        }}
      >
        <Tabs
          value={activeTab}
          onChange={(_, v) => setActiveTab(v)}
          variant={isMobile ? "fullWidth" : "standard"}
          sx={{
            "& .MuiTab-root": {
              textTransform: "none",
              fontWeight: 600,
              fontSize: { xs: "0.85rem", sm: "0.95rem" },
              minHeight: 56,
              gap: 1,
            },
            "& .Mui-selected": {
              color: `${tabConfig[activeTab].color} !important`,
            },
            "& .MuiTabs-indicator": {
              height: 3,
              borderRadius: "3px 3px 0 0",
              background: tabConfig[activeTab].gradient,
            },
          }}
        >
          {tabConfig.map((tab, idx) => (
            <Tab
              key={idx}
              icon={tab.icon}
              label={tab.label}
              iconPosition="start"
            />
          ))}
        </Tabs>
      </Paper>

      {/* Depo Tab */}
      {activeTab === 0 && (
        <Box>
          {/* Stats */}
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" },
              gap: 2,
              mb: 3,
            }}
          >
            <Card
              sx={{
                background: tabConfig[0].gradient,
                color: "white",
                borderRadius: 3,
              }}
            >
              <CardContent>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                >
                  <Box>
                    <Typography variant="body2" sx={{ opacity: 0.9 }}>
                      Toplam Depo
                    </Typography>
                    <Typography variant="h3" fontWeight={800}>
                      {totalDepots}
                    </Typography>
                  </Box>
                  <Warehouse sx={{ fontSize: 48, opacity: 0.3 }} />
                </Stack>
              </CardContent>
            </Card>
          </Box>

          {/* Depo List */}
          <Paper sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 2,
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                Depo Listesi
              </Typography>
              <Button
                variant="contained"
                startIcon={
                  syncingDepots ? (
                    <CircularProgress size={18} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncDepots}
                disabled={syncingDepots}
                sx={{
                  background: tabConfig[0].gradient,
                  borderRadius: 2,
                  textTransform: "none",
                  fontWeight: 600,
                }}
              >
                {syncingDepots ? "Senkronize Ediliyor..." : "Senkronize Et"}
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
                      <TableCell>ID</TableCell>
                      <TableCell>Kod</TableCell>
                      <TableCell>Tanım</TableCell>
                      <TableCell>Kategori</TableCell>
                      <TableCell>Şehir</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {depots.map((depo, idx) => (
                      <TableRow key={`depot-${depo.depoId ?? idx}`} hover>
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

      {/* Tedarikçi Tab */}
      {activeTab === 1 && (
        <Box>
          {/* Stats */}
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "repeat(2, 1fr)",
                sm: "repeat(4, 1fr)",
              },
              gap: 2,
              mb: 3,
            }}
          >
            <Card
              sx={{
                background: tabConfig[1].gradient,
                color: "white",
                borderRadius: 3,
              }}
            >
              <CardContent sx={{ py: 2 }}>
                <Typography variant="body2" sx={{ opacity: 0.9 }}>
                  Toplam Tedarikçi
                </Typography>
                <Typography variant="h4" fontWeight={800}>
                  {suppliers.length}
                </Typography>
              </CardContent>
            </Card>
            {supplierSyncResult && (
              <>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent sx={{ py: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Başarılı
                    </Typography>
                    <Typography
                      variant="h4"
                      fontWeight={800}
                      color="success.main"
                    >
                      {supplierSyncResult.successCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent sx={{ py: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Atlandı
                    </Typography>
                    <Typography variant="h4" fontWeight={800} color="info.main">
                      {supplierSyncResult.skippedCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent sx={{ py: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Hatalı
                    </Typography>
                    <Typography
                      variant="h4"
                      fontWeight={800}
                      color="error.main"
                    >
                      {supplierSyncResult.errorCount}
                    </Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>

          {/* Supplier List */}
          <Paper sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 2,
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                Tedarikçi Listesi
              </Typography>
              <Button
                variant="contained"
                startIcon={
                  syncingSuppliers ? (
                    <CircularProgress size={18} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncSuppliers}
                disabled={syncingSuppliers}
                sx={{
                  background: tabConfig[1].gradient,
                  borderRadius: 2,
                  textTransform: "none",
                  fontWeight: 600,
                }}
              >
                {syncingSuppliers ? "Senkronize Ediliyor..." : "Senkronize Et"}
              </Button>
            </Box>

            {loadingSuppliers ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : suppliers.length === 0 ? (
              <Alert severity="info">Henüz tedarikçi kaydı yok.</Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>ID</TableCell>
                      <TableCell>Kod</TableCell>
                      <TableCell>Tanım</TableCell>
                      {!isMobile && <TableCell>Vergi No</TableCell>}
                      {!isMobile && <TableCell>İletişim</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {suppliers.map((sup, idx) => (
                      <TableRow key={`supplier-${sup.finansalNesneId ?? idx}`} hover>
                        <TableCell>{sup.finansalNesneId ?? "-"}</TableCell>
                        <TableCell>
                          <Chip
                            label={sup.kod ?? "-"}
                            size="small"
                            color="warning"
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell>{sup.tanim ?? "-"}</TableCell>
                        {!isMobile && (
                          <TableCell>{sup.vergiNo ?? "-"}</TableCell>
                        )}
                        {!isMobile && (
                          <TableCell>
                            {sup.telefon ?? sup.email ?? "-"}
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}

      {/* Müşteri Tab */}
      {activeTab === 2 && (
        <Box>
          {/* Stats */}
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "repeat(2, 1fr)",
                sm: "repeat(3, 1fr)",
              },
              gap: 2,
              mb: 3,
            }}
          >
            <Card
              sx={{
                background: tabConfig[2].gradient,
                color: "white",
                borderRadius: 3,
              }}
            >
              <CardContent sx={{ py: 2 }}>
                <Typography variant="body2" sx={{ opacity: 0.9 }}>
                  Toplam Müşteri
                </Typography>
                <Typography variant="h4" fontWeight={800}>
                  {customers.length}
                </Typography>
              </CardContent>
            </Card>
            {customerSyncResult && (
              <>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent sx={{ py: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Başarılı
                    </Typography>
                    <Typography
                      variant="h4"
                      fontWeight={800}
                      color="success.main"
                    >
                      {customerSyncResult.successCount}
                    </Typography>
                  </CardContent>
                </Card>
                <Card sx={{ borderRadius: 3 }}>
                  <CardContent sx={{ py: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Hatalı
                    </Typography>
                    <Typography
                      variant="h4"
                      fontWeight={800}
                      color="error.main"
                    >
                      {customerSyncResult.errorCount}
                    </Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>

          {/* Customer List */}
          <Paper sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                mb: 2,
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                Müşteri Listesi
              </Typography>
              <Button
                variant="contained"
                startIcon={
                  syncingCustomers ? (
                    <CircularProgress size={18} color="inherit" />
                  ) : (
                    <SyncIcon />
                  )
                }
                onClick={syncCustomers}
                disabled={syncingCustomers}
                sx={{
                  background: tabConfig[2].gradient,
                  borderRadius: 2,
                  textTransform: "none",
                  fontWeight: 600,
                }}
              >
                {syncingCustomers ? "Senkronize Ediliyor..." : "Senkronize Et"}
              </Button>
            </Box>

            {loadingCustomers ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
                <CircularProgress />
              </Box>
            ) : customers.length === 0 ? (
              <Alert severity="info">Henüz müşteri kaydı yok.</Alert>
            ) : (
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>ID</TableCell>
                      <TableCell>Kod</TableCell>
                      <TableCell>Tanım</TableCell>
                      {!isMobile && <TableCell>Vergi No</TableCell>}
                      {!isMobile && <TableCell>Telefon</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {customers.map((cust, idx) => (
                      <TableRow key={`customer-${cust.finansalNesneId ?? idx}`} hover>
                        <TableCell>{cust.finansalNesneId ?? "-"}</TableCell>
                        <TableCell>
                          <Chip
                            label={cust.kod || "-"}
                            size="small"
                            color="success"
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell>{cust.tanim || "-"}</TableCell>
                        {!isMobile && (
                          <TableCell>{cust.vergiNo ?? "-"}</TableCell>
                        )}
                        {!isMobile && (
                          <TableCell>{cust.telefon ?? "-"}</TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Box>
      )}
    </Container>
  );
};

export default KozaIntegrationPage;
