/**
 * useSignalRConnection Hook
 * Advanced SignalR connection management with exponential backoff, fallback polling, and status tracking
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import { useFeedback } from '../providers/FeedbackProvider';
import { startConnection, stopConnection } from '../services/signalr';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'polling' | 'error';

interface UseSignalRConnectionOptions {
  enabled?: boolean;
  maxRetries?: number;
  initialDelayMs?: number;
  maxDelayMs?: number;
  fallbackToPolling?: boolean;
  pollingIntervalMs?: number;
  showNotifications?: boolean;
  onStatusChange?: (status: ConnectionStatus) => void;
}

interface UseSignalRConnectionResult {
  isConnected: boolean;
  status: ConnectionStatus;
  error: Error | null;
  retryCount: number;
  isPolling: boolean;
  reconnect: () => Promise<void>;
}

/**
 * Advanced hook for SignalR connection management
 * 
 * Features:
 * - Exponential backoff retry logic (max 3 retries by default)
 * - Fallback to polling if WebSocket fails
 * - Connection status tracking
 * - User-friendly notifications
 * - Automatic reconnection
 * - Comprehensive logging
 * 
 * @example
 * const { isConnected, status, error } = useSignalRConnection({
 *   maxRetries: 3,
 *   showNotifications: true,
 *   onStatusChange: (status) => console.log('Status:', status)
 * });
 */
export function useSignalRConnection(
  options: UseSignalRConnectionOptions = {}
): UseSignalRConnectionResult {
  const {
    enabled = true,
    maxRetries = 3,
    initialDelayMs = 1000,
    maxDelayMs = 30000,
    fallbackToPolling = true,
    pollingIntervalMs = 30000,
    showNotifications = true,
    onStatusChange,
  } = options;

  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const [error, setError] = useState<Error | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const [isPolling, setIsPolling] = useState(false);

  const attemptsRef = useRef(0);
  const pollingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const retryTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const { showToast } = useFeedback();

  // Calculate exponential backoff delay
  const getBackoffDelay = useCallback((attempt: number): number => {
    const delay = Math.min(
      initialDelayMs * Math.pow(2, attempt),
      maxDelayMs
    );
    return delay + Math.random() * 1000; // Add jitter
  }, [initialDelayMs, maxDelayMs]);

  // Update status and call callback
  const updateStatus = useCallback((newStatus: ConnectionStatus) => {
    setStatus(newStatus);
    onStatusChange?.(newStatus);

    // Log connection status changes
    const statusEmojis: Record<ConnectionStatus, string> = {
      'disconnected': '❌',
      'connecting': '🔄',
      'connected': '✅',
      'polling': '📡',
      'error': '⚠️',
    };

    console.log(
      `[SignalR Hook] ${statusEmojis[newStatus]} Connection status: ${newStatus}`
    );
  }, [onStatusChange]);

  // Attempt to connect with exponential backoff
  const attemptConnection = useCallback(async (attempt: number = 0) => {
    if (!enabled) return;

    if (attempt > maxRetries) {
      const errorMsg = `Failed to connect after ${maxRetries} retries`;
      const err = new Error(errorMsg);
      setError(err);
      updateStatus('error');

      if (showNotifications && fallbackToPolling) {
        showToast({
          message: 'SignalR bağlantısı başarısız. Polling moduna geçildi.',
          severity: 'warning',
          durationMs: 5000,
        });
        startPolling();
      } else if (showNotifications) {
        showToast({
          message: 'Gerçek zamanlı güncellemeler kullanılamıyor. Sayfayı yenileyin.',
          severity: 'error',
          durationMs: 6000,
        });
      }

      console.warn(
        `[SignalR Hook] ⚠️ Max retries (${maxRetries}) reached. Fallback activated.`
      );
      return;
    }

    try {
      console.log(
        `[SignalR Hook] 🔄 Attempting connection (attempt ${attempt + 1}/${maxRetries + 1})...`
      );
      updateStatus('connecting');
      setRetryCount(attempt);
      attemptsRef.current = attempt;

      await startConnection();

      console.log('[SignalR Hook] ✅ Connection established successfully');
      updateStatus('connected');
      setError(null);
      setRetryCount(0);
      attemptsRef.current = 0;

      if (isPolling) {
        stopPolling();
      }

      if (showNotifications && attempt > 0) {
        showToast({
          message: 'Gerçek zamanlı güncellemeler yeniden bağlandı',
          severity: 'success',
          durationMs: 3000,
        });
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      console.error(
        `[SignalR Hook] ❌ Connection attempt ${attempt + 1} failed:`,
        error.message
      );

      setError(error);

      if (attempt < maxRetries) {
        const delay = getBackoffDelay(attempt);
        console.log(
          `[SignalR Hook] ⏳ Retrying in ${Math.round(delay / 1000)} seconds...`
        );

        // Schedule next retry
        retryTimeoutRef.current = setTimeout(() => {
          attemptConnection(attempt + 1);
        }, delay);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, maxRetries, getBackoffDelay, updateStatus, showNotifications, fallbackToPolling, isPolling, showToast]);

  // Polling fallback (simple periodic check without WebSocket)
  const startPolling = useCallback(() => {
    if (isPolling || !fallbackToPolling) return;

    console.log(
      `[SignalR Hook] 📡 Starting polling fallback (interval: ${pollingIntervalMs}ms)`
    );
    updateStatus('polling');
    setIsPolling(true);

    if (showNotifications) {
      showToast({
        message: 'Polling modunda çalışıyor. Gerçek zamanlı güncellemeler yavaş olabilir.',
        severity: 'info',
        durationMs: 4000,
      });
    }

    // Simple polling: just check if we can reach the API
    pollingIntervalRef.current = setInterval(async () => {
      try {
        const response = await fetch('/api/health', {
          method: 'GET',
          headers: { 'Content-Type': 'application/json' },
        });

        if (response.ok && status === 'polling') {
          console.log('[SignalR Hook] 📡 Polling: API is healthy. Still in polling mode.');
        }
      } catch (err) {
        console.warn('[SignalR Hook] 📡 Polling: API check failed', err);
      }
    }, pollingIntervalMs);
  }, [isPolling, fallbackToPolling, pollingIntervalMs, updateStatus, status, showNotifications, showToast]);

  // Stop polling
  const stopPolling = useCallback(() => {
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
    }
    setIsPolling(false);
    console.log('[SignalR Hook] ⏹️ Polling stopped');
  }, []);

  // Manual reconnect
  const reconnect = useCallback(async () => {
    console.log('[SignalR Hook] 🔁 Manual reconnect requested');
    clearTimeout(retryTimeoutRef.current as NodeJS.Timeout);
    stopPolling();
    await attemptConnection(0);
  }, [attemptConnection, stopPolling]);

  // Initialize connection on mount
  useEffect(() => {
    if (!enabled) return;

    console.log('[SignalR Hook] 🚀 Initializing SignalR connection');
    attemptConnection(0);

    // Cleanup on unmount
    return () => {
      console.log('[SignalR Hook] 🧹 Cleaning up SignalR connection');
      clearTimeout(retryTimeoutRef.current as NodeJS.Timeout);
      stopPolling();
      stopConnection().catch((err) => {
        console.warn('[SignalR Hook] ⚠️ Error stopping connection:', err);
      });
    };
  }, [enabled, attemptConnection, stopPolling]);

  return {
    isConnected: status === 'connected',
    status,
    error,
    retryCount,
    isPolling,
    reconnect,
  };
}

/**
 * Hook for connection status indicator UI
 * Returns icon, label, and color for status display
 */
export function useSignalRStatusUI(status: ConnectionStatus) {
  const statusConfig: Record<ConnectionStatus, { label: string; icon: string; color: string }> = {
    'disconnected': { label: 'Bağlantı Yok', icon: '⭕', color: '#9CA3AF' },
    'connecting': { label: 'Bağlanıyor...', icon: '🔄', color: '#F59E0B' },
    'connected': { label: 'Bağlı', icon: '🟢', color: '#10B981' },
    'polling': { label: 'Polling Modu', icon: '📡', color: '#3B82F6' },
    'error': { label: 'Hata', icon: '⚠️', color: '#EF4444' },
  };

  return statusConfig[status];
}

export default useSignalRConnection;
