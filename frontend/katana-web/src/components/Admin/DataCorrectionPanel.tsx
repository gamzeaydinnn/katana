import React, { useState, useEffect } from "react";
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  IconButton,
  Tooltip,
  Stack,
  Chip,
  useMediaQuery,
} from "@mui/material";
import CompareArrowsIcon from "@mui/icons-material/CompareArrows";
import EditIcon from "@mui/icons-material/Edit";
import RefreshIcon from "@mui/icons-material/Refresh";
import SaveIcon from "@mui/icons-material/Save";
import api from "../../services/api";
import { decodeJwtPayload, getJwtRoles } from "../../utils/jwt";

interface KatanaProduct {
  id: string;
  sku: string;
  name: string;
  salesPrice?: number;
  onHand?: number;
}

interface LucaProduct {
  id: number;
  productCode: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

interface ComparisonResult {
  sku: string;
  katanaProduct?: KatanaProduct;
  lucaProduct?: LucaProduct;
  issues: {
    field: string;
    katanaValue: any;
    lucaValue: any;
    issue: string;
  }[];
}

const DataCorrectionPanel: React.FC = () => {
  const [sourceTab, setSourceTab] = useState<"comparison" | "katana" | "luca">(
    "comparison"
  );
  const isMobile = useMediaQuery("(max-width:900px)");

  // Karşılaştırma
  const [comparisons, setComparisons] = useState<ComparisonResult[]>([]);

  // Katana
  const [katanaIssueProducts, setKatanaIssueProducts] = useState<
    KatanaProduct[]
  >([]);
  const [selectedKatana, setSelectedKatana] = useState<KatanaProduct | null>(
    null
  );
  const [katanaEditOpen, setKatanaEditOpen] = useState(false);
  const [katanaEditData, setKatanaEditData] = useState({
    name: "",
    salesPrice: 0,
    onHand: 0,
  });

  // Luca
  const [lucaIssueProducts, setLucaIssueProducts] = useState<LucaProduct[]>([]);
  const [selectedLuca, setSelectedLuca] = useState<LucaProduct | null>(null);
  const [lucaEditOpen, setLucaEditOpen] = useState(false);
  const [lucaEditData, setLucaEditData] = useState({
    productName: "",
    unitPrice: 0,
    quantity: 0,
  });

  // Common
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const _token =
    typeof window !== "undefined"
      ? window.localStorage.getItem("authToken")
      : null;
  const _roles = getJwtRoles(decodeJwtPayload(_token));
  const canEdit = _roles.includes("admin") || _roles.includes("stokyonetici");

  // Compare Katana vs Luca data
  const performComparison = async () => {
    setLoading(true);
    setError(null);
    try {
      // Fetch both data sets
      const [katanaResponse, lucaResponse] = await Promise.all([
        api.get<any>("/Products/katana?sync=true"),
        api.get<any>("/Products/luca"),
      ]);

      const katanaData = katanaResponse.data?.data || [];
      const lucaData = lucaResponse.data?.data || [];

      // Compare data
      const comparisonResults: ComparisonResult[] = [];
      const katanaIssues: KatanaProduct[] = [];
      const lucaIssues: LucaProduct[] = [];

      katanaData.forEach((katanaProduct: KatanaProduct) => {
        const lucaProduct = lucaData.find(
          (lp: LucaProduct) => lp.productCode === katanaProduct.sku
        );

        const issues: any[] = [];
        let hasKatanaIssue = false;
        let hasLucaIssue = false;

        if (lucaProduct) {
          // Compare price
          if (
            Math.abs((katanaProduct.salesPrice || 0) - lucaProduct.unitPrice) >
            0.01
          ) {
            issues.push({
              field: "Fiyat",
              katanaValue: katanaProduct.salesPrice?.toFixed(2) || "0.00",
              lucaValue: lucaProduct.unitPrice.toFixed(2),
              issue: "Fiyat uyuşmazlığı",
            });
            hasKatanaIssue = true;
            hasLucaIssue = true;
          }

          // Compare stock
          if ((katanaProduct.onHand || 0) !== lucaProduct.quantity) {
            issues.push({
              field: "Stok",
              katanaValue: katanaProduct.onHand || 0,
              lucaValue: lucaProduct.quantity,
              issue: "Stok uyuşmazlığı",
            });
            hasKatanaIssue = true;
            hasLucaIssue = true;
          }

          // Compare name
          if (katanaProduct.name !== lucaProduct.productName) {
            issues.push({
              field: "İsim",
              katanaValue: katanaProduct.name,
              lucaValue: lucaProduct.productName,
              issue: "Ürün adı uyuşmazlığı",
            });
            hasKatanaIssue = true;
            hasLucaIssue = true;
          }
        } else {
          // Product only exists in Katana
          issues.push({
            field: "Varlık",
            katanaValue: "Mevcut",
            lucaValue: "Yok",
            issue: "Ürün sadece Katana'da var",
          });
          hasLucaIssue = true;
        }

        if (issues.length > 0) {
          comparisonResults.push({
            sku: katanaProduct.sku,
            katanaProduct,
            lucaProduct,
            issues,
          });

          if (hasKatanaIssue) {
            katanaIssues.push(katanaProduct);
          }
          if (hasLucaIssue && lucaProduct) {
            lucaIssues.push(lucaProduct);
          }
        }
      });

      // Check for Luca-only products
      lucaData.forEach((lucaProduct: LucaProduct) => {
        const katanaExists = katanaData.find(
          (kp: KatanaProduct) => kp.sku === lucaProduct.productCode
        );

        if (!katanaExists) {
          comparisonResults.push({
            sku: lucaProduct.productCode,
            lucaProduct,
            issues: [
              {
                field: "Varlık",
                katanaValue: "Yok",
                lucaValue: "Mevcut",
                issue: "Ürün sadece Luca'da var",
              },
            ],
          });
          lucaIssues.push(lucaProduct);
        }
      });

      setComparisons(comparisonResults);
      setKatanaIssueProducts(katanaIssues);
      setLucaIssueProducts(lucaIssues);
    } catch (err: any) {
      setError(err.response?.data?.error || "Karşılaştırma başarısız");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    performComparison();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Katana edit handlers
  const handleKatanaEdit = (product: KatanaProduct) => {
    setSelectedKatana(product);
    setKatanaEditData({
      name: product.name,
      salesPrice: product.salesPrice || 0,
      onHand: product.onHand || 0,
    });
    setKatanaEditOpen(true);
  };

  const handleKatanaSave = async () => {
    if (!selectedKatana) return;
    setSaving(true);
    setError(null);
    try {
      await api.put(`/Products/${selectedKatana.id}`, {
        Name: katanaEditData.name,
        SKU: selectedKatana.sku,
        Price: katanaEditData.salesPrice,
        Stock: katanaEditData.onHand,
        CategoryId: 1001,
        IsActive: true,
      });
      setSuccess("Katana ürünü başarıyla güncellendi!");
      setKatanaEditOpen(false);
      // Refresh comparison after edit
      performComparison();
    } catch (err: any) {
      setError(err.response?.data?.error || "Güncelleme başarısız");
    } finally {
      setSaving(false);
    }
  };

  // Luca edit handlers
  const handleLucaEdit = (product: LucaProduct) => {
    setSelectedLuca(product);
    setLucaEditData({
      productName: product.productName,
      unitPrice: product.unitPrice,
      quantity: product.quantity,
    });
    setLucaEditOpen(true);
  };

  const handleLucaSave = async () => {
    if (!selectedLuca) return;
    setSaving(true);
    setError(null);
    try {
      await api.put(`/Products/luca/${selectedLuca.id}`, {
        productCode: selectedLuca.productCode,
        productName: lucaEditData.productName,
        unit: "Adet",
        quantity: lucaEditData.quantity,
        unitPrice: lucaEditData.unitPrice,
        vatRate: 20,
      });
      setSuccess("Luca ürünü başarıyla güncellendi!");
      setLucaEditOpen(false);
      // Refresh comparison after edit
      performComparison();
    } catch (err: any) {
      setError(err.response?.data?.error || "Güncelleme başarısız");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
            mb={2}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <CompareArrowsIcon color="primary" />
              <Typography variant="h5">
                Veri Düzeltme ve Karşılaştırma
              </Typography>
            </Stack>
            <Tooltip title="Yenile">
              <IconButton
                onClick={() => performComparison()}
                disabled={loading}
              >
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Stack>

          <Box sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}>
            <Tabs value={sourceTab} onChange={(_, v) => setSourceTab(v)}>
              <Tab label="Karşılaştırma" value="comparison" />
              <Tab label="Katana Sorunları" value="katana" />
              <Tab label="Luca Sorunları" value="luca" />
            </Tabs>
          </Box>
        </CardContent>
      </Card>

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

      {loading ? (
        <Box display="flex" justifyContent="center" p={4}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* Comparison Tab */}
          {sourceTab === "comparison" &&
            (isMobile ? (
              <Stack spacing={1.5}>
                {comparisons.length === 0 && (
                  <Typography
                    color="text.secondary"
                    align="center"
                    sx={{ py: 2 }}
                  >
                    Uyuşmazlık bulunamadı - Tüm veriler senkronize!
                  </Typography>
                )}
                {comparisons.map((comp) => (
                  <Paper
                    key={comp.sku}
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: "1px solid",
                      borderColor: "divider",
                    }}
                  >
                    <Typography variant="subtitle1" fontWeight={700}>
                      {comp.sku} •{" "}
                      {comp.katanaProduct?.name || comp.lucaProduct?.productName}
                    </Typography>
                    <Box
                      sx={{
                        display: "grid",
                        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                        columnGap: 1,
                        rowGap: 1,
                        mt: 1,
                      }}
                    >
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Katana
                        </Typography>
                        {comp.katanaProduct ? (
                          <Typography fontWeight={600}>
                            Fiyat:{" "}
                            {comp.katanaProduct.salesPrice?.toFixed(2) || "0.00"}{" "}
                            ₺ • Stok: {comp.katanaProduct.onHand || 0}
                          </Typography>
                        ) : (
                          <Chip label="Yok" size="small" color="error" />
                        )}
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Luca
                        </Typography>
                        {comp.lucaProduct ? (
                          <Typography fontWeight={600}>
                            Fiyat: {comp.lucaProduct.unitPrice.toFixed(2)} ₺ •
                            Stok: {comp.lucaProduct.quantity}
                          </Typography>
                        ) : (
                          <Chip label="Yok" size="small" color="error" />
                        )}
                      </Box>
                    </Box>
                    <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                      {comp.issues.map((issue, idx) => (
                        <Chip
                          key={idx}
                          label={issue.issue}
                          size="small"
                          color="warning"
                          variant="outlined"
                        />
                      ))}
                    </Stack>
                    {canEdit && (
                      <Stack
                        direction="row"
                        spacing={1}
                        sx={{ mt: 1 }}
                        flexWrap="wrap"
                      >
                        {comp.katanaProduct && (
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => handleKatanaEdit(comp.katanaProduct!)}
                          >
                            Katana Düzelt
                          </Button>
                        )}
                        {comp.lucaProduct && (
                          <Button
                            size="small"
                            variant="outlined"
                            color="secondary"
                            onClick={() => handleLucaEdit(comp.lucaProduct!)}
                          >
                            Luca Düzelt
                          </Button>
                        )}
                      </Stack>
                    )}
                  </Paper>
                ))}
              </Stack>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                      <TableCell>
                        <strong>SKU</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ürün Adı</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Katana Veri</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Luca Veri</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Sorunlar</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>İşlemler</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {comparisons.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={6} align="center">
                          <Typography color="textSecondary">
                            Uyuşmazlık bulunamadı - Tüm veriler senkronize!
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      comparisons.map((comp) => (
                        <TableRow key={comp.sku} hover>
                          <TableCell>
                            <strong>{comp.sku}</strong>
                          </TableCell>
                          <TableCell>
                            {comp.katanaProduct?.name ||
                              comp.lucaProduct?.productName}
                          </TableCell>
                          <TableCell>
                            {comp.katanaProduct ? (
                              <Box>
                                <Typography variant="caption">
                                  Fiyat:{" "}
                                  {comp.katanaProduct.salesPrice?.toFixed(2) ||
                                    "0.00"}{" "}
                                  ₺
                                </Typography>
                                <br />
                                <Typography variant="caption">
                                  Stok: {comp.katanaProduct.onHand || 0}
                                </Typography>
                              </Box>
                            ) : (
                              <Chip label="Yok" size="small" color="error" />
                            )}
                          </TableCell>
                          <TableCell>
                            {comp.lucaProduct ? (
                              <Box>
                                <Typography variant="caption">
                                  Fiyat: {comp.lucaProduct.unitPrice.toFixed(2)}{" "}
                                  ₺
                                </Typography>
                                <br />
                                <Typography variant="caption">
                                  Stok: {comp.lucaProduct.quantity}
                                </Typography>
                              </Box>
                            ) : (
                              <Chip label="Yok" size="small" color="error" />
                            )}
                          </TableCell>
                          <TableCell>
                            <Stack spacing={0.5}>
                              {comp.issues.map((issue, idx) => (
                                <Chip
                                  key={idx}
                                  label={issue.issue}
                                  size="small"
                                  color="warning"
                                  variant="outlined"
                                />
                              ))}
                            </Stack>
                          </TableCell>
                          <TableCell align="center">
                            <Stack
                              direction="row"
                              spacing={1}
                              justifyContent="center"
                            >
                              {comp.katanaProduct && canEdit && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  color="primary"
                                  onClick={() =>
                                    handleKatanaEdit(comp.katanaProduct!)
                                  }
                                >
                                  Katana Düzelt
                                </Button>
                              )}
                              {comp.lucaProduct && canEdit && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  color="secondary"
                                  onClick={() =>
                                    handleLucaEdit(comp.lucaProduct!)
                                  }
                                >
                                  Luca Düzelt
                                </Button>
                              )}
                            </Stack>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            ))}

          {/* Katana Issues Tab */}
          {sourceTab === "katana" &&
            (isMobile ? (
              <Stack spacing={1.5}>
                {katanaIssueProducts.length === 0 && (
                  <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
                    Katana sorun yaşayan ürün bulunamadı
                  </Typography>
                )}
                {katanaIssueProducts.map((product) => (
                  <Paper
                    key={product.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: "1px solid",
                      borderColor: "divider",
                    }}
                  >
                    <Typography variant="subtitle1" fontWeight={600}>
                      {product.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      SKU: <strong>{product.sku}</strong>
                    </Typography>
                    <Box
                      sx={{
                        display: "grid",
                        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                        columnGap: 1,
                        rowGap: 1,
                        mt: 1,
                      }}
                    >
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Fiyat
                        </Typography>
                        <Typography fontWeight={600}>
                          {product.salesPrice?.toFixed(2) || "0.00"} ₺
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Stok
                        </Typography>
                        <Typography fontWeight={600}>
                          {product.onHand || 0}
                        </Typography>
                      </Box>
                    </Box>
                    {canEdit && (
                      <Button
                        fullWidth
                        size="small"
                        variant="outlined"
                        startIcon={<EditIcon />}
                        sx={{ mt: 1.25 }}
                        onClick={() => handleKatanaEdit(product)}
                      >
                        Katana Düzelt
                      </Button>
                    )}
                  </Paper>
                ))}
              </Stack>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                      <TableCell>
                        <strong>SKU</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ürün Adı</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Fiyat (₺)</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Stok</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>İşlemler</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {katanaIssueProducts.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={5} align="center">
                          <Typography color="textSecondary">
                            Katana sorun yaşayan ürün bulunamadı
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      katanaIssueProducts.map((product) => (
                        <TableRow key={product.id} hover>
                          <TableCell>
                            <strong>{product.sku}</strong>
                          </TableCell>
                          <TableCell>{product.name}</TableCell>
                          <TableCell align="right">
                            {product.salesPrice?.toFixed(2) || "0.00"}
                          </TableCell>
                          <TableCell align="right">
                            {product.onHand || 0}
                          </TableCell>
                          <TableCell align="center">
                            {canEdit ? (
                              <IconButton
                                size="small"
                                onClick={() => handleKatanaEdit(product)}
                                color="primary"
                              >
                                <EditIcon />
                              </IconButton>
                            ) : (
                              <Typography variant="body2" color="text.secondary">
                                -
                              </Typography>
                            )}
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            ))}

          {/* Luca Issues Tab */}
          {sourceTab === "luca" &&
            (isMobile ? (
              <Stack spacing={1.5}>
                {lucaIssueProducts.length === 0 && (
                  <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
                    Luca sorun yaşayan ürün bulunamadı
                  </Typography>
                )}
                {lucaIssueProducts.map((product) => (
                  <Paper
                    key={product.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 2,
                      border: "1px solid",
                      borderColor: "divider",
                    }}
                  >
                    <Typography variant="subtitle1" fontWeight={600}>
                      {product.productName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Kod: <strong>{product.productCode}</strong>
                    </Typography>
                    <Box
                      sx={{
                        display: "grid",
                        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                        columnGap: 1,
                        rowGap: 1,
                        mt: 1,
                      }}
                    >
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Birim Fiyat
                        </Typography>
                        <Typography fontWeight={600}>
                          {product.unitPrice.toFixed(2)} ₺
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">
                          Miktar
                        </Typography>
                        <Typography fontWeight={600}>
                          {product.quantity}
                        </Typography>
                      </Box>
                    </Box>
                    {canEdit && (
                      <Button
                        fullWidth
                        size="small"
                        variant="outlined"
                        startIcon={<EditIcon />}
                        sx={{ mt: 1.25 }}
                        onClick={() => handleLucaEdit(product)}
                      >
                        Luca Düzelt
                      </Button>
                    )}
                  </Paper>
                ))}
              </Stack>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                      <TableCell>
                        <strong>Ürün Kodu</strong>
                      </TableCell>
                      <TableCell>
                        <strong>Ürün Adı</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Birim Fiyat (₺)</strong>
                      </TableCell>
                      <TableCell align="right">
                        <strong>Miktar</strong>
                      </TableCell>
                      <TableCell align="center">
                        <strong>İşlemler</strong>
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {lucaIssueProducts.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={5} align="center">
                          <Typography color="textSecondary">
                            Luca sorun yaşayan ürün bulunamadı
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      lucaIssueProducts.map((product) => (
                        <TableRow key={product.id} hover>
                          <TableCell>
                            <strong>{product.productCode}</strong>
                          </TableCell>
                          <TableCell>{product.productName}</TableCell>
                          <TableCell align="right">
                            {product.unitPrice.toFixed(2)}
                          </TableCell>
                          <TableCell align="right">{product.quantity}</TableCell>
                          <TableCell align="center">
                            {canEdit ? (
                              <IconButton
                                size="small"
                                onClick={() => handleLucaEdit(product)}
                                color="primary"
                              >
                                <EditIcon />
                              </IconButton>
                            ) : (
                              <Typography variant="body2" color="text.secondary">
                                -
                              </Typography>
                            )}
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            ))}
        </>
      )}

      {/* Katana Edit Dialog */}
      <Dialog
        open={katanaEditOpen}
        onClose={() => setKatanaEditOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Katana Ürünü Düzelt</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 2 }}>
            <TextField
              fullWidth
              label="Ürün Adı"
              value={katanaEditData.name}
              onChange={(e) =>
                setKatanaEditData({ ...katanaEditData, name: e.target.value })
              }
              size="small"
            />
            <TextField
              fullWidth
              label="Fiyat (₺)"
              type="number"
              inputProps={{ step: "0.01" }}
              value={
                katanaEditData.salesPrice === 0 ? "" : katanaEditData.salesPrice
              }
              onChange={(e) => {
                const value = e.target.value.trim();
                const parsed = value === "" ? 0 : parseFloat(value);
                setKatanaEditData({
                  ...katanaEditData,
                  salesPrice: isNaN(parsed) ? 0 : parsed,
                });
              }}
              size="small"
            />
            <TextField
              fullWidth
              label="Stok"
              type="number"
              value={katanaEditData.onHand === 0 ? "" : katanaEditData.onHand}
              onChange={(e) => {
                const value = e.target.value.trim();
                const parsed = value === "" ? 0 : parseInt(value);
                setKatanaEditData({
                  ...katanaEditData,
                  onHand: isNaN(parsed) ? 0 : parsed,
                });
              }}
              size="small"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setKatanaEditOpen(false)}
            variant="outlined"
            sx={{
              fontWeight: 600,
              borderColor: "#64748b",
              color: "#64748b",
              "&:hover": {
                borderColor: "#475569",
                backgroundColor: "rgba(100, 116, 139, 0.04)",
              },
            }}
          >
            İptal
          </Button>
          <Button
            onClick={handleKatanaSave}
            variant="contained"
            disabled={saving}
            startIcon={<SaveIcon />}
            sx={{
              fontWeight: 600,
              color: "white",
              backgroundColor: "#3b82f6",
              "&:hover": {
                backgroundColor: "#2563eb",
              },
            }}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      {/* Luca Edit Dialog */}
      <Dialog
        open={lucaEditOpen}
        onClose={() => setLucaEditOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Luca Ürünü Düzelt</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 2 }}>
            <TextField
              fullWidth
              label="Ürün Adı"
              value={lucaEditData.productName}
              onChange={(e) =>
                setLucaEditData({
                  ...lucaEditData,
                  productName: e.target.value,
                })
              }
              size="small"
            />
            <TextField
              fullWidth
              label="Birim Fiyat (₺)"
              type="number"
              inputProps={{ step: "0.01" }}
              value={lucaEditData.unitPrice === 0 ? "" : lucaEditData.unitPrice}
              onChange={(e) => {
                const value = e.target.value.trim();
                const parsed = value === "" ? 0 : parseFloat(value);
                setLucaEditData({
                  ...lucaEditData,
                  unitPrice: isNaN(parsed) ? 0 : parsed,
                });
              }}
              size="small"
            />
            <TextField
              fullWidth
              label="Miktar"
              type="number"
              value={lucaEditData.quantity === 0 ? "" : lucaEditData.quantity}
              onChange={(e) => {
                const value = e.target.value.trim();
                const parsed = value === "" ? 0 : parseInt(value);
                setLucaEditData({
                  ...lucaEditData,
                  quantity: isNaN(parsed) ? 0 : parsed,
                });
              }}
              size="small"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setLucaEditOpen(false)}
            variant="outlined"
            sx={{
              fontWeight: 600,
              borderColor: "#64748b",
              color: "#64748b",
              "&:hover": {
                borderColor: "#475569",
                backgroundColor: "rgba(100, 116, 139, 0.04)",
              },
            }}
          >
            İptal
          </Button>
          <Button
            onClick={handleLucaSave}
            variant="contained"
            disabled={saving}
            startIcon={<SaveIcon />}
            sx={{
              fontWeight: 600,
              color: "white",
              backgroundColor: "#3b82f6",
              "&:hover": {
                backgroundColor: "#2563eb",
              },
            }}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default DataCorrectionPanel;
