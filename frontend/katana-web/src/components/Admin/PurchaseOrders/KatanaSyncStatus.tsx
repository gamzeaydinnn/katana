import {
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
} from "@mui/icons-material";
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from "@mui/material";
import React from "react";

export interface KatanaSyncResult {
  sku: string;
  productName: string;
  success: boolean;
  action: "created" | "updated";
  newStock?: number;
  error?: string;
}

interface KatanaSyncStatusProps {
  results: KatanaSyncResult[];
}

const KatanaSyncStatus: React.FC<KatanaSyncStatusProps> = ({ results }) => {
  if (!results || results.length === 0) {
    return (
      <Card>
        <CardHeader title="Katana Senkronizasyon Durumu" />
        <CardContent>
          <Typography color="textSecondary">
            Henüz senkronizasyon yapılmadı.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  const successCount = results.filter((r) => r.success).length;
  const failCount = results.filter((r) => !r.success).length;

  return (
    <Card>
      <CardHeader title="Katana Senkronizasyon Durumu" />
      <CardContent>
        <Box sx={{ mb: 2, display: "flex", gap: 1 }}>
          <Chip
            label={`${successCount} Başarılı`}
            color="success"
            size="small"
          />
          {failCount > 0 && (
            <Chip label={`${failCount} Hatalı`} color="error" size="small" />
          )}
        </Box>

        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SKU</TableCell>
              <TableCell>Ürün</TableCell>
              <TableCell>İşlem</TableCell>
              <TableCell align="center">Durum</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {results.map((result, idx) => (
              <TableRow key={idx}>
                <TableCell>
                  <Typography variant="body2" fontFamily="monospace">
                    {result.sku}
                  </Typography>
                </TableCell>
                <TableCell>{result.productName}</TableCell>
                <TableCell>
                  {result.action === "created" ? "Oluşturuldu" : "Güncellendi"}
                  {result.newStock !== undefined &&
                    ` (Stok: ${result.newStock})`}
                </TableCell>
                <TableCell align="center">
                  {result.success ? (
                    <CheckCircleIcon color="success" fontSize="small" />
                  ) : (
                    <Tooltip title={result.error || "Bilinmeyen hata"}>
                      <ErrorIcon color="error" fontSize="small" />
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
};

export default KatanaSyncStatus;
