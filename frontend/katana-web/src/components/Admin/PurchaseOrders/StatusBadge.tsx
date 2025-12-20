import {
  Cancel as CancelIcon,
  CheckCircle as CheckIcon,
  DoneAll as DoneAllIcon,
  HourglassEmpty as PendingIcon,
} from "@mui/icons-material";
import { Chip, ChipProps } from "@mui/material";
import React from "react";

export type OrderStatus = "Pending" | "Approved" | "Received" | "Cancelled";

interface StatusBadgeProps {
  status: OrderStatus;
  showLabel?: boolean;
  size?: "small" | "medium";
}

interface StatusConfig {
  color: ChipProps["color"];
  icon: React.ReactElement;
  label: string;
}

const STATUS_CONFIG: Record<OrderStatus, StatusConfig> = {
  Pending: {
    color: "warning",
    icon: <PendingIcon />,
    label: "Beklemede",
  },
  Approved: {
    color: "info",
    icon: <CheckIcon />,
    label: "Onaylandı",
  },
  Received: {
    color: "success",
    icon: <DoneAllIcon />,
    label: "Teslim Alındı",
  },
  Cancelled: {
    color: "error",
    icon: <CancelIcon />,
    label: "İptal",
  },
};

const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  showLabel = true,
  size = "small",
}) => {
  const config = STATUS_CONFIG[status];

  if (!config) {
    console.warn(`Unknown status: ${status}`);
    return <Chip label={status} size={size} color="default" />;
  }

  return (
    <Chip
      icon={config.icon}
      label={showLabel ? config.label : status}
      color={config.color}
      size={size}
    />
  );
};

export default StatusBadge;
