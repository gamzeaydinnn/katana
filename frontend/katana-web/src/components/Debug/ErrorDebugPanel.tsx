import BugReportIcon from '@mui/icons-material/BugReport';
import CloseIcon from '@mui/icons-material/Close';
import DeleteIcon from '@mui/icons-material/Delete';
import DownloadIcon from '@mui/icons-material/Download';
import RefreshIcon from '@mui/icons-material/Refresh';
import {
    Box,
    Button,
    Chip,
    Dialog,
    DialogContent,
    DialogTitle,
    IconButton,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Tooltip
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { clearErrors, downloadErrorLog, getAllErrors } from '../../utils/errorLogger';

interface LogEntry {
  timestamp: string;
  level: 'error' | 'warning' | 'info' | 'debug';
  message: string;
  source?: string;
  context?: any;
}

export const ErrorDebugPanel: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [filter, setFilter] = useState<'all' | 'error' | 'warning'>('all');

  useEffect(() => {
    if (open) {
      refreshLogs();
      const interval = setInterval(refreshLogs, 2000);
      return () => clearInterval(interval);
    }
  }, [open]);

  const refreshLogs = () => {
    const allLogs = getAllErrors();
    setLogs(allLogs);
  };

  const handleClear = () => {
    if (window.confirm('Tüm hataları temizlemek istediğinizden emin misiniz?')) {
      clearErrors();
      setLogs([]);
    }
  };

  const handleDownload = () => {
    downloadErrorLog();
  };

  const filteredLogs = logs.filter(log =>
    filter === 'all' ? true : log.level === filter
  );

  const errorCount = logs.filter(l => l.level === 'error').length;
  const warningCount = logs.filter(l => l.level === 'warning').length;

  return (
    <>
      {/* Floating Button */}
      <Tooltip title="Debug - Hataları Gör">
        <IconButton
          onClick={() => setOpen(true)}
          sx={{
            position: 'fixed',
            bottom: 20,
            right: 20,
            backgroundColor: errorCount > 0 ? '#ff5252' : '#4caf50',
            color: 'white',
            zIndex: 10000,
            '&:hover': {
              backgroundColor: errorCount > 0 ? '#ff1744' : '#45a049',
            },
          }}
        >
          <BugReportIcon />
          {(errorCount + warningCount) > 0 && (
            <Box
              sx={{
                position: 'absolute',
                top: -5,
                right: -5,
                backgroundColor: '#ff5252',
                borderRadius: '50%',
                width: 20,
                height: 20,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '12px',
                fontWeight: 'bold',
              }}
            >
              {errorCount + warningCount}
            </Box>
          )}
        </IconButton>
      </Tooltip>

      {/* Dialog */}
      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="lg" fullWidth>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Error Debug Panel ({logs.length} logs)</span>
          <IconButton size="small" onClick={() => setOpen(false)}>
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ mb: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
            <Button
              size="small"
              variant={filter === 'all' ? 'contained' : 'outlined'}
              onClick={() => setFilter('all')}
            >
              Tümü ({logs.length})
            </Button>
            <Button
              size="small"
              variant={filter === 'error' ? 'contained' : 'outlined'}
              color="error"
              onClick={() => setFilter('error')}
            >
              Hatalar ({errorCount})
            </Button>
            <Button
              size="small"
              variant={filter === 'warning' ? 'contained' : 'outlined'}
              color="warning"
              onClick={() => setFilter('warning')}
            >
              Uyarılar ({warningCount})
            </Button>
            <Box sx={{ ml: 'auto', display: 'flex', gap: 1 }}>
              <Button
                size="small"
                startIcon={<RefreshIcon />}
                onClick={refreshLogs}
              >
                Yenile
              </Button>
              <Button
                size="small"
                startIcon={<DownloadIcon />}
                onClick={handleDownload}
              >
                İndir
              </Button>
              <Button
                size="small"
                startIcon={<DeleteIcon />}
                color="error"
                onClick={handleClear}
              >
                Temizle
              </Button>
            </Box>
          </Box>

          <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
            <Table stickyHeader size="small">
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell width="80px">Level</TableCell>
                  <TableCell width="180px">Zaman</TableCell>
                  <TableCell>Mesaj</TableCell>
                  <TableCell width="120px">Kaynak</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredLogs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} align="center" sx={{ py: 3 }}>
                      Hata bulunamadı
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredLogs.map((log, idx) => (
                    <TableRow
                      key={`${log.timestamp}-${log.message}-${idx}`}
                      sx={{
                        backgroundColor: log.level === 'error' ? '#ffebee' : log.level === 'warning' ? '#fff3e0' : 'inherit',
                        '&:hover': { backgroundColor: '#f5f5f5' },
                      }}
                    >
                      <TableCell>
                        <Chip
                          label={log.level.toUpperCase()}
                          size="small"
                          color={log.level === 'error' ? 'error' : log.level === 'warning' ? 'warning' : 'default'}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell sx={{ fontSize: '11px' }}>
                        {new Date(log.timestamp).toLocaleTimeString('tr-TR')}
                      </TableCell>
                      <TableCell sx={{ fontSize: '12px', maxWidth: 400, wordBreak: 'break-word' }}>
                        {log.message}
                        {log.context && (
                          <Box sx={{ mt: 0.5, fontSize: '10px', color: '#666', fontFamily: 'monospace' }}>
                            {JSON.stringify(log.context).substring(0, 100)}...
                          </Box>
                        )}
                      </TableCell>
                      <TableCell sx={{ fontSize: '11px' }}>
                        {log.source}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </DialogContent>
      </Dialog>
    </>
  );
};

export default ErrorDebugPanel;
