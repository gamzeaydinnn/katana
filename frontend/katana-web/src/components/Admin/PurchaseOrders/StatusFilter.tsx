import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  SelectChangeEvent,
} from "@mui/material";
import React from "react";

export interface OrderStats {
  total: number;
  synced: number;
  notSynced: number;
  withErrors: number;
  pending: number;
  approved: number;
  received: number;
  cancelled: number;
}

interface StatusFilterProps {
  value: string;
  onChange: (status: string) => void;
  stats: OrderStats;
}

const StatusFilter: React.FC<StatusFilterProps> = ({
  value,
  onChange,
  stats,
}) => {
  const handleChange = (event: SelectChangeEvent) => {
    onChange(event.target.value);
  };

  return (
    <FormControl size="small" sx={{ minWidth: 200 }}>
      <InputLabel id="status-filter-label">Sipariş Durumu</InputLabel>
      <Select
        labelId="status-filter-label"
        id="status-filter"
        value={value}
        label="Sipariş Durumu"
        onChange={handleChange}
      >
        <MenuItem value="all">Tümü ({stats.total})</MenuItem>
        <MenuItem value="Pending">Beklemede ({stats.pending})</MenuItem>
        <MenuItem value="Approved">Onaylandı ({stats.approved})</MenuItem>
        <MenuItem value="Received">Teslim Alındı ({stats.received})</MenuItem>
        <MenuItem value="Cancelled">İptal ({stats.cancelled})</MenuItem>
      </Select>
    </FormControl>
  );
};

export default StatusFilter;
