import React from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemButton,
  Typography,
} from "@mui/material";

export interface Branch {
  id?: number | string;
  Id?: number | string;
  branchId?: number | string;
  subeId?: number | string;
  orgSirketSubeId?: number | string;
  companyId?: number | string;
  ack?: string;
  sirketSubeAdi?: string;
  [key: string]: any;
}

interface Props {
  open: boolean;
  onClose: () => void;
  branches: Branch[];
  onSelect: (branch: Branch) => void;
}

const BranchSelector: React.FC<Props> = ({
  open,
  onClose,
  branches,
  onSelect,
}) => {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Şirket / Şube Seçimi</DialogTitle>
      <DialogContent>
        {branches && branches.length > 0 ? (
          <List>
            {branches.map((b, idx) => {
              const name =
                b.sirketSubeAdi ??
                b.ack ??
                b.name ??
                b.label ??
                `Şube ${idx + 1}`;
              const uniqueKey = String(
                b.id ??
                  b.Id ??
                  b.branchId ??
                  b.orgSirketSubeId ??
                  b.companyId ??
                  idx
              );
              const secondary = String(
                b.sirketSubeAdi ?? b.ack ?? b.name ?? b.orgSirketSubeId ?? ""
              );
              return (
                <ListItem key={uniqueKey} disablePadding>
                  <ListItemButton onClick={() => onSelect(b)}>
                    <ListItemText
                      primary={name}
                      secondary={`ID: ${secondary}`}
                    />
                  </ListItemButton>
                </ListItem>
              );
            })}
          </List>
        ) : (
          <Typography variant="body2" color="text.secondary">
            Yetkili şirket/şube bulunamadı.
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} variant="contained" sx={{ fontWeight: 600 }}>
          Kapat
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default BranchSelector;
