import {
  CheckCircle as CheckIcon,
  DoneAll as DoneAllIcon,
} from "@mui/icons-material";
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from "@mui/material";
import React, { useState } from "react";
import { OrderStatus } from "./StatusBadge";

interface StatusActionsProps {
  currentStatus: OrderStatus;
  orderNo: string;
  onStatusChange: (newStatus: OrderStatus) => Promise<void>;
  loading: boolean;
}

const StatusActions: React.FC<StatusActionsProps> = ({
  currentStatus,
  orderNo,
  onStatusChange,
  loading,
}) => {
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    action: "approve" | "receive" | null;
  }>({
    open: false,
    action: null,
  });

  const canApprove = currentStatus === "Pending";
  const canReceive = currentStatus === "Approved";

  const handleOpenConfirm = (action: "approve" | "receive") => {
    setConfirmDialog({ open: true, action });
  };

  const handleCloseConfirm = () => {
    setConfirmDialog({ open: false, action: null });
  };

  const handleConfirm = async () => {
    if (confirmDialog.action === "approve") {
      await onStatusChange("Approved");
    } else if (confirmDialog.action === "receive") {
      await onStatusChange("Received");
    }
    handleCloseConfirm();
  };

  const getDialogContent = () => {
    if (confirmDialog.action === "approve") {
      return {
        title: "Siparişi Onayla",
        message: `${orderNo} numaralı siparişi onaylamak istediğinizden emin misiniz? Onaylanan sipariş için Luca'da stok kartları oluşturulacaktır.`,
      };
    }
    return {
      title: "Teslim Alındı Olarak İşaretle",
      message: `${orderNo} numaralı siparişin teslim alındığını onaylıyor musunuz? Bu işlem stok hareketlerini kaydedecektir.`,
    };
  };

  if (!canApprove && !canReceive) {
    return null;
  }

  const dialogContent = getDialogContent();

  return (
    <>
      <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
        {canApprove && (
          <Button
            variant="contained"
            color="primary"
            startIcon={
              loading ? (
                <CircularProgress size={20} color="inherit" />
              ) : (
                <CheckIcon />
              )
            }
            onClick={() => handleOpenConfirm("approve")}
            disabled={loading}
            size="large"
          >
            Siparişi Onayla
          </Button>
        )}

        {canReceive && (
          <Button
            variant="contained"
            color="success"
            startIcon={
              loading ? (
                <CircularProgress size={20} color="inherit" />
              ) : (
                <DoneAllIcon />
              )
            }
            onClick={() => handleOpenConfirm("receive")}
            disabled={loading}
            size="large"
          >
            Teslim Alındı
          </Button>
        )}
      </Box>

      <Dialog
        open={confirmDialog.open}
        onClose={handleCloseConfirm}
        aria-labelledby="confirm-dialog-title"
        aria-describedby="confirm-dialog-description"
      >
        <DialogTitle id="confirm-dialog-title">
          {dialogContent.title}
        </DialogTitle>
        <DialogContent>
          <DialogContentText id="confirm-dialog-description">
            {dialogContent.message}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseConfirm} disabled={loading}>
            İptal
          </Button>
          <Button
            onClick={handleConfirm}
            variant="contained"
            color="primary"
            disabled={loading}
            autoFocus
          >
            {loading ? <CircularProgress size={20} /> : "Onayla"}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default StatusActions;
