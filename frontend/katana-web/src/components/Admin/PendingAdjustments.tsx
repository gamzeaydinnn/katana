import React, { useEffect, useState } from "react";
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from "@mui/material";
import { pendingAdjustmentsAPI } from "../../services/api";

interface PendingItem {
  id: number;
  externalOrderId?: string;
  productId: number;
  sku?: string;
  quantity: number;
  requestedBy?: string;
  requestedAt?: string;
  status?: string;
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  notes?: string;
}

export default function PendingAdjustments() {
  const [items, setItems] = useState<PendingItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<PendingItem | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejecting, setRejecting] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const res = await pendingAdjustmentsAPI.list();
      // API may return either an array or an object { items, total }
      // Be defensive: accept both shapes.
      if (Array.isArray(res)) {
        setItems(res as any);
      } else if (res && Array.isArray((res as any).items)) {
        setItems((res as any).items);
      } else if (res && Array.isArray((res as any).data)) {
        // some APIs nest under data
        setItems((res as any).data);
      } else {
        setItems([]);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleApprove = async (id: number) => {
    try {
      await pendingAdjustmentsAPI.approve(id);
      await load();
    } catch (e) {
      console.error(e);
    }
  };

  const openReject = (item: PendingItem) => {
    setSelected(item);
    setRejectReason("");
  };

  const handleReject = async () => {
    if (!selected) return;
    setRejecting(true);
    try {
      await pendingAdjustmentsAPI.reject(selected.id, "admin", rejectReason);
      setSelected(null);
      await load();
    } catch (e) {
      console.error(e);
    } finally {
      setRejecting(false);
    }
  };

  return (
    <Box p={2}>
      <Typography variant="h5" gutterBottom>
        Stok Hareketleri
      </Typography>

      <Paper>
        {loading ? (
          <Box p={4} display="flex" justifyContent="center">
            <CircularProgress />
          </Box>
        ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>SKU</TableCell>
                  <TableCell>ProductId</TableCell>
                  <TableCell>Quantity</TableCell>
                  <TableCell>RequestedAt</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((it) => (
                  <TableRow key={it.id}>
                    <TableCell>{it.id}</TableCell>
                    <TableCell>{it.sku}</TableCell>
                    <TableCell>{it.productId}</TableCell>
                    <TableCell>{it.quantity}</TableCell>
                    <TableCell>
                      {it.requestedAt
                        ? new Date(it.requestedAt).toLocaleString()
                        : "-"}
                    </TableCell>
                    <TableCell>{it.status}</TableCell>
                    <TableCell align="right">
                      <Button
                        size="small"
                        variant="contained"
                        color="primary"
                        onClick={() => handleApprove(it.id)}
                        sx={{ mr: 1 }}
                      >
                        Onayla
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color="secondary"
                        onClick={() => openReject(it)}
                      >
                        Reddet
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      No pending adjustments
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>

      <Dialog
        open={!!selected}
        onClose={() => setSelected(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Bekleyen Stok Ayarlamasını Reddet</DialogTitle>
        <DialogContent>
          <Typography variant="body2" gutterBottom>
            Bu bekleyen stok ayarlamasını reddetme nedenini girin (isteğe
            bağlı).
          </Typography>
          <TextField
            fullWidth
            multiline
            rows={4}
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            placeholder="Sebep"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelected(null)}>İptal</Button>
          <Button onClick={handleReject} disabled={rejecting} color="secondary">
            {rejecting ? "Reddediliyor..." : "Reddet"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
