import React, { useCallback, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  Box,
  Alert,
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
import {
  startConnection,
  stopConnection,
  onPendingCreated,
  offPendingCreated,
  onPendingApproved,
  offPendingApproved,
} from "../../services/signalr";
import { useFeedback } from "../../providers/FeedbackProvider";
import { decodeJwtPayload, tryGetJwtUsername } from "../../utils/jwt";

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
  const [highlightedId, setHighlightedId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejecting, setRejecting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showToast } = useFeedback();

  const isPendingStatus = (status?: string) =>
    (status ?? "").trim().toLowerCase() === "pending";

  const resolveActor = () => {
    if (typeof window === "undefined") return "admin";
    try {
      const token = window.localStorage.getItem("authToken");
      const payload = decodeJwtPayload(token);
      return tryGetJwtUsername(payload) ?? "admin";
    } catch {
      return "admin";
    }
  };

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await pendingAdjustmentsAPI.list();
      if (Array.isArray(res)) {
        setItems(res as any);
      } else if (res && Array.isArray((res as any).items)) {
        setItems((res as any).items);
      } else if (res && Array.isArray((res as any).data)) {
        setItems((res as any).data);
      } else {
        setItems([]);
      }
    } catch (e: any) {
      console.error(e);
      const message =
        e?.response?.data?.error ||
        e?.message ||
        "Bekleyen ayarlamalar yüklenemedi";
      setError(message);
      showToast({ message, severity: "error" });
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    load();
  }, [load]);

  // read focusPending query param to highlight/scroll to a row
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const focus = params.get("focusPending");
    if (!focus) return;
    const id = parseInt(focus, 10);
    if (isNaN(id)) return;
    // wait until items loaded
    const t = setTimeout(() => {
      const el = document.getElementById(`pending-row-${id}`);
      if (el) {
        el.scrollIntoView({ behavior: "smooth", block: "center" });
        setHighlightedId(id);
        setTimeout(() => setHighlightedId(null), 6000);
      }
      // remove query param without reloading
      params.delete("focusPending");
      navigate(
        `${location.pathname}${
          params.toString() ? "?" + params.toString() : ""
        }`,
        { replace: true }
      );
    }, 600);
    return () => clearTimeout(t);
  }, [location.search, navigate]);

  useEffect(() => {
    // start SignalR and subscribe to events to keep the list live
    let createdHandler = (payload: any) => {
      try {
        const item = payload?.pending || payload;
        if (!item || !item.id) return;
        setItems((prev) => {
          // if already exists, replace; otherwise add to top
          const exists = prev.find((p) => p.id === item.id);
          if (exists) return prev.map((p) => (p.id === item.id ? item : p));
          return [item as any, ...prev];
        });
        showToast({
          message: `Yeni bekleyen stok #${item.id}`,
          severity: "info",
        });
      } catch (e) {
        console.warn("Error handling PendingStockAdjustmentCreated", e);
      }
    };

    let approvedHandler = (payload: any) => {
      try {
        const id = payload?.pendingId || payload?.id || payload;
        if (!id) return;
        setItems((prev) =>
          prev.map((p) =>
            p.id === id
              ? { ...p, status: "Approved", approvedBy: payload?.approvedBy }
              : p
          )
        );
        showToast({
          message: `Stok ayarlaması #${id} onaylandı`,
          severity: "success",
        });
      } catch (e) {
        console.warn("Error handling PendingStockAdjustmentApproved", e);
      }
    };

    startConnection()
      .then(() => {
        onPendingCreated(createdHandler);
        onPendingApproved(approvedHandler);
      })
      .catch((err) => console.warn("SignalR start failed", err));

    return () => {
      try {
        offPendingCreated(createdHandler);
        offPendingApproved(approvedHandler);
      } catch {}
      stopConnection().catch(() => {});
    };
  }, [showToast]);

  const handleApprove = useCallback(
    async (item: PendingItem) => {
      if (!isPendingStatus(item.status)) return;
      try {
        await pendingAdjustmentsAPI.approve(item.id, resolveActor());
        showToast({
          message: `Stok ayarlaması #${item.id} onaylandı`,
          severity: "success",
        });
        await load();
      } catch (e: any) {
        console.error(e);
        const message =
          e?.response?.data?.error ||
          e?.message ||
          "Stok ayarlaması onaylanamadı";
        showToast({ message, severity: "error" });
      }
    },
    [load, showToast]
  );

  const openReject = (item: PendingItem) => {
    setSelected(item);
    setRejectReason("");
  };

  const handleReject = useCallback(async () => {
    if (!selected || !isPendingStatus(selected.status)) return;
    setRejecting(true);
    try {
      await pendingAdjustmentsAPI.reject(
        selected.id,
        resolveActor(),
        rejectReason.trim() || undefined
      );
      showToast({
        message: `Stok ayarlaması #${selected.id} reddedildi`,
        severity: "success",
      });
      setSelected(null);
      await load();
    } catch (e) {
      console.error(e);
      const message =
        (e as any)?.response?.data?.error ||
        (e as any)?.message ||
        "Stok ayarlaması reddedilemedi";
      showToast({ message, severity: "error" });
    }
    setRejecting(false);
  }, [load, rejectReason, selected, showToast]);

  return (
    <Box p={2}>
      <Typography variant="h5" gutterBottom>
        Stok Hareketleri
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

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
                  <TableRow
                    id={`pending-row-${it.id}`}
                    key={it.id}
                    sx={
                      highlightedId === it.id
                        ? {
                            backgroundColor: (theme) =>
                              `${theme.palette.primary.light}33`,
                          }
                        : {}
                    }
                  >
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
                        disabled={!isPendingStatus(it.status)}
                        onClick={() => handleApprove(it)}
                        sx={{ mr: 1 }}
                      >
                        Onayla
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color="secondary"
                        disabled={!isPendingStatus(it.status)}
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
