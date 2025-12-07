/**
 * SignalR Connection Status Indicator
 * Displays real-time connection status with visual feedback
 */

import CloudDoneIcon from '@mui/icons-material/CloudDone';
import CloudOffIcon from '@mui/icons-material/CloudOff';
import ErrorIcon from '@mui/icons-material/Error';
import SyncAltIcon from '@mui/icons-material/SyncAlt';
import {
    Box,
    Chip,
    CircularProgress,
    Tooltip,
    useMediaQuery,
    useTheme,
} from '@mui/material';
import React from 'react';
import { ConnectionStatus, useSignalRStatusUI } from '../hooks/useSignalRConnection';

interface SignalRStatusIndicatorProps {
  status: ConnectionStatus;
  error?: Error | null;
  retryCount?: number;
  isPolling?: boolean;
  onClick?: () => void;
}

/**
 * Visual indicator for SignalR connection status
 * 
 * @example
 * const { status, error, retryCount, isPolling } = useSignalRConnection();
 * 
 * return (
 *   <SignalRStatusIndicator
 *     status={status}
 *     error={error}
 *     retryCount={retryCount}
 *     isPolling={isPolling}
 *     onClick={reconnect}
 *   />
 * );
 */
export const SignalRStatusIndicator: React.FC<SignalRStatusIndicatorProps> = ({
  status,
  error,
  retryCount = 0,
  isPolling = false,
  onClick,
}) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const statusUI = useSignalRStatusUI(status);

  // Render icon based on status
  const renderIcon = () => {
    switch (status) {
      case 'connected':
        return <CloudDoneIcon fontSize="small" />;
      case 'connecting':
        return <CircularProgress size={16} />;
      case 'polling':
        return <SyncAltIcon fontSize="small" />;
      case 'error':
        return <ErrorIcon fontSize="small" />;
      case 'disconnected':
      default:
        return <CloudOffIcon fontSize="small" />;
    }
  };

  // Build detailed tooltip text
  const tooltipText = (() => {
    let text = `SignalR: ${statusUI.label}`;

    if (retryCount > 0 && status !== 'connected') {
      text += `\nYeniden deneme: ${retryCount}`;
    }

    if (isPolling) {
      text += '\nPolling modunda çalışıyor';
    }

    if (error && status === 'error') {
      text += `\nHata: ${error.message}`;
    }

    if (status === 'connecting') {
      text += '\nBağlanmaya çalışılıyor...';
    }

    if (onClick) {
      text += '\nYeniden bağlanmak için tıklayın';
    }

    return text;
  })();

  // Determine chip color
  const getChipColor = (): 'default' | 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success' => {
    switch (status) {
      case 'connected':
        return 'success';
      case 'polling':
        return 'info';
      case 'error':
        return 'error';
      case 'connecting':
        return 'warning';
      case 'disconnected':
      default:
        return 'default';
    }
  };

  return (
    <Tooltip title={tooltipText} arrow>
      <Box
        component="button"
        onClick={onClick}
        sx={{
          border: 'none',
          background: 'none',
          padding: 0,
          cursor: onClick ? 'pointer' : 'default',
          transition: 'all 0.2s ease',
          opacity: onClick ? 1 : 0.8,

          '&:hover': onClick ? {
            transform: 'scale(1.05)',
            opacity: 1,
          } : {},
        }}
      >
        <Chip
          size={isMobile ? 'small' : 'medium'}
          icon={renderIcon()}
          label={isMobile ? statusUI.label.split(' ')[0] : statusUI.label}
          color={getChipColor()}
          variant={status === 'connected' ? 'filled' : 'outlined'}
          sx={{
            fontSize: isMobile ? '0.7rem' : '0.8rem',
            height: isMobile ? 24 : 32,
            animation: status === 'connecting' ? 'pulse 2s infinite' : 'none',
            '@keyframes pulse': {
              '0%, 100%': { opacity: 1 },
              '50%': { opacity: 0.7 },
            },
          }}
        />
      </Box>
    </Tooltip>
  );
};

/**
 * Connection status badge for integration into existing UI
 * Smaller variant for header/toolbar
 */
export const SignalRStatusBadge: React.FC<{
  status: ConnectionStatus;
  onClick?: () => void;
}> = ({ status, onClick }) => {
  const statusUI = useSignalRStatusUI(status);

  return (
    <Tooltip title={`Gerçek zamanlı: ${statusUI.label}`}>
      <Box
        component="button"
        onClick={onClick}
        sx={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 0.5,
          border: 'none',
          background: 'none',
          padding: '0.25rem 0.5rem',
          borderRadius: '4px',
          cursor: onClick ? 'pointer' : 'default',
          color: statusUI.color,
          fontSize: '0.75rem',
          fontWeight: 600,
          transition: 'all 0.2s ease',

          '&:hover': onClick ? {
            backgroundColor: `${statusUI.color}20`,
          } : {},
        }}
      >
        <span style={{
          display: 'inline-block',
          width: '6px',
          height: '6px',
          borderRadius: '50%',
          backgroundColor: statusUI.color,
          animation: status === 'connecting' ? 'pulse 2s infinite' : 'none',
        }} />
        {statusUI.label}
      </Box>
    </Tooltip>
  );
};

export default SignalRStatusIndicator;
